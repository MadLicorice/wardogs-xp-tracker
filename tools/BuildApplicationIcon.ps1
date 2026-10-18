param(
    [Parameter(Mandatory)][string]$InputImage,
    [Parameter(Mandatory)][string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing.Common, System.Drawing.Primitives, System.Private.Windows.GdiPlus, System.Private.Windows.Core, System.Collections -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

public static class ApplicationIconBuilder
{
    private static int ReadBigEndianInt(byte[] data, int offset)
    {
        return (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
    }

    private static byte[] RequiredPngChunks(byte[] input)
    {
        using var output = new MemoryStream();
        output.Write(input, 0, 8);
        for (int offset = 8; offset < input.Length;)
        {
            int length = ReadBigEndianInt(input, offset);
            if (length < 0 || (long)offset + length + 12 > input.Length)
                throw new InvalidDataException("Invalid PNG chunk length.");
            string type = System.Text.Encoding.ASCII.GetString(input, offset + 4, 4);
            if (type == "IHDR" || type == "PLTE" || type == "tRNS" || type == "IDAT" || type == "IEND")
                output.Write(input, offset, length + 12);
            offset += length + 12;
            if (type == "IEND") break;
        }
        return output.ToArray();
    }

    private static byte[] Frame(Image source, int size)
    {
        using var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.Transparent);
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            using var attributes = new ImageAttributes();
            attributes.SetWrapMode(WrapMode.TileFlipXY);
            graphics.DrawImage(source, new Rectangle(0, 0, size, size), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
        }
        using var png = new MemoryStream();
        bitmap.Save(png, ImageFormat.Png);
        return RequiredPngChunks(png.ToArray());
    }

    public static void Build(string inputFile, string outputDirectory)
    {
        using var image = Image.FromFile(inputFile);
        if (image.Width != image.Height) throw new InvalidDataException("The icon image must be square.");
        Directory.CreateDirectory(outputDirectory);
        File.WriteAllBytes(Path.Combine(outputDirectory, "wardogs-xp.png"), Frame(image, 512));
        int[] sizes = { 16, 20, 24, 32, 40, 48, 64, 96, 128, 256 };
        var frames = new List<byte[]>();
        foreach (int size in sizes) frames.Add(Frame(image, size));
        using var stream = File.Create(Path.Combine(outputDirectory, "wardogs-xp.ico"));
        using var writer = new BinaryWriter(stream);
        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write((ushort)sizes.Length);
        int frameOffset = 6 + 16 * sizes.Length;
        for (int index = 0; index < sizes.Length; index++)
        {
            byte dimension = sizes[index] == 256 ? (byte)0 : (byte)sizes[index];
            writer.Write(dimension);
            writer.Write(dimension);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((ushort)1);
            writer.Write((ushort)32);
            writer.Write(frames[index].Length);
            writer.Write(frameOffset);
            frameOffset += frames[index].Length;
        }
        foreach (byte[] frame in frames) writer.Write(frame);
    }
}
'@

[ApplicationIconBuilder]::Build((Resolve-Path -LiteralPath $InputImage).Path, [System.IO.Path]::GetFullPath($OutputDirectory))
Write-Output 'Created metadata-free PNG and ICO assets with 10 icon sizes.'
