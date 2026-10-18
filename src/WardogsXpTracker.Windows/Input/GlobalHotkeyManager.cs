using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace WardogsXpTracker.Windows.Input;

public enum HotkeyAction { Snapshot, Check }

public sealed record HotkeyBinding(Key Key, ModifierKeys Modifiers)
{
    public override string ToString() => Modifiers == ModifierKeys.None ? Key.ToString() : $"{Modifiers}+{Key}";
}

/// <summary>Registers system hotkeys without keyboard hooks or game input.</summary>
public sealed class GlobalHotkeyManager : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModNoRepeat = 0x4000;
    private readonly HwndSource source;
    private readonly Dictionary<HotkeyAction, (int Id, HotkeyBinding Binding)> registrations = [];
    private int nextId = 0x4100;
    private bool disposed;

    public event Action<HotkeyAction>? HotkeyPressed;

    public GlobalHotkeyManager(HwndSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        this.source = source;
        source.AddHook(OnWindowMessage);
    }

    public HotkeyBinding? GetBinding(HotkeyAction action) =>
        registrations.TryGetValue(action, out var entry) ? entry.Binding : null;

    public bool TryRegister(HotkeyAction action, Key key, ModifierKeys modifiers, out string? error)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        source.Dispatcher.VerifyAccess();
        error = null;
        if (!IsSupportedKey(key))
        {
            error = "Choose another key. This key cannot be a global hotkey.";
            return false;
        }
        var binding = new HotkeyBinding(key, modifiers);
        if (registrations.TryGetValue(action, out var previous) && previous.Binding == binding) return true;
        if (registrations.Any(p => p.Key != action && p.Value.Binding == binding))
        {
            error = "Snapshot and Check must use different hotkeys.";
            return false;
        }
        if (nextId > 0xBFFF)
        {
            error = "The hotkey limit was reached. Restart the app to change the binding.";
            return false;
        }
        int id = nextId++;
        uint nativeModifiers = (uint)modifiers | ModNoRepeat;
        if (!RegisterHotKey(source.Handle, id, nativeModifiers, (uint)KeyInterop.VirtualKeyFromKey(key)))
        {
            int code = Marshal.GetLastWin32Error();
            error = code == 1409
                ? $"{binding} is already in use. Choose another hotkey."
                : $"Windows could not register {binding}: {new Win32Exception(code).Message}";
            return false;
        }
        if (registrations.TryGetValue(action, out previous)) UnregisterHotKey(source.Handle, previous.Id);
        registrations[action] = (id, binding);
        return true;
    }

    /// <summary>Changes both shortcuts, including swaps. A failed change attempts to restore both previous bindings.</summary>
    public bool TryRegisterPair(HotkeyBinding snapshot, HotkeyBinding check, out string? error)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        source.Dispatcher.VerifyAccess();
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(check);
        error = null;
        if (snapshot == check)
        {
            error = "Snapshot and Check must use different hotkeys.";
            return false;
        }
        if (!IsSupportedKey(snapshot.Key) || !IsSupportedKey(check.Key))
        {
            error = "Choose another key. This key cannot be a global hotkey.";
            return false;
        }

        var original = new Dictionary<HotkeyAction, HotkeyBinding?>
        {
            [HotkeyAction.Snapshot] = GetBinding(HotkeyAction.Snapshot),
            [HotkeyAction.Check] = GetBinding(HotkeyAction.Check)
        };
        var desired = new Dictionary<HotkeyAction, HotkeyBinding>
        {
            [HotkeyAction.Snapshot] = snapshot,
            [HotkeyAction.Check] = check
        };
        var changed = desired.Keys.Where(action => original[action] != desired[action]).ToArray();
        if (changed.Length == 0) return true;

        // Reserve IDs for both the new bindings and a possible rollback before releasing a key.
        int requiredIds = changed.Length + changed.Count(action => original[action] is not null);
        if ((long)nextId + requiredIds - 1 > 0xBFFF)
        {
            error = "The hotkey limit was reached. Restart the app to change the binding.";
            return false;
        }

        // Release changed actions first so Snapshot and Check can exchange their keys.
        foreach (var action in changed)
        {
            if (!UnregisterAction(action, out var releaseError))
                return RestorePairAfterFailure(original, releaseError!, out error);
        }
        foreach (var action in changed)
        {
            var binding = desired[action];
            if (!TryRegister(action, binding.Key, binding.Modifiers, out var registrationError))
                return RestorePairAfterFailure(original, registrationError!, out error);
        }
        return true;
    }

    private bool RestorePairAfterFailure(IReadOnlyDictionary<HotkeyAction, HotkeyBinding?> original,
        string failure, out string? error)
    {
        var rollbackErrors = new List<string>();
        foreach (var pair in original)
        {
            if (GetBinding(pair.Key) == pair.Value) continue;
            if (!UnregisterAction(pair.Key, out var releaseError)) rollbackErrors.Add(releaseError!);
        }
        foreach (var pair in original)
        {
            if (GetBinding(pair.Key) == pair.Value || pair.Value is null) continue;
            if (GetBinding(pair.Key) is not null)
            {
                rollbackErrors.Add($"{pair.Key} still uses its changed shortcut.");
                continue;
            }
            if (!TryRegister(pair.Key, pair.Value.Key, pair.Value.Modifiers, out var restoreError))
                rollbackErrors.Add($"{pair.Key}: {restoreError}");
        }

        error = rollbackErrors.Count == 0
            ? $"{failure} Previous shortcut settings were restored."
            : $"{failure} Previous shortcuts could not be fully restored. {string.Join(" ", rollbackErrors)} " +
              $"Current Snapshot: {GetBinding(HotkeyAction.Snapshot)?.ToString() ?? "unregistered"}. " +
              $"Current Check: {GetBinding(HotkeyAction.Check)?.ToString() ?? "unregistered"}.";
        return false;
    }

    private bool UnregisterAction(HotkeyAction action, out string? error)
    {
        error = null;
        if (!registrations.TryGetValue(action, out var registration)) return true;
        if (!UnregisterHotKey(source.Handle, registration.Id))
        {
            int code = Marshal.GetLastWin32Error();
            // Windows already removed this registration. Keep GetBinding consistent with that result.
            if (code != 1419)
            {
                error = $"Windows could not release {action}: {new Win32Exception(code).Message}";
                return false;
            }
        }
        registrations.Remove(action);
        return true;
    }

    private static bool IsSupportedKey(Key key) =>
        key is not (Key.None or Key.System or Key.ImeProcessed or Key.DeadCharProcessed or Key.F12) &&
        KeyInterop.VirtualKeyFromKey(key) != 0;

    public void Dispose()
    {
        if (disposed) return;
        source.Dispatcher.VerifyAccess();
        disposed = true;
        foreach (var entry in registrations.Values) UnregisterHotKey(source.Handle, entry.Id);
        registrations.Clear();
        source.RemoveHook(OnWindowMessage);
        GC.SuppressFinalize(this);
    }

    private nint OnWindowMessage(nint window, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message != WmHotkey || disposed) return 0;
        foreach (var pair in registrations)
        {
            if (pair.Value.Id != (int)wParam) continue;
            handled = true;
            HotkeyPressed?.Invoke(pair.Key);
            break;
        }
        return 0;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(nint window, int id, uint modifiers, uint virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(nint window, int id);
}
