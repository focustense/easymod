# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.9.6] - 2022-11-10
### Added
- #80: New "Provided in" filter in profile view, shows all NPCs which *can* be affected by a given mod, even if they are currently pointing to different mods.
- #139: Able to scroll long mugshot rows using shift + mouse wheel.
- Output mod directory now contains a `build_info.json` which includes a list of any files that failed to copy or analyze, for help with troubleshooting missing meshes/textures and other in-game issues.

### Fixed
- #131: App no longer crashes silently when there are no build alerts to show.
- #133: Beast transformations (werewolf, vampire lord, etc.) should now work with all modded NPCs.
  - All Worn Armors, whether vanilla or modded, are patched in order to match the specific NPC's race and automatically include beast addons, even if the original mod left them out.
- #135: Eliminate `WindowChrome` related crashes associated Logitech SetPoint software.
- #164: All build tasks honor dewiggifier setting; blackface will no longer occur with mods such as High Poly NPC Overhaul 2.0 when wig conversion is disabled.
- #166: Avoid a stack overflow crash when launched with mods containing circular references.

### Changed
- #164: Wig conversion is now disabled by default, since Worn Armors (including wigs) are fully supported.
  - There is not much of an advantage to dewiggifying anymore; it makes the build take longer, and creates a minor risk of in-game issues.
- #165: Throttle texture path extraction and add a per-file timeout.
  - This should help many (not necessarily all) users who are experiencing long wait times during the "Extract Texture Paths" part of the build, and make the progress reporting more accurate for all users.
  - Timeout can be configured in the Output section of the build screen. Default, and recommended, is 30 seconds per file. Any files that time out will be reported in the new `build_info.json` for potential follow-up.

## [0.9.5] - 2021-10-09
### Added
- Brand-new build screen. Details on the [wiki page](https://github.com/focustense/easymod/wiki/EasyNPC-%E2%80%90-Build). Main features:
  - Single-click builds - no more having to click through multiple screens.
  - Lots more statistics about the pending build, and some predictive info such as file sizes.
  - New "NPCs" report showing both the NPCs that are _and aren't_ included in the build.
  - Improved UI for Master Dependencies, easier to read and includes a category for each.
  - Missing-assets check to warn about files that can't be found and won't be copied.
  - All checks done in real-time - build stats automatically update as settings and profile are changed.
  - Option to disable BSA creation (i.e. loose files only).

### Changed
- #124: Ensure NPC head parts are flagged as non-playable so that they don't crash the race menu in game.
- #127: Show a useful error when EasyNPC is started with invalid command-line options.
- #121: [Vortex Extension] Obtain correct game data path from Vortex.

### Fixed
- #95: Injected records from mods such as Interesting NPCs Visual Overhaul are now merged properly.
- #118: Fixed single-template detection logic that was accidentally excluding some NPCs from the profile.
- #119: Patch the Valid Races form lists copied from overhauls to prevent unexpected master dependencies.
- #122: Double-clicking on vanilla mugshot now updates the face selection.
- #123: Read file priorities from Vortex so that Post-Build Report shows correct status.
- #126: Skip and report broken plugins instead of "crashing" on startup.
- #128: [Vortex Extension] Use correct substitution for `USERDATA` token.

## [0.9.4] - 2021-09-21
### Added
- **Post-Build Report**: A major new feature/app mode designed to be run on the _final_ mod order, just before launching the game. Features include:
  - Checks for the integrity of the EasyNPC mod itself, including merge plugin, dummy plugins and archives.
  - Facegen/facetint consistency checks on all NPCs managed by EasyNPC. (NPCs _not_ customized by EasyNPC are ignored.)
  - An automated workaround to **extract conflicting files** from the EasyNPC archives in order to resolve loose-file conflicts.
    - This workaround is primarily intended for scenarios where the conflicts cannot be eliminated by disabling the conflicting mod - especially mods with a wide scope, such as EEO or BUVARP, or some of the conflict-resolution patches included in popular mod guides and Wabbajack lists.
  - To run the Post-Build Report, add the `-z` or `--post-build` command-line option to your normal options.
    - Vortex users will see a new "EasyNPC Post-Build" action next to the EasyNPC Launcher after upgrading their extension.
    - Mod Organizer users should configure this as a new executable for convenience.
    - Make sure to run this mode on your _final game profile_ - i.e. the one with the EasyNPC mod active and other overhauls disabled, _not_ the profile you'd normally use to run EasyNPC to build a new merge.

### Changed
- [Vortex Extension] Game ID is obtained from the current profile, instead of being hardcoded to `skyrimse`. Note that it is still required to set the `-g` or `--game` command-line option in order to use EasyNPC with a game other than Skyrim Special Edition - this change only ensures that the correct Vortex mod list and staging directory are used.
- [Vortex Extension] Don't show EasyNPC actions in the mod toolbar for unsupported games.

### Fixed
- Multiple marks/scars and other additional/extra head parts are now correctly carried over in the merged plugin. Previously, only one was being copied, which may have resulted in a few rare blackface issues.
- Fixed reversed archive ordering in `GameFileProvider`, which caused the lowest-priority mod to be used for shared assets instead of the highest-priority mod. Facegen data was never affected, only shared textures, morphs, etc.
- [Vortex Extension] Additional command-line parameters configured in the dashboard are now passed to the app.
- [Vortex Extension] Correctly handle path substitutions like `{userdata}` and `{game}` in the staging directory path.

## [0.9.3] - 2021-09-06
### Added
- #107: Essential NPC references are now checked on startup, and errors reported. Prevents late-manifesting crashes during build due to being unable to import some dependencies.

### Changed
- #102: Don't check for named instances of Mod Organizer when started under a "locked portable" instance (e.g. Wabbajack builds).
- #114: Obtain game path from Mod Organizer configuration. Makes EasyNPC compatible with Serenity 2 and any other instances with a "stock game" copy. Frequently eliminates the need to configure a `-p`/`--game-path` option.
- Prioritize the mod manager's mod directory by default, only using the manually-configured directory as a fallback. Can be disabled in the app settings to restore old behavior of always using the same mod directory.
- Build pipeline now logs every item per task in debug mode, to help with identifying obscure mod-specific errors.

### Fixed
- #100: Updated icon and highlight colors under Windows dark theme to improve visibility/legibility.
- #103: Synchronize profile writes to prevent corruption when importing a saved profile.
- #105: Ensure output directory exists when saving patch, in case it wasn't created by previous steps.
- #113: Fix plugin customizations being ignored due to premature activation of Mutagen environment.
- Sort master references when saving merge plugin. Fixes the semi-infamous "Finding Helgi" infinite-load bug.

## [0.9.2] - 2021-08-27
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

[Unreleased]: https://github.com/focustense/easymod/compare/v0.9.6...HEAD
[0.9.6]: https://github.com/focustense/easymod/compare/v0.9.5...v0.9.6
[0.9.5]: https://github.com/focustense/easymod/compare/v0.9.4...v0.9.5
[0.9.4]: https://github.com/focustense/easymod/compare/v0.9.3...v0.9.4
[0.9.3]: https://github.com/focustense/easymod/compare/v0.9.2...v0.9.3
[0.9.2]: https://github.com/focustense/easymod/compare/v0.9.1...v0.9.2
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
