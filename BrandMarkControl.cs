namespace CleanYourSetup;

internal sealed class BrandMarkControl : Control
{
    public BrandMarkControl()
    {
        Width = 66;
        Height = 34;
        BackColor = BrandColors.Black;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint |
            ControlStyles.ResizeRedraw,
            true
        );
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

        using Font font = CreateFont(18f, FontStyle.Bold);
        using Brush vpBrush = new SolidBrush(BrandColors.OffWhite);
        using Brush dotBrush = new SolidBrush(BrandColors.Crimson);
        using StringFormat format = (StringFormat)StringFormat.GenericTypographic.Clone();

        format.FormatFlags |= StringFormatFlags.NoClip;

        e.Graphics.DrawString("VP", font, vpBrush, 0f, 0f, format);

        float vpWidth = e.Graphics.MeasureString("VP", font, PointF.Empty, format).Width;
        e.Graphics.DrawString(".", font, dotBrush, vpWidth - 1.5f, 0f, format);
    }

    private static Font CreateFont(float size, FontStyle style)
    {
        try
        {
            return new Font("Inter", size, style);
        }
        catch
        {
            return new Font("Segoe UI", size, style);
        }
    }
}
