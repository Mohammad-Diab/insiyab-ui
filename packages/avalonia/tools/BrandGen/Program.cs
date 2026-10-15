using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace BrandGen;

public sealed class App : Application { }

// One drawing of the logo in tile space: the 224-unit rounded tile, its corners already cut.
public sealed record Shape(string Deep, string Pane);

public sealed class Mark(Shape shape, bool gauge, int size) : Control
{
    public override void Render(DrawingContext context) => Program.Draw(context, shape, gauge, size);
}

public static class Program
{
    const double W = 224, R = 60, Centre = W * 0.60;
    const double VeilDeep = 0.20, VeilPaneStart = 0.44, VeilPaneEnd = 0.17;
    const double GaugeScale = 4.5, GaugeX = 112, GaugeY = 48, GaugeStroke = 10.8, AppGaugeStroke = 12;
    static readonly int[] AppSizes = [16, 24, 32, 48, 64, 128, 256];
    static readonly int[] FaviconSizes = [16, 32, 48];
    static readonly Color[] DeepStops = [Color.Parse("#704AAA"), Color.Parse("#4C1D95"), Color.Parse("#341465")];
    static readonly Color[] PaneStops = [Color.Parse("#9A7BD0"), Color.Parse("#6A3FB5")];

    [STAThread]
    public static int Main(string[] args)
    {
        string root = Path.GetFullPath(args.Length > 0 ? args[0] : ".");
        string gallery = Path.Combine(root, "packages", "avalonia", "gallery", "Insiyab.Avalonia.Gallery");
        string perfLab = Path.Combine(root, "packages", "avalonia", "tools", "PerfLab");
        if (!Directory.Exists(gallery) || !Directory.Exists(perfLab))
        {
            Console.Error.WriteLine($"not the repository root: {root}");
            return 1;
        }
        AppBuilder.Configure<App>().UsePlatformDetect().SetupWithoutStarting();

        string brand = Path.Combine(root, "brand");
        Directory.CreateDirectory(brand);
        var large = ShapeFor(512);
        Write(Path.Combine(brand, "mark.svg"), Svg(large, false, false));
        Write(Path.Combine(brand, "mark-mono.svg"), Svg(large, true, false));
        Write(Path.Combine(brand, "tile.svg"), Svg(large, false, true));
        foreach (int small in new[] { 16, 32 })
        {
            Write(Path.Combine(brand, $"mark-{small}px.svg"), Svg(ShapeFor(small), false, false));
            Write(Path.Combine(brand, $"mark-mono-{small}px.svg"), Svg(ShapeFor(small), true, false));
        }
        foreach (int size in new[] { 128, 256, 512 })
            File.WriteAllBytes(Path.Combine(brand, $"mark-{size}.png"), Png(size, false));
        Ico(Path.Combine(brand, "favicon.ico"), FaviconSizes, false);
        Write(Path.Combine(brand, "geometry.json"), Json());

        Directory.CreateDirectory(Path.Combine(gallery, "Assets"));
        Ico(Path.Combine(gallery, "Assets", "gallery.ico"), AppSizes, false);
        Write(Path.Combine(gallery, "BrandMark.axaml"), Xaml());
        Directory.CreateDirectory(Path.Combine(perfLab, "Assets"));
        Ico(Path.Combine(perfLab, "Assets", "perflab.ico"), AppSizes, true);

        Console.WriteLine($"brand written under {root}");
        return 0;
    }

    static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    static string Hex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    static void Write(string path, string text) => File.WriteAllText(path, text, new UTF8Encoding(false));

    // The wave's height and the gap under it for a drawing size; 16 and 32 pixels are hand-tuned to stay readable.
    static (double Amp, double Gap) Wave(int size)
    {
        if (size <= 16) return (Math.Min(40, W - 36 - Centre - 26), 52);
        if (size <= 32) return (Math.Min(36, W - 34 - Centre - 20), 40);
        return (30, 26);
    }

    public static Shape ShapeFor(int size)
    {
        var (amp, gap) = Wave(size);
        double top = Centre - gap / 2, bottom = Centre + gap / 2;
        if (top <= R || bottom >= W - R || top - amp <= 0 || bottom + amp >= W)
            throw new InvalidOperationException($"the wave for {size}px leaves the tile");
        string pane = $"M 0 {F(top)} L 0 {F(R)} A {F(R)} {F(R)} 0 0 1 {F(R)} 0 L {F(W - R)} 0 A {F(R)} {F(R)} 0 0 1 {F(W)} {F(R)} L {F(W)} {F(top)}{Segments(amp, top, true)} Z";
        string deep = $"M 0 {F(bottom)}{Segments(amp, bottom, false)} L {F(W)} {F(W - R)} A {F(R)} {F(R)} 0 0 1 {F(W - R)} {F(W)} L {F(R)} {F(W)} A {F(R)} {F(R)} 0 0 1 0 {F(W - R)} Z";
        return new Shape(deep, pane);
    }

    // One period of the sine across the tile, as eight cubic pieces.
    static string Segments(double amp, double mid, bool reverse)
    {
        const int n = 8;
        double k = 2 * Math.PI / W;
        double Y(double x) => mid - amp * Math.Sin(k * x);
        double Dy(double x) => -amp * k * Math.Cos(k * x);
        var sb = new StringBuilder();
        for (int i = 0; i < n; i++)
        {
            int a = reverse ? n - i : i, b = reverse ? n - i - 1 : i + 1;
            double xa = W * a / n, xb = W * b / n, h = (xb - xa) / 3;
            sb.Append($" C {F(xa + h)} {F(Y(xa) + Dy(xa) * h)} {F(xb - h)} {F(Y(xb) - Dy(xb) * h)} {F(xb)} {F(Y(xb))}");
        }
        return sb.ToString();
    }

    // PerfLab's gauge (the 24-unit icon it used before) placed on the light pane.
    static string GaugePath()
    {
        double k = GaugeScale;
        double X(double x) => GaugeX + (x - 12) * k;
        double Y(double y) => GaugeY + (y - 12) * k;
        return $"M {F(X(5.07))} {F(Y(18))} A {F(8 * k)} {F(8 * k)} 0 1 1 {F(X(18.93))} {F(Y(18))} M {F(X(12))} {F(Y(14))} L {F(X(15.55))} {F(Y(9.46))} M {F(X(11))} {F(Y(14))} A {F(k)} {F(k)} 0 1 0 {F(X(13))} {F(Y(14))} A {F(k)} {F(k)} 0 1 0 {F(X(11))} {F(Y(14))}";
    }

    static RelativePoint P(double x, double y) => new(x, y, RelativeUnit.Relative);

    static IBrush Diagonal(Color[] colors)
    {
        var stops = new GradientStops();
        for (int i = 0; i < colors.Length; i++) stops.Add(new GradientStop(colors[i], (double)i / (colors.Length - 1)));
        return new LinearGradientBrush { StartPoint = P(1, 0), EndPoint = P(0, 1), GradientStops = stops };
    }

    public static void Draw(DrawingContext context, Shape shape, bool gauge, int size)
    {
        double s = size / 256.0;
        using var place = context.PushTransform(Matrix.CreateTranslation(16, 16) * Matrix.CreateScale(s, s));
        var pane = StreamGeometry.Parse(shape.Pane);
        context.DrawGeometry(Diagonal(DeepStops), null, StreamGeometry.Parse(shape.Deep));
        context.DrawGeometry(Diagonal(PaneStops), null, pane);
        var gloss = new LinearGradientBrush { StartPoint = P(0, 0), EndPoint = P(0, 1), GradientStops = [new GradientStop(Color.FromArgb(97, 255, 255, 255), 0), new GradientStop(Color.FromArgb(0, 255, 255, 255), 0.75)] };
        context.DrawGeometry(gloss, null, pane);
        if (gauge)
            context.DrawGeometry(null, new Pen(Brushes.White, Math.Max(GaugeStroke, 1.25 / s), lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round), StreamGeometry.Parse(GaugePath()));
    }

    static byte[] Png(int size, bool gauge)
    {
        var mark = new Mark(ShapeFor(size), gauge, size);
        mark.Measure(new Size(size, size));
        mark.Arrange(new Rect(0, 0, size, size));
        using var bitmap = new RenderTargetBitmap(new PixelSize(size, size), new Vector(96, 96));
        bitmap.Render(mark);
        using var stream = new MemoryStream();
        bitmap.Save(stream, PngBitmapEncoderOptions.Default);
        return stream.ToArray();
    }

    // A Windows icon whose every size is a PNG, which Windows reads from Vista on.
    static void Ico(string path, int[] sizes, bool gauge)
    {
        var images = sizes.Select(size => (size, Png(size, gauge))).ToList();
        using var output = new BinaryWriter(File.Create(path));
        output.Write((short)0);
        output.Write((short)1);
        output.Write((short)images.Count);
        int offset = 6 + 16 * images.Count;
        foreach (var (size, png) in images)
        {
            output.Write((byte)(size >= 256 ? 0 : size));
            output.Write((byte)(size >= 256 ? 0 : size));
            output.Write((byte)0);
            output.Write((byte)0);
            output.Write((short)1);
            output.Write((short)32);
            output.Write(png.Length);
            output.Write(offset);
            offset += png.Length;
        }
        foreach (var (_, png) in images) output.Write(png);
    }

    static string Gradient(string id, Color[] colors)
    {
        var sb = new StringBuilder($"    <linearGradient id=\"{id}\" x1=\"1\" y1=\"0\" x2=\"0\" y2=\"1\">\n");
        for (int i = 0; i < colors.Length; i++) sb.Append($"      <stop offset=\"{F((double)i / (colors.Length - 1))}\" stop-color=\"{Hex(colors[i])}\"/>\n");
        return sb.Append("    </linearGradient>\n").ToString();
    }

    static string Svg(Shape shape, bool mono, bool tile)
    {
        int side = tile ? 224 : 256;
        var sb = new StringBuilder($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"{(tile ? "0 0 224 224" : "-16 -16 256 256")}\" width=\"{side}\" height=\"{side}\"{(mono ? " style=\"color:#4C1D95\"" : "")}>\n");
        sb.Append("  <title>Insiyab</title>\n");
        if (mono)
        {
            sb.Append($"  <path d=\"{shape.Deep}\" fill=\"currentColor\"/>\n");
            sb.Append($"  <path d=\"{shape.Pane}\" fill=\"currentColor\"/>\n");
        }
        else
        {
            sb.Append("  <defs>\n").Append(Gradient("brand", DeepStops)).Append(Gradient("pane", PaneStops));
            sb.Append("    <linearGradient id=\"gloss\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\">\n      <stop offset=\"0\" stop-color=\"#FFFFFF\" stop-opacity=\".38\"/>\n      <stop offset=\".75\" stop-color=\"#FFFFFF\" stop-opacity=\"0\"/>\n    </linearGradient>\n");
            sb.Append("  </defs>\n");
            sb.Append($"  <path d=\"{shape.Deep}\" fill=\"url(#brand)\"/>\n");
            sb.Append($"  <path d=\"{shape.Pane}\" fill=\"url(#pane)\"/>\n");
            sb.Append($"  <path d=\"{shape.Pane}\" fill=\"url(#gloss)\"/>\n");
        }
        return sb.Append("</svg>\n").ToString();
    }

    static string Json()
    {
        var sb = new StringBuilder("{\n");
        foreach (var (name, size) in new[] { ("small16", 16), ("small32", 32), ("large", 512) })
        {
            var shape = ShapeFor(size);
            sb.Append($"  \"{name}\": {{\n    \"deep\": \"{shape.Deep}\",\n    \"pane\": \"{shape.Pane}\"\n  }},\n");
        }
        sb.Append($"  \"gauge\": \"{GaugePath()}\",\n");
        sb.Append($"  \"veil\": {{ \"deep\": {F(VeilDeep)}, \"paneStart\": {F(VeilPaneStart)}, \"paneEnd\": {F(VeilPaneEnd)} }}\n");
        return sb.Append("}\n").ToString();
    }

    static string Alpha(double veil) => $"#{(int)Math.Round(veil * 255):X2}FFFFFF";

    static string Fill(string geometry, string start, string end, params (string Offset, string Color)[] stops)
    {
        var sb = new StringBuilder($"        <Path Data=\"{{StaticResource {geometry}}}\">\n          <Path.Fill>\n            <LinearGradientBrush StartPoint=\"{start}\" EndPoint=\"{end}\">\n");
        foreach (var (offset, color) in stops) sb.Append($"              <GradientStop Offset=\"{offset}\" Color=\"{color}\" />\n");
        return sb.Append("            </LinearGradientBrush>\n          </Path.Fill>\n        </Path>\n").ToString();
    }

    // The deep pane is the brand gradient, and white veils over the brand colour make the light pane, so the mark follows InsiyabTheme.SetPrimary.
    static string Template(string key, bool gauge)
    {
        var sb = new StringBuilder($"  <ControlTemplate x:Key=\"{key}\" TargetType=\"TemplatedControl\">\n    <Viewbox FlowDirection=\"LeftToRight\">\n      <Canvas Width=\"{F(W)}\" Height=\"{F(W)}\">\n");
        sb.Append(Fill("BrandDeep", "100%,0%", "0%,100%", ("0", "{DynamicResource InsPrimaryColor}"), ("0.5", "{DynamicResource InsPrimaryColor}"), ("1", "{DynamicResource InsPrimaryDarkColor}")));
        sb.Append(Fill("BrandDeep", "100%,0%", "0%,100%", ("0", Alpha(VeilDeep)), ("0.5", "#00FFFFFF")));
        sb.Append("        <Path Data=\"{StaticResource BrandPane}\" Fill=\"{DynamicResource InsPrimary}\" />\n");
        sb.Append(Fill("BrandPane", "100%,0%", "0%,100%", ("0", Alpha(VeilPaneStart)), ("1", Alpha(VeilPaneEnd))));
        sb.Append(Fill("BrandPane", "0%,0%", "0%,100%", ("0", "#61FFFFFF"), ("0.75", "#00FFFFFF")));
        if (gauge) sb.Append($"        <Path Data=\"{{StaticResource BrandGauge}}\" Stroke=\"White\" StrokeThickness=\"{F(AppGaugeStroke)}\" StrokeLineCap=\"Round\" StrokeJoin=\"Round\" />\n");
        return sb.Append("      </Canvas>\n    </Viewbox>\n  </ControlTemplate>\n").ToString();
    }

    static string Xaml()
    {
        var shape = ShapeFor(32);
        var sb = new StringBuilder("<!-- Generated by packages/avalonia/tools/BrandGen from the brand geometry; do not edit. -->\n");
        sb.Append("<ResourceDictionary xmlns=\"https://github.com/avaloniaui\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">\n");
        sb.Append($"  <StreamGeometry x:Key=\"BrandDeep\">{shape.Deep}</StreamGeometry>\n");
        sb.Append($"  <StreamGeometry x:Key=\"BrandPane\">{shape.Pane}</StreamGeometry>\n");
        sb.Append($"  <StreamGeometry x:Key=\"BrandGauge\">{GaugePath()}</StreamGeometry>\n");
        sb.Append(Template("BrandMark", false)).Append(Template("BrandMarkGauge", true));
        return sb.Append("</ResourceDictionary>\n").ToString();
    }
}
