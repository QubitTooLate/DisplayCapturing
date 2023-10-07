namespace Qtl.DisplayCapture;

public record Rect(int Left, int Top, int Right, int Bottom)
{
    public int X => Left;
    public int Y => Top;
    public int Width => Right - Left;
    public int Height => Bottom - Top;
}
