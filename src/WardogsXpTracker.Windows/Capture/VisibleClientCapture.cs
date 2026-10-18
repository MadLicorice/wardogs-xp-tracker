using System.Runtime.InteropServices;

namespace WardogsXpTracker.Windows.Capture;

/// <summary>Copies only the explicitly selected visible client rectangle. It does not create a monitor image.</summary>
internal static class VisibleClientCapture
{
    internal const string Method = "Visible client rectangle · foreground and uncovered only";
    private const int MaximumDimension = 16384;

    internal static CapturedFrame Capture(CaptureTarget target, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var previousDpi = WindowNative.SetThreadDpiAwarenessContext(-4);
        nint source = 0, memory = 0, bitmap = 0, previousBitmap = 0;
        try
        {
            var rectangle = ValidateVisibleRectangle(target);
            source = GetDC(0);
            if (source == 0 || (memory = CreateCompatibleDC(source)) == 0) Fail();
            var info = new BitmapInfo { Header = new() { Size = 40, Width = rectangle.Width,
                Height = -rectangle.Height, Planes = 1, BitCount = 32 } };
            bitmap = CreateDIBSection(source, ref info, 0, out var pixels, 0, 0);
            if (bitmap == 0 || pixels == 0) Fail();
            previousBitmap = SelectObject(memory, bitmap);
            if (previousBitmap == 0 || previousBitmap == -1) Fail();
            cancellationToken.ThrowIfCancellationRequested();
            if (!BitBlt(memory, 0, 0, rectangle.Width, rectangle.Height, source, rectangle.Left, rectangle.Top, 0x40CC0020)) Fail();
            var result = new byte[checked(rectangle.Width * rectangle.Height * 4)];
            Marshal.Copy(pixels, result, 0, result.Length);
            for (var index = 3; index < result.Length; index += 4) result[index] = 255;
            cancellationToken.ThrowIfCancellationRequested();
            var after = ValidateVisibleRectangle(target);
            if (rectangle.Left != after.Left || rectangle.Top != after.Top || rectangle.Right != after.Right || rectangle.Bottom != after.Bottom)
                throw new WindowCaptureException(CaptureFailure.WindowChanged, "The game window moved or resized during capture. Read again.");
            return new(rectangle.Width, rectangle.Height, result, DateTimeOffset.UtcNow, target) { Method = Method };
        }
        finally
        {
            if (previousBitmap != 0 && previousBitmap != -1 && memory != 0) SelectObject(memory, previousBitmap);
            if (bitmap != 0) DeleteObject(bitmap);
            if (memory != 0) DeleteDC(memory);
            if (source != 0) ReleaseDC(0, source);
            if (previousDpi != 0) WindowNative.SetThreadDpiAwarenessContext(previousDpi);
        }
    }

    private static WindowNative.Rect ValidateVisibleRectangle(CaptureTarget target)
    {
        if (WindowNative.GetForegroundWindow() != target.WindowHandle)
            throw new WindowCaptureException(CaptureFailure.NotForeground, "Switch to the game, then use the capture shortcut. Quiet capture waits while another window has focus.");
        if (!WindowNative.GetClientRect(target.WindowHandle, out var client) || client.Width < 1 || client.Height < 1
            || client.Width > MaximumDimension || client.Height > MaximumDimension)
            throw new WindowCaptureException(CaptureFailure.EmptyWindow, "The selected window has no supported client image.");
        var origin = new WindowNative.Point();
        if (!WindowNative.ClientToScreen(target.WindowHandle, ref origin)) Fail();
        var rectangle = new WindowNative.Rect { Left = origin.X, Top = origin.Y,
            Right = checked(origin.X + client.Width), Bottom = checked(origin.Y + client.Height) };
        var desktop = new WindowNative.Rect { Left = WindowNative.GetSystemMetrics(76), Top = WindowNative.GetSystemMetrics(77) };
        desktop.Right = checked(desktop.Left + WindowNative.GetSystemMetrics(78));
        desktop.Bottom = checked(desktop.Top + WindowNative.GetSystemMetrics(79));
        if (rectangle.Left < desktop.Left || rectangle.Top < desktop.Top || rectangle.Right > desktop.Right || rectangle.Bottom > desktop.Bottom)
            throw new WindowCaptureException(CaptureFailure.OutsideDesktop, "Keep the whole game window on the visible desktop for quiet capture.");

        var visited = new HashSet<nint>();
        var window = WindowNative.GetTopWindow(0);
        for (; window != 0 && window != target.WindowHandle; window = WindowNative.GetWindow(window, 2))
        {
            if (!visited.Add(window))
                throw new WindowCaptureException(CaptureFailure.WindowChanged, "The visible window order changed. Read again.");
            if (!WindowNative.IsWindowVisible(window) || WindowNative.IsIconic(window)) continue;
            if (WindowNative.GetWindowCloaked(window, WindowNative.DwmCloaked, out int cloaked, sizeof(int)) == 0 && cloaked != 0) continue;
            if (!WindowNative.GetWindowRect(window, out var bounds)) continue;
            if (bounds.Left < rectangle.Right && bounds.Right > rectangle.Left && bounds.Top < rectangle.Bottom && bounds.Bottom > rectangle.Top)
                throw new WindowCaptureException(CaptureFailure.ObscuredWindow, "Another window covers the game. Move it away for quiet capture.");
        }
        if (window != target.WindowHandle)
            throw new WindowCaptureException(CaptureFailure.WindowChanged, "The game window order changed. Read again.");
        return rectangle;
    }

    private static void Fail() => throw new WindowCaptureException(CaptureFailure.CaptureFailed, "Windows could not copy the visible game rectangle. Use borderless or windowed mode, or choose Windows Graphics Capture.");
    [StructLayout(LayoutKind.Sequential)] private struct BitmapHeader { public uint Size; public int Width, Height; public ushort Planes, BitCount; public uint Compression, ImageSize; public int XPelsPerMeter, YPelsPerMeter; public uint ColorsUsed, ColorsImportant; }
    [StructLayout(LayoutKind.Sequential)] private struct BitmapInfo { public BitmapHeader Header; public uint Colors; }
    [DllImport("user32.dll")] private static extern nint GetDC(nint window);
    [DllImport("user32.dll")] private static extern int ReleaseDC(nint window, nint dc);
    [DllImport("gdi32.dll")] private static extern nint CreateCompatibleDC(nint dc);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(nint dc);
    [DllImport("gdi32.dll")] private static extern nint CreateDIBSection(nint dc, ref BitmapInfo info, uint usage, out nint pixels, nint section, uint offset);
    [DllImport("gdi32.dll")] private static extern nint SelectObject(nint dc, nint value);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(nint value);
    [DllImport("gdi32.dll")] private static extern bool BitBlt(nint destination, int x, int y, int width, int height, nint source, int sourceX, int sourceY, uint operation);
}
