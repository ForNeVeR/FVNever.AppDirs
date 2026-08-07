<!--
SPDX-FileCopyrightText: 2024-2026 Friedrich von Never <friedrich@fornever.me>

SPDX-License-Identifier: MIT
-->

Changelog
=========
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased] (0.0.0)
### Added
- **Application state folder** ([#2](https://github.com/ForNeVeR/FVNever.AppDirs/issues/2)): a new `ApplicationDirectories` type with a `StateDirectory` member that resolves the per-OS application state directory as a [TruePath](https://github.com/ForNeVeR/TruePath) `AbsolutePath`.
  - Windows: `%LOCALAPPDATA%\[<Vendor>\]<App>\.state`; macOS: `<Application Support>/<BundleId>/.state`; Linux: `$XDG_STATE_HOME/<App>` or `$HOME/.local/state/<App>`.
  - The macOS and Windows bases are resolved via the .NET system API, and resolution fails fast with an exception rather than ever falling back to the current working directory.
- **Roamable state directory**: `StateDirectory` now accepts an optional `bool roamable = false` parameter to request a location intended to roam or sync between machines.
  - Windows: `%APPDATA%\[<Vendor>\]<App>\.state` (the Roaming profile); macOS: `<Application Support>/<BundleId>/.roamableState`; Linux: `$XDG_CONFIG_HOME/<App>/.roamableState` or `$HOME/.config/<App>/.roamableState`.

[0.0.0]: https://github.com/ForNeVeR/FVNever.AppDirs/releases/tag/v0.0.0
[Unreleased]: https://github.com/ForNeVeR/FVNever.AppDirs/compare/v0.0.0...HEAD
