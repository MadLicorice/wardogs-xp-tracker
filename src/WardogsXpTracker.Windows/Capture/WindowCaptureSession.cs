using System.Diagnostics;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;

namespace WardogsXpTracker.Windows.Capture;

/// <summary>Keeps one explicit-window session alive between reads. The Windows capture border remains enabled.</summary>
internal sealed class WindowCaptureSession : IDisposable
{
    private readonly IDirect3DDevice device;
    private readonly GraphicsCaptureItem item;
    private readonly Direct3D11CaptureFramePool pool;
    private readonly GraphicsCaptureSession session;
    private readonly object sync = new();
    private Request? pending;
    private SizeInt32 size;
    private bool started, closed, disposed;
    internal CaptureTarget Target { get; }

    internal WindowCaptureSession(CaptureTarget target)
    {
        Target = target;
        device = Direct3DInterop.CreateDevice();
        try
        {
            item = Direct3DInterop.CreateItem(target.WindowHandle);
            size = item.Size;
            ValidateSize(size);
            pool = Direct3D11CaptureFramePool.CreateFreeThreaded(device, DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, size);
            GraphicsCaptureSession? createdSession = null;
            try
            {
                session = pool.CreateCaptureSession(item);
                createdSession = session;
                session.IsCursorCaptureEnabled = false;
                pool.FrameArrived += OnFrameArrived;
                item.Closed += OnClosed;
            }
            catch { createdSession?.Dispose(); pool.Dispose(); throw; }
        }
        catch { device.Dispose(); throw; }
    }

    internal async Task<(Direct3D11CaptureFrame Frame, DateTimeOffset At)> TakeFrameAsync(CancellationToken cancellationToken)
    {
        if (closed || disposed)
            throw new WindowCaptureException(CaptureFailure.MissingWindow, "The selected window closed. Select its game window again.");
        var currentSize = item.Size;
        ValidateSize(currentSize);
        if (currentSize.Width != size.Width || currentSize.Height != size.Height)
        {
            pool.Recreate(device, DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, currentSize);
            size = currentSize;
        }
        var request = new Request(TimeSpan.FromSeconds((double)Stopwatch.GetTimestamp() / Stopwatch.Frequency));
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(4));
        using var registration = timeout.Token.Register(() => request.Completion.TrySetCanceled(timeout.Token));
        lock (sync) pending = request;
        try
        {
            if (!started) { session.StartCapture(); started = true; }
            return await request.Completion.Task.ConfigureAwait(false);
        }
        finally { lock (sync) { if (ReferenceEquals(pending, request)) pending = null; } }
    }

    private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
    {
        try
        {
            while (sender.TryGetNextFrame() is { } frame)
            {
                Request? request = null;
                lock (sync)
                {
                    if (pending is not null && frame.SystemRelativeTime >= pending.After)
                    { request = pending; pending = null; }
                }
                if (request is null || !request.Completion.TrySetResult((frame, DateTimeOffset.UtcNow))) frame.Dispose();
            }
        }
        catch (Exception exception) { lock (sync) { pending?.Completion.TrySetException(exception); } }
    }

    private void OnClosed(GraphicsCaptureItem sender, object args)
    {
        lock (sync)
        {
            closed = true;
            pending?.Completion.TrySetException(new WindowCaptureException(CaptureFailure.MissingWindow,
                "The selected window closed during capture."));
        }
    }

    public void Dispose()
    {
        lock (sync)
        {
            if (disposed) return;
            disposed = true;
            pending?.Completion.TrySetCanceled(); pending = null;
        }
        pool.FrameArrived -= OnFrameArrived;
        item.Closed -= OnClosed;
        try { session.Dispose(); }
        finally { try { pool.Dispose(); } finally { device.Dispose(); } }
    }

    private static void ValidateSize(SizeInt32 value)
    {
        if (value.Width < 1 || value.Height < 1 || value.Width > 16384 || value.Height > 16384)
            throw new WindowCaptureException(CaptureFailure.EmptyWindow, "The selected window has no supported image size.");
    }
    private sealed record Request(TimeSpan After)
    {
        internal TaskCompletionSource<(Direct3D11CaptureFrame Frame, DateTimeOffset At)> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
