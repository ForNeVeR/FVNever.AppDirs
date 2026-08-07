// SPDX-FileCopyrightText: 2026 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

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
    [Arguments(false, "XDG_STATE_HOME", null)]
    [Arguments(true, "XDG_CONFIG_HOME", ".roamableState")]
    public async Task Linux_WithAbsoluteXdgBase_UsesIt(bool roamable, string envVar, string? leaf)
    {
        var xdg = HostAbsolute("xdg-base");
        var env = new FakeSystemEnvironment(OperatingSystemKind.Linux)
        {
            Home = HostAbsolute("home")
        }.WithEnvironmentVariable(envVar, xdg.Value);

        var actual = new ApplicationDirectories(AppName, env).StateDirectory(roamable);

        var expected = xdg / AppName;
        if (leaf is not null) expected = expected / leaf;
        await Assert.That(actual).IsEqualTo(expected);
    }

    [Test]
    [Arguments(false, false)]
    [Arguments(false, true)]
    [Arguments(true, false)]
    [Arguments(true, true)]
    public async Task Linux_WithoutAbsoluteXdgBase_UsesHomeDefault(bool roamable, bool setRelativeXdg)
    {
        var home = HostAbsolute("home");
        var env = new FakeSystemEnvironment(OperatingSystemKind.Linux) { Home = home };

        // Per the XDG spec, a relative XDG_* value is ignored and the $HOME-based default is used.
        if (setRelativeXdg)
        {
            env = env.WithEnvironmentVariable(roamable ? "XDG_CONFIG_HOME" : "XDG_STATE_HOME", "relative/path");
        }

        var actual = new ApplicationDirectories(AppName, env).StateDirectory(roamable);

        var expected = roamable
            ? home / ".config" / AppName / ".roamableState"
            : home / ".local" / "state" / AppName;
        await Assert.That(actual).IsEqualTo(expected);
    }

    [Test]
    public async Task DefaultAndExplicitNonRoamable_AreEquivalent()
    {
        var home = HostAbsolute("home");
        var env = new FakeSystemEnvironment(OperatingSystemKind.Linux) { Home = home };
        var dirs = new ApplicationDirectories(AppName, env);

        await Assert.That(dirs.StateDirectory()).IsEqualTo(dirs.StateDirectory(roamable: false));
    }

    [Test]
    [Arguments(false, ".state")]
    [Arguments(true, ".roamableState")]
    public async Task MacOs_ExplicitBundleIdentifier_UsesLeafUnderApplicationSupport(bool roamable, string leaf)
    {
        var appSupport = HostAbsolute("Application Support");
        var env = new FakeSystemEnvironment(OperatingSystemKind.MacOs)
            .WithSpecialFolder(Environment.SpecialFolder.ApplicationData, appSupport);

        // An explicit bundle identifier wins regardless of the compatibility flag.
        var actual = new ApplicationDirectories(AppName, env, macOsBundleIdentifier: "com.acme.MyApp").StateDirectory(roamable);

        await Assert.That(actual).IsEqualTo(appSupport / "com.acme.MyApp" / leaf);
    }

    [Test]
    [Arguments(false, ".state")]
    [Arguments(true, ".roamableState")]
    public async Task MacOs_CompatModeWithVendor_ReconstructsIdentifier(bool roamable, string leaf)
    {
        var appSupport = HostAbsolute("Application Support");
        var env = new FakeSystemEnvironment(OperatingSystemKind.MacOs)
            .WithSpecialFolder(Environment.SpecialFolder.ApplicationData, appSupport);

        var actual = new ApplicationDirectories(AppName, env, vendorName: "Acme", allowCompatMode: true).StateDirectory(roamable);

        await Assert.That(actual).IsEqualTo(appSupport / "Acme.MyApp" / leaf);
    }

    [Test]
    [Arguments(false, ".state")]
    [Arguments(true, ".roamableState")]
    public async Task MacOs_CompatModeWithoutVendor_UsesAppName(bool roamable, string leaf)
    {
        var appSupport = HostAbsolute("Application Support");
        var env = new FakeSystemEnvironment(OperatingSystemKind.MacOs)
            .WithSpecialFolder(Environment.SpecialFolder.ApplicationData, appSupport);

        var actual = new ApplicationDirectories(AppName, env, allowCompatMode: true).StateDirectory(roamable);

        await Assert.That(actual).IsEqualTo(appSupport / AppName / leaf);
    }

    [Test]
    [Arguments(false, Environment.SpecialFolder.LocalApplicationData, null)]
    [Arguments(true, Environment.SpecialFolder.ApplicationData, null)]
    [Arguments(false, Environment.SpecialFolder.LocalApplicationData, "Acme")]
    [Arguments(true, Environment.SpecialFolder.ApplicationData, "Acme")]
    public async Task Windows_UsesCorrectBaseWithDotState(bool roamable, Environment.SpecialFolder baseFolder, string? vendor)
    {
        var baseDir = HostAbsolute("WindowsBase");
        var env = new FakeSystemEnvironment(OperatingSystemKind.Windows)
            .WithSpecialFolder(baseFolder, baseDir);

        var actual = new ApplicationDirectories(AppName, env, vendorName: vendor).StateDirectory(roamable);

        var expected = vendor is null
            ? baseDir / AppName / ".state"
            : baseDir / vendor / AppName / ".state";
        await Assert.That(actual).IsEqualTo(expected);
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Linux_IgnoresVendorAndBundleIdentifier(bool roamable)
    {
        var home = HostAbsolute("home");
        var env = new FakeSystemEnvironment(OperatingSystemKind.Linux) { Home = home };

        var actual = new ApplicationDirectories(
            AppName, env, vendorName: "Acme", macOsBundleIdentifier: "com.acme.MyApp", allowCompatMode: true).StateDirectory(roamable);

        var expected = roamable
            ? home / ".config" / AppName / ".roamableState"
            : home / ".local" / "state" / AppName;
        await Assert.That(actual).IsEqualTo(expected);
    }

    // -------------------- Application-name edge cases --------------------

    [Test]
    [Arguments("My App With Spaces")]
    [Arguments("Company.Product")]
    public async Task ApplicationName_PreservedAsSingleSegment(string appName)
    {
        var home = HostAbsolute("home");
        var env = new FakeSystemEnvironment(OperatingSystemKind.Linux) { Home = home };

        var actual = new ApplicationDirectories(appName, env).StateDirectory();

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

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    public async Task WhitespaceVendorName_Throws(string vendorName)
    {
        var env = new FakeSystemEnvironment(OperatingSystemKind.Linux) { Home = HostAbsolute("home") };
        await Assert.That(() => new ApplicationDirectories(AppName, env, vendorName: vendorName)).Throws<ArgumentException>();
    }

    [Test]
    [Arguments("")]
    [Arguments("   ")]
    public async Task WhitespaceMacOSBundleIdentifier_Throws(string bundleId)
    {
        var env = new FakeSystemEnvironment(OperatingSystemKind.Linux) { Home = HostAbsolute("home") };
        await Assert.That(() => new ApplicationDirectories(AppName, env, macOsBundleIdentifier: bundleId)).Throws<ArgumentException>();
    }

    // -------------------- Fail-fast / never-CWD --------------------

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Linux_MissingHome_ThrowsAndNeverReturnsCwd(bool roamable)
    {
        // No home and no XDG_STATE_HOME / XDG_CONFIG_HOME configured: resolution must fail fast, not degrade to CWD.
        var env = new FakeSystemEnvironment(OperatingSystemKind.Linux);
        await Assert.That(() => new ApplicationDirectories(AppName, env).StateDirectory(roamable))
            .Throws<InvalidOperationException>();
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task MacOs_UnresolvableApplicationSupport_Throws(bool roamable)
    {
        // Special folder not configured emulates an unresolvable value; compat mode enabled so the failure is the base.
        var env = new FakeSystemEnvironment(OperatingSystemKind.MacOs);
        await Assert.That(() => new ApplicationDirectories(AppName, env, allowCompatMode: true).StateDirectory(roamable))
            .Throws<InvalidOperationException>();
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task MacOs_NoBundleIdentifierAndCompatDisabled_Throws(bool roamable)
    {
        var appSupport = HostAbsolute("Application Support");
        var env = new FakeSystemEnvironment(OperatingSystemKind.MacOs)
            .WithSpecialFolder(Environment.SpecialFolder.ApplicationData, appSupport);

        // Compat mode disabled and no explicit identifier: fail fast rather than guessing.
        await Assert.That(() => new ApplicationDirectories(AppName, env).StateDirectory(roamable))
            .Throws<InvalidOperationException>();
    }

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Windows_UnresolvableBase_Throws(bool roamable)
    {
        var env = new FakeSystemEnvironment(OperatingSystemKind.Windows);
        await Assert.That(() => new ApplicationDirectories(AppName, env).StateDirectory(roamable))
            .Throws<InvalidOperationException>();
    }

    // -------------------- Base/leaf invariant --------------------

    [Test]
    public async Task LeafDirectories_DoNotContainEachOther()
    {
        // Convention: no leaf directory may be an ancestor of (or equal to) another leaf on any OS.
        foreach (var os in new[] { OperatingSystemKind.Linux, OperatingSystemKind.MacOs, OperatingSystemKind.Windows })
        {
            // Enable compat mode so macOS can resolve an identifier for the invariant check.
            var dirs = new ApplicationDirectories(AppName, FullyConfiguredEnvironment(os), allowCompatMode: true);
            var leaves = EnumerateLeafDirectories(dirs);

            for (var i = 0; i < leaves.Count; i++)
            for (var j = 0; j < leaves.Count; j++)
            {
                if (i == j) continue;
                await Assert.That(leaves[i].IsPrefixOf(leaves[j]))
                    .IsFalse();
            }
        }
    }

    // -------------------- Real-system integration tests (one per OS) --------------------

    [Test]
    public async Task RealSystem_Windows()
    {
        if (!OperatingSystem.IsWindows()) return;

        var state = new ApplicationDirectories(AppName).StateDirectory();
        var localAppData = new AbsolutePath(Environment.GetEnvironmentVariable("USERPROFILE")!) / "AppData" / "Local";

        await Assert.That(state).IsEqualTo(localAppData / AppName / ".state");
    }

    [Test]
    public async Task RealSystem_Linux()
    {
        if (!OperatingSystem.IsLinux()) return;

        var state = new ApplicationDirectories(AppName).StateDirectory();

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

        var state = new ApplicationDirectories(AppName, allowCompatMode: true).StateDirectory();
        var appSupport = new AbsolutePath(Environment.GetEnvironmentVariable("HOME")!) / "Library" / "Application Support";

        await Assert.That(state).IsEqualTo(appSupport / AppName / ".state");
        await Assert.That(state.Value.Contains("Library/Application Support", StringComparison.Ordinal)).IsTrue();
    }

    // -------------------- Helpers --------------------

    private static FakeSystemEnvironment FullyConfiguredEnvironment(OperatingSystemKind os) =>
        new FakeSystemEnvironment(os) { Home = HostAbsolute("home") }
            .WithSpecialFolder(Environment.SpecialFolder.ApplicationData, HostAbsolute("Application Support"))
            .WithSpecialFolder(Environment.SpecialFolder.LocalApplicationData, HostAbsolute("LocalAppData"));

    /// <summary>
    /// Evaluates every leaf directory the application exposes. <see cref="ApplicationDirectories.StateDirectory"/> is a
    /// method (not a property), so its roamable and non-roamable variants are enumerated explicitly.
    /// </summary>
    private static List<AbsolutePath> EnumerateLeafDirectories(ApplicationDirectories dirs) =>
    [
        dirs.StateDirectory(roamable: false),
        dirs.StateDirectory(roamable: true)
    ];
}
