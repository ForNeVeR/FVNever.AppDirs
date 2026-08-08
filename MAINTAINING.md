<!--
SPDX-FileCopyrightText: 2024-2026 Friedrich von Never <friedrich@fornever.me>

SPDX-License-Identifier: MIT
-->

Maintainer Guide
================

Publish a New Version
---------------------
1. Choose the new version according to the project's versioning scheme.
2. Update the project's status in the `README.md` file, if required.
3. Update the copyright statement in the `LICENSE.txt` file, if required.
4. Update the `<Copyright>` statement and `<PackageLicenseExpression>` field in the `Directory.Build.props`, if required.
5. Update the `<Version>` in `Directory.Build.props`.
6. Prepare a corresponding entry in the `CHANGELOG.md` file (usually by renaming the "Unreleased" section).
7. Merge the aforementioned changes via a pull request.
8. Push a tag in form of `v<VERSION>`, e.g. `v0.0.0`. GitHub Actions will do the rest (push a NuGet package).

NuGet Publishing Policy
-----------------------
This repository relies on [NuGet Trusted Publishing][docs.nuget-trusted-publishing] policy. In case you need to create it again, follow these steps:

1. Sign in to nuget.org.
2. Go to the [Trusted Publishing][nuget.trusted-publishing] section.
3. Create a new policy, filling it with details of the current GitHub repository.
4. Put your nuget.org username into the `NUGET_USER` variable on the [action secrets][github.secrets] section of GitHub settings.

[docs.nuget-trusted-publishing]: https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing
[github.secrets]: https://github.com/ForNeVeR/FVNever.AppDirs/settings/secrets/actions
[nuget.trusted-publishing]: https://www.nuget.org/account/trustedpublishing
