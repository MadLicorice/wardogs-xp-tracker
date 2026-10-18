using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace WardogsXpTracker.Windows.Capture;

/// <summary>Captures only an explicitly selected window through Windows Graphics Capture.</summary>
public sealed class WindowCaptureService : IAsyncDisposable
{
    public const string CaptureMethod = "Windows Graphics Capture · selected window only";

    public IReadOnlyList<CaptureTarget> GetCaptureTargets()
    {
        var targets = new List<CaptureTarget>();
        WindowNative.EnumWindows((window, _) =>
        {
            if (!WindowNative.IsWindowVisible(window) || WindowNative.IsToolWindow(window)) return true;
            if (WindowNative.GetWindowCloaked(window, WindowNative.DwmCloaked, out int cloaked, sizeof(int)) == 0 && cloaked != 0)
                return true;
            int length = WindowNative.GetWindowTextLength(window);
            if (length <= 0) return true;
            WindowNative.GetWindowThreadProcessId(window, out uint processId);
            if (processId == Environment.ProcessId) return true;
            var title = new StringBuilder(length + 1);
            WindowNative.GetWindowText(window, title, title.Capacity);
            if (string.IsNullOrWhiteSpace(title.ToString())) return true;
            try
            {
                using var process = Process.GetProcessById((int)processId);
                targets.Add(new(window, processId, process.ProcessName, title.ToString()));
            }
            catch (ArgumentException) { }
            catch (InvalidOperationException) { }
            catch (System.ComponentModel.Win32Exception) { }
            return true;
        }, 0);
        return targets.OrderBy(t => t.Title, StringComparer.CurrentCultureIgnoreCase).ToArray();
    }

    private readonly SemaphoreSlim captureGate = new(1, 1);
    private WindowCaptureSession? activeSession;
    private bool disposed;

    public Task<CapturedFrame> CaptureAsync(CaptureTarget target, CancellationToken cancellationToken = default)
        => CaptureAsync(target, CaptureMode.WindowGraphicsCapture, cancellationToken);

    public async Task<CapturedFrame> CaptureAsync(CaptureTarget target, CaptureMode mode, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        await captureGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return await Task.Run(async () =>
            {
                try
                {
                    ValidateTarget(target);
                    if (mode == CaptureMode.QuietVisibleClient)
                    {
                        StopCore();
                        var visible = VisibleClientCapture.Capture(target, cancellationToken);
                        ValidateTarget(target);
                        if (IsBlank(visible.Bgra32Pixels))
                            throw new WindowCaptureException(CaptureFailure.BlankFrame, "The visible game area was blank. Use borderless or windowed mode.");
                        return visible;
                    }
                    if (!GraphicsCaptureSession.IsSupported())
                        throw new WindowCaptureException(CaptureFailure.Unsupported,
                            "Windows Graphics Capture is unavailable. Use Windows 10 version 2004 or later with a supported display driver.");
                    if (activeSession is not null && (activeSession.Target.WindowHandle != target.WindowHandle
                        || activeSession.Target.ProcessId != target.ProcessId)) StopCore();
                    activeSession ??= new(target);
                    var captured = await activeSession.TakeFrameAsync(cancellationToken).ConfigureAwait(false);
                    return await CopyFrameAsync(target, captured.Frame, captured.At, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    StopCore();
                    throw;
                }
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (WindowCaptureException) { throw; }
        catch (ObjectDisposedException) { throw; }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (OperationCanceledException exception)
        {
            throw new WindowCaptureException(CaptureFailure.NoFrame,
                "The selected window supplied no fresh frame. Restore the game and use borderless or windowed mode, then try again.", exception);
        }
        catch (COMException exception) when (exception.HResult == unchecked((int)0x80070005))
        {
            throw new WindowCaptureException(CaptureFailure.AccessDenied,
                "Windows blocked capture of the selected window. Check the game's capture permissions and try again.", exception);
        }
        catch (Exception exception) when (exception is COMException or InvalidOperationException or ArgumentException)
        {
            throw new WindowCaptureException(CaptureFailure.CaptureFailed,
                "Capture failed for the selected window. Restore the game or select its window again.", exception);
        }
        finally { captureGate.Release(); }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await captureGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try { await Task.Run(StopCore).ConfigureAwait(false); }
        finally { captureGate.Release(); }
    }

    public async ValueTask DisposeAsync()
    {
        await captureGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (disposed) return;
            disposed = true;
            await Task.Run(StopCore).ConfigureAwait(false);
        }
        finally { captureGate.Release(); }
    }

    private void StopCore()
    {
        var previous = activeSession;
        activeSession = null;
        previous?.Dispose();
    }

    private static async Task<CapturedFrame> CopyFrameAsync(CaptureTarget target, Direct3D11CaptureFrame captured,
        DateTimeOffset capturedAt, CancellationToken cancellationToken)
    {
        using var frame = captured;
        cancellationToken.ThrowIfCancellationRequested();
        ValidateTarget(target);
        var client = GetPhysicalClientCrop(target.WindowHandle, frame.ContentSize.Width, frame.ContentSize.Height);
        using var bitmap = await SoftwareBitmap.CreateCopyFromSurfaceAsync(frame.Surface, BitmapAlphaMode.Ignore);
        cancellationToken.ThrowIfCancellationRequested();
        if (bitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8)
            throw new WindowCaptureException(CaptureFailure.CaptureFailed, "The selected window returned an unsupported pixel format.");
        if (bitmap.PixelWidth < frame.ContentSize.Width || bitmap.PixelHeight < frame.ContentSize.Height)
            throw new WindowCaptureException(CaptureFailure.WindowChanged, "The window size changed. Try capture again.");
        uint size = checked((uint)(bitmap.PixelWidth * bitmap.PixelHeight * 4));
        var buffer = new global::Windows.Storage.Streams.Buffer(size) { Length = size };
        bitmap.CopyToBuffer(buffer);
        var pixels = new byte[size];
        using (var reader = DataReader.FromBuffer(buffer)) reader.ReadBytes(pixels);
        int stride = checked(client.Width * 4);
        var cropped = new byte[checked(stride * client.Height)];
        for (int y = 0; y < client.Height; y++)
            System.Buffer.BlockCopy(pixels, checked(((client.Top + y) * bitmap.PixelWidth + client.Left) * 4), cropped, y * stride, stride);
        ValidateTarget(target);
        if (IsBlank(cropped))
            throw new WindowCaptureException(CaptureFailure.BlankFrame,
                "The selected window returned a blank image. Restore the game and use borderless or windowed mode, then try again.");
        return new(client.Width, client.Height, cropped, capturedAt, target);
    }
    private static void ValidateTarget(CaptureTarget target)
    {
        if (target.WindowHandle == 0 || !WindowNative.IsWindow(target.WindowHandle))
            throw new WindowCaptureException(CaptureFailure.MissingWindow, "Select an open game window before capture.");
        WindowNative.GetWindowThreadProcessId(target.WindowHandle, out uint processId);
        if (processId != target.ProcessId)
            throw new WindowCaptureException(CaptureFailure.WindowChanged, "The selected window changed. Select the game window again.");
        if (processId == Environment.ProcessId)
            throw new WindowCaptureException(CaptureFailure.Unsupported, "Select the game window instead of the tracker window.");
        if (WindowNative.IsIconic(target.WindowHandle))
            throw new WindowCaptureException(CaptureFailure.MinimizedWindow, "Restore the game window before capture.");
        if (!WindowNative.IsWindowVisible(target.WindowHandle) ||
            (WindowNative.GetWindowCloaked(target.WindowHandle, WindowNative.DwmCloaked, out int cloaked, sizeof(int)) == 0 && cloaked != 0))
            throw new WindowCaptureException(CaptureFailure.HiddenWindow, "Make the selected game window visible before capture.");
    }

    private static WindowNative.Rect GetPhysicalClientCrop(nint window, int contentWidth, int contentHeight)
    {
        // Native geometry must use physical pixels even when the UI has a different DPI context.
        nint previousDpi = WindowNative.SetThreadDpiAwarenessContext(-4);
        try
        {
            if (!WindowNative.GetClientRect(window, out var client) || client.Width <= 0 || client.Height <= 0)
                throw new WindowCaptureException(CaptureFailure.EmptyWindow, "The game window has an empty client area.");
            if (client.Width == contentWidth && client.Height == contentHeight)
                return new WindowNative.Rect { Right = contentWidth, Bottom = contentHeight };
            var origin = new WindowNative.Point();
            if (!WindowNative.ClientToScreen(window, ref origin) ||
                WindowNative.GetWindowBounds(window, WindowNative.DwmExtendedFrameBounds, out var windowBounds,
                    Marshal.SizeOf<WindowNative.Rect>()) != 0)
                throw new WindowCaptureException(CaptureFailure.WindowChanged, "The game window bounds are unavailable. Try capture again.");
            int x = origin.X - windowBounds.Left;
            int y = origin.Y - windowBounds.Top;
            if (Math.Abs(windowBounds.Width - contentWidth) > 2 || Math.Abs(windowBounds.Height - contentHeight) > 2 ||
                x < 0 || y < 0 || x + client.Width > contentWidth || y + client.Height > contentHeight)
                throw new WindowCaptureException(CaptureFailure.WindowChanged,
                    "The game window changed size or has unsupported borders. Use borderless mode and try again.");
            return new WindowNative.Rect { Left = x, Top = y, Right = x + client.Width, Bottom = y + client.Height };
        }
        finally
        {
            if (previousDpi != 0) WindowNative.SetThreadDpiAwarenessContext(previousDpi);
        }
    }

    private static bool IsBlank(byte[] pixels)
    {
        int[] minimum = [255, 255, 255];
        int[] maximum = [0, 0, 0];
        // Sample evenly across the image. Alpha is not evidence of visible game content.
        int step = Math.Max(4, (pixels.Length / 4096) & ~3);
        for (int i = 0; i + 2 < pixels.Length; i += step)
        {
            for (int channel = 0; channel < 3; channel++)
            {
                minimum[channel] = Math.Min(minimum[channel], pixels[i + channel]);
                maximum[channel] = Math.Max(maximum[channel], pixels[i + channel]);
                if (maximum[channel] - minimum[channel] >= 4) return false;
            }
        }
        return true;
    }
}
