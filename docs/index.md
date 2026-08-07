---
_disableBreadcrumb: true
---

<!--
SPDX-FileCopyrightText: 2024-2026 Friedrich von Never <friedrich@fornever.me>

SPDX-License-Identifier: MIT
-->

FVNever.AppDirs
============
A .NET library providing XDG-like base-directory resolution for application config, data, cache, and state directories in a cross-platform way.

## Motivation

Many applications need to store files that outlive a single run — logs, command history, recent-files lists, window layout — but the _correct_ place for them is different on each operating system, and encoded in platform-specific conventions that are easy to get subtly wrong. **FVNever.AppDirs** encapsulates these conventions so that you never assemble platform paths by hand.

Different ecosystems have different conventions on where to store different kinds of data:
- Linux has [XDG Base Directory Specification][spec.xdg],
- Windows provides [Known Folders][spec.windows],
- macOS documents its [Standard Directories][spec.macos].

This library brings access to these standards from .NET, in a portable manner (so you can find a location for any supported kind of data for every supported operating system).

## Getting started

Construct an `ApplicationDirectories` instance with your application's name and read the directory you need:

```csharp
using FVNever.AppDirs;
using TruePath;

var dirs = new ApplicationDirectories("MyApp");

AbsolutePath state = dirs.StateDirectory;
Console.WriteLine(state); // e.g. C:\Users\me\AppData\Local\MyApp\.state on Windows
```

The constructor also accepts optional identity data that shapes the per-OS paths:

```csharp
var dirs = new ApplicationDirectories(
    "MyApp",
    vendorName: "Acme",
    macOSBundleIdentifier: "com.acme.MyApp",
    allowCompatMode: true);
```

- `vendorName` (optional): used as an intermediate path segment on Windows and to reconstruct the macOS bundle identifier in compatibility mode. It is ignored on Linux.
- `macOSBundleIdentifier` (optional): used verbatim as the macOS `Application Support` segment.
- `allowCompatMode` (optional): when no explicit `macOSBundleIdentifier` is supplied, permits reconstructing it as `<Vendor>.<App>` (or `<App>`); otherwise macOS resolution throws instead of guessing.

## Per-OS state mapping

`StateDirectory` resolves as follows:

| OS      | State directory                                                                                                          |
|---------|--------------------------------------------------------------------------------------------------------------------------|
| Linux   | `$XDG_STATE_HOME/<App>` when `XDG_STATE_HOME` is an absolute path, otherwise `$HOME/.local/state/<App>`                  |
| macOS   | `<Application Support>/<id>/.state`, where `<id>` is the bundle identifier (see below)                                   |
| Windows | `<LocalApplicationData>\<Vendor>\<App>\.state` when `VendorName` is set, otherwise `<LocalApplicationData>\<App>\.state` |

On macOS the `Application Support` base and on Windows the `LocalApplicationData` base are obtained through the .NET system API (`Environment.GetFolderPath(..., SpecialFolderOption.DoNotVerify)`) rather than assembled by hand, so sandboxed, containerized or relocated installations resolve correctly. Per the [XDG Base Directory Specification](https://specifications.freedesktop.org/basedir/latest/), a relative `XDG_STATE_HOME` is ignored and the `$HOME`-based default is used instead.

On **Windows**, when `VendorName` is set it becomes an intermediate segment (`<LocalApplicationData>\<Vendor>\<App>\.state`), grouping the application under the vendor folder; otherwise it is omitted. This is independent of `AllowCompatMode`.

On **macOS**, the `<id>` segment is resolved as follows:

- if `MacOSBundleIdentifier` is set, it is used verbatim;
- otherwise, when `AllowCompatMode` is enabled, the identifier is reconstructed as `<Vendor>.<App>` (or `<App>` when no vendor is given);
- otherwise resolution throws (see the fail-fast section below), because AppDirs never guesses the identifier when compatibility mode is disabled.

## Leaf directories vs. base directories

Some of the paths are differently mapped on different operating systems: say, the XDG specification provides three different non-intersecting paths,
- `$XDG_DATA_HOME`,
- `$XDG_CONFIG_HOME`,
- `$XDG_STATE_HOME`.

When mapping these on Windows, where we only have two base paths (`%APPDATA%` and `%LOCALAPPDATA%`), we inevitably will have to make some compromise. **AppDirs** makes a choice to store the data paths in a nested manner: for example, since Windows has no separate concept of the **state directory**, the application's state data will be stored in `%LOCALAPPDATA%\AppName\.state`.

Which leads to the following issue: whet if your application obtains the path to the `%LOCALAPPDATA%\AppName` and wants to store **its own** entry with the name `.state`? This would lead to a directory conflict and possible data corruption.

**AppDirs** resolves this conundrum by introducing two concepts: **base** and **leaf** directories.

Members whose name ends with `Directory` (such as `StateDirectory`) denote **leaf** directories: locations your application writes into directly. AppDirs guarantees that **no leaf directory contains another AppDirs-generated leaf directory on any operating system**, so different kinds of data never nest inside one another.

The intermediate **base** directories used to derive the leaves (for example the macOS `Application Support` base, or the Linux data base) are an implementation detail and are not exposed directly.

This means that your application has full control over any **leaf** directories, and can store whatever entry it wishes. If you go outside of the leaf directory, though, then you should be careful to not call your entries similarly to the entries used by the **AppDirs**.

Note that to be absolutely safe, you should never use entry names that start from dot `.` in any of the base directories. **AppDirs** gives you no guarantee that a new minor version of the **AppDirs** won't introduce a new dot-named directory and break your layout somehow. **AppDirs** guarantees, though, that any such change will be documented in the changelog. In addition, we guarantee the data dir location stability — they won't be changed without major version bump.

## Fail-fast behavior (never the current working directory)

Resolution is pure and performs no filesystem I/O, and it **never** falls back to the current working directory (unlike `Environment.GetFolderPath` .NET API). If a required base cannot be determined as a rooted absolute path, the corresponding member throws instead of trying to guess or resolve the folder. For example,

- on Linux, a missing `$HOME` (when `XDG_STATE_HOME` is also absent or relative) throws an `InvalidOperationException`.
- on macOS / Windows, an empty, relative or otherwise unresolvable special-folder value throws an `InvalidOperationException`.
- on macOS, a disabled compatibility mode combined with a missing `MacOSBundleIdentifier` prevents us from guessing the macOS package identifier, and therefore throws an `InvalidOperationException`.

```csharp
try
{
    var state = new ApplicationDirectories("MyApp").StateDirectory;
}
catch (InvalidOperationException ex)
{
    // The environment could not supply a valid absolute base directory.
    // AppDirs fails fast here rather than returning the current working directory.
    Console.Error.WriteLine(ex.Message);
}
```

## References

- [XDG Base Directory Specification][spec.xdg] (Linux)
- [Known Folders][spec.windows] (Windows)
- [macOS Standard Directories][spec.macos] (macOS)

[spec.macos]: https://developer.apple.com/library/archive/documentation/FileManagement/Conceptual/FileSystemProgrammingGuide/MacOSXDirectories/MacOSXDirectories.html
[spec.windows]: https://learn.microsoft.com/en-us/windows/win32/shell/known-folders
[spec.xdg]: https://specifications.freedesktop.org/basedir/latest/
