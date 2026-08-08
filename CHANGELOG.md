<!--
SPDX-FileCopyrightText: 2024-2026 Friedrich von Never <friedrich@fornever.me>

SPDX-License-Identifier: MIT
-->

Changelog
=========
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-08-08
### Added
- **Application state folder** ([#2](https://github.com/ForNeVeR/FVNever.AppDirs/issues/2)): a new `ApplicationDirectories` type with a `StateDirectory` method that resolves the per-OS application state directory as a [TruePath](https://github.com/ForNeVeR/TruePath) `AbsolutePath`.
  - non-roamable: Windows: `%LOCALAPPDATA%\[<Vendor>\]<App>\.state`; macOS: `<Application Support>/<BundleId>/.state`; Linux: `$XDG_STATE_HOME/<App>` or `$HOME/.local/state/<App>`;
  - roamable: Windows: `%APPDATA%\[<Vendor>\]<App>\.state` (the Roaming profile); macOS: `<Application Support>/<BundleId>/.roamableState`; Linux: `$XDG_CONFIG_HOME/<App>/.roamableState` or `$HOME/.config/<App>/.roamableState`.

[1.0.0]: https://github.com/ForNeVeR/FVNever.AppDirs/releases/tag/v1.0.0
[Unreleased]: https://github.com/ForNeVeR/FVNever.AppDirs/compare/v1.0.0...HEAD
