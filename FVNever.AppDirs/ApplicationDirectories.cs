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
    private readonly string _applicationName;
    private readonly ISystemEnvironment _environment;

    /// <summary>
    /// Creates an instance that resolves directories for the current operating system and environment.
    /// </summary>
    /// <param name="applicationName">
    /// The application name, used verbatim as a path segment. Must not be <see langword="null"/>, empty or
    /// whitespace.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="applicationName"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="applicationName"/> is <see langword="null"/>.</exception>
    public ApplicationDirectories(string applicationName)
        : this(applicationName, SystemEnvironment.Instance)
    {
    }

    /// <summary>
    /// Creates an instance that resolves directories against a substituted operating system and environment.
    /// Used by tests to verify cross-platform mappings from any host.
    /// </summary>
    internal ApplicationDirectories(string applicationName, ISystemEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(applicationName);
        if (string.IsNullOrWhiteSpace(applicationName))
        {
            throw new ArgumentException("The application name must not be empty or whitespace.", nameof(applicationName));
        }

        _applicationName = applicationName;
        _environment = environment;
    }

    /// <summary>
    /// The directory holding the application's <em>state</em>: data that persists between runs but is
    /// non-portable and non-essential (logs, history, recent files, window/layout state).
    /// </summary>
    /// <value>
    /// A leaf directory resolved per operating system:
    /// <list type="bullet">
    /// <item><description>
    /// Linux: <c>$XDG_STATE_HOME/&lt;App&gt;</c> when <c>XDG_STATE_HOME</c> is an absolute path, otherwise
    /// <c>$HOME/.local/state/&lt;App&gt;</c>.
    /// </description></item>
    /// <item><description>
    /// macOS: <c>&lt;ApplicationSupport&gt;/&lt;App&gt;/.state</c>, where the Application Support base is resolved
    /// via the .NET system API.
    /// </description></item>
    /// <item><description>
    /// Windows: <c>&lt;LocalApplicationData&gt;\&lt;App&gt;\.state</c>, where the Local Application Data base is
    /// resolved via the .NET system API.
    /// </description></item>
    /// </list>
    /// </value>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the required base directory cannot be determined as a rooted absolute path (for example a
    /// missing <c>$HOME</c> or an unresolvable special folder). The current working directory is never used.
    /// </exception>
    public AbsolutePath StateDirectory =>
        ApplicationDirectoriesResolver.ResolveStateDirectory(_environment, _applicationName);
}
