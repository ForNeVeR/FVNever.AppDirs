// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace FVNever.AppDirs;

/// <summary>
/// The operating-system families that <see cref="ApplicationDirectories"/> knows how to map
/// application directories for.
/// </summary>
internal enum OperatingSystemKind
{
    /// <summary>Microsoft Windows.</summary>
    Windows,

    /// <summary>Apple macOS.</summary>
    MacOs,

    /// <summary>Linux and other Unix-like systems following the XDG Base Directory conventions.</summary>
    Linux
}
