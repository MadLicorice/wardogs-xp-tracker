using System.IO;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Markup;
using System.Xml.Linq;
using WardogsXpTracker.App;
using WardogsXpTracker.Core.Reading;
using WardogsXpTracker.Core.Tracking;
using WardogsXpTracker.Windows.Input;

internal static class Program
{
    private static MainWindow? window;
    private static nint handle;
    private static string output = "";
    private static string profileDirectory = "";
    private static readonly List<Check> checks = [];
    private static readonly List<Measurement> measurements = [];
    private static readonly DateTimeOffset At = DateTimeOffset.UtcNow;

    [STAThread]
    private static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        SetProcessDpiAwarenessContext(-4);
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Supply one report directory. Use scripts/Test-TrackerViewport.ps1 from the repository.");
            return 1;
        }
        output = Path.GetFullPath(args[0]);
        Directory.CreateDirectory(output);
        profileDirectory = Path.Combine(output, "isolated-profile-" + Guid.NewGuid().ToString("N"));
        Environment.SetEnvironmentVariable("WARDOGS_XP_DATA_DIRECTORY", profileDirectory);
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown, Resources = LoadAppResources() };
        var dispatcher = Dispatcher.CurrentDispatcher;
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
        try
        {
            var task = Run();
            var frame = new DispatcherFrame();
            _ = task.ContinueWith(_ => dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() => frame.Continue = false)), TaskScheduler.Default);
            if (!task.IsCompleted) Dispatcher.PushFrame(frame);
            task.GetAwaiter().GetResult();
            Save();
            Console.WriteLine($"RESULT: {checks.Count(check => check.Passed)} passed, {checks.Count(check => !check.Passed)} failed. Native Window test.");
            return checks.All(check => check.Passed) ? 0 : 2;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); Save(); return 1; }
        finally
        {
            if (window is not null) { Field("closeApproved").SetValue(window, true); window.Close(); }
            app.Shutdown();
        }
    }

    private static ResourceDictionary LoadAppResources()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "src", "WardogsXpTracker.App", "App.xaml")))
            directory = directory.Parent;
        if (directory is null) throw new DirectoryNotFoundException("Run the viewport checks from the repository build output.");
        var appDirectory = Path.Combine(directory.FullName, "src", "WardogsXpTracker.App");
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        var document = XDocument.Load(Path.Combine(appDirectory, "App.xaml"));
        var resources = document.Root!.Element(presentation + "Application.Resources")!;
        var dictionary = new XElement(resources.Elements().Single());
        dictionary.SetAttributeValue(XNamespace.Xmlns + "x", "http://schemas.microsoft.com/winfx/2006/xaml");
        foreach (var source in dictionary.Descendants().Where(element => element.Attribute("Source") is not null).ToArray())
        {
            var linked = XDocument.Load(Path.Combine(appDirectory, source.Attribute("Source")!.Value)).Root!;
            linked.SetAttributeValue(XNamespace.Xmlns + "local", "clr-namespace:WardogsXpTracker.App;assembly=WardogsXpTracker");
            source.ReplaceWith(linked);
        }
        return (ResourceDictionary)XamlReader.Parse(dictionary.ToString());
    }

    private static async Task Run()
    {
        window = new MainWindow { ShowInTaskbar = false, ShowActivated = false,
            WindowStartupLocation = WindowStartupLocation.Manual, Left = -20000, Top = -20000 };
        var init = typeof(MainWindow).GetMethod("InitializeHotkey", BindingFlags.NonPublic | BindingFlags.Instance)!;
        window.SourceInitialized -= (EventHandler)Delegate.CreateDelegate(typeof(EventHandler), window, init);
        var dismissedSetup = false;
        var setupTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(25) };
        setupTimer.Tick += (_, _) =>
        {
            var guide = Application.Current.Windows.Cast<Window>().FirstOrDefault(item => item.Name == "OnboardingWindow" && item.Owner == window);
            var skip = guide is null ? null : Descendants<Button>(guide).FirstOrDefault(item => item.Name == "OnboardingSkipButton");
            if (skip is null) return;
            setupTimer.Stop(); dismissedSetup = true;
            typeof(Button).GetMethod("OnClick", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(skip, null);
        };
        setupTimer.Start();
        window.Show();
        await Pump();
        if (Field("trackerInitialization").GetValue(window) is Task startup) await startup;
        var setupDeadline = DateTime.UtcNow.AddSeconds(15);
        while ((!((bool)Field("onboardingStartupChecked").GetValue(window)!) || (bool)Field("onboardingOpen").GetValue(window)!) && DateTime.UtcNow < setupDeadline)
        { await Task.Delay(25); await Pump(); }
        setupTimer.Stop();
        Record(dismissedSetup && ((TrackerPreferences)Field("preferences").GetValue(window)!).OnboardingDeferredVersion == OnboardingProgress.CurrentVersion,
            "fresh-setup-skip", "The real first-launch guide appeared and Skip saved its deferral before layout checks.");
        Record((bool)Field("trackerReady").GetValue(window)!, "native-init", "Native MainWindow Loaded initialization completed in an isolated profile.");
        Record(Field("hotkeys").GetValue(window) is null, "no-global-hotkeys", "The global-hotkey hook remained disconnected for the test.");
        Record(Application.Current.Windows.OfType<MainWindow>().Count() == 1, "one-native-window-on-start", "Only the fixture's MainWindow exists after startup.");
        ((TrackerPreferences)Field("preferences").GetValue(window)!).CompletionSound = false;
        E<CheckBox>("SoundToggle").IsChecked = false;
        E<CheckBox>("AutoReadToggle").IsChecked = false;
        Record(E<TabItem>("TestToolsTab").Visibility == Visibility.Collapsed, "test-tools-default-hidden", "A new profile hides Test Tools until the user enables it.");
        Record(E<Button>("LoadFakeStatsButton").Visibility == Visibility.Collapsed, "fake-stats-default-hidden", "A new profile hides fake analytics actions.");
        handle = new WindowInteropHelper(window).Handle;
        foreach (var size in new[] { (1280, 672), (1920, 672), (1280, 850), (816, 672) })
        {
            var dpi = GetDpiForWindow(handle);
            window.Width = size.Item1 * 96d / dpi;
            window.Height = size.Item2 * 96d / dpi;
            await Pump();
            foreach (var state in new[] { "missing-baseline", "ready", "gain", "rejected", "pending", "resume", "inactive" })
            {
                SetState(state);
                var guide = Descendants<Expander>(window).First(item => item.Header?.ToString() == "Quick guide");
                guide.IsExpanded = false;
                await Pump();
                E<ScrollViewer>("MainScrollViewer").ScrollToTop();
                await Pump();
                var before = Measure(size.Item1, size.Item2, state, false);
                AssertFitness(before);
                if (state == "pending")
                {
                    var feedback = typeof(MainWindow).GetMethod("SetReadFeedback", BindingFlags.Instance | BindingFlags.NonPublic)!;
                    feedback.Invoke(window, [false, "A later frame was rejected. Saved final totals remain available."]);
                    await Pump();
                    var rejectedFinal = Measure(size.Item1, size.Item2, state, false);
                    AssertFitness(rejectedFinal);
                    AssertStable(before, rejectedFinal);
                    var action = E<Button>("RecordFinalButton");
                    var center = action.TransformToAncestor(window).Transform(new Point(action.ActualWidth / 2, action.ActualHeight / 2));
                    var hit = window.InputHitTest(center) as DependencyObject;
                    while (hit is not null && !ReferenceEquals(hit, action)) hit = VisualTreeHelper.GetParent(hit);
                    Record(ReferenceEquals(hit, action), Key(rejectedFinal, "record-action-after-read-error"), "A later read error cannot cover the saved final-result action.");
                    if (size == (1280, 672)) Render("1280x672-pending-with-read-error.png");
                    feedback.Invoke(window, [true, ""]);
                    await Pump();
                }
                if (state is "ready" or "rejected" || size == (1280, 672)) Render($"{size.Item1}x{size.Item2}-{state}.png");
                var toggle = Descendants<ToggleButton>(guide).First();
                typeof(ToggleButton).GetMethod("OnClick", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(toggle, null);
                await Pump();
                var expanded = Measure(size.Item1, size.Item2, state, true);
                Record(guide.IsExpanded, Key(before, "guide-open"), "The actual guide header OnClick opened its content.");
                AssertFitness(expanded);
                AssertStable(before, expanded);
                if (size == (1280, 672) || size == (1920, 672) && state is "ready" or "rejected") Render($"{size.Item1}x{size.Item2}-{state}-guide-open.png");
                typeof(ToggleButton).GetMethod("OnClick", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(toggle, null);
                await Pump();
                var closed = Measure(size.Item1, size.Item2, state, false);
                AssertFitness(closed);
                AssertStable(before, closed);
                if (state is "inactive" or "resume")
                {
                    E<ScrollViewer>("MainScrollViewer").ScrollToBottom();
                    await Pump();
                    var atBottom = Measure(size.Item1, size.Item2, state + "-bottom", false);
                    Record(atBottom.OffsetY == 0, Key(atBottom, "no-page-scroll"), "The complete tracker must fit without a page scroll range.");
                    typeof(ToggleButton).GetMethod("OnClick", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(toggle, null);
                    await Pump();
                    AssertStable(atBottom, Measure(size.Item1, size.Item2, state + "-bottom", true));
                    guide.IsExpanded = false;
                    await Pump();
                }
            }
        }
        var extendedBinding = new HotkeyBinding(System.Windows.Input.Key.F11,
            System.Windows.Input.ModifierKeys.Control | System.Windows.Input.ModifierKeys.Shift);
        var extendedLabel = (string)typeof(MainWindow).GetMethod("ShortcutLabel", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [extendedBinding])!;
        Record(extendedLabel == "Ctrl+Shift+F11", "production-shortcut-label", $"Production ShortcutLabel returned {extendedLabel}.");
        Field("snapshotBinding").SetValue(window, extendedBinding);
        Field("checkBinding").SetValue(window, extendedBinding);
        E<Button>("SnapshotButton").Content = $"Snapshot · {extendedLabel}";
        E<Button>("CaptureButton").Content = $"Check XP · {extendedLabel}";
        foreach (var width in new[] { 816, 1280, 1920 })
        {
            var dpi = GetDpiForWindow(handle);
            window.Width = width * 96d / dpi;
            window.Height = 672 * 96d / dpi;
            SetState("ready");
            var guide = Descendants<Expander>(window).First(item => item.Header?.ToString() == "Quick guide");
            guide.IsExpanded = false;
            await Pump();
            var result = Measure(width, 672, "long-hotkeys", false);
            AssertFitness(result);
            foreach (var name in new[] { "SnapshotButton", "CaptureButton" })
            {
                var button = E<Button>(name);
                Record(TextFits(button.Content?.ToString() ?? "", button), $"{width}:shortcut-label:{name}",
                    $"{button.Content}; actual button width {button.ActualWidth:F2}, content capacity {button.ActualWidth - button.Padding.Left - button.Padding.Right - button.BorderThickness.Left - button.BorderThickness.Right:F2} DIP.");
            }
            foreach (var tab in E<TabControl>("MainTabs").Items.OfType<TabItem>().Where(tab => tab.Visibility == Visibility.Visible))
            {
                Record(InViewport(Box(tab), result), $"{width}:tab-visible:{tab.Header}", $"Actual tab bounds {Box(tab)}.");
                Record(TextFits(tab.Header?.ToString() ?? "", tab), $"{width}:tab-label:{tab.Header}", $"Header fits its actual tab width {tab.ActualWidth:F2} DIP.");
                var clip = VisualTreeHelper.GetClip(tab)?.Bounds;
                Record(clip is null || clip.Value.Right >= tab.ActualWidth - .1 && clip.Value.Bottom >= tab.ActualHeight - .1,
                    $"{width}:tab-edges:{tab.Header}", "The complete tab border fits without the specialized tab-host clip.");
                tab.IsSelected = true;
                await Pump();
                var border = (Border)tab.Template.FindName("TabBorder", tab);
                var background = border.Background.ToString();
                var hoverKey = (DependencyPropertyKey)typeof(UIElement).GetField("IsMouseOverPropertyKey", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
                tab.SetValue(hoverKey, true);
                await Pump();
                Record(border.Background.ToString() == background, $"{width}:selected-hover:{tab.Header}", "Hover retains the selected background.");
                tab.SetValue(hoverKey, false);
            }
            E<TabItem>("SessionTrackerTab").IsSelected = true;
            await Pump();
            Render($"{width}x672-long-hotkeys.png");
            var guidePreferences = (TrackerPreferences)Field("preferences").GetValue(window)!;
            guidePreferences.InteractiveWalkthrough = InteractiveWalkthroughProgress.Start(At);
            Call("RefreshTracker");
            await Pump();
            var withWalkthrough = Measure(width, 672, "interactive-walkthrough", false);
            AssertFitness(withWalkthrough);
            AssertStable(result, withWalkthrough);
            Record(E<Border>("InteractiveWalkthroughPanel").Visibility == Visibility.Visible,
                $"{width}:interactive-guide-visible", "The modeless walkthrough occupies only the scrolling sidebar.");
            Render($"{width}x672-interactive-walkthrough.png");
            guidePreferences.InteractiveWalkthrough = InteractiveWalkthroughProgress.Pause(guidePreferences.InteractiveWalkthrough);
            Call("RefreshTracker");
            await Pump();
            AssertStable(result, Measure(width, 672, "paused-walkthrough", false));
            guidePreferences.InteractiveWalkthrough = null;
            Call("RefreshTracker");
            await Pump();
            foreach (var tab in E<TabControl>("MainTabs").Items.OfType<TabItem>().Where(item => item.Visibility == Visibility.Visible && item != E<TabItem>("SessionTrackerTab")).ToArray())
            {
                tab.IsSelected = true;
                await Pump();
                Record(E<ScrollViewer>("MainScrollViewer").VerticalScrollBarVisibility != ScrollBarVisibility.Disabled,
                    $"{width}:other-page-scroll:{tab.Header}", "Other pages retain their own page-scroll behavior.");
                E<TabItem>("SessionTrackerTab").IsSelected = true;
                await Pump();
                var returned = Measure(width, 672, "return-from-" + tab.Header, false);
                AssertStable(result, returned);
                AssertFitness(returned);
                Record(E<ScrollViewer>("MainScrollViewer").VerticalScrollBarVisibility == ScrollBarVisibility.Disabled,
                    $"{width}:tracker-no-scroll-after:{tab.Header}", "Returning to the tracker restores the finite-height page.");
            }
        }
        foreach (var width in new[] { 1280, 1920 })
        {
            window.Width = width * 96d / GetDpiForWindow(handle);
            window.Height = 672 * 96d / GetDpiForWindow(handle);
            SetState("ready");
            var sidebar = E<ScrollViewer>("TrackerSidebar");
            var guide = Descendants<Expander>(window).First(item => item.Header?.ToString() == "Quick guide");
            guide.IsExpanded = false;
            sidebar.ScrollToBottom();
            await Pump();
            var before = Measure(width, 672, "visible-guide", false);
            var toggle = Descendants<ToggleButton>(guide).First();
            Record(InViewport(Box(toggle), before), Key(before, "guide-header-visible"), "Guide header is visible after scrolling only the bounded sidebar.");
            typeof(ToggleButton).GetMethod("OnClick", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(toggle, null);
            await Pump();
            AssertStable(before, Measure(width, 672, "visible-guide", true));
            sidebar.ScrollToBottom();
            await Pump();
            var expanded = Measure(width, 672, "visible-guide-bottom", true);
            AssertStable(before, expanded);
            AssertFitness(expanded);
            Render($"{width}x672-visible-guide-open.png");
            typeof(ToggleButton).GetMethod("OnClick", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(toggle, null);
            await Pump();
            AssertStable(before, Measure(width, 672, "visible-guide", false));
        }
        Record(Application.Current.Windows.OfType<MainWindow>().Count() == 1, "one-native-window-on-finish", "No second MainWindow opened during the checks.");
    }

    private static void SetState(string state)
    {
        var tracker = new SessionTracker(clock: () => At);
        if (state != "inactive") tracker.StartSession();
        if (state is not ("missing-baseline" or "inactive"))
        {
            foreach (var trackClass in Enum.GetValues<XpClass>())
            {
                var xp = Values[(int)trackClass];
                tracker.Observe(Read(trackClass, xp), ReadingSource.SnapshotHotkey, true);
                if (state is "gain" or "rejected" or "pending") tracker.Observe(Read(trackClass, xp + 1000), ReadingSource.CheckHotkey);
            }
        }
        if (state == "pending")
        {
            tracker.Observe(new(ScreenKind.EndScore, ReadStatus.Accepted, "QA end.", [],
                new([2, 3], false, [new(PlacementCue.Defeat, [2, 3], new(.3, .3, .4, .1), "DEFEAT")]), [], []), ReadingSource.AutomaticOcr);
            tracker.Observe(Read(XpClass.Assault, Values[0] + 6500, ScreenKind.EndProgression), ReadingSource.AutomaticOcr);
        }
        Field("tracker").SetValue(window, tracker);
        Field("resumePending").SetValue(window, state == "resume");
        Call("RefreshTracker");
        E<TabItem>("SessionTrackerTab").IsSelected = true;
        Call("SetReadFeedback", state != "rejected", state == "rejected"
            ? "Only completed rewards are visible. Open the next rewards page to show current XP and read again. Previous accepted XP remains unchanged."
            : "Layout test ready.");
    }

    private static Measurement Measure(int width, int height, string state, bool expanded)
    {
        GetWindowRect(handle, out var nativeWindow);
        GetClientRect(handle, out var nativeClient);
        var dpi = VisualTreeHelper.GetDpi(window!);
        var scroll = E<ScrollViewer>("MainScrollViewer");
        var cards = E<ItemsControl>("TrackerCards");
        var rows = new List<CardBounds>();
        for (var index = 0; index < cards.Items.Count; index++)
        {
            var presenter = (FrameworkElement)cards.ItemContainerGenerator.ContainerFromIndex(index);
            var card = Descendants<Border>(presenter).First();
            var buttons = Descendants<Button>(card).ToArray();
            rows.Add(new(index, Box(card), Box(buttons.Single(button => button.Content?.ToString() == "Manual XP")),
                Box(buttons.Single(button => button.Content?.ToString() == "Re-Establish Baseline"))));
        }
        var error = E<Border>("TrackerReadFeedback");
        var overlay = E<Border>("InactiveSessionOverlay");
        var measurement = new Measurement(width, height, state, expanded, GetDpiForSystem(), GetDpiForWindow(handle),
            dpi.DpiScaleX, dpi.DpiScaleY, new(nativeWindow.Left, nativeWindow.Top, nativeWindow.Right - nativeWindow.Left, nativeWindow.Bottom - nativeWindow.Top),
            new(0, 0, nativeClient.Right, nativeClient.Bottom), window!.ActualWidth, window.ActualHeight,
            scroll.ViewportWidth, scroll.ViewportHeight, scroll.HorizontalOffset, scroll.VerticalOffset,
            rows, Box(overlay), overlay.Visibility.ToString(), error.Visibility == Visibility.Visible ? Box(error) : null);
        measurements.Add(measurement);
        Console.WriteLine(JsonSerializer.Serialize(measurement));
        return measurement;
    }

    private static void AssertFitness(Measurement result)
    {
        Record(Math.Abs(result.Outer.Width - result.RequestedOuterWidth) <= 2 && Math.Abs(result.Outer.Height - result.RequestedOuterHeight) <= 2,
            Key(result, "outer-size"), $"Requested physical outer {result.RequestedOuterWidth}×{result.RequestedOuterHeight}, actual {result.Outer.Width}×{result.Outer.Height}, window DPI {result.WindowDpi}.");
        Record(result.OffsetX == 0 && result.OffsetY == 0, Key(result, "no-page-scroll"), "Page offsets remain zero before checking the complete tracker.");
        Record(result.Cards.Count == 6, Key(result, "six-cards"), "All six actual card visuals exist.");
        var texts = Descendants<TextBlock>(E<ItemsControl>("TrackerCards")).ToArray();
        var names = texts.Where(text => text.GetBindingExpression(TextBlock.TextProperty)?.ParentBinding.Path?.Path == "ClassName").ToArray();
        Record(names.Length == 6 && names.All(text => text.FontSize >= 16), Key(result, "class-name-size"), "All six class names use at least 16-point text.");
        foreach (var binding in new[] { "BaselineText", "CurrentText", "Detail" })
        {
            var values = texts.Where(text => text.GetBindingExpression(TextBlock.TextProperty)?.ParentBinding.Path?.Path == binding).ToArray();
            Record(values.Length == 6 && values.All(text => text.FontSize >= 12), Key(result, "xp-detail-size:" + binding), "All six XP details use at least 12-point text.");
        }
        Record(Descendants<TextBlock>(E<Border>("AnalyticsPanel")).Any(text => text.Text == "For Analytics ONLY"), Key(result, "analytics-only-title"), "Readiness is explicitly labeled For Analytics ONLY.");
        var sidebarBar = Descendants<ScrollBar>(E<ScrollViewer>("TrackerSidebar")).FirstOrDefault(bar => bar.Orientation == Orientation.Vertical && bar.Visibility == Visibility.Visible);
        if (sidebarBar is not null)
            Record(Box(E<Border>("AnalyticsPanel")).Right + 4 <= Box(sidebarBar).X, Key(result, "sidebar-scrollbar-clearance"), "The rounded panel remains at least four units clear of its scrollbar.");
        foreach (var card in result.Cards)
        {
            Record(InViewport(card.Manual, result), Key(result, $"manual-visible:{card.Index}"), $"Manual XP {card.Manual}, viewport {result.ViewportWidth:F2}×{result.ViewportHeight:F2} DIP.");
            Record(InViewport(card.Rebase, result), Key(result, $"rebase-visible:{card.Index}"), $"Rebase {card.Rebase}, viewport {result.ViewportWidth:F2}×{result.ViewportHeight:F2} DIP.");
            Record(Contained(card.Manual, card.Card), Key(result, $"manual-in-card:{card.Index}"), "The actual Manual XP button remains inside its own card.");
            Record(Contained(card.Rebase, card.Card), Key(result, $"rebase-in-card:{card.Index}"), "The actual rebase button remains inside its own card.");
        }
        if (result.State == "rejected")
        {
            Record(result.Error is { } error && InViewport(error, result), Key(result, "error-fully-visible"), $"Rejected-read banner: {result.Error}.");
            Record(result.Error is { } above && above.Bottom <= result.Cards.Min(card => card.Card.Y) + 1,
                Key(result, "error-above-cards"), "The rejected-read banner is entirely above the six cards.");
        }
        if (result.State == "pending")
        {
            Record(Box(E<Border>("PendingFinalPanel")).Bottom <= result.Cards.Min(card => card.Card.Y) + 1,
                Key(result, "pending-banner-above-cards"), "The end-of-match message and actions stay above the class cards.");
            var button = E<Button>("RecordFinalButton");
            Record(button.Visibility == Visibility.Visible && InViewport(Box(button), result), Key(result, "pending-record-visible"), $"Record Final XP bounds {Box(button)}.");
            Record(Contained(Box(button), Box(E<Border>("PendingFinalPanel"))), Key(result, "pending-record-in-panel"), "The pending-record action fits inside its panel.");
        }
    }

    private static bool TextFits(string text, Control control)
    {
        var measured = new FormattedText(text, CultureInfo.CurrentUICulture, control.FlowDirection,
            new Typeface(control.FontFamily, control.FontStyle, control.FontWeight, control.FontStretch),
            control.FontSize, control.Foreground, VisualTreeHelper.GetDpi(control).PixelsPerDip);
        return measured.WidthIncludingTrailingWhitespace <= control.ActualWidth - control.Padding.Left - control.Padding.Right
            - control.BorderThickness.Left - control.BorderThickness.Right + 1;
    }

    private static void AssertStable(Measurement baseline, Measurement changed)
    {
        foreach (var card in baseline.Cards)
            Record(Same(card.Card, changed.Cards[card.Index].Card), Key(changed, $"guide-card-stable:{card.Index}"),
                $"Guide {changed.GuideExpanded}: card before {card.Card}, after {changed.Cards[card.Index].Card}.");
        Record(Same(baseline.Overlay, changed.Overlay), Key(changed, "guide-overlay-stable"), $"Inactive overlay before {baseline.Overlay}, after {changed.Overlay}.");
        Record(Math.Abs(baseline.OffsetY - changed.OffsetY) <= .1 && Math.Abs(baseline.OffsetX - changed.OffsetX) <= .1,
            Key(changed, "guide-offset-stable"), $"Page offset before {baseline.OffsetY:F2}, after {changed.OffsetY:F2}.");
    }

    private static bool InViewport(Bounds box, Measurement result) => box.Width > 0 && box.Height > 0
        && box.X >= -1 && box.Y >= -1 && box.Right <= result.ViewportWidth + 1 && box.Bottom <= result.ViewportHeight + 1;
    private static bool Contained(Bounds inner, Bounds outer) => inner.X >= outer.X - 1 && inner.Y >= outer.Y - 1
        && inner.Right <= outer.Right + 1 && inner.Bottom <= outer.Bottom + 1;
    private static Bounds Box(FrameworkElement element)
    {
        var origin = element.TransformToAncestor(E<ScrollViewer>("MainScrollViewer")).Transform(new Point());
        return new(origin.X, origin.Y, element.ActualWidth, element.ActualHeight);
    }
    private static bool Same(Bounds before, Bounds after) => Math.Abs(before.X - after.X) <= .5 && Math.Abs(before.Y - after.Y) <= .5
        && Math.Abs(before.Width - after.Width) <= .5 && Math.Abs(before.Height - after.Height) <= .5;
    private static string Key(Measurement item, string check) => $"{item.RequestedOuterWidth}x{item.RequestedOuterHeight}:{item.State}:{(item.GuideExpanded ? "open" : "closed")}:{check}";
    private static void Record(bool passed, string id, string detail)
    { checks.Add(new(passed, id, detail)); if (!passed) Console.WriteLine("FAIL " + id + " " + detail); }
    private static async Task Pump() { await Dispatcher.Yield(DispatcherPriority.ApplicationIdle); await Task.Delay(35); window?.UpdateLayout(); }
    private static void Save() => File.WriteAllText(Path.Combine(output, "results.json"), JsonSerializer.Serialize(new
        { Version = typeof(MainWindow).Assembly.GetName().Version?.ToString(),
            AppSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(MainWindow).Assembly.Location))),
            RunStartedAt = At, ProfileDirectory = Path.GetRelativePath(output, profileDirectory),
            Passed = checks.Count(check => check.Passed),
            Failed = checks.Count(check => !check.Passed), Checks = checks, Measurements = measurements }, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
    private static void Render(string name)
    {
        var visual = (FrameworkElement)window!.Content;
        var dpi = VisualTreeHelper.GetDpi(visual);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(visual.ActualWidth * dpi.DpiScaleX),
            (int)Math.Ceiling(visual.ActualHeight * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(output, name)); encoder.Save(stream);
    }
    private static readonly long[] Values = [56486, 58891, 31175, 48059, 59653, 44702];
    private static readonly long[] Thresholds = [64500, 64500, 33750, 54750, 64500, 54750];
    private static readonly int[] Levels = [15, 15, 11, 14, 15, 14];
    private static ScreenReadResult Read(XpClass trackClass, long xp, ScreenKind screen = ScreenKind.Unlock)
        => new(screen, ReadStatus.Accepted, "Native layout fixture.",
            [new(trackClass, xp, Thresholds[(int)trackClass], Levels[(int)trackClass], Levels[(int)trackClass] + 1,
                screen, At, new(.4, .21, .1, .02), $"{xp}/{Thresholds[(int)trackClass]}XP", .99)], PlacementEvidence.None, [], []);
    private static T E<T>(string name) where T : FrameworkElement => (T)window!.FindName(name);
    private static FieldInfo Field(string name) => typeof(MainWindow).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!;
    private static object? Call(string name, params object?[] args) => typeof(MainWindow).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(window, args);
    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        { var child = VisualTreeHelper.GetChild(parent, index); if (child is T match) yield return match; foreach (var nested in Descendants<T>(child)) yield return nested; }
    }
    private sealed record Check(bool Passed, string Id, string Detail);
    private sealed record Bounds(double X, double Y, double Width, double Height)
    { public double Right => X + Width; public double Bottom => Y + Height; }
    private sealed record CardBounds(int Index, Bounds Card, Bounds Manual, Bounds Rebase);
    private sealed record Measurement(int RequestedOuterWidth, int RequestedOuterHeight, string State, bool GuideExpanded,
        uint SystemDpi, uint WindowDpi, double DpiScaleX, double DpiScaleY, Bounds Outer, Bounds Client,
        double WindowDipWidth, double WindowDipHeight, double ViewportWidth, double ViewportHeight,
        double OffsetX, double OffsetY, IReadOnlyList<CardBounds> Cards, Bounds Overlay, string OverlayVisibility, Bounds? Error);
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] private static extern bool SetProcessDpiAwarenessContext(nint context);
    [DllImport("user32.dll")] private static extern uint GetDpiForSystem();
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(nint handle);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(nint handle, out NativeRect rect);
    [DllImport("user32.dll")] private static extern bool GetClientRect(nint handle, out NativeRect rect);
}
