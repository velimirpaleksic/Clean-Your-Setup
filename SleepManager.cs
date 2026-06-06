using System.Runtime.InteropServices;

namespace CleanYourSetup;

internal static class SleepManager
{
    private const uint ES_CONTINUOUS = 0x80000000;
    private const uint ES_SYSTEM_REQUIRED = 0x00000001;
    private const uint ES_DISPLAY_REQUIRED = 0x00000002;

    public static void PreventSleep()
    {
        try
        {
            SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_DISPLAY_REQUIRED);
        }
        catch
        {
            // Optional safety feature.
        }
    }

    public static void AllowSleep()
    {
        try
        {
            SetThreadExecutionState(ES_CONTINUOUS);
        }
        catch
        {
            // Optional cleanup.
        }
    }

    [DllImport("kernel32.dll")]
    private static extern uint SetThreadExecutionState(uint esFlags);
}
