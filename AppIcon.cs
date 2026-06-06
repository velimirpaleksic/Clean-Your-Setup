namespace CleanYourSetup;

internal static class AppIcon
{
    public static Icon? Load()
    {
        try
        {
            string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
            if (File.Exists(iconPath))
            {
                return new Icon(iconPath);
            }
        }
        catch
        {
            // The app can run without a custom icon.
        }

        return null;
    }
}
