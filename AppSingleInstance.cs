using System.Threading;

namespace CleanYourSetup;

internal sealed class AppSingleInstance : IDisposable
{
    private const string MutexName = @"Local\CleanYourSetup_Minimal_SingleInstance";
    private const string ActivateEventName = @"Local\CleanYourSetup_Minimal_ActivateRequest";

    private readonly Mutex _mutex;
    private readonly EventWaitHandle _activateEvent;
    private readonly CancellationTokenSource _listenerCancellation = new();
    private Thread? _listenerThread;

    private AppSingleInstance(Mutex mutex, EventWaitHandle activateEvent, bool isFirstInstance)
    {
        _mutex = mutex;
        _activateEvent = activateEvent;
        IsFirstInstance = isFirstInstance;
    }

    public bool IsFirstInstance { get; }

    public static AppSingleInstance Create()
    {
        Mutex mutex = new(true, MutexName, out bool createdNew);
        EventWaitHandle activateEvent = new(false, EventResetMode.AutoReset, ActivateEventName);
        return new AppSingleInstance(mutex, activateEvent, createdNew);
    }

    public static bool TryNotifyExistingInstance()
    {
        try
        {
            using EventWaitHandle activateEvent = EventWaitHandle.OpenExisting(ActivateEventName);
            activateEvent.Set();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void StartActivationListener(Action restoreAction)
    {
        if (!IsFirstInstance || _listenerThread != null)
        {
            return;
        }

        _listenerThread = new Thread(() =>
        {
            WaitHandle[] waitHandles = [_activateEvent, _listenerCancellation.Token.WaitHandle];

            while (!_listenerCancellation.IsCancellationRequested)
            {
                int index = WaitHandle.WaitAny(waitHandles);
                if (index != 0 || _listenerCancellation.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    restoreAction();
                }
                catch
                {
                    // Activation requests are non-critical.
                }
            }
        })
        {
            IsBackground = true,
            Name = "CleanYourSetup activation listener"
        };

        _listenerThread.Start();
    }

    public void Dispose()
    {
        try
        {
            _listenerCancellation.Cancel();
            _activateEvent.Set();
        }
        catch
        {
            // Best-effort shutdown.
        }

        try
        {
            if (IsFirstInstance)
            {
                _mutex.ReleaseMutex();
            }
        }
        catch
        {
            // Best-effort release.
        }

        _activateEvent.Dispose();
        _listenerCancellation.Dispose();
        _mutex.Dispose();
    }
}
