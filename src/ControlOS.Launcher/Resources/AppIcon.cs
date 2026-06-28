using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ControlOS.Tray.Resources;

/// <summary>
/// Generates the Control OS CPU-chip icon programmatically.
/// Dark navy background (#0f172a) with sky-blue (#38bdf8) chip design.
/// </summary>
public static class AppIcon
{
    private static Icon? _cached;

    public static Icon GetIcon()
    {
        if (_cached is not null)
            return _cached;

        // Render at 256x256 then downscale to produce crisp multi-size .ico
        using var bmp256 = Render(256);
        using var bmp48  = Render(48);
        using var bmp32  = Render(32);
        using var bmp16  = Render(16);

        _cached = BuildMultiSizeIcon(bmp16, bmp32, bmp48, bmp256);
        return _cached;
    }

    // ── Drawing ─────────────────────────────────────────────────────────────

    private static Bitmap Render(int size)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);

        g.SmoothingMode      = SmoothingMode.AntiAlias;
        g.InterpolationMode  = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode    = PixelOffsetMode.HighQuality;

        float s = size;

        // Background – rounded rectangle, dark navy
        var bgColor  = Color.FromArgb(15, 23, 42);      // #0f172a
        var chipColor = Color.FromArgb(56, 189, 248);   // #38bdf8

        float cornerRadius = s * 0.22f;
        using (var bgBrush = new SolidBrush(bgColor))
        using (var bgPath  = RoundedRect(0, 0, s, s, cornerRadius))
        {
            g.FillPath(bgBrush, bgPath);
        }

        // Margins for the chip area
        float margin  = s * 0.28f;   // chip outer rect starts here
        float cx      = s * 0.5f;    // centre
        float chipW   = s - margin * 2;

        float bodyX = margin;
        float bodyY = margin;
        float bodyW = chipW;
        float bodyH = chipW;

        // Outer CPU border
        using (var pen = new Pen(chipColor, s * 0.055f) { LineJoin = LineJoin.Round })
        using (var path = RoundedRect(bodyX, bodyY, bodyW, bodyH, s * 0.06f))
        {
            g.DrawPath(pen, path);
        }

        // Inner chip square (filled)
        float innerMargin = s * 0.14f;
        using (var innerBrush = new SolidBrush(Color.FromArgb(230, 56, 189, 248)))
        using (var path = RoundedRect(innerMargin, innerMargin,
                                      s - innerMargin * 2, s - innerMargin * 2, s * 0.04f))
        {
            // Only draw if it's inside the body
            if (innerMargin > margin + s * 0.04f)
                g.FillPath(innerBrush, path);
        }

        // Simpler inner box centred
        float boxPad = s * 0.375f;
        using (var innerBrush = new SolidBrush(chipColor))
        {
            g.FillRectangle(innerBrush,
                boxPad, boxPad,
                s - boxPad * 2, s - boxPad * 2);
        }

        // Pins – 3 on each side
        float pinLen   = s * 0.13f;
        float pinW     = s * 0.055f;
        float pinColor_a = 255;
        using var pinBrush = new SolidBrush(Color.FromArgb((int)pinColor_a, 56, 189, 248));

        float[] pinOffsets = { -0.19f, 0f, 0.19f };

        foreach (float off in pinOffsets)
        {
            float px = cx + off * s;

            // Top pin
            g.FillRectangle(pinBrush,
                px - pinW / 2, bodyY - pinLen,
                pinW, pinLen);

            // Bottom pin
            g.FillRectangle(pinBrush,
                px - pinW / 2, bodyY + bodyH,
                pinW, pinLen);
        }

        foreach (float off in pinOffsets)
        {
            float py = cx + off * s;

            // Left pin
            g.FillRectangle(pinBrush,
                bodyX - pinLen, py - pinW / 2,
                pinLen, pinW);

            // Right pin
            g.FillRectangle(pinBrush,
                bodyX + bodyW, py - pinW / 2,
                pinLen, pinW);
        }

        return bmp;
    }

    private static GraphicsPath RoundedRect(float x, float y, float w, float h, float r)
    {
        var path = new GraphicsPath();
        path.AddArc(x,         y,         r * 2, r * 2, 180, 90);
        path.AddArc(x + w - r*2, y,       r * 2, r * 2, 270, 90);
        path.AddArc(x + w - r*2, y + h - r*2, r*2, r*2, 0, 90);
        path.AddArc(x,         y + h - r*2, r*2, r*2, 90, 90);
        path.CloseFigure();
        return path;
    }

    // ── ICO builder ─────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a multi-size .ico stream from the provided bitmaps and wraps it in an Icon.
    /// </summary>
    private static Icon BuildMultiSizeIcon(params Bitmap[] sizes)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // ICO header
        bw.Write((short)0);           // reserved
        bw.Write((short)1);           // type = icon
        bw.Write((short)sizes.Length);

        // We need to calculate offsets; each ICONDIRENTRY = 16 bytes
        int dataOffset = 6 + 16 * sizes.Length;
        var pngChunks = new List<byte[]>();

        foreach (var bmp in sizes)
        {
            using var pngStream = new MemoryStream();
            bmp.Save(pngStream, ImageFormat.Png);
            byte[] png = pngStream.ToArray();
            pngChunks.Add(png);

            byte w = bmp.Width  >= 256 ? (byte)0 : (byte)bmp.Width;
            byte h = bmp.Height >= 256 ? (byte)0 : (byte)bmp.Height;

            bw.Write(w);               // width  (0 = 256)
            bw.Write(h);               // height (0 = 256)
            bw.Write((byte)0);         // colour count
            bw.Write((byte)0);         // reserved
            bw.Write((short)1);        // colour planes
            bw.Write((short)32);       // bits per pixel
            bw.Write(png.Length);      // size of image data
            bw.Write(dataOffset);      // offset to image data
            dataOffset += png.Length;
        }

        foreach (var chunk in pngChunks)
            bw.Write(chunk);

        ms.Seek(0, SeekOrigin.Begin);
        return new Icon(ms);
    }
}
