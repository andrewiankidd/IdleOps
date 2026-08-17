namespace playbk.Execution;

/// <summary>
/// What a run's transport can actually do. An action declares the capabilities it
/// needs; a <see cref="DeviceProfile"/> grants a set. Static validation rejects a
/// runbook up front when a step needs something the profile can't provide (e.g. UI
/// Automation over an HDMI-capture + USB-HID link, where there is no software access
/// to the target at all).
/// </summary>
[Flags]
internal enum Capability
{
    None = 0,
    LocalProcess = 1 << 0, // launch/manage a process on the host running playbk (exec)
    Input = 1 << 1,        // synthetic key/mouse — SendInput locally, or HID reports off-box
    Vision = 1 << 2,       // OCR / template / pixel over a captured frame (window grab or capture card)
    WindowHandle = 1 << 3, // needs a real target HWND: window-title matching, background PostMessage delivery
    Uia = 1 << 4,          // UI Automation accessibility tree (software access to the target)
}

/// <summary>A named transport target and the capabilities it grants.</summary>
internal sealed record DeviceProfile(string Name, Capability Capabilities, string Description)
{
    public bool Grants(Capability needed) => (Capabilities & needed) == needed;

    /// <summary>Local desktop: SendInput + window capture + UI Automation. Everything on-box.</summary>
    public static readonly DeviceProfile Local = new(
        "local",
        Capability.LocalProcess | Capability.Input | Capability.Vision | Capability.WindowHandle | Capability.Uia,
        "local desktop (SendInput + window capture + UI Automation)");

    /// <summary>
    /// Off-box: drive another machine over a USB-HID cable (input) with an HDMI
    /// capture card (vision). No process control, no window handles, no UIA — the
    /// target is opaque, so it is vision-only.
    /// </summary>
    public static readonly DeviceProfile Offbox = new(
        "offbox",
        Capability.Input | Capability.Vision,
        "off-box HID + capture (drives another machine; vision-only, no UIA/window/process access)");

    public static IReadOnlyList<DeviceProfile> All => [Local, Offbox];

    /// <summary>Resolve a profile by name, or null if unknown (caller reports the error).</summary>
    public static DeviceProfile? Resolve(string name) =>
        All.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
}
