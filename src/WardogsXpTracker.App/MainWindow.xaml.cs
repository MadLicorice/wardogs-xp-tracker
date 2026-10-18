using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using WardogsXpTracker.Core.Reading;
using WardogsXpTracker.Windows.Capture;
using WardogsXpTracker.Windows.Input;
using WardogsXpTracker.Windows.Reading;

namespace WardogsXpTracker.App;

public partial class MainWindow : Window
{
    private readonly WindowCaptureService capture = new();
    private readonly DispatcherTimer automaticTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    private readonly CancellationTokenSource lifetime = new();
    private GlobalHotkeyManager? hotkeys;
    private XpImageReader? reader;
    private bool busy;
    private bool closed;
    private bool f4Registered;
    private string currentSource = "No frame captured.";
    private ReadReportAttempt? currentAttempt;
    private ReadReportAttempt? lastRejectedAttempt;
    private ReadReportAttempt? lastAcceptedAttempt;

    public MainWindow()
    {
        InitializeComponent();
        var workArea = SystemParameters.WorkArea;
        var availableWidth = Math.Max(320, workArea.Width - 16);
        var availableHeight = Math.Max(240, workArea.Height - 16);
        MinWidth = Math.Min(MinWidth, availableWidth);
        MinHeight = Math.Min(MinHeight, availableHeight);
        Width = Math.Min(Width, availableWidth);
        Height = Math.Min(Height, availableHeight);
        ApplyLayoutProfile(Width);
        UpdatePageScrollMode();
        ReadingsList.ItemsSource = EmptyReadings();
        automaticTimer.Tick += async (_, _) => await CaptureWindowAsync(automatic: true);
        SourceInitialized += InitializeHotkey;
        Loaded += (_, _) => RefreshTargets();
        Loaded += async (_, _) =>
        {
            trackerInitialization = InitializeTrackerAsync();
            await trackerInitialization;
            await ShowInitialOnboardingAsync();
        };
        Closing += TrackerWindow_Closing;
        PreviewKeyDown += Window_KeyDown;
    }

    private void InitializeHotkey(object? sender, EventArgs e)
    {
        if (PresentationSource.FromVisual(this) is not HwndSource source) return;
        hotkeys = new(source);
        hotkeys.HotkeyPressed += async action =>
        {
            if (action == HotkeyAction.Check) await CaptureWindowAsync();
            else if (action == HotkeyAction.Snapshot) await SnapshotAsync();
        };
        f4Registered = hotkeys.TryRegister(HotkeyAction.Check, Key.F4, ModifierKeys.None, out var error);
        HotkeyStatus.Text = f4Registered
            ? "F4 works while the game has focus. Capture reads only the selected window."
            : $"Global F4 unavailable: {error} The Capture button and F4 in this app still work.";
        f3Registered = hotkeys.TryRegister(HotkeyAction.Snapshot, Key.F3, ModifierKeys.None, out _);
    }

    private async void Window_KeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == snapshotBinding.Key && Keyboard.Modifiers == snapshotBinding.Modifiers && !f3Registered)
        { e.Handled = true; await SnapshotAsync(); }
        else if (key == checkBinding.Key && Keyboard.Modifiers == checkBinding.Modifiers && !f4Registered)
        { e.Handled = true; await CaptureWindowAsync(); }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        if (await ActivateGameSourceAsync()) RefreshTargets();
    }

    private void RefreshTargets()
    {
        if (busy || closed) return;
        try
        {
            var previous = WindowSelector.SelectedItem as CaptureTarget;
            var targets = capture.GetCaptureTargets();
            WindowSelector.ItemsSource = targets;
            // Do not select an unrelated window after the player's game closes.
            WindowSelector.SelectedItem = previous is null ? null
                : targets.FirstOrDefault(t => t.WindowHandle == previous.WindowHandle && t.ProcessId == previous.ProcessId);
            if (targets.Count == 0)
            {
                ShowState("NO WINDOWS FOUND", "Open the game, then select Refresh.", false);
                SessionNotice.Text = "No game windows found. Open WARDOGS, then select Refresh.";
            }
            else if (trackerReady)
                SessionNotice.Text = "Game window list refreshed. Choose the WARDOGS window.";
        }
        catch (Exception exception)
        {
            ShowFailure("Window list failed", exception);
            SessionNotice.Text = $"Window list failed: {exception.Message}";
        }
    }

    private async void Capture_Click(object sender, RoutedEventArgs e) => await CaptureWindowAsync();

    private async Task CaptureWindowAsync(bool asSnapshot = false, bool automatic = false)
    {
        if (busy || closed) return;
        if (automatic && (!CanAutomaticallyReadNow || !AutomaticTargetHasFocus)) return;
        if (mockScreenshotMode)
        {
            if (!automatic) await ReadMockScreenshotAsync(asSnapshot);
            return;
        }
        if (WindowSelector.SelectedItem is not CaptureTarget target)
        {
            PauseAutomaticReading();
            ShowState("SELECT THE GAME WINDOW", $"Choose the game in the window list, then push {checkBinding}.", false);
            return;
        }
        SetBusy(true);
        var trackerContext = GetTrackerCaptureContext(asSnapshot,
            asSnapshot ? WardogsXpTracker.Core.Tracking.ReadingSource.SnapshotHotkey
                : automatic ? WardogsXpTracker.Core.Tracking.ReadingSource.AutomaticOcr
                : WardogsXpTracker.Core.Tracking.ReadingSource.CheckHotkey);
        try
        {
            if (!automatic) ShowState("CAPTURING", "Waiting for a frame from the selected game window.", null);
            var frame = await capture.CaptureAsync(target, SelectedCaptureMode, lifetime.Token);
            if (closed) return;
            var bitmap = BitmapSource.Create(frame.Width, frame.Height, 96, 96, PixelFormats.Bgra32,
                null, frame.Bgra32Pixels, frame.Stride);
            bitmap.Freeze();
            currentSource = $"{target.ProcessName} · {frame.Width} × {frame.Height} · {frame.CapturedAt.ToLocalTime():T}";
            await ReadImageAsync(bitmap, new("window", frame.Method, currentSource,
                frame.CapturedAt, new($"0x{target.WindowHandle.ToInt64():X}", target.ProcessId, target.ProcessName, target.Title)), trackerContext);
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        catch (WindowCaptureException exception) when (automatic && exception.Failure is
            CaptureFailure.NotForeground or CaptureFailure.ObscuredWindow or CaptureFailure.OutsideDesktop
            or CaptureFailure.MinimizedWindow or CaptureFailure.HiddenWindow)
        {
            // A background scan waits for the selected game rather than reading another app.
        }
        catch (Exception exception)
        {
            if (!closed)
            {
                PauseAutomaticReading();
                ShowFailure("Capture failed", exception);
            }
        }
        finally { FinishWork(); }
    }

    private async void LoadImage_Click(object sender, RoutedEventArgs e)
    {
        if (busy || closed) return;
        PauseAutomaticReading();
        SetBusy(true);
        try
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open a WARDOGS screenshot",
                Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff|All files|*.*",
                CheckFileExists = true,
                Multiselect = false
            };
            if (dialog.ShowDialog(this) != true || closed) return;
            var bitmap = XpImageReader.LoadImage(dialog.FileName);
            currentSource = $"{System.IO.Path.GetFileName(dialog.FileName)} · {bitmap.PixelWidth} × {bitmap.PixelHeight}";
            await ReadImageAsync(bitmap, new("import", "Local image import", currentSource, DateTimeOffset.UtcNow, null));
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        catch (Exception exception)
        {
            if (!closed) ShowFailure("Image read failed", exception);
        }
        finally { FinishWork(); }
    }

    private async Task ReadImageAsync(BitmapSource image, ReadReportContext context, TrackerCaptureContext? trackerContext = null)
    {
        var automatic = trackerContext?.Source == WardogsXpTracker.Core.Tracking.ReadingSource.AutomaticOcr;
        if (!automatic) ShowState("READING LOCALLY", "Reading class names, levels, and exact XP numbers.", null);
        reader ??= new();
        var analysis = await reader.ReadAsync(image, lifetime.Token);
        if (closed) return;
        if (automatic && !AutomaticReadPolicy.IsRelevantScene(analysis.Result)) return;
        FramePreview.Source = image;
        PreviewPlaceholder.Visibility = Visibility.Collapsed;
        SourceStatus.Text = currentSource;
        PresentAnalysis(analysis);
        currentAttempt = new(Guid.NewGuid(), image, analysis, context, DiagnosticsText.Text);
        if (analysis.Result.IsAccepted) lastAcceptedAttempt = currentAttempt;
        else lastRejectedAttempt = currentAttempt;
        ReportStatus.Text = (lastRejectedAttempt is { } rejected && rejected.Id != currentAttempt.Id)
            || (lastAcceptedAttempt is { } accepted && accepted.Id != currentAttempt.Id)
            ? "A report includes this read and the latest different successful or rejected read."
            : "A report includes this read. No files are saved until you choose a location.";
        await ApplyTrackerReadAsync(analysis, trackerContext);
    }

    private async void SaveReport_Click(object sender, RoutedEventArgs e)
    {
        if (busy || closed || currentAttempt is null) return;
        PauseAutomaticReading();
        SetBusy(true);
        try
        {
            var current = currentAttempt!;
            var rejected = lastRejectedAttempt is { } previous && previous.Id != current.Id ? previous : null;
            var accepted = lastAcceptedAttempt is { } successful && successful.Id != current.Id
                && successful.Id != rejected?.Id ? successful : null;
            var dialog = new SaveFileDialog
            {
                Title = "Save a local read report",
                Filter = "Read report ZIP|*.zip",
                FileName = $"wardogs-read-report-{DateTime.Now:yyyyMMdd-HHmmss}.zip",
                DefaultExt = ".zip",
                AddExtension = true,
                OverwritePrompt = true
            };
            if (dialog.ShowDialog(this) != true || closed) return;
            ReportStatus.Text = "Saving the captured image and read details locally…";
            var token = lifetime.Token;
            await Task.Run(() => WriteReadReport(dialog.FileName, current, rejected, accepted, token), token);
            if (!closed) ReportStatus.Text = $"Saved {Path.GetFileName(dialog.FileName)}. No report was uploaded.";
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        catch (Exception exception)
        {
            if (!closed) ReportStatus.Text = $"The report was not saved: {exception.Message}";
        }
        finally { FinishWork(); }
    }

    private static void WriteReadReport(string path, ReadReportAttempt current, ReadReportAttempt? rejected,
        ReadReportAttempt? accepted, CancellationToken cancellationToken)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            Converters = { new JsonStringEnumConverter() }
        };
        var assembly = Assembly.GetExecutingAssembly();
        var appVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString() ?? "unknown";
        object Summary(ReadReportAttempt attempt) => new
        {
            attempt.Id,
            attempt.Context,
            PixelWidth = attempt.Image.PixelWidth,
            PixelHeight = attempt.Image.PixelHeight,
            attempt.Image.DpiX,
            attempt.Image.DpiY,
            PixelFormat = attempt.Image.Format.ToString(),
            attempt.Analysis.Result.Screen,
            attempt.Analysis.Result.Status,
            attempt.Analysis.Result.Message
        };
        var manifest = new
        {
            SchemaVersion = 1,
            AppVersion = appVersion,
            ExportedAtUtc = DateTimeOffset.UtcNow,
            Current = Summary(current),
            LastRejected = rejected is null ? null : Summary(rejected),
            LastAccepted = accepted is null ? null : Summary(accepted),
            Description = "User-requested local export. Final totals can include bonuses. Images are the exact reader inputs."
        };
        var temporaryPath = path + $".{Guid.NewGuid():N}.tmp";
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                WriteTextEntry(archive, "manifest.json", JsonSerializer.Serialize(manifest, options));
                WriteAttempt(archive, "current", current, options, cancellationToken);
                if (rejected is not null) WriteAttempt(archive, "last-rejected", rejected, options, cancellationToken);
                if (accepted is not null) WriteAttempt(archive, "last-accepted", accepted, options, cancellationToken);
            }
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private static void WriteAttempt(ZipArchive archive, string folder, ReadReportAttempt attempt,
        JsonSerializerOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var imageEntry = archive.CreateEntry($"{folder}/frame.png", CompressionLevel.Fastest);
        using var encodedImage = new MemoryStream();
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(attempt.Image));
        encoder.Save(encodedImage);
        encodedImage.Position = 0;
        using (var imageStream = imageEntry.Open())
            encodedImage.CopyTo(imageStream);
        WriteTextEntry(archive, $"{folder}/analysis.json", JsonSerializer.Serialize(attempt.Analysis, options));
        WriteTextEntry(archive, $"{folder}/diagnostics.txt", attempt.DiagnosticsText);
    }

    private static void WriteTextEntry(ZipArchive archive, string name, string text)
    {
        using var stream = archive.CreateEntry(name, CompressionLevel.Fastest).Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.Write(text);
    }

    private void PresentAnalysis(ImageReadAnalysis analysis)
    {
        var result = analysis.Result;
        var title = result.IsAccepted
            ? result.Screen switch
            {
                ScreenKind.Unlock => "CURRENT CLASS XP READ",
                ScreenKind.EndProgression => "FINAL RESULTS READ · BONUS MAY BE INCLUDED",
                ScreenKind.EndScore => "END SCREEN DETECTED",
                _ => "READ ACCEPTED"
            }
            : $"READ REJECTED · {ReadableStatus(result.Status)}";
        ShowState(title, result.Message, result.IsAccepted);
        // Each frame gets its own values. A rejected frame cannot display an older total as a fresh reading.
        ReadingsList.ItemsSource = Enum.GetValues<XpClass>().Select(track =>
        {
            var reading = result.IsAccepted ? result.Readings.FirstOrDefault(r => r.Class == track) : null;
            return reading is null ? new ClassObservation(track.ToString().ToUpperInvariant(), "—", "Not read in this frame")
                : new ClassObservation(track.ToString().ToUpperInvariant(),
                    reading.CumulativeXp.ToString("N0", CultureInfo.CurrentCulture),
                    $"{(reading.CurrentLevel is { } level ? $"Level {level} · " : "")}Threshold {reading.NextThreshold:N0}");
        }).ToArray();
        ReadingsHeading.Text = result.Screen == ScreenKind.EndProgression ? "OBSERVED FINAL TOTALS" : "OBSERVED CLASS TOTALS";
        PlacementStatus.Text = result.Placement.HasConflict ? "Placement: conflicting evidence. Review required."
            : result.Placement.PossiblePlaces.Count switch
            {
                0 => "Placement: no evidence",
                1 => $"Placement: {Ordinal(result.Placement.PossiblePlaces[0])}",
                _ => "Placement: 2nd or 3rd"
            };
        SourceStatus.Text = $"{currentSource} · OCR {analysis.ElapsedMilliseconds / 1000:F2} s";
        var text = new StringBuilder()
            .AppendLine($"Screen: {result.Screen} | State: {result.Status} | Selected class: {analysis.SelectedClass?.ToString() ?? "not established"}")
            .AppendLine(result.Message).AppendLine().AppendLine("OCR observations:");
        foreach (var line in result.RawLines)
            text.AppendLine($"[{line.Bounds.X:F3},{line.Bounds.Y:F3}] {line.Text}");
        text.AppendLine().AppendLine("Reader diagnostics:");
        foreach (var diagnostic in analysis.Diagnostics) text.AppendLine(diagnostic);
        DiagnosticsText.Text = text.ToString();
    }

    private void SetBusy(bool value)
    {
        busy = value;
        RefreshTargetsButton.IsEnabled = !value;
        CaptureButton.IsEnabled = !value;
        LoadImageButton.IsEnabled = !value;
        WindowSelector.IsEnabled = !value;
        AutoReadToggle.IsEnabled = !value && !mockScreenshotMode;
        CaptureModeSelector.IsEnabled = !value && !mockScreenshotMode;
        SaveReportButton.IsEnabled = !value && currentAttempt is not null;
        SetTrackerBusy(value);
    }

    private void FinishWork()
    {
        if (!closed) { SetBusy(false); SyncAutomaticReading(); }
        else
        {
            busy = false;
            reader?.Dispose();
            lifetime.Dispose();
        }
    }

    private void ShowFailure(string title, Exception exception)
    {
        ShowState(title.ToUpperInvariant(), exception.Message, false);
        ReadingsList.ItemsSource = EmptyReadings();
        ReadingsHeading.Text = "NO NEW READINGS";
        PlacementStatus.Text = "Placement: no new evidence";
        DiagnosticsText.Text = $"{title}: {exception.GetType().Name}{Environment.NewLine}{exception.Message}";
        // Keep the preview paired with the retained report after a read throws before it returns an analysis.
        FramePreview.Source = currentAttempt?.Image;
        PreviewPlaceholder.Visibility = currentAttempt is null ? Visibility.Visible : Visibility.Collapsed;
        SourceStatus.Text = currentAttempt?.Context.CurrentSource ?? "No completed read.";
        ReportStatus.Text = currentAttempt is null ? "No completed read is available to export."
            : "A report still contains the last completed read and any retained successful or rejected read.";
    }

    private void ShowState(string title, string message, bool? accepted)
    {
        if (trackerReady && accepted is { } wasAccepted)
        {
            SetReadFeedback(wasAccepted, message);
            if (!wasAccepted) SessionNotice.Text = message;
        }
        ReadStateTitle.Text = title;
        ReadStateMessage.Text = message;
        ReadStateBorder.Background = new SolidColorBrush(accepted switch
        {
            true => Color.FromRgb(28, 57, 67),
            false => Color.FromRgb(68, 43, 39),
            null => Color.FromRgb(38, 49, 61)
        });
        ReadStateTitle.Foreground = new SolidColorBrush(accepted == false
            ? Color.FromRgb(244, 185, 162) : Color.FromRgb(160, 219, 238));
    }

    private static ClassObservation[] EmptyReadings() => Enum.GetValues<XpClass>()
        .Select(track => new ClassObservation(track.ToString().ToUpperInvariant(), "—", "No accepted reading")).ToArray();
    private static string ReadableStatus(ReadStatus status) => status switch
    {
        ReadStatus.NoCurrentXp => "CURRENT XP NOT VISIBLE",
        ReadStatus.ClassNotEstablished => "SELECT A CLASS",
        ReadStatus.UnsupportedScreen => "UNSUPPORTED SCREEN",
        ReadStatus.InconsistentLevel => "LEVEL MISMATCH",
        ReadStatus.LowConfidence => "UNCLEAR DIGITS",
        ReadStatus.InvalidNumber => "INVALID NUMBERS",
        ReadStatus.WrongClass => "WRONG CLASS",
        _ => "AMBIGUOUS READING"
    };
    private static string Ordinal(int place) => place switch { 1 => "1st", 2 => "2nd", 3 => "3rd", _ => "unknown" };
    private void Diagnostics_Expanded(object sender, RoutedEventArgs e) => DiagnosticsText.ScrollToHome();

    protected override void OnClosed(EventArgs e)
    {
        closed = true;
        StopTestToolsFlash();
        trackerSaveLock?.Dispose();
        automaticTimer.Stop();
        lifetime.Cancel();
        _ = capture.DisposeAsync().AsTask();
        hotkeys?.Dispose();
        if (!busy)
        {
            reader?.Dispose();
            lifetime.Dispose();
        }
        base.OnClosed(e);
    }

    public sealed record ClassObservation(string ClassName, string Xp, string Detail);
    private sealed record ReadReportTarget(string WindowHandle, uint ProcessId, string ProcessName, string Title);
    private sealed record ReadReportContext(string SourceKind, string CaptureMethod, string CurrentSource,
        DateTimeOffset FrameAt, ReadReportTarget? Target);
    private sealed record ReadReportAttempt(Guid Id, BitmapSource Image, ImageReadAnalysis Analysis,
        ReadReportContext Context, string DiagnosticsText);
}
