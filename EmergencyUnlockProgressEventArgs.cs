namespace CleanYourSetup;

internal sealed class EmergencyUnlockProgressEventArgs : EventArgs
{
    public EmergencyUnlockProgressEventArgs(int current, int required, string keyName)
    {
        Current = current;
        Required = required;
        KeyName = keyName;
    }

    public int Current { get; }
    public int Required { get; }
    public string KeyName { get; }
}
