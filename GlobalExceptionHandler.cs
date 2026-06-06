namespace CleanYourSetup;

internal static class GlobalExceptionHandler
{
    public static void Register()
    {
        Application.ThreadException += (_, e) => Handle(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception exception)
            {
                Handle(exception);
            }
        };
    }

    private static void Handle(Exception exception)
    {
        AppSafety.ReleaseAll();

        try
        {
            MessageBox.Show(
                "Clean Your Setup hit an error and released input blocking.\n\n" + exception.Message,
                "Clean Your Setup",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
        catch
        {
            // Nothing else to do.
        }
    }
}
