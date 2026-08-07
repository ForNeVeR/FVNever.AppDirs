<!--
SPDX-FileCopyrightText: 2024-2025 Friedrich von Never <friedrich@fornever.me>

SPDX-License-Identifier: MIT
-->

Changelog
=========
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased] (0.0.0)
### Added
- **Application state folder** ([#2](https://github.com/ForNeVeR/FVNever.AppDirs/issues/2)): a new `ApplicationDirectories` type with a `StateDirectory` property that resolves the per-OS application state directory as a [TruePath](https://github.com/ForNeVeR/TruePath) `AbsolutePath`.
  - Windows: `%LOCALAPPDATA%\<App>\.state`; macOS: `<Application Support>/<App>/.state`; Linux: `$XDG_STATE_HOME/<App>` or `$HOME/.local/state/<App>`.
  - The macOS and Windows bases are resolved via the .NET system API, and resolution fails fast with an exception rather than ever falling back to the current working directory.

[0.0.0]: https://github.com/ForNeVeR/FVNever.AppDirs/releases/tag/v0.0.0
[Unreleased]: https://github.com/ForNeVeR/FVNever.AppDirs/compare/v0.0.0...HEAD
