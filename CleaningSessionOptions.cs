namespace CleanYourSetup;

internal sealed class CleaningSessionOptions
{
    public int DurationSeconds { get; init; } = 60;
    public InputBlockMode Mode { get; init; } = InputBlockMode.KeyboardAndMouse;
    public bool ScreenOverlayEnabled { get; init; } = true;
    public bool BeepLastFiveSeconds { get; init; } = true;
    public int UnlockTapCount { get; init; } = 8;
}
