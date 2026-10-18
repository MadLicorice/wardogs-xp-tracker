using System.Diagnostics;
using System.IO;
using System.Text.Json;
using WardogsXpTracker.Windows.Capture;

internal static class CaptureProbe
{
    internal static int Run(string[] args)
    {
        try { return ExecuteAsync(args).GetAwaiter().GetResult(); }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }
    private static async Task<int> ExecuteAsync(string[] args)
    {
        using var input = JsonDocument.Parse(File.ReadAllText(args[0]));
        var target = new CaptureTarget((nint)input.RootElement.GetProperty("Handle").GetInt64(),
            input.RootElement.GetProperty("ProcessId").GetUInt32(), "FixtureHost", "Owned capture-border fixture");
        await using var service = new WindowCaptureService();
        if (args.Length > 1 && args[1] == "guards")
        {
            await GuardChecks(service, target, args[0]);
            await File.WriteAllTextAsync(args[0] + ".close", "close");
            return 0;
        }
        var watch = Stopwatch.StartNew();
        for (var index = 0; index < 6; index++)
        {
            var before = watch.ElapsedMilliseconds;
            var frame = args.Length > 1 && args[1] == "quiet" ? await CaptureOwnQuietAsync(service, target, args[0])
                : await service.CaptureAsync(target);
            if (frame.Width != 640 || frame.Height != 360) throw new InvalidOperationException("Wrong fixture dimensions.");
            var pixel = ((frame.Height / 2) * frame.Width + frame.Width / 2) * 4;
            if (Math.Abs(frame.Bgra32Pixels[pixel] - 100) > 2 || Math.Abs(frame.Bgra32Pixels[pixel + 1] - 65) > 2 || Math.Abs(frame.Bgra32Pixels[pixel + 2] - 30) > 2)
                throw new InvalidOperationException("Fixture pixels did not come from the expected owned window.");
            Console.WriteLine($"Read {index + 1}: {before}..{watch.ElapsedMilliseconds} ms, {frame.Width}x{frame.Height}");
            await Task.Delay(700);
        }
        await service.StopAsync();
        await Task.Delay(600);
        await File.WriteAllTextAsync(args[0] + ".close", "close");
        return 0;
    }

    private static async Task<CapturedFrame> CaptureOwnQuietAsync(WindowCaptureService service, CaptureTarget target, string state)
    {
        for (var attempt = 0; ; attempt++)
        {
            await FixtureCommandAsync(state, "focus-target");
            try { return await service.CaptureAsync(target, CaptureMode.QuietVisibleClient); }
            catch (WindowCaptureException exception) when (exception.Failure == CaptureFailure.NotForeground && attempt < 3)
            { await Task.Delay(40); }
        }
    }

    private static async Task FixtureCommandAsync(string state, string command, int settleMilliseconds = 0)
    {
        File.Delete(state + ".ack");
        await File.WriteAllTextAsync(state + ".command", command);
        for (var index = 0; index < 100; index++)
        {
            try
            {
                if (File.Exists(state + ".ack") && await File.ReadAllTextAsync(state + ".ack") == command)
                { if (settleMilliseconds > 0) await Task.Delay(settleMilliseconds); return; }
            }
            catch (IOException) { }
            await Task.Delay(30);
        }
        throw new InvalidOperationException($"Fixture command timed out: {command}");
    }

    private static async Task GuardChecks(WindowCaptureService service, CaptureTarget target, string state)
    {
        var checks = 0;
        void Check(bool condition, string text) { checks++; if (!condition) throw new InvalidOperationException(text); Console.WriteLine($"PASS {text}"); }
        Task Command(string command) => FixtureCommandAsync(state, command, 100);
        async Task Reject(CaptureFailure expected)
        {
            try { await service.CaptureAsync(target, CaptureMode.QuietVisibleClient); throw new InvalidOperationException($"Expected {expected} rejection."); }
            catch (WindowCaptureException exception) { Check(exception.Failure == expected, $"quiet rejects {expected} (actual {exception.Failure})"); }
        }
        await Command("focus-target");
        var visible = await CaptureOwnQuietAsync(service, target, state);
        Check(visible.Width == 640 && visible.Height == 360 && visible.Method.Contains("Visible client"), "quiet client dimensions and method provenance");
        var pixel = (180 * 640 + 320) * 4;
        Check(visible.Bgra32Pixels[pixel] == 100 && visible.Bgra32Pixels[pixel + 1] == 65 && visible.Bgra32Pixels[pixel + 2] == 30, "quiet pixels match owned target");
        await Command("focus-backplate"); await Reject(CaptureFailure.NotForeground);
        await Command("focus-target"); await Command("occlude"); await Reject(CaptureFailure.ObscuredWindow);
        await Command("uncover");
        await Command("minimize"); await Reject(CaptureFailure.MinimizedWindow);
        await Command("restore"); await Command("focus-target");
        await Command("move-outside"); await Reject(CaptureFailure.OutsideDesktop);
        await Command("move-inside"); await Command("focus-target");
        await Command("hide"); await Reject(CaptureFailure.HiddenWindow);
        await Command("restore"); await Command("focus-target");
        var sessionField = typeof(WindowCaptureService).GetField("activeSession", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        await service.CaptureAsync(target);
        var session = sessionField.GetValue(service);
        await service.CaptureAsync(target);
        Check(session is not null && ReferenceEquals(session, sessionField.GetValue(service)), "WGC session is reused across reads");
        await Command("static"); await Task.Delay(500);
        var staticFrame = await service.CaptureAsync(target);
        Check(staticFrame.Width == 640, "persistent WGC reads a static fixture");
        await Command("animate"); await Command("resize");
        CapturedFrame resized;
        try { resized = await service.CaptureAsync(target); }
        catch (WindowCaptureException exception) when (exception.Failure == CaptureFailure.WindowChanged)
        { Check(true, "resized texture is safely rejected before a retry"); await Task.Delay(200); resized = await service.CaptureAsync(target); }
        Check(resized.Width == 720 && resized.Height == 400, "WGC recovers correct target dimensions after resize");
        await service.StopAsync();
        Check(sessionField.GetValue(service) is null, "StopAsync releases WGC resources");
        await service.StopAsync();
        await service.CaptureAsync(target);
        Check(!ReferenceEquals(session, sessionField.GetValue(service)), "capture after StopAsync creates a new session");
        await Command("focus-target");
        await CaptureOwnQuietAsync(service, target, state);
        Check(sessionField.GetValue(service) is null, "explicit quiet switch releases the WGC session");
        await Command("close-target"); await Reject(CaptureFailure.MissingWindow);
        Console.WriteLine($"PASS {checks} capture safety and lifetime checks");
    }
}
