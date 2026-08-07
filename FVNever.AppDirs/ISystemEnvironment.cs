// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

using TruePath;

namespace FVNever.AppDirs;

/// <summary>
/// An abstraction over the current operating system and its environment.
/// </summary>
/// <remarks>
/// This lets the directory-resolution engine be driven by a substituted  operating system and environment (for
/// cross-platform unit testing) instead of always reading the real host. The production implementation is
/// <see cref="SystemEnvironment"/>.
/// </remarks>
internal interface ISystemEnvironment
{
    /// <summary>The operating-system family the resolver should map directories for.</summary>
    OperatingSystemKind OperatingSystem { get; }

    /// <summary>
    /// The current user's home directory, resolved as a rooted absolute path.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the home directory cannot be determined as an absolute path (for example when
    /// <c>$HOME</c> is unset on Unix). The resolver never falls back to the current working directory, unlike
    /// <see cref="Environment.GetFolderPath(System.Environment.SpecialFolder)"/>.
    /// </exception>
    AbsolutePath HomeDirectory { get; }

    /// <summary>Reads an environment variable, returning <see langword="null"/> when it is not set.</summary>
    /// <param name="name">The environment variable name.</param>
    string? GetEnvironmentVariable(string name);

    /// <summary>Resolves a well-known system folder to a rooted absolute path.</summary>
    /// <param name="folder">The special folder to resolve.</param>
    /// <remarks>
    /// Backed by <see cref="Environment.GetFolderPath(Environment.SpecialFolder, Environment.SpecialFolderOption)"/>
    /// with <see cref="Environment.SpecialFolderOption.DoNotVerify"/> in production, and fakeable in tests.
    /// Used by the macOS mapping (<see cref="Environment.SpecialFolder.ApplicationData"/>) and the Windows
    /// mapping (<see cref="Environment.SpecialFolder.LocalApplicationData"/>).
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the folder cannot be resolved to a rooted absolute path (for example an empty or relative
    /// value returned by the system API). The resolver never falls back to the current working directory.
    /// </exception>
    AbsolutePath GetFolderPath(Environment.SpecialFolder folder);
}
