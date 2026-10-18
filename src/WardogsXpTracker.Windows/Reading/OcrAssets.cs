using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Tesseract;

namespace WardogsXpTracker.Windows.Reading;

public sealed record OcrAssetPaths(string TessdataDirectory, string NativeDirectory);

/// <summary>Extracts and checks the offline OCR files embedded in the executable.</summary>
public static class OcrAssets
{
    private sealed record Asset(string Resource, string RelativePath, string Sha256);

    private static readonly Asset[] Assets =
    [
        new("WardogsXpTracker.Ocr.eng.traineddata", "tessdata/eng.traineddata", "7d4322bd2a7749724879683fc3912cb542f19906c83bcc1a52132556427170b2"),
        new("WardogsXpTracker.Native.leptonica-1.82.0.dll", "x64/leptonica-1.82.0.dll", "dfcb3e6ed0b16bc55bfdbcf53543cfe42a354b87c3e35bd3a95eebf005d73e76"),
        new("WardogsXpTracker.Native.tesseract50.dll", "x64/tesseract50.dll", "de4d04ec75095374d98f5dd7a60d14d7e2e0f76589db693eccf7ae658be8cb2b"),
        new("WardogsXpTracker.Native.msvcp140.dll", "x64/msvcp140.dll", "0f885b509a685d2bbfa652fed26b5fb31d88fbdab0a978c641d1c7b8aa460aa9"),
        new("WardogsXpTracker.Native.vcruntime140.dll", "x64/vcruntime140.dll", "d5e4d9a3e835fa679450145d6a7d94e36573a509317111904d9b3712c30d9066"),
        new("WardogsXpTracker.Native.vcruntime140_1.dll", "x64/vcruntime140_1.dll", "1f2d41c4aa5db0bc33ebf7b66d72943a817d7ce6cbe880502a9403823633093f")
    ];

    private static readonly object Sync = new();
    private static readonly List<nint> NativeHandles = [];
    private static OcrAssetPaths? installed;

    public static OcrAssetPaths EnsureInstalled()
    {
        lock (Sync)
        {
            if (installed is not null)
                return installed;

            if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64)
                throw new PlatformNotSupportedException("The bundled OCR engine requires Windows x64.");

            var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
                string.Join("\n", Assets.Select(asset => asset.Resource + ":" + asset.Sha256)))))
                .ToLowerInvariant()[..16];
            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WardogsXpTracker", "ocr", fingerprint);
            var assembly = typeof(OcrAssets).Assembly;

            foreach (var asset in Assets)
                ExtractAsset(assembly, root, asset);

            var nativeDirectory = Path.Combine(root, "x64");

            // Load the bundled C++ runtime before the OCR wrapper loads its native engine.
            foreach (var name in new[] { "vcruntime140.dll", "vcruntime140_1.dll", "msvcp140.dll" })
                NativeHandles.Add(NativeLibrary.Load(Path.Combine(nativeDirectory, name)));

            // The wrapper appends x64 to this path.
            TesseractEnviornment.CustomSearchPath = root;
            installed = new OcrAssetPaths(Path.Combine(root, "tessdata"), nativeDirectory);
            return installed;
        }
    }

    private static void ExtractAsset(Assembly assembly, string root, Asset asset)
    {
        using var source = assembly.GetManifestResourceStream(asset.Resource)
            ?? throw new InvalidDataException($"The executable is missing OCR asset {asset.Resource}.");
        using var content = new MemoryStream();
        source.CopyTo(content);
        var bytes = content.ToArray();
        if (!MatchesHash(bytes, asset.Sha256))
            throw new InvalidDataException($"The embedded OCR asset failed its SHA-256 check: {asset.Resource}.");

        var target = Path.Combine(root, asset.RelativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        if (FileMatches(target, asset.Sha256))
            return;

        var temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                output.Write(bytes);
                output.Flush(flushToDisk: true);
            }

            try
            {
                File.Move(temporary, target, overwrite: true);
            }
            catch (IOException) when (FileMatches(target, asset.Sha256))
            {
                // Another process installed the same checked asset.
            }

            if (!FileMatches(target, asset.Sha256))
                throw new InvalidDataException($"The extracted OCR asset failed its SHA-256 check: {asset.Resource}.");
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }

    private static bool FileMatches(string path, string expected)
    {
        if (!File.Exists(path))
            return false;
        try
        {
            using var file = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(file)).Equals(expected, StringComparison.OrdinalIgnoreCase);
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static bool MatchesHash(ReadOnlySpan<byte> content, string expected) =>
        Convert.ToHexString(SHA256.HashData(content)).Equals(expected, StringComparison.OrdinalIgnoreCase);
}
