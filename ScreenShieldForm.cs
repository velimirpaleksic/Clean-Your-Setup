namespace CleanYourSetup;

internal sealed class ScreenShieldForm : Form
{
    public ScreenShieldForm(Screen screen)
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = screen.Bounds;
        BackColor = BrandColors.Black;
        ForeColor = BrandColors.OffWhite;
        TopMost = true;
        ShowInTaskbar = false;
        KeyPreview = true;
        Icon = AppIcon.Load();
        Cursor = Cursors.Default;

        Paint += (_, e) =>
        {
            using Pen pen = new(BrandColors.Crimson, 2);
            e.Graphics.DrawRectangle(pen, 1, 1, Width - 3, Height - 3);
        };
    }
}
