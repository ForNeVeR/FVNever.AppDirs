// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

using TruePath;

namespace FVNever.AppDirs.Tests;

/// <summary>
/// A configurable <see cref="ISystemEnvironment"/> used to verify per-operating-system mappings from any host.
/// </summary>
internal sealed class FakeSystemEnvironment(OperatingSystemKind operatingSystem) : ISystemEnvironment
{
    private readonly Dictionary<string, string> _environmentVariables = new(StringComparer.Ordinal);
    private readonly Dictionary<Environment.SpecialFolder, AbsolutePath> _specialFolders = new();

    public OperatingSystemKind OperatingSystem { get; } = operatingSystem;

    /// <summary>The home directory to report; when <see langword="null"/>, <see cref="HomeDirectory"/> throws.</summary>
    public AbsolutePath? Home { get; init; }

    public AbsolutePath HomeDirectory =>
        Home ?? throw new InvalidOperationException("The fake home directory has not been configured.");

    public string? GetEnvironmentVariable(string name) =>
        _environmentVariables.GetValueOrDefault(name);

    public AbsolutePath GetFolderPath(Environment.SpecialFolder folder) =>
        _specialFolders.TryGetValue(folder, out var path)
            ? path
            : throw new InvalidOperationException($"The fake special folder '{folder}' has not been configured.");

    public FakeSystemEnvironment WithEnvironmentVariable(string name, string value)
    {
        _environmentVariables[name] = value;
        return this;
    }

    public FakeSystemEnvironment WithSpecialFolder(Environment.SpecialFolder folder, AbsolutePath path)
    {
        _specialFolders[folder] = path;
        return this;
    }
}
