# NPC Bundler Backlog

## Near-term (0.9 / Beta)

- Built-in BSA archiving
- Preserve profile state on tab change (sort, filter, scroll position, etc.)
- "NoMO" support - plugin only
- Moar Mugshots
- Mugshot "redirects", e.g. if Hair Physics Version looks same as original
- Bug: ESP/BSA files remain locked until program exit
  - May be obsolete with Mutagen migration

## Medium-term (1.0 / Release)

- Windows 7 targeting
- Support for Outfit (DOFT/SOFT) and Body (WNAM) carryover
- NifTools integration (i.e. for texture scans)
- Advanced filters: override names, plugin/mod selections
- Better MO2 integration - read profiles, mod lists, detect disable mods, etc.
- "Easy Mode" - hide plugin/mod distinction unless there's a conflict
- Incremental merges - i.e. for new mod installs, or minor tweaks to profiles

## Long-term (2.0+)

- Advanced safety checks, e.g. comparison of NPC headparts to facegens
- Facegen live previews - replace labor-intensive mugshots
  - **OR:** tools for modders to auto-generate them for their mods
- Additional Bethesda games support
- Facegen creation, if that's even possible
  - Automating the CK is not; but it might be possible to "mix" facegen files
    by e.g. taking the head node from one and hair from another.

## Done

- Alpha prototype
- Convert from XeLibSharp to Mutagen
- Placeholder images for face mods without mugshots
- Build operations should output to app log
- Secondary log (logfile) in case of program crashes
  - ~~On crash detected, try to read and copy last bits of XEdit log~~
- Filter NPCs by Plugin, EditorID, etc. (type to search)
- Auto-save/auto-load profiles