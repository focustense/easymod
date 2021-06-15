# EasyNPC

Painlessly mix, merge, and resolve conflicts and compatibility issues for all of your NPC overhauls.

- [Introduction](#Introduction)
- [Features](#Features)
- [Getting Started](#Getting-Started)
- [Reporting Issues](#Reporting-Issues)

## Introduction

Bethesda games - and in particular, Skyrim Special Edition, which this app is currently targeting - have extremely active modding communities and exceptionally high-quality NPC overhauls. But the modding system is fragile, even with the excellent [Mod Organizer](https://github.com/ModOrganizer2/modorganizer). Here are just a few of the problems facing the novice to intermediate mod user:

- [Black face](https://forums.nexusmods.com/index.php?/topic/5219960-weird-skin-colourhead-mismatching/), AKA _dark face_, _gray face_, _face discoloration_, and so on, which happens when the attributes describing a face (known as the _winning override_) has major inconsistencies with the pre-built 3D model for that face (known as the _facegen_ or _face mesh_).

- NPC overhauls that re-introduce bugs, such as those solved by [USSEP](https://www.nexusmods.com/skyrimspecialedition/mods/266/), or break unrelated mods, such as [AI Overhaul](https://www.nexusmods.com/skyrimspecialedition/mods/21654). This is a direct result of the game's _Record_ or _Major Record_ system, which provides mod creators with an "all-or-nothing" choice: they can either override everything about an NPC, including attributes totally unrelated to their appearance, or simply leave that NPC alone. This results in a theoretical combinatorial explosion of compatibility patches for every visual overhaul and every other kind of mod or _combination of mods_ that touch NPCs, and places an unfair burden on mod creators to provide such patches.

- Occasional game crashes, from those players unlucky or inexperienced enough to end up with a particularly severe conflict, especially if mods are changed in-game, and especially if any use scripts. This is not particularly unique to NPC appearance mods, except that most players - and mod creators - rationally believe that those mods should be safer to experiment with.

- Limited options for players who want to use these mods but don't know the ins and outs of [xEdit](https://github.com/TES5Edit/TES5Edit) and the nuances of the facegen system. Even if you get everything right, with zero bugs or conflicts, the best you can achieve by simply reordering mods and patches - i.e. without directly editing the mods or creating custom patches - is to set up mod-level priorities: **all** of the NPC appearances from mod X will take precedence over **any** appearances from mod Y, which wins over mod Z, and so on. There is no easy way for a player to say: "I like a few of the NPCs in this mod, but would rather not use all of them". This community behavior has also led many mod creators to try to avoid overlap with other mods, because doing _too many_ NPCs can actually _hurt_ the mod's adoption if the player community is worried about conflicts.

- Body conflicts with older NPC overhauls, especially for [BodySlide](https://github.com/ousnius/BodySlide-and-Outfit-Studio) users, often resulting in clipping, seams, physics/BBP inconsistencies, and assorted other nuisances.

Easy NPC aims to make all of these problems a thing of the past. More importantly, its ultimate goal is to make this level of customization accessible to novice and intermediate modders - who have a basic working knowledge of how mods and load orders work, but don't have the time or inclination to start tearing them open in xEdit and Creation Kit to find and patch every conflict - while still providing all of the detailed information and manual-override capability that advanced users would expect. This is achieved by the time-honored software traditions of:

- Picking good defaults
- Providing immediate, visual feedback
- Warning you when something doesn't look right
- ...but ultimately leaving the final decision up to you.

## Features

### Basic Features

Here's what EasyNPC does, in the order that it does them:

1. Examine the current load order and mod list to figure out which mods are NPC visual overhauls (_face mods_) and which are changing other aspects (_behavior mods_).
 
2. Provide a point-and-click interface to choose which faces will appear in game, with visual previews (if available).

3. Build a completely standalone mod which:  

    a) inherits non-visual attributes from the winning behavior mod, effectively eliminating the need for compatibility patches;  

    b) incorporates both the record edits and facegen data from the chosen face mods, to get the correct in-game appearance;

    c) makes those NPCs use the same body type as the PC, so they can wear any outfit without issues<sup>*</sup>;

    d) does not depend on any of the face mods - meaning that they can all be disabled afterward, freeing up those slots in the load order.

4. Pack all of the required files into one or more archives (BSA) to optimize performance and load times.

<sub>* A future release may support body carryover, if the feature is in high demand. Most newer NPC overhauls do not use custom bodies anymore - the practice has fallen out of favor largely due to the conflicts they are known to create.</sub>

### Advanced Features

- Search/filters to make it easy to find a specific NPC or group of NPCs. (More options are planned)
- Check for potential errors or conflicts in the current profile, or even the source mods - highly recommended before each build!
- Import/export profiles to an ordinary pipe-delimited text file
- *** Experimental *** Conversion of "wigs" to normal hair - currently requires the original hair mod and a reasonably consistent record-naming scheme in the wig mod. This is mainly for unusual mods such as [High Poly NPC Overhaul 2.0](https://www.nexusmods.com/skyrimspecialedition/mods/44155).

## Requirements

EasyNPC runs on .NET 5, which also requires Windows 10.

This a hard requirement for several dependencies, including [Mutagen](https://github.com/Mutagen-Modding/Mutagen), so unfortunately it cannot be retargeted for older Windows versions.

Currently it is designed to work with Skyrim Special Edition (SSE). Support for other games is likely a possibility, but not currently a priority.

## Getting Started

Refer to the [Getting Started](Docs/getting-started.md) guide.

### Reporting Issues

EasyNPC produces log files for all of its major operations. If the program crashes, a dialog should open with a link to the log directory and the name of the current session's log file. Please include this in any bug report.

In the unlikely event of a crash happening before any log output can be produced, check the Windows event log: Start Menu -> Event Viewer -> Windows Logs -> Application. Look for an error with a Source of `.NET Runtime`, whose first line says `Application: EasyNPC.exe`. Copy the details of this into the issue report; without this information, it may not be possible to fix.

The in-app Maintenance tab includes a few other useful troubleshooting utilities specific to the app, the most important one being the "Reset" actions. Since EasyNPC uses an autosave system, this is the quickest way to fix a seriously compromised profile, or incorporate significant changes to a mod list/load order.