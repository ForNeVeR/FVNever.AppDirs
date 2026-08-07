// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

using System.Reflection;
using TruePath;

namespace FVNever.AppDirs.Tests;

public class StateDirectoryTests
{
    private const string AppName = "MyApp";

    /// <summary>Builds an absolute path that is valid on the current host, so TruePath never rejects it.</summary>
    private static AbsolutePath HostAbsolute(string name) =>
        OperatingSystem.IsWindows() ? new AbsolutePath($@"C:\{name}") : new AbsolutePath($"/{name}");

    // -------------------- Cross-OS mapping (via the fake) --------------------

    [Test]
    public async Task Linux_WithAbsoluteXdgStateHome_UsesIt()
    {
        var xdg = HostAbsolute("xdg-state");
        var env = new FakeSystemEnvironment(OperatingSystemKind.Linux)
        {
            Home = HostAbsolute("home")
        }.WithEnvironmentVariable("XDG_STATE_HOME", xdg.Value);

        var actual = new ApplicationDirectories(AppName, env).StateDirectory;

        await Assert.That(actual).IsEqualTo(xdg / AppName);
    }

    [Test]
    public async Task Linux_WithoutXdgStateHome_UsesHomeDefault()
    {
        var home = HostAbsolute("home");
        var env = new FakeSystemEnvironment(OperatingSystemKind.Linux) { Home = home };

        var actual = new ApplicationDirectories(AppName, env).StateDirectory;

        await Assert.That(actual).IsEqualTo(home / ".local" / "state" / AppName);
    }

    [Test]
    public async Task Linux_WithRelativeXdgStateHome_IsIgnored()
    {
        var home = HostAbsolute("home");
        var env = new FakeSystemEnvironment(OperatingSystemKind.Linux) { Home = home }
            .WithEnvironmentVariable("XDG_STATE_HOME", "relative/state");

        var actual = new ApplicationDirectories(AppName, env).StateDirectory;

        await Assert.That(actual).IsEqualTo(home / ".local" / "state" / AppName);
    }

    [Test]
    public async Task MacOs_UsesApplicationSupportBaseWithDotState()
    {
        var appSupport = HostAbsolute("Application Support");
        var env = new FakeSystemEnvironment(OperatingSystemKind.MacOs)
            .WithSpecialFolder(Environment.SpecialFolder.ApplicationData, appSupport);

        var actual = new ApplicationDirectories(AppName, env).StateDirectory;

        await Assert.That(actual).IsEqualTo(appSupport / AppName / ".state");
    }

    [Test]
    public async Task Windows_UsesLocalAppDataBaseWithDotState()
    {
        var localAppData = HostAbsolute("LocalAppData");
        var env = new FakeSystemEnvironment(OperatingSystemKind.Windows)
            .WithSpecialFolder(Environment.SpecialFolder.LocalApplicationData, localAppData);

        var actual = new ApplicationDirectories(AppName, env).StateDirectory;

        await Assert.That(actual).IsEqualTo(localAppData / AppName / ".state");
    }

    // -------------------- Application-name edge cases --------------------

    [Test]
    [Arguments("My App With Spaces")]
    [Arguments("Company.Product")]
    public async Task ApplicationName_PreservedAsSingleSegment(string appName)
    {
        var home = HostAbsolute("home");
        var env = new FakeSystemEnvironment(OperatingSystemKind.Linux) { Home = home };

        var actual = new ApplicationDirectories(appName, env).StateDirectory;

        await Assert.That(actual).IsEqualTo(home / ".local" / "state" / appName);
    }

    [Test]
    public async Task NullApplicationName_Throws()
    {
        var env = new FakeSystemEnvironment(OperatingSystemKind.Linux) { Home = HostAbsolute("home") };
        await Assert.That(() => new ApplicationDirectories(null!, env)).Throws<ArgumentNullException>();
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    public async Task EmptyOrWhitespaceApplicationName_Throws(string appName)
    {
        var env = new FakeSystemEnvironment(OperatingSystemKind.Linux) { Home = HostAbsolute("home") };
        await Assert.That(() => new ApplicationDirectories(appName, env)).Throws<ArgumentException>();
    }

    // -------------------- Fail-fast / never-CWD --------------------

    [Test]
    public async Task Linux_MissingHome_ThrowsAndNeverReturnsCwd()
    {
        // No home and no XDG_STATE_HOME configured: resolution must fail fast, not degrade to CWD.
        var env = new FakeSystemEnvironment(OperatingSystemKind.Linux);
        await Assert.That(() => new ApplicationDirectories(AppName, env).StateDirectory)
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task MacOs_UnresolvableApplicationSupport_Throws()
    {
        // Special folder not configured emulates an unresolvable value (e.g. compat mode disabled).
        var env = new FakeSystemEnvironment(OperatingSystemKind.MacOs);
        await Assert.That(() => new ApplicationDirectories(AppName, env).StateDirectory)
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Windows_UnresolvableLocalAppData_Throws()
    {
        var env = new FakeSystemEnvironment(OperatingSystemKind.Windows);
        await Assert.That(() => new ApplicationDirectories(AppName, env).StateDirectory)
            .Throws<InvalidOperationException>();
    }

    // -------------------- Base/leaf invariant --------------------

    [Test]
    public async Task LeafDirectories_DoNotContainEachOther()
    {
        // Convention: no leaf directory may be an ancestor of (or equal to) another leaf on any OS.
        foreach (var os in new[] { OperatingSystemKind.Linux, OperatingSystemKind.MacOs, OperatingSystemKind.Windows })
        {
            var dirs = new ApplicationDirectories(AppName, FullyConfiguredEnvironment(os));
            var leaves = EnumerateLeafDirectories(dirs);

            foreach (var a in leaves)
            foreach (var b in leaves)
            {
                if (ReferenceEquals(a.Property, b.Property)) continue;
                await Assert.That(a.Path.IsPrefixOf(b.Path))
                    .IsFalse();
            }
        }
    }

    // -------------------- Real-system integration tests (one per OS) --------------------

    [Test]
    public async Task RealSystem_Windows()
    {
        if (!OperatingSystem.IsWindows()) return;

        var state = new ApplicationDirectories(AppName).StateDirectory;
        var localAppData = new AbsolutePath(Environment.GetEnvironmentVariable("USERPROFILE")!) / "AppData" / "Local";

        await Assert.That(state).IsEqualTo(localAppData / AppName / ".state");
    }

    [Test]
    public async Task RealSystem_Linux()
    {
        if (!OperatingSystem.IsLinux()) return;

        var state = new ApplicationDirectories(AppName).StateDirectory;

        var xdg = Environment.GetEnvironmentVariable("XDG_STATE_HOME");
        AbsolutePath expected;
        if (!string.IsNullOrEmpty(xdg) && new LocalPath(xdg).IsAbsolute)
        {
            expected = new AbsolutePath(xdg) / AppName;
        }
        else
        {
            var home = new AbsolutePath(Environment.GetEnvironmentVariable("HOME")!);
            expected = home / ".local" / "state" / AppName;
        }

        await Assert.That(state).IsEqualTo(expected);
    }

    [Test]
    public async Task RealSystem_MacOs()
    {
        if (!OperatingSystem.IsMacOS()) return;

        var state = new ApplicationDirectories(AppName).StateDirectory;
        var appSupport = new AbsolutePath(Environment.GetEnvironmentVariable("HOME")!) / "Application Support";

        await Assert.That(state).IsEqualTo(appSupport / AppName / ".state");
        await Assert.That(state.Value.Contains("Library/Application Support", StringComparison.Ordinal)).IsTrue();
    }

    // -------------------- Helpers --------------------

    private static FakeSystemEnvironment FullyConfiguredEnvironment(OperatingSystemKind os) =>
        new FakeSystemEnvironment(os) { Home = HostAbsolute("home") }
            .WithSpecialFolder(Environment.SpecialFolder.ApplicationData, HostAbsolute("Application Support"))
            .WithSpecialFolder(Environment.SpecialFolder.LocalApplicationData, HostAbsolute("LocalAppData"));

    private static List<(PropertyInfo Property, AbsolutePath Path)> EnumerateLeafDirectories(ApplicationDirectories dirs) =>
        typeof(ApplicationDirectories)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.Name.EndsWith("Directory", StringComparison.Ordinal) && p.PropertyType == typeof(AbsolutePath))
            .Select(p => (p, (AbsolutePath)p.GetValue(dirs)!))
            .ToList();
}
