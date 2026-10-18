namespace WardogsXpTracker.Windows.Capture;

public enum CaptureMode { WindowGraphicsCapture, QuietVisibleClient }

public sealed record CaptureTarget(nint WindowHandle, uint ProcessId, string ProcessName, string Title)
{
    public override string ToString() => $"{Title} ({ProcessName})";
}

/// <summary>A client-area image in physical pixels. The byte order is B, G, R, A.</summary>
public sealed record CapturedFrame(
    int Width,
    int Height,
    byte[] Bgra32Pixels,
    DateTimeOffset CapturedAt,
    CaptureTarget Target)
{
    public int Stride => checked(Width * 4);
    public string Method { get; init; } = WindowCaptureService.CaptureMethod;
}

public enum CaptureFailure
{
    Unsupported,
    MissingWindow,
    MinimizedWindow,
    HiddenWindow,
    EmptyWindow,
    WindowChanged,
    NoFrame,
    BlankFrame,
    AccessDenied,
    CaptureFailed,
    NotForeground,
    ObscuredWindow,
    OutsideDesktop
}

public sealed class WindowCaptureException(CaptureFailure failure, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public CaptureFailure Failure { get; } = failure;
}
