﻿# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Added
- Always show NPCs with available FaceGen Overrides, even if they have no override records.

### Fixed
- #101: Fix the FaceGen Override system in general - almost no part of it worked correctly.
  Standalone (no-plugin) overrides can now be double-clicked properly, overrides will be restored when relaunching the app,
  will not be overwritten by implicit (non-user-initiated) plugin syncs, checkboxes actually show on the correct tile, etc.
- Don't include mugshots referencing archives in disabled mods.
- Fix missing-plugin reset becoming broken again due to the loading process selecting an alternative plugin and the reset not actually
  changing it away from that value - however, it still needs to write to the profile and clear the "missing" flag.

## [0.9.1] - 2021-08-22
### Added
- Hide disabled Vortex mods from mugshots/profile - for parity with Mod Organizer. (Requires Vortex extension 0.1.4)

### Removed
- Remove "Only Face Overrides" filter from the UI since it no longer does anything. NPCs without face overrides are excluded at load.
- Remove most dewiggifier warnings. These now fall back to Worn Armor import and are not important. A lower-priority, generic
  informational message is produced instead.

### Changed
- Audio Template NPCs are filtered from the profile.
- Make imported Worn Armor/Addon races match actual usage by NPCs instead of trying to generate the list from playable races.
  Solves a few issues related to missing/invisible bodies for less-common overhaul races such as Elder.

### Fixed
- #98: Default mugshot paths (i.e. inside EasyNPC directory) no longer break mugshots.
- #99: Fix duplicate mods showing up in mugshots when running under Mod Organizer due to missing or deleted download metadata.
- Fix build failures due to "hard null references" in record data - references set to zero value instead of being unset.
- Fix game path (`-p` or ``--game-path`) parameter which was not working correctly.

## [0.9.0] - 2021-08-19
### Added
- #7: Import custom bodies (Worn Armors) from overhauls. Includes wigs that cannot be de-wiggified.
- #32: Preliminary support for Skyrim VR, and possibly other editions, through the `-g` or `--game` parameter.
  Also supports custom game paths with `-p` or `--game-path` parameter.
- #90: Recognize templated NPCs and handle accordingly. Depending on the scenario, this may block changes and display a warning in the UI,
  or exclude them from the profile entirely.

### Removed
- #64: No longer show warnings when NPC race is changed. This is unnecessary with full head-part checks.

### Changed
- #10: Read mod and profile metadata from Mod Organizer. Disabled mods will no longer show up, and mugshot synonyms are required less often.
- #49: Most patches for NPC overhauls are detected, and will not be chosen at first load or on profile reset.
- #64: Override all head parts in merged patch, which supports many if not most changes to NPC race or sex.
- #74: Faster builds with parallel file copying and parallel tasks in general.
- #76: Use entire mod list for locating dependent resources. Eliminates most in-game problems that were due to missing assets.

### Fixed
- #42: Make colors referenced by Head Parts standalone. Fixes another rare "unexpected masters" issue.
- #75: Properly reset filters when jumping to a master dependency from the build screen.
- #77: Fix scrolling in pre-build screens.

## [0.8.8] - 2021-07-24
### Added
- #17: Prominently display master dependencies before build, and provide a quick path to fixing them.

### Changed
- #71: Automatically exclude NPC races that don't use the facegen system (most non-human NPCs).

### Fixed
- #63: Fix the previously-nonfunctional corrupt BSA detection.
- #67, #68: Significantly improved handling of bad form ID references when loading, none should crash the loader anymore.

## [0.8.7] - 2021-07-22
### Changed
- #40: Add warning text and coloring around "trim" operation due to potential of profile corruption.
- #56: Pre-test plugins and mark as unreadable in loader instead of allowing them to crash the app on startup.
- #60: Exclude child NPCs due to severe incompatibility with child mods such as RS Children and The Kids Are Alright.
- #62: Show clearer warning when game data cannot be found.
- #64: Show build warnings when an overhaul changes an NPC's race.

### Fixed
- #63: Unreadable BSAs will emit a build warning instead of crashing the app.
- #65: Prevent profile corruption when a new mod is added that overhauls previously-ignored NPCs.
- #66: Fix some profile entries pointing to missing records failing to reset.

## [0.8.6] - 2021-07-18
### Fixed
- #36: Ignore non-race entries in Valid Races form list.
- #46: Fix another case-sensitivity bug for files in BSAs leading to build failures.
- #47: Use default when face tint record has null interpolation value.

## [0.8.5] - 2021-07-18
### Fixed
- #43: Handle additional texture path scenarios that were leading to missing textures.

## [0.8.4] - 2021-07-18
### Fixed
- #41: Use case-insensitive name comparisons in build checks (fix spurious "mismatch" warnings).
- #42: Ensure that Hair Color and Alternate Textures are made standalone.
- #43: Fix missing textures due to absolute/rooted texture paths in FaceGen files.

## [0.8.3] - 2021-07-17
### Fixed
- #34: Fix crash on startup due to missing mod names in Vortex manifest.
- #35: Fix crash on build due to case-sensitive mod name comparisons.

## [0.8.2] - 2021-07-17
### Fixed
- #31: Fix crash on build due to missing `BuildReportPath` default (MO2 only).

## [0.8.1] - 2021-07-17
### Fixed
- #28: Fix crash on load due to case-sensitive comparisons of archive and plugin names.
- #29: Support launching from Mod Organizer named instances.
- #30: Fix crash on startup due to JSON parse error from non-numeric Vortex mod IDs.

## [0.8.0] - 2021-07-11
### Added
- Support for Vortex Mod Manager.

### Changed
- Mod directory is auto-detected on first start.

### Fixed
- External hyperlinks now open the browser as expected.

## [0.3.0] - 2021-06-28
### Added
- #1: Smarter plugin selection screen which labels and prevents missing masters and other common loading issues.
- #2: Report missing plugins (i.e. targeted by the profile, but no longer in the load order) as warnings prior to build.
- #3: Mugshot synonyms (redirects) for differently-named mods.
- #5: New filters in Profile tab, including plugin selections.
- #14: Double-click on build warning to jump to profile row.
- #18: Option to reset only missing references in Maintenance tab.

### Changed
- #20: Ignore invalid NPC overrides (AKA "injected records") until a sane strategy for dealing with them can be developed.

## [0.2.1] - 2021-06-21
### Fixed
- #15: Fixed infinite-loop bug causing unrecoverable freeze while loading.

## [0.2.0] - 2021-06-20 [YANKED]
### Fixed
- App settings no longer reset with each new release.
- First-time profile significantly less likely to choose a visual overhaul as the default plugin.

## [0.1.2] - 2021-06-19
### Fixed
- Dark theme UI colors.

## [0.1.1] - 2021-06-19
### Fixed
- Fixed game detection on newer Steam installations.

## [0.1.0] - 2021-06-15
### Added
- Initial release with basic record-facegen sync. Profiles, build, settings, and high-level maintenance functions.

[Unreleased]: https://github.com/focustense/easymod/compare/v0.9.1...HEAD
[0.9.1]: https://github.com/focustense/easymod/compare/v0.9.0...v0.9.1
[0.9.0]: https://github.com/focustense/easymod/compare/v0.8.8...v0.9.0
[0.8.8]: https://github.com/focustense/easymod/compare/v0.8.7...v0.8.8
[0.8.7]: https://github.com/focustense/easymod/compare/v0.8.6...v0.8.7
[0.8.6]: https://github.com/focustense/easymod/compare/v0.8.5...v0.8.6
[0.8.5]: https://github.com/focustense/easymod/compare/v0.8.4...v0.8.5
[0.8.4]: https://github.com/focustense/easymod/compare/v0.8.3...v0.8.4
[0.8.3]: https://github.com/focustense/easymod/compare/v0.8.2...v0.8.3
[0.8.2]: https://github.com/focustense/easymod/compare/v0.8.1...v0.8.2
[0.8.1]: https://github.com/focustense/easymod/compare/v0.8.0...v0.8.1
[0.8.0]: https://github.com/focustense/easymod/compare/v0.3.0...v0.8.0
[0.3.0]: https://github.com/focustense/easymod/compare/v0.2.1...v0.3.0
[0.2.1]: https://github.com/focustense/easymod/compare/v0.2.0...v0.2.1
[0.2.0]: https://github.com/focustense/easymod/compare/v0.1.2...v0.2.0
[0.1.2]: https://github.com/focustense/easymod/compare/v0.1.1...v0.1.2
[0.1.1]: https://github.com/focustense/easymod/compare/v0.1.0...v0.1.1
[0.1.0]: https://github.com/focustense/easymod/tree/v0.1.0