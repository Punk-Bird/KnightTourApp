namespace KnightTourApp;

/// <summary>
/// Panel с включённой двойной буферизацией.
/// Устраняет мерцание при частой перерисовке (анимация коня).
/// </summary>
public class DoubleBufferedPanel : Panel
{
    public DoubleBufferedPanel()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
        UpdateStyles();
    }
}