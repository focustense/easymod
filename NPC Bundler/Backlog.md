# NPC Bundler Backlog

## Near-term (0.9 / Beta)

- Built-in BSA archiving
- Moar Mugshots
- Make de-wiggification optional on build and/or settings screen
  - Try to give coherent explanation of pros and cons
- Mugshot "redirects", e.g. if Hair Physics Version looks same as original
- Bug: ESP/BSA files remain locked until program exit
  - May be obsolete with Mutagen migration
- More advanced operations
  - View full history

## Medium-term (1.0 / Release)

- Windows 7 targeting
- Support for Outfit (DOFT/SOFT) and Body (WNAM) carryover
- NifTools integration (i.e. for texture scans)
- Advanced filters: override names, plugin/mod selections
- Better MO2 integration - read profiles, mod lists, detect disable mods, etc.
- "Easy Mode" - hide plugin/mod distinction unless there's a conflict
- Incremental merges - i.e. for new mod installs, or minor tweaks to profiles
- Undo/redo

## Long-term (2.0+)

- Advanced safety checks, e.g. comparison of NPC headparts to facegens
- Facegen live previews - replace labor-intensive mugshots
  - **OR:** tools for modders to auto-generate them for their mods
- Additional Bethesda games support
- Facegen creation, if that's even possible
  - Automating the CK is not; but it might be possible to "mix" facegen files
    by e.g. taking the head node from one and hair from another.
  - De-wiggify project has proven that this *is* possible in limited scope.
- Hair-only mod support (VHR, ApachiiHair3DNPC, etc.)
  - Unsure how to "preview" this, but execution seems possible now with NIF edits
- Automatic hair physics replacement (when available)
  - i.e. given an original ESP and a physics version, replace all of the form
    links and facegen sub-meshes for some or all NPCs.
- General utilities not directly related to merge
  - Could make use of the code already here for some community-neglected areas
  - Check for missing textures (in plugins AND facegens)

## Punted

- "NoMO" support - plugin only

## Done

- Alpha prototype
- Convert from XeLibSharp to Mutagen
- Placeholder images for face mods without mugshots
- Build operations should output to app log
- Secondary log (logfile) in case of program crashes
  - ~~On crash detected, try to read and copy last bits of XEdit log~~
- Filter NPCs by Plugin, EditorID, etc. (type to search)
- Auto-save/auto-load profiles
- Preserve profile state on tab change (sort, filter, scroll position, etc.)
- De-wiggify
  - High Poly NPC Overhauls seem to use bald hair with wigs
  - May be able to "fix" this with mesh manipulation alone, i.e. no CK
- Advanced operations
  - Log file maintenance
  - Autosave maintenance (clear history)
  - Rescan defaults / evolve to load order