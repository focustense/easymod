# BSA Facts

Some of the claims about what can and can't be done with Bethesda archives may have you wondering "why?" So, here is some information about the BSA format that may help to clear those questions up.

## BSA Format

In general, a BSA consists of 3 parts, simplified for discussion purposes:

1. Archive header containing general metadata - whether it's compressed, what file types are in it, etc.
2. File index that includes folder names and byte offsets in the file, and file names with their byte offsets in their respective folders.
3. The actual binary file data, whether compressed or uncompressed.

Or, visually:

```
[ Header ][ Index ][ File 1 ][ File 2 ][...][ File N ]
                   |
                   | (Start of file data)
```

## Consequences

From the format above, we can already deduce two important properties:

- The "start of file data" position in the BSA is not fixed. If more files are added, then the file index becomes larger, and the file data must start at a later position.
- As the archive size grows by adding more files, so do the offsets in the index. The largest such offset is approximately the same as the total size.

This explains the reasons for two of the limitations:

- If the size of the index depends on the manifest, and the start-of-data position depends on the size of the index, then no files can possibly be written to the archive until the index size can be determined. Therefore, the entire list of file names must be provided _in advance_, and cannot be changed on an existing archive without having to "move" the entire files block - which accounts for almost all of the total size of the archive. It is literally faster to rebuild the entire archive.
- Because the files must be written sequentially to disk, it is not possible to parallelize the I/O itself. However, compression can take place in memory, before the files are ever written to the archive. Therefore, file compression can be parallelized, but file inclusion cannot. Although it is rarely the case in practice, with enough CPU/GPU power it is technically possible for it to be _faster_ to create a compressed archive than an uncompressed one, because the compressed data may be much smaller (e.g. 1 GB vs 10 GB) and I/O writes are slow and sequential. This is especially likely to be the case with a mechanical HDD (not an SSD).

## The 2 GB limit

What the file format doesn't explain is why archives over 2 GB are inherently unstable. In fact, if you've been doing Skyrim modding for any significant amount of time, you may not even _believe_ that this is true, having heard from multiple sources that it is "about 2 GB" and that sometimes a larger archive is actually OK. Because we cannot see the source code for Skyrim SE, this requires a little more intuition.

First of all, the maximum size is a testable claim, but is too often tested imprecisely, without controlling for all variables. The way to test this claim is not simply to write a BSA, open SSE (or load a save game) and check for bugs or crashes. It is to **randomize the file order within the BSA**, and then check for bugs or crashes. Using this test method, anyone can verify that it is possible to create two BSAs of identical size and contents, but with files written in a different order (thus, having different byte offsets), with one causing the game to crash on load or when opening a savegame, and the other appearing to have no problems at all - at least, not at that moment.

There is obviously no code in SkyrimSE that says "crash now if BSA > 2 GB". What is far more likely, and corroborated by evidence from [.NET Script Framework](https://www.nexusmods.com/skyrimspecialedition/mods/21294) crash dumps, is that some BSA code in the SSE engine is still using a 32-bit signed integer, and when a file's byte offset does not fit within the range of a 32-bit signed integer (which is 2<sup>31</sup>, or 2 GB), it overflows and ends up reading from a quasi-random location in the BSA, which could be somewhere in the header, or in the middle of some other file. It doesn't really matter, because what the game actually sees there appears to be garbage, and depending on what it's trying to do (such as decompress a texture), this may cause an immediate crash, or some hard-to-pinpoint instability.

This limitation creates an unusual scenario wherein external tools - like this one, Mod Organizer, BSArch, and so on - have no trouble reading and writing BSAs that are far larger than 2 GB, because the file format, which makes 32-bit file offsets relative to directory offsets, allows for it; but the game itself cannot handle them, because according to all available evidence, it still tries to store the total offset (that is, `folder offset + file offset in folder`) in a 32-bit variable or pass it to a 32-bit API.

Unless the game engine is patched, **all BSAs larger than 2 GB are unstable**. You may not become _aware_ of that instability until the game needs to load a file with one of these out-of-bounds offsets, which could happen when you start the game, when you load your save game, when you exit to an exterior cell, when a certain NPC is present, or perhaps never at all - no one interacts with 100% of all Skyrim content, including mods, in a single game.

The moral of the story is:

- Don't write BSAs that are larger than 2 GB.
- Don't assume that your large BSA is stable just because you managed to start the game, load a savegame, or even play for 8 uninterrupted hours.
- If you choose to override the default settings and end up generating BSAs that are as small as 2.000000001 GB, and you get crashes or your users report crashes, don't complain or
  file a bug here or in libbsarch, as there's nothing either of us can do to fix it.