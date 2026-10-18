using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WardogsXpTracker.Windows.Capture;
using WardogsXpTracker.Windows.Reading;

namespace WardogsXpTracker.CaptureProbe;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        options.Converters.Add(new JsonStringEnumConverter());
        try
        {
            if (args.Length == 1 && args[0] == "--list-windows")
            {
                Console.WriteLine(JsonSerializer.Serialize(new WindowCaptureService().GetCaptureTargets(), options));
                return 0;
            }

            using var reader = new XpImageReader();
            if (args.Length == 2 && args[0] == "--capture")
            {
                var handle = nint.Parse(args[1]);
                var capture = new WindowCaptureService();
                var target = capture.GetCaptureTargets().Single(t => t.WindowHandle == handle);
                var frame = capture.CaptureAsync(target).GetAwaiter().GetResult();
                var image = BitmapSource.Create(frame.Width, frame.Height, 96, 96, PixelFormats.Bgra32,
                    null, frame.Bgra32Pixels, frame.Stride);
                image.Freeze();
                Console.WriteLine(JsonSerializer.Serialize(reader.ReadAsync(image).GetAwaiter().GetResult(), options));
                return 0;
            }

            string[] files;
            if (args.Length == 2 && args[0] == "--fixtures")
                files = Directory.GetFiles(args[1], "*.png").Order().ToArray();
            else if (args.Length >= 1 && args.All(File.Exists))
                files = args;
            else
            {
                Console.Error.WriteLine("Use --fixtures <directory>, --list-windows, --capture <handle>, or one or more image paths.");
                return 2;
            }

            var results = new List<object>();
            foreach (var file in files)
            {
                var analysis = reader.ReadAsync(XpImageReader.LoadImage(file)).GetAwaiter().GetResult();
                results.Add(new { File = Path.GetFileName(file), Analysis = analysis });
            }
            Console.WriteLine(JsonSerializer.Serialize(results, options));
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }
    }
}
