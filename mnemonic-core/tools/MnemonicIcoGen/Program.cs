using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
const string defaultOutput = "src/Mnemonic.Windows/assets/mnemonic.ico";
var outputPath = args.Length > 0 ? args[0] : defaultOutput;
var fullPath = Path.IsPathRooted(outputPath)
    ? outputPath
    : Path.GetFullPath(outputPath, Directory.GetCurrentDirectory());
Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

var sizes = new[] { 16, 24, 32, 48, 256 };
var bitmaps = sizes.Select(CreateMnemonicBitmap).ToList();
try
{
    WriteIco(fullPath, bitmaps);
    Console.WriteLine($"Wrote {fullPath} ({sizes.Length} sizes).");
}
finally
{
    foreach (var bitmap in bitmaps)
    {
        bitmap.Dispose();
    }
}

static Bitmap CreateMnemonicBitmap(int size)
{
    var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
    using var graphics = Graphics.FromImage(bitmap);
    graphics.SmoothingMode = SmoothingMode.AntiAlias;
    graphics.Clear(Color.Transparent);

    var margin = Math.Max(1, (int)Math.Round(size * 0.04));
    var rect = new Rectangle(margin, margin, size - (margin * 2), size - (margin * 2));
    var radius = Math.Max(2, (int)Math.Round(size * 0.16));
    using var path = CreateRoundedRect(rect, radius);
    using var background = new SolidBrush(Color.FromArgb(255, 54, 61, 82));
    using var border = new Pen(Color.FromArgb(255, 33, 37, 50), Math.Max(1f, size / 24f));
    graphics.FillPath(background, path);
    graphics.DrawPath(border, path);

    var ringSize = (int)Math.Round(size * 0.72);
    var ringX = (size - ringSize) / 2;
    var ringY = (size - ringSize) / 2;
    using var ringPen = new Pen(Color.FromArgb(235, 255, 255, 255), Math.Max(1.5f, size / 14f));
    graphics.DrawEllipse(ringPen, ringX, ringY, ringSize, ringSize);

    var dotSize = Math.Max(4, (int)Math.Round(size * 0.2));
    var dotX = (size - dotSize) / 2;
    var dotY = (size - dotSize) / 2;
    using var dotBrush = new SolidBrush(Color.FromArgb(255, 71, 140, 191));
    graphics.FillEllipse(dotBrush, dotX, dotY, dotSize, dotSize);

    return bitmap;
}

static GraphicsPath CreateRoundedRect(Rectangle bounds, int radius)
{
    var path = new GraphicsPath();
    var d = radius * 2;
    path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
    path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
    path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
    path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
    path.CloseFigure();
    return path;
}

static void WriteIco(string path, IReadOnlyList<Bitmap> bitmaps)
{
    using var stream = File.Create(path);
    using var writer = new BinaryWriter(stream);
    writer.Write((ushort)0);
    writer.Write((ushort)1);
    writer.Write((ushort)bitmaps.Count);

    var offset = 6 + (16 * bitmaps.Count);
    var imageData = new List<byte[]>(bitmaps.Count);
    foreach (var bitmap in bitmaps)
    {
        imageData.Add(CreateIcoBitmapPayload(bitmap));
    }

    for (var i = 0; i < bitmaps.Count; i++)
    {
        var bitmap = bitmaps[i];
        writer.Write((byte)(bitmap.Width >= 256 ? 0 : bitmap.Width));
        writer.Write((byte)(bitmap.Height >= 256 ? 0 : bitmap.Height));
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write((ushort)1);
        writer.Write((ushort)32);
        writer.Write((uint)imageData[i].Length);
        writer.Write((uint)offset);
        offset += imageData[i].Length;
    }

    foreach (var payload in imageData)
    {
        writer.Write(payload);
    }
}

static byte[] CreateIcoBitmapPayload(Bitmap bitmap)
{
    using var stream = new MemoryStream();
    WriteIcoBitmap(bitmap, stream);
    return stream.ToArray();
}

static void WriteIcoBitmap(Bitmap bitmap, Stream stream)
{
    using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);
    var width = bitmap.Width;
    var height = bitmap.Height;
    var pixelDataSize = width * height * 4;
    var maskStride = ((width + 31) / 32) * 4;
    var maskSize = maskStride * height;

    writer.Write((uint)40);
    writer.Write((int)width);
    writer.Write((int)(height * 2));
    writer.Write((ushort)1);
    writer.Write((ushort)32);
    writer.Write((uint)0);
    writer.Write((uint)pixelDataSize);
    writer.Write(0);
    writer.Write(0);
    writer.Write((uint)0);
    writer.Write((uint)0);

    var pixels = new byte[pixelDataSize];
    var index = 0;
    for (var y = height - 1; y >= 0; y--)
    {
        for (var x = 0; x < width; x++)
        {
            var color = bitmap.GetPixel(x, y);
            pixels[index++] = color.B;
            pixels[index++] = color.G;
            pixels[index++] = color.R;
            pixels[index++] = color.A;
        }
    }

    writer.Write(pixels);
    writer.Write(new byte[maskSize]);
}
