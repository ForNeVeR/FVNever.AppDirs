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

## Per-OS state mapping

`StateDirectory` resolves as follows:

| OS      | State directory                                                                                         |
|---------|---------------------------------------------------------------------------------------------------------|
| Linux   | `$XDG_STATE_HOME/<App>` when `XDG_STATE_HOME` is an absolute path, otherwise `$HOME/.local/state/<App>` |
| macOS   | `<Application Support>/<App>/.state`                                                                    |
| Windows | `<LocalApplicationData>\<App>\.state`                                                                   |

On macOS the `Application Support` base and on Windows the `LocalApplicationData` base are obtained through the .NET system API (`Environment.GetFolderPath(..., SpecialFolderOption.DoNotVerify)`) rather than assembled by hand, so sandboxed, containerized or relocated installations resolve correctly. Per the [XDG Base Directory Specification](https://specifications.freedesktop.org/basedir/latest/), a relative `XDG_STATE_HOME` is ignored and the `$HOME`-based default is used instead.

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
- on macOS / Windows, an empty, relative or otherwise unresolvable special-folder value (for example when a disabled compatibility mode prevents us from guessing the macOS package identifier) throws an `InvalidOperationException`.

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
