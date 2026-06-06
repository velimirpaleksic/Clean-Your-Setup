namespace CleanYourSetup;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using AppSingleInstance singleInstance = AppSingleInstance.Create();
        if (!singleInstance.IsFirstInstance)
        {
            bool notified = AppSingleInstance.TryNotifyExistingInstance();
            if (!notified)
            {
                MessageBox.Show(
                    "Clean Your Setup is already running.",
                    "Clean Your Setup",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }

            return;
        }

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        GlobalExceptionHandler.Register();

        try
        {
            MainForm mainForm = new();
            singleInstance.StartActivationListener(() =>
            {
                if (!mainForm.IsDisposed && mainForm.IsHandleCreated)
                {
                    mainForm.BeginInvoke(new Action(mainForm.RestoreFromExternalLaunch));
                }
            });

            Application.Run(mainForm);
        }
        finally
        {
            AppSafety.ReleaseAll();
        }
    }
}
