// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

using TruePath;

namespace FVNever.AppDirs;

/// <summary>
/// Pure, side-effect-free per-operating-system mapping logic for the application directories.
/// </summary>
/// <remarks>
/// The resolver only decides <em>which</em> base directory and environment variables drive the mapping. It performs no
/// filesystem I/O and never returns the current working directory: bases are obtained through
/// <see cref="ISystemEnvironment"/>, which fails fast when required environment data is missing or invalid.
/// </remarks>
internal static class ApplicationDirectoriesResolver
{
    /// <summary>Resolves the application state directory (a leaf directory).</summary>
    /// <param name="environment">The operating system and environment to map against.</param>
    /// <param name="identity">The application identity data driving the per-OS mapping.</param>
    /// <param name="roamable">
    /// When <see langword="false"/> (the default variant), resolves a machine-local, non-roaming location. When
    /// <see langword="true"/>, resolves a location intended to roam or sync between machines.
    /// </param>
    /// <remarks>
    /// <para>
    /// On Windows, the base special folder is <c>LocalApplicationData</c> for the non-roamable variant and
    /// <c>ApplicationData</c> (the Roaming profile) for the roamable variant; <see cref="ApplicationIdentity.VendorName"/>
    /// (when set) is inserted as an intermediate segment and the <c>.state</c> leaf is appended for both.
    /// On macOS, the Application Support segment is <see cref="ApplicationIdentity.MacOsBundleIdentifier"/> when set;
    /// otherwise, if <see cref="ApplicationIdentity.AllowCompatMode"/> is enabled, it is reconstructed as
    /// <c>&lt;Vendor&gt;.&lt;App&gt;</c> (or <c>&lt;App&gt;</c> without a vendor); otherwise resolution throws. The leaf
    /// segment is <c>.state</c> for the non-roamable variant and <c>.roamableState</c> for the roamable variant.
    /// On Linux, the non-roamable variant follows <c>XDG_STATE_HOME</c>, while the roamable variant follows
    /// <c>XDG_CONFIG_HOME</c>. Linux ignores the vendor and bundle identifier.
    /// </para>
    /// </remarks>
    public static AbsolutePath ResolveStateDirectory(
        ISystemEnvironment environment, ApplicationIdentity identity, bool roamable) =>
        environment.OperatingSystem switch
        {
            OperatingSystemKind.Linux => roamable
                ? ResolveLinuxRoamableState(environment, identity.AppName)
                : ResolveLinuxState(environment, identity.AppName),
            OperatingSystemKind.MacOs =>
                environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                    / ResolveMacOsBundleIdentifier(identity) / (roamable ? ".roamableState" : ".state"),
            OperatingSystemKind.Windows =>
                ResolveWindowsAppBase(
                    environment,
                    identity,
                    roamable ? Environment.SpecialFolder.ApplicationData : Environment.SpecialFolder.LocalApplicationData)
                / ".state",
            var other => throw new PlatformNotSupportedException($"Unsupported operating system: {other}.")
        };

    /// <summary>
    /// Windows application base: <c>&lt;baseFolder&gt;\&lt;Vendor&gt;\&lt;App&gt;</c> when a vendor is set,
    /// otherwise <c>&lt;baseFolder&gt;\&lt;App&gt;</c>. The <c>baseFolder</c> is <c>LocalApplicationData</c> for the
    /// non-roamable variant or <c>ApplicationData</c> (Roaming) for the roamable variant.
    /// </summary>
    private static AbsolutePath ResolveWindowsAppBase(
        ISystemEnvironment environment, ApplicationIdentity identity, Environment.SpecialFolder baseFolder)
    {
        var baseDir = environment.GetFolderPath(baseFolder);
        return identity.VendorName is { } vendor ? baseDir / vendor / identity.AppName : baseDir / identity.AppName;
    }

    /// <summary>
    /// The macOS bundle identifier segment: the explicit identifier when set; otherwise, in compatibility mode,
    /// <c>&lt;Vendor&gt;.&lt;App&gt;</c> (or <c>&lt;App&gt;</c> without a vendor); otherwise resolution throws.
    /// </summary>
    private static string ResolveMacOsBundleIdentifier(ApplicationIdentity identity) =>
        identity.MacOsBundleIdentifier
        ?? (identity.AllowCompatMode
            ? identity.VendorName is { } vendor ? $"{vendor}.{identity.AppName}" : identity.AppName
            : throw new InvalidOperationException(
                "Cannot determine the macOS bundle identifier: provide a macOS bundle identifier or enable compatibility mode."));

    /// <summary>
    /// Linux state directory per the XDG Base Directory specification: <c>$XDG_STATE_HOME/&lt;App&gt;</c> when
    /// <c>XDG_STATE_HOME</c> is set to an absolute path, otherwise <c>$HOME/.local/state/&lt;App&gt;</c>.
    /// </summary>
    private static AbsolutePath ResolveLinuxState(ISystemEnvironment environment, string applicationName)
    {
        var xdgStateHome = environment.GetEnvironmentVariable("XDG_STATE_HOME");

        // Per the XDG spec, a relative (or empty) XDG_STATE_HOME must be ignored, falling back to the default.
        if (!string.IsNullOrEmpty(xdgStateHome) && new LocalPath(xdgStateHome).IsAbsolute)
        {
            return new AbsolutePath(xdgStateHome) / applicationName;
        }

        return environment.HomeDirectory / ".local" / "state" / applicationName;
    }

    /// <summary>
    /// Linux roamable state directory. XDG has no native roaming concept, so the roamable variant is placed under the
    /// configuration base — the bucket users most commonly sync or back up: <c>$XDG_CONFIG_HOME/&lt;App&gt;/.roamableState</c>
    /// when <c>XDG_CONFIG_HOME</c> is set to an absolute path, otherwise <c>$HOME/.config/&lt;App&gt;/.roamableState</c>.
    /// </summary>
    private static AbsolutePath ResolveLinuxRoamableState(ISystemEnvironment environment, string applicationName)
    {
        var xdgConfigHome = environment.GetEnvironmentVariable("XDG_CONFIG_HOME");

        // Per the XDG spec, a relative (or empty) XDG_CONFIG_HOME must be ignored, falling back to the default.
        if (!string.IsNullOrEmpty(xdgConfigHome) && new LocalPath(xdgConfigHome).IsAbsolute)
        {
            return new AbsolutePath(xdgConfigHome) / applicationName / ".roamableState";
        }

        return environment.HomeDirectory / ".config" / applicationName / ".roamableState";
    }
}
