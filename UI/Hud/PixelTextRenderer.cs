using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.Versioning;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Font = System.Drawing.Font;
using FontStyle = System.Drawing.FontStyle;
using Graphics = System.Drawing.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace PyGame.UI.Hud;

[SupportedOSPlatform("windows")]
public sealed class PixelTextRenderer : IDisposable
{
    private readonly SpriteBatch _spriteBatch;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly Dictionary<TextCacheKey, CachedText> _cache = [];
    private bool _disposed;

    public PixelTextRenderer(SpriteBatch spriteBatch, Texture2D pixel)
    {
        _spriteBatch = spriteBatch;
        _graphicsDevice = spriteBatch.GraphicsDevice;
        _ = pixel;
    }

    public void DrawText(Vector2 position, string text, int scale, Color color)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var key = new TextCacheKey(text, scale, color.PackedValue);
        if (!_cache.TryGetValue(key, out var cached))
        {
            cached = CreateTexture(text, scale, color);
            _cache[key] = cached;
        }

        _spriteBatch.Draw(cached.Texture, position, Color.White);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var cached in _cache.Values)
        {
            cached.Texture.Dispose();
        }

        _cache.Clear();
        _disposed = true;
    }

    private CachedText CreateTexture(string text, int scale, Color color)
    {
        var fontSize = Math.Max(10, scale * 8);
        using var font = CreateFont(fontSize);
        using var measureBitmap = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
        using var measureGraphics = Graphics.FromImage(measureBitmap);
        measureGraphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
        var measured = measureGraphics.MeasureString(text, font, PointF.Empty, StringFormat.GenericTypographic);

        var width = Math.Max(1, (int)Math.Ceiling(measured.Width) + 2);
        var height = Math.Max(1, (int)Math.Ceiling(measured.Height) + 2);

        using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B)))
        {
            graphics.Clear(System.Drawing.Color.Transparent);
            graphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
            graphics.DrawString(text, font, brush, new PointF(0, 0), StringFormat.GenericTypographic);
        }

        var data = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var source = bitmap.GetPixel(x, y);
                var index = (y * width * 4) + (x * 4);
                data[index] = source.R;
                data[index + 1] = source.G;
                data[index + 2] = source.B;
                data[index + 3] = source.A;
            }
        }

        var texture = new Texture2D(_graphicsDevice, width, height, false, SurfaceFormat.Color);
        texture.SetData(data);
        return new CachedText(texture, width, height);
    }

    private static Font CreateFont(int fontSize)
    {
        try
        {
            return new Font("Malgun Gothic", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
        }
        catch
        {
            return new Font(FontFamily.GenericSansSerif, fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
        }
    }

    private readonly record struct TextCacheKey(string Text, int Scale, uint Color);
    private readonly record struct CachedText(Texture2D Texture, int Width, int Height);
}
