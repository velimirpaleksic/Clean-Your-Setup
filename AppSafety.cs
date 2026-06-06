namespace CleanYourSetup;

internal static class AppSafety
{
    private static readonly object SyncRoot = new();
    private static readonly List<InputBlocker> ActiveBlockers = new();

    public static void Register(InputBlocker blocker)
    {
        lock (SyncRoot)
        {
            if (!ActiveBlockers.Contains(blocker))
            {
                ActiveBlockers.Add(blocker);
            }
        }
    }

    public static void Unregister(InputBlocker blocker)
    {
        lock (SyncRoot)
        {
            ActiveBlockers.Remove(blocker);
        }
    }

    public static void ReleaseAll()
    {
        InputBlocker[] blockers;

        lock (SyncRoot)
        {
            blockers = ActiveBlockers.ToArray();
            ActiveBlockers.Clear();
        }

        foreach (InputBlocker blocker in blockers)
        {
            try
            {
                blocker.Dispose();
            }
            catch
            {
                // Safety cleanup is best-effort.
            }
        }

        SleepManager.AllowSleep();
    }
}
