# NPC Bundler Backlog

## Near-term (0.9 / Beta)

- Auto-save/auto-load profiles
- Built-in BSA archiving
- Convert from XeLibSharp to Mutagen
- Build operations should output to app log
- Secondary log (logfile) in case of program crashes
  - On crash detected, try to read and copy last bits of XEdit log
- Preserve profile state on tab change (sort, filter, scroll position, etc.)
- "NoMO" support - plugin only
- Filter NPCs by Plugin, EditorID, etc. (type to search)
- Bug: ESP/BSA files remain locked until program exit

## Medium-term (1.0 / Release)

- Windows 7 targeting
- NifTools integration (i.e. for texture scans)
- Advanced filters: override names, plugin/mod selections
- Better MO2 integration - read profiles, mod lists, detect disable mods, etc.
- "Easy Mode" - hide plugin/mod distinction unless there's a conflict

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