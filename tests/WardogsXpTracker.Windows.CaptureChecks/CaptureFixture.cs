using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

internal static class CaptureFixture
{
    internal static void Run(string[] args)
    {
        if (args.Length < 1) return;
        SetProcessDpiAwarenessContext(-4);
        var app = new Application();
        var backplate = new Window { Title = "Owned capture-border test background", Left = 40, Top = 40,
            Width = 760, Height = 480, WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize,
            ShowActivated = false, ShowInTaskbar = false, Topmost = true, Background = new SolidColorBrush(Color.FromRgb(20, 25, 32)) };
        var text = new TextBlock { Text = "Owned capture test", FontSize = 32, Foreground = Brushes.White, Margin = new Thickness(28) };
        var target = new Window { Title = "Owned WARDOGS capture-border fixture", Left = 100, Top = 100,
            Width = 640, Height = 360, WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize,
            ShowActivated = args.Length > 1 && args[1] == "quiet", ShowInTaskbar = false, Topmost = true, Background = new SolidColorBrush(Color.FromRgb(30, 65, 100)), Content = text };
        var watch = Stopwatch.StartNew();
        var animate = true;
        Window? occluder = null;
        var samples = new List<object>();
        nint desktop = 0;
        var sample = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(15) };
        sample.Tick += (_, _) =>
        {
            if (File.Exists(args[0] + ".close") || watch.Elapsed > TimeSpan.FromSeconds(30)) { app.Shutdown(); return; }
            var commandPath = args[0] + ".command";
            if (File.Exists(commandPath))
            {
                var command = File.ReadAllText(commandPath); File.Delete(commandPath);
                switch (command)
                {
                    case "focus-target": FocusOwnedWindow(new WindowInteropHelper(target).Handle); break;
                    case "focus-backplate": FocusOwnedWindow(new WindowInteropHelper(backplate).Handle); break;
                    case "occlude":
                        occluder = new Window { Left = 280, Top = 220, Width = 160, Height = 100, WindowStyle = WindowStyle.None,
                            ShowActivated = false, ShowInTaskbar = false, Topmost = true, Background = Brushes.Red };
                        occluder.Show(); FocusOwnedWindow(new WindowInteropHelper(target).Handle); SetWindowPos(new WindowInteropHelper(occluder).Handle, -1, 0, 0, 0, 0, 0x13); break;
                    case "uncover": occluder?.Close(); occluder = null; break;
                    case "minimize": target.WindowState = WindowState.Minimized; break;
                    case "restore": target.Show(); target.WindowState = WindowState.Normal; break;
                    case "hide": target.Hide(); break;
                    case "move-outside": SetWindowPos(new WindowInteropHelper(target).Handle, 0, GetSystemMetrics(76) - 100, 100, 0, 0, 0x15); break;
                    case "move-inside": SetWindowPos(new WindowInteropHelper(target).Handle, 0, 100, 100, 0, 0, 0x15); break;
                    case "resize": SetWindowPos(new WindowInteropHelper(target).Handle, 0, 0, 0, 720, 400, 0x16); break;
                    case "static": animate = false; break;
                    case "animate": animate = true; break;
                    case "close-target": target.Close(); break;
                }
                File.WriteAllText(args[0] + ".ack", command);
            }
            if (!GetWindowRect(new WindowInteropHelper(target).Handle, out var rectangle)) return;
            var yellow = 0;
            for (var offset = -4; offset <= 4 && rectangle.Left >= 45 && rectangle.Top >= 45 && rectangle.Right <= 795 && rectangle.Bottom <= 515; offset++)
            {
                yellow += IsYellow(GetPixel(desktop, (rectangle.Left + rectangle.Right) / 2, rectangle.Top + offset)) ? 1 : 0;
                yellow += IsYellow(GetPixel(desktop, (rectangle.Left + rectangle.Right) / 2, rectangle.Bottom + offset)) ? 1 : 0;
                yellow += IsYellow(GetPixel(desktop, rectangle.Left + offset, (rectangle.Top + rectangle.Bottom) / 2)) ? 1 : 0;
                yellow += IsYellow(GetPixel(desktop, rectangle.Right + offset, (rectangle.Top + rectangle.Bottom) / 2)) ? 1 : 0;
            }
            samples.Add(new { Ms = watch.ElapsedMilliseconds, Yellow = yellow });
            if (animate) text.Text = $"Owned capture test\n{watch.ElapsedMilliseconds / 100}";
            if (File.Exists(args[0] + ".close") || watch.Elapsed > TimeSpan.FromSeconds(30)) app.Shutdown();
        };
        backplate.Loaded += (_, _) =>
        {
            target.Show();
            if (args.Length > 1 && args[1] == "quiet") FocusOwnedWindow(new WindowInteropHelper(target).Handle);
            desktop = GetDC(0);
            var handle = new WindowInteropHelper(target).Handle;
            File.WriteAllText(args[0], JsonSerializer.Serialize(new { Handle = handle.ToInt64(), ProcessId = Environment.ProcessId, Width = 640, Height = 360, Foreground = GetForegroundWindow().ToInt64() }));
            sample.Start();
        };
        app.Exit += (_, _) =>
        {
            sample.Stop();
            if (desktop != 0) ReleaseDC(0, desktop);
            File.WriteAllText(args[0] + ".samples.json", JsonSerializer.Serialize(samples));
        };
        app.Run(backplate);
    }
    private static bool IsYellow(uint pixel) { var r = pixel & 255; var g = pixel >> 8 & 255; var b = pixel >> 16 & 255; return r > 170 && g > 120 && b < 110; }
    private static void FocusOwnedWindow(nint window)
    {
        var thread = GetCurrentThreadId();
        var foregroundThread = GetWindowThreadProcessId(GetForegroundWindow(), out _);
        var attached = foregroundThread != 0 && foregroundThread != thread && AttachThreadInput(thread, foregroundThread, true);
        try { BringWindowToTop(window); SetForegroundWindow(window); SetFocus(window); }
        finally { if (attached) AttachThreadInput(thread, foregroundThread, false); }
    }
    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] private static extern bool SetProcessDpiAwarenessContext(nint context);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(nint window, nint order, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")] private static extern bool AttachThreadInput(uint thread, uint other, bool attach);
    [DllImport("user32.dll")] private static extern bool BringWindowToTop(nint window);
    [DllImport("user32.dll")] private static extern nint SetFocus(nint window);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] private static extern bool GetWindowRect(nint window, out Rect rectangle);
    [DllImport("user32.dll")] private static extern nint GetDC(nint window);
    [DllImport("user32.dll")] private static extern int ReleaseDC(nint window, nint dc);
    [DllImport("gdi32.dll")] private static extern uint GetPixel(nint dc, int x, int y);
}
