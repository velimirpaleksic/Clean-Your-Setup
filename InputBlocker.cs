using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CleanYourSetup;

internal sealed class InputBlocker : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WH_MOUSE_LL = 14;

    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    private static readonly TimeSpan EmergencyResetWindow = TimeSpan.FromSeconds(3);

    private readonly LowLevelProc _keyboardProc;
    private readonly LowLevelProc _mouseProc;
    private readonly HashSet<int> _pressedEmergencyKeys = new();
    private readonly int _requiredEmergencyPresses;

    private IntPtr _keyboardHook = IntPtr.Zero;
    private IntPtr _mouseHook = IntPtr.Zero;
    private bool _isBlocking;
    private bool _blockKeyboard;
    private bool _blockMouse;
    private int _emergencyPresses;
    private DateTimeOffset _lastEmergencyPressUtc = DateTimeOffset.MinValue;

    public InputBlocker(int requiredEmergencyPresses)
    {
        _requiredEmergencyPresses = Math.Clamp(requiredEmergencyPresses, 3, 20);
        _keyboardProc = KeyboardCallback;
        _mouseProc = MouseCallback;
    }

    public event EventHandler? EmergencyUnlockRequested;
    public event EventHandler<EmergencyUnlockProgressEventArgs>? EmergencyUnlockProgressChanged;

    public bool IsBlocking => _isBlocking;

    public void StartBlocking(InputBlockMode mode)
    {
        if (_isBlocking || mode == InputBlockMode.OverlayOnly)
        {
            return;
        }

        _blockKeyboard = mode is InputBlockMode.KeyboardAndMouse or InputBlockMode.KeyboardOnly;
        _blockMouse = mode is InputBlockMode.KeyboardAndMouse or InputBlockMode.MouseOnly;

        try
        {
            // Keyboard hook is always installed so emergency unlock works even in mouse-only mode.
            _keyboardHook = SetHook(WH_KEYBOARD_LL, _keyboardProc);

            if (_blockMouse)
            {
                _mouseHook = SetHook(WH_MOUSE_LL, _mouseProc);
            }

            _isBlocking = true;
            AppSafety.Register(this);
        }
        catch
        {
            StopBlocking();
            throw;
        }
    }

    public void StopBlocking()
    {
        _isBlocking = false;
        _blockKeyboard = false;
        _blockMouse = false;
        _emergencyPresses = 0;
        _pressedEmergencyKeys.Clear();

        if (_keyboardHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHook);
            _keyboardHook = IntPtr.Zero;
        }

        if (_mouseHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_mouseHook);
            _mouseHook = IntPtr.Zero;
        }

        AppSafety.Unregister(this);
    }

    public void Dispose()
    {
        StopBlocking();
    }

    private IntPtr SetHook(int hookType, LowLevelProc proc)
    {
        using Process currentProcess = Process.GetCurrentProcess();
        using ProcessModule? currentModule = currentProcess.MainModule;

        if (currentModule == null)
        {
            throw new InvalidOperationException("Could not read the current process module.");
        }

        IntPtr moduleHandle = GetModuleHandle(currentModule.ModuleName);
        IntPtr hook = SetWindowsHookEx(hookType, proc, moduleHandle, 0);

        if (hook == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not install the input hook.");
        }

        return hook;
    }

    private IntPtr KeyboardCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _isBlocking)
        {
            int message = wParam.ToInt32();
            int virtualKeyCode = Marshal.ReadInt32(lParam);
            bool isKeyDown = message is WM_KEYDOWN or WM_SYSKEYDOWN;
            bool isKeyUp = message is WM_KEYUP or WM_SYSKEYUP;
            bool isEmergencyKey = IsEmergencyKey(virtualKeyCode);

            if (isKeyDown && isEmergencyKey)
            {
                CountEmergencyPress(virtualKeyCode);
                return (IntPtr)1;
            }

            if (isKeyUp && isEmergencyKey)
            {
                _pressedEmergencyKeys.Remove(virtualKeyCode);

                if (_blockKeyboard)
                {
                    return (IntPtr)1;
                }
            }

            if (_blockKeyboard)
            {
                return (IntPtr)1;
            }
        }

        return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    private void CountEmergencyPress(int virtualKeyCode)
    {
        // Holding a key should not satisfy the spam-to-unlock action.
        if (_pressedEmergencyKeys.Contains(virtualKeyCode))
        {
            return;
        }

        _pressedEmergencyKeys.Add(virtualKeyCode);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (now - _lastEmergencyPressUtc > EmergencyResetWindow)
        {
            _emergencyPresses = 0;
        }

        _lastEmergencyPressUtc = now;
        _emergencyPresses = Math.Min(_emergencyPresses + 1, _requiredEmergencyPresses);

        string keyName = virtualKeyCode switch
        {
            (int)Keys.Escape => "Esc",
            (int)Keys.Space => "Space",
            (int)Keys.Enter => "Enter",
            _ => "Key"
        };

        EmergencyUnlockProgressChanged?.Invoke(
            this,
            new EmergencyUnlockProgressEventArgs(_emergencyPresses, _requiredEmergencyPresses, keyName)
        );

        if (_emergencyPresses >= _requiredEmergencyPresses)
        {
            EmergencyUnlockRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private static bool IsEmergencyKey(int virtualKeyCode)
    {
        return virtualKeyCode is (int)Keys.Escape or (int)Keys.Space or (int)Keys.Enter;
    }

    private IntPtr MouseCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _isBlocking && _blockMouse)
        {
            return (IntPtr)1;
        }

        return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
}
