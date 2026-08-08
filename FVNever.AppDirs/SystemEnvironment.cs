// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;
using TruePath;

namespace FVNever.AppDirs;

/// <summary>
/// The production <see cref="ISystemEnvironment"/> implementation backed by the real operating system.
/// </summary>
/// <remarks>
/// Every resolved value is validated to be an absolute path; otherwise an exception is thrown.
/// </remarks>
internal sealed class SystemEnvironment : ISystemEnvironment
{
    /// <summary>
    /// The shared, stateless instance backed by the real operating system.
    /// </summary>
    public static SystemEnvironment Instance { get; } = new();

    /// <inheritdoc />
    public OperatingSystemKind OperatingSystem
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return OperatingSystemKind.Windows;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return OperatingSystemKind.MacOs;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return OperatingSystemKind.Linux;

            throw new PlatformNotSupportedException(
                "FVNever.AppDirs supports Windows, macOS and Linux only; the current platform is not recognized.");
        }
    }

    /// <inheritdoc />
    public AbsolutePath HomeDirectory =>
        ToAbsolutePath(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify),
            "the home directory"
        );

    /// <inheritdoc />
    public string? GetEnvironmentVariable(string name) => Environment.GetEnvironmentVariable(name);

    /// <inheritdoc />
    public AbsolutePath GetFolderPath(Environment.SpecialFolder folder)
    {
        var value = Environment.GetFolderPath(folder, Environment.SpecialFolderOption.DoNotVerify);
        return ToAbsolutePath(value, $"the system folder '{folder}'");
    }

    /// <summary>
    /// Converts a raw path string into an <see cref="AbsolutePath"/>, failing with a clear exception
    /// when the value is missing, empty or not rooted.
    /// </summary>
    private static AbsolutePath ToAbsolutePath(string? value, string description)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException(
                $"Unable to determine {description}: the environment returned no value.");
        }

        if (!new LocalPath(value).IsAbsolute)
        {
            throw new InvalidOperationException(
                $"Unable to determine {description}: the value \"{value}\" is not an absolute path.");
        }

        // Constructing an AbsolutePath additionally asserts absoluteness, giving a fail-fast checkpoint.
        return new AbsolutePath(value);
    }
}
