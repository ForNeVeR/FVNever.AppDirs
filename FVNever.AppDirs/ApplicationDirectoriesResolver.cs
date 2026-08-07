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
    /// <param name="applicationName">The application name, used verbatim as a path segment.</param>
    public static AbsolutePath ResolveStateDirectory(ISystemEnvironment environment, string applicationName) =>
        environment.OperatingSystem switch
        {
            OperatingSystemKind.Linux => ResolveLinuxState(environment, applicationName),
            OperatingSystemKind.MacOs =>
                environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) / applicationName / ".state",
            OperatingSystemKind.Windows =>
                environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) / applicationName / ".state",
            var other => throw new PlatformNotSupportedException($"Unsupported operating system: {other}.")
        };

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
}
