<!--
SPDX-FileCopyrightText: 2024-2026 Friedrich von Never <friedrich@fornever.me>

SPDX-License-Identifier: MIT
-->

FVNever.AppDirs [![Status Zero][status-zero]][andivionian-status-classifier] [![FVNever.AppDirs on nuget.org][nuget.badge]][nuget]
========
A .NET library providing XDG-like base-directory resolution for application config, data, cache, and state directories in a cross-platform way.

Motivation
----------
Many applications need to store files that outlive a single run — logs, command history, recent-files lists, window layout — but the _correct_ place for them is different on each operating system, and encoded in platform-specific conventions that are easy to get subtly wrong. **FVNever.AppDirs** encapsulates these conventions so that you never assemble platform paths by hand.

Different ecosystems have different conventions on where to store different kinds of data:
- Linux has [XDG Base Directory Specification][spec.xdg],
- Windows provides [Known Folders][spec.windows],
- macOS documents its [Standard Directories][spec.macos].

This library brings access to these standards from .NET, in a portable manner (so you can find a location for any supported kind of data for every supported operating system).

Usage
-----
```csharp
using FVNever.AppDirs;

var dirs = new ApplicationDirectories("MyApp");
AbsolutePath state = dirs.StateDirectory();
// Windows: %LOCALAPPDATA%\MyApp\.state
// macOS:   throws unless a bundle identifier or compatibility mode is supplied (see below)
// Linux:   $XDG_STATE_HOME/MyApp (or ~/.local/state/MyApp)

AbsolutePath roamable = dirs.StateDirectory(roamable: true);
// Windows: %APPDATA%\MyApp\.state (the Roaming profile)
// macOS:   throws unless a bundle identifier or compatibility mode is supplied (see below)
// Linux:   $XDG_CONFIG_HOME/MyApp/.roamableState (or ~/.config/MyApp/.roamableState)
```

`StateDirectory` is a method taking an optional `bool roamable = false`. The default (`roamable: false`) resolves a machine-local, non-roaming location; `roamable: true` resolves a location intended to roam or sync between machines (for example the Windows Roaming profile).

You can also supply optional identity data to shape the per-OS paths:

```csharp
var dirs = new ApplicationDirectories(
    "MyApp",
    vendorName: "Acme",
    macOsBundleIdentifier: "com.acme.MyApp",
    allowCompatMode: true);
AbsolutePath state = dirs.StateDirectory();
// Windows: %LOCALAPPDATA%\Acme\MyApp\.state       (vendorName is an intermediate segment)
// macOS:   ~/Library/Application Support/com.acme.MyApp/.state
```

- `vendorName` (optional): used as an intermediate path segment on Windows, and to reconstruct the macOS bundle identifier in compatibility mode.
- `macOsBundleIdentifier` (optional): used verbatim as the macOS Application Support segment.
- `allowCompatMode` (optional): when no explicit `macOsBundleIdentifier` is given, reconstructs it as `<Vendor>.<App>` (or `<App>`); otherwise macOS resolution throws instead of guessing.

All three inputs are ignored on Linux.

Both variants of `StateDirectory` return a **leaf** directory: a location your application writes into directly. AppDirs guarantees no leaf directory contains another AppDirs-generated leaf on any OS. See the [documentation site][docs] for larger examples and the full explanation of the leaf/base convention and the fail-fast behavior.

References
----------
The per-OS mappings follow the authoritative platform conventions:
- [XDG Base Directory Specification][spec.xdg] (Linux)
- [Known Folders][spec.windows] (Windows)
- [macOS Standard Directories][spec.macos] (macOS)

Documentation
-------------
- [Project Documentation Site (API Reference)][docs]
- [Changelog][docs.changelog]
- [Contributor Guide][docs.contributing]
- [Maintainer Guide][docs.maintaining]

License
-------
The project is distributed under the terms of [the MIT license][docs.license].

The license indication in the project's sources is compliant with the [REUSE specification v3.3][reuse.spec].

[andivionian-status-classifier]: https://andivionian.fornever.me/v1/#status-zero-
[docs.changelog]: CHANGELOG.md
[docs.contributing]: CONTRIBUTING.md
[docs.license]: LICENSE.txt
[docs.maintaining]: MAINTAINING.md
[docs]: https://ForNeVeR.github.io/FVNever.AppDirs
[nuget.badge]: https://img.shields.io/nuget/v/FVNever.AppDirs
[nuget]: https://www.nuget.org/packages/FVNever.AppDirs
[reuse.spec]: https://reuse.software/spec-3.3/
[reuse]: https://reuse.software/
[spec.macos]: https://developer.apple.com/library/archive/documentation/FileManagement/Conceptual/FileSystemProgrammingGuide/MacOSXDirectories/MacOSXDirectories.html
[spec.windows]: https://learn.microsoft.com/en-us/windows/win32/shell/known-folders
[spec.xdg]: https://specifications.freedesktop.org/basedir/latest/
[status-zero]: https://img.shields.io/badge/status-zero-lightgrey.svg
[truepath]: https://github.com/ForNeVeR/TruePath
