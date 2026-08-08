// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace FVNever.AppDirs;

/// <summary>
/// The bundle of identity data used to derive per-operating-system application paths.
/// </summary>
/// <param name="AppName">The application name, used verbatim as a path segment.</param>
/// <param name="VendorName">
/// An optional vendor name, used as an intermediate path segment on Windows and to reconstruct the macOS bundle
/// identifier in compatibility mode.
/// </param>
/// <param name="MacOsBundleIdentifier">
/// An optional explicit macOS bundle identifier, used as the Application Support path segment on macOS.
/// </param>
/// <param name="AllowCompatMode">
/// A flag permitting reconstruction of the macOS bundle identifier from the available data when none is supplied.
/// </param>
internal readonly record struct ApplicationIdentity(
    string AppName,
    string? VendorName,
    string? MacOsBundleIdentifier,
    bool AllowCompatMode);
