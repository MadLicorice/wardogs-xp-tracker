using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;

internal static class Program
{
    [STAThread] private static int Main(string[] args)
    {
        try
        {
            if (args.Length > 0 && args[0] == "--fixture") { CaptureFixture.Run(args[1..]); return 0; }
            if (args.Length > 0 && args[0] == "--probe") return CaptureProbe.Run(args[1..]);
            return RunAsync(args).GetAwaiter().GetResult();
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }

    private static async Task<int> RunAsync(string[] args)
    {
        var scenario = args.Length == 0 ? "all" : args[0];
        if (scenario is not ("all" or "quiet" or "persistent" or "guards"))
            throw new ArgumentException("Choose all, quiet, persistent, or guards.");
        var output = Path.Combine(Path.GetTempPath(), "WardogsXpTracker-CaptureChecks-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        var reports = new List<object>();
        foreach (var name in scenario == "all" ? new[] { "quiet", "persistent", "guards" } : new[] { scenario })
        {
            var state = Path.Combine(output, name + ".json");
            using var fixture = StartOwn("--fixture", state, name);
            try
            {
                for (var attempt = 0; attempt < 100 && !File.Exists(state) && !fixture.HasExited; attempt++) await Task.Delay(100);
                if (!File.Exists(state)) throw new InvalidOperationException("The owned fixture did not become ready.");
                await Task.Delay(500);
                using var probe = StartOwn("--probe", state, name);
                var stdout = probe.StandardOutput.ReadToEndAsync();
                var stderr = probe.StandardError.ReadToEndAsync();
                await Task.WhenAny(probe.WaitForExitAsync(), Task.Delay(25000));
                if (!probe.HasExited)
                { probe.Kill(entireProcessTree: true); throw new TimeoutException("The owned capture probe timed out."); }
                Console.Write(await stdout);
                if (probe.ExitCode != 0) throw new InvalidOperationException(await stderr);
            }
            finally
            {
                await File.WriteAllTextAsync(state + ".close", "close");
                await Task.WhenAny(fixture.WaitForExitAsync(), Task.Delay(5000));
                if (!fixture.HasExited) fixture.Kill(entireProcessTree: true);
            }
            using var samples = JsonDocument.Parse(await File.ReadAllTextAsync(state + ".samples.json"));
            var previous = false; var onsets = 0; var yellow = 0; var count = 0;
            foreach (var sample in samples.RootElement.EnumerateArray())
            {
                count++;
                var present = sample.GetProperty("Yellow").GetInt32() > 0;
                if (present && !previous) onsets++;
                if (present) yellow++;
                previous = present;
            }
            if (count == 0) throw new InvalidOperationException("The owned border sampler returned no observations.");
            if (name == "quiet" && onsets != 0) throw new InvalidOperationException("Quiet capture displayed a yellow outline.");
            if (name == "persistent" && onsets > 1) throw new InvalidOperationException("The capture outline flashed again between reads.");
            var report = new { Scenario = name, Samples = count, YellowSamples = yellow, BorderOnsets = onsets, Os = Environment.OSVersion.Version.ToString() };
            reports.Add(report);
            Console.WriteLine(JsonSerializer.Serialize(report));
        }
        await File.WriteAllTextAsync(Path.Combine(output, "results.json"), JsonSerializer.Serialize(reports, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"PASS native capture checks. Local reports: {output}");
        return 0;
    }

    private static Process StartOwn(params string[] args)
    {
        var executable = Environment.ProcessPath ?? throw new InvalidOperationException("The current process path is unavailable.");
        var info = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden, RedirectStandardOutput = true, RedirectStandardError = true };
        if (Path.GetFileNameWithoutExtension(executable).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
            info.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        foreach (var argument in args) info.ArgumentList.Add(argument);
        return Process.Start(info) ?? throw new InvalidOperationException("The owned test helper did not start.");
    }
}
