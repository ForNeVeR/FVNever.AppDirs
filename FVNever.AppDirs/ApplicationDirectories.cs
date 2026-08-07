// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

using TruePath;

namespace FVNever.AppDirs;

/// <summary>
/// Resolves the per-application directories (config, data, cache, state, …) for the current operating
/// system in an XDG-like, cross-platform way.
/// </summary>
/// <remarks>
/// <para>
/// Members whose name ends with <c>Directory</c> denote <em>leaf</em> directories: locations the
/// application is expected to write into directly. AppDirs guarantees that no leaf directory contains
/// another AppDirs-generated leaf directory on any operating system, so different kinds of data never
/// nest inside one another. Any intermediate <em>base</em> directories used to derive the leaves are an
/// implementation detail and are not exposed.
/// </para>
/// <para>
/// Resolution is pure and performs no filesystem I/O. It always yields a rooted absolute path and never
/// falls back to the current working directory: if a required base cannot be determined (for example a
/// missing <c>$HOME</c> on Linux, or an unresolvable macOS/Windows special folder), an exception is thrown.
/// </para>
/// </remarks>
public sealed class ApplicationDirectories
{
    private readonly ApplicationIdentity _identity;
    private readonly ISystemEnvironment _environment;

    /// <summary>
    /// Creates an instance that resolves directories for the current operating system and environment.
    /// </summary>
    /// <param name="applicationName">
    /// The application name, used verbatim as a path segment. Must not be <see langword="null"/>, empty or
    /// whitespace.
    /// </param>
    /// <param name="vendorName">
    /// An optional vendor name. When set, it is used as an intermediate path segment on Windows
    /// (<c>&lt;LocalApplicationData&gt;\&lt;Vendor&gt;\&lt;App&gt;\.state</c>) and to reconstruct the macOS bundle
    /// identifier in compatibility mode. Must not be empty or whitespace when non-<see langword="null"/>.
    /// </param>
    /// <param name="macOsBundleIdentifier">
    /// An optional explicit macOS bundle identifier. When set, it is used verbatim as the Application Support path
    /// segment on macOS. Must not be empty or whitespace when non-<see langword="null"/>.
    /// </param>
    /// <param name="allowCompatMode">
    /// When <see langword="true"/>, permits reconstructing the macOS bundle identifier from the available data
    /// (<c>&lt;Vendor&gt;.&lt;App&gt;</c> or <c>&lt;App&gt;</c>) when <paramref name="macOsBundleIdentifier"/> is not
    /// supplied. When <see langword="false"/>, macOS resolution throws instead of guessing the identifier.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="applicationName"/> is empty or whitespace, or
    /// when <paramref name="vendorName"/> / <paramref name="macOsBundleIdentifier"/> is non-<see langword="null"/> but
    /// empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="applicationName"/> is <see langword="null"/>.</exception>
    public ApplicationDirectories(
        string applicationName,
        string? vendorName = null,
        string? macOsBundleIdentifier = null,
        bool allowCompatMode = false)
        : this(applicationName, SystemEnvironment.Instance, vendorName, macOsBundleIdentifier, allowCompatMode)
    {
    }

    /// <summary>
    /// Creates an instance that resolves directories against a substituted operating system and environment.
    /// Used by tests to verify cross-platform mappings from any host.
    /// </summary>
    internal ApplicationDirectories(
        string applicationName,
        ISystemEnvironment environment,
        string? vendorName = null,
        string? macOsBundleIdentifier = null,
        bool allowCompatMode = false)
    {
        ArgumentNullException.ThrowIfNull(applicationName);
        if (string.IsNullOrWhiteSpace(applicationName))
        {
            throw new ArgumentException("The application name must not be empty or whitespace.", nameof(applicationName));
        }

        ValidateOptional(vendorName, nameof(vendorName));
        ValidateOptional(macOsBundleIdentifier, nameof(macOsBundleIdentifier));

        _identity = new ApplicationIdentity(applicationName, vendorName, macOsBundleIdentifier, allowCompatMode);
        _environment = environment;
    }

    /// <summary>
    /// Rejects a non-<see langword="null"/> optional string argument that is empty or whitespace; <see langword="null"/>
    /// remains a valid value.
    /// </summary>
    private static void ValidateOptional(string? value, string parameterName)
    {
        if (value is not null && string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"The {parameterName} must not be empty or whitespace when specified.", parameterName);
        }
    }

    /// <summary>
    /// The directory holding the application's <em>state</em>: data that persists between runs but is
    /// non-portable and non-essential (logs, history, recent files, window/layout state).
    /// </summary>
    /// <param name="roamable">
    /// Selects the state variant. When <see langword="false"/> (the default), resolves a machine-local, non-roaming
    /// location. When <see langword="true"/>, resolves a location intended to roam or sync between machines (for
    /// example the Windows Roaming profile).
    /// </param>
    /// <returns>
    /// A leaf directory resolved per operating system and variant:
    /// <list type="bullet">
    /// <item><description>
    /// Linux, non-roamable: <c>$XDG_STATE_HOME/&lt;App&gt;</c> when <c>XDG_STATE_HOME</c> is an absolute path,
    /// otherwise <c>$HOME/.local/state/&lt;App&gt;</c>.
    /// </description></item>
    /// <item><description>
    /// Linux, roamable: <c>$XDG_CONFIG_HOME/&lt;App&gt;/.roamableState</c> when <c>XDG_CONFIG_HOME</c> is an absolute
    /// path, otherwise <c>$HOME/.config/&lt;App&gt;/.roamableState</c>. XDG has no native roaming concept, so the
    /// configuration base — the bucket users most commonly sync or back up — is used.
    /// </description></item>
    /// <item><description>
    /// macOS, non-roamable: <c>&lt;ApplicationSupport&gt;/&lt;App&gt;/.state</c>; roamable:
    /// <c>&lt;ApplicationSupport&gt;/&lt;App&gt;/.roamableState</c>, where the Application Support base is resolved
    /// via the .NET system API.
    /// </description></item>
    /// <item><description>
    /// Windows, non-roamable: <c>&lt;LocalApplicationData&gt;\&lt;App&gt;\.state</c>; roamable:
    /// <c>&lt;ApplicationData&gt;\&lt;App&gt;\.state</c> (the Roaming profile). The vendor segment (when set) is
    /// inserted for both variants, and the bases are resolved via the .NET system API.
    /// </description></item>
    /// </list>
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the required base directory cannot be determined as a rooted absolute path (for example a
    /// missing <c>$HOME</c> or an unresolvable special folder). The current working directory is never used.
    /// </exception>
    public AbsolutePath StateDirectory(bool roamable = false) =>
        ApplicationDirectoriesResolver.ResolveStateDirectory(_environment, _identity, roamable);
}
