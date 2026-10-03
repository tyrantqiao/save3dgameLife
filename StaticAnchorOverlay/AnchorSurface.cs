using System;
using System.Buffers.Binary;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace StaticAnchorOverlay;

// WPF retains this drawing. No timer, render-loop handler, input polling, or animation.
public sealed class AnchorSurface : FrameworkElement
{
    private Preset preset = new();
    private BitmapSource? png;
    private string loadedPath = "";
    public string? ImageError { get; private set; }
    public void Update(Preset value)
    {
        preset = value;
        if (value.Png.ImagePath != loadedPath)
        {
            loadedPath = value.Png.ImagePath;
            png = null; ImageError = null;
            if (loadedPath.Length > 0)
            {
                try
                {
                    LocalFilePolicy.Check(loadedPath);
                    using var stream = File.OpenRead(loadedPath);
                    if (stream.Length > 16 * 1024 * 1024) throw new InvalidOperationException("PNG 不得超过 16 MB。");
                    Span<byte> header = stackalloc byte[24]; stream.ReadExactly(header);
                    if (!header[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ||
                        BinaryPrimitives.ReadUInt32BigEndian(header[16..20]) is 0 or > 4096 ||
                        BinaryPrimitives.ReadUInt32BigEndian(header[20..24]) is 0 or > 4096)
                        throw new InvalidOperationException("PNG 格式无效或单边超过 4096 像素。");
                    stream.Position = 0;
                    var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    var frame = decoder.Frames[0];
                    if (frame.PixelWidth > 4096 || frame.PixelHeight > 4096) throw new InvalidOperationException("PNG 单边不得超过 4096 像素。");
                    frame.Freeze(); png = frame;
                }
                catch (Exception ex) { ImageError = "PNG 无法加载：" + ex.Message; }
            }
        }
        InvalidateVisual();
    }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        double w = ActualWidth, h = ActualHeight;
        if (w <= 0 || h <= 0) return;
        dc.PushClip(new RectangleGeometry(new Rect(0, 0, w, h)));
        void Draw(Anchor a, Action<Brush, Pen, double, double> action)
        {
            if (!a.Enabled || a.Opacity <= 0) return;
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(a.Color)); brush.Freeze();
            var pen = new Pen(brush, a.Thickness); pen.Freeze();
            dc.PushOpacity(a.Opacity);
            action(brush, pen, w / 2 + a.X, h / 2 + a.Y);
            dc.Pop();
        }
        Draw(preset.Vignette, (b, _, _, _) =>
        {
            var a = preset.Vignette;
            var color = ((SolidColorBrush)b).Color;
            double depth = Math.Min(a.Depth, Math.Min(w, h) / 2);
            if (depth <= 0) return;
            void Edge(Rect rect, Point start, Point end)
            {
                var gradient = new LinearGradientBrush(color, Colors.Transparent, start, end); gradient.Freeze();
                dc.DrawRectangle(gradient, null, rect);
            }
            Edge(new Rect(0, 0, w, depth), new Point(0, 0), new Point(0, 1));
            Edge(new Rect(0, h - depth, w, depth), new Point(0, 1), new Point(0, 0));
            Edge(new Rect(0, 0, depth, h), new Point(0, 0), new Point(1, 0));
            Edge(new Rect(w - depth, 0, depth, h), new Point(1, 0), new Point(0, 0));
        });
        Draw(preset.Grid, (_, pen, x, y) =>
        {
            double step = preset.Grid.Spacing;
            for (double xx = ((x % step) + step) % step; xx < w; xx += step) dc.DrawLine(pen, new Point(xx, 0), new Point(xx, h));
            for (double yy = ((y % step) + step) % step; yy < h; yy += step) dc.DrawLine(pen, new Point(0, yy), new Point(w, yy));
        });
        Draw(preset.Border, (_, pen, _, _) =>
        {
            var a = preset.Border;
            double ww = w - 2 * a.Inset, hh = h - 2 * a.Inset;
            if (ww > 0 && hh > 0) dc.DrawRoundedRectangle(null, pen, new Rect(a.Inset + a.X, a.Inset + a.Y, ww, hh), a.CornerRadius, a.CornerRadius);
        });
        Draw(preset.Corners, (_, pen, _, _) =>
        {
            var a = preset.Corners;
            foreach (int sx in new[] { -1, 1 }) foreach (int sy in new[] { -1, 1 })
            {
                double x = (sx < 0 ? a.Inset : w - a.Inset) + a.X, y = (sy < 0 ? a.Inset : h - a.Inset) + a.Y;
                dc.DrawLine(pen, new Point(x, y), new Point(x - sx * a.Length, y));
                dc.DrawLine(pen, new Point(x, y), new Point(x, y - sy * a.Length));
            }
        });
        Draw(preset.Crosshair, (brush, pen, x, y) =>
        {
            var a = preset.Crosshair;
            foreach (int s in new[] { -1, 1 })
            {
                dc.DrawLine(pen, new Point(x + s * a.Gap, y), new Point(x + s * (a.Gap + a.Length), y));
                dc.DrawLine(pen, new Point(x, y + s * a.Gap), new Point(x, y + s * (a.Gap + a.Length)));
            }
            if (a.DotSize > 0) dc.DrawEllipse(brush, null, new Point(x, y), a.DotSize / 2, a.DotSize / 2);
        });
        Draw(preset.CenterDot, (brush, _, x, y) => dc.DrawEllipse(brush, null, new Point(x, y), preset.CenterDot.Size / 2, preset.CenterDot.Size / 2));
        Draw(preset.Horizontal, (_, pen, x, y) => dc.DrawLine(pen, new Point(x - preset.Horizontal.Length / 2, y), new Point(x + preset.Horizontal.Length / 2, y)));
        Draw(preset.Vertical, (_, pen, x, y) => dc.DrawLine(pen, new Point(x, y - preset.Vertical.Length / 2), new Point(x, y + preset.Vertical.Length / 2)));
        if (png is not null) Draw(preset.Png, (_, _, x, y) =>
        {
            var a = preset.Png;
            if (a.Width > 0 && a.Height > 0) dc.DrawImage(png, new Rect(x - a.Width / 2, y - a.Height / 2, a.Width, a.Height));
        });
        dc.Pop();
    }
}
