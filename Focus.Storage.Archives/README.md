# Focus.Storage.Archives

.NET library for reading, extracting and creating Bethesda archives (BSA), based on [libbsarch](https://github.com/Guekka/libbsarch).

The implementation uses P/Invoke interop - i.e. it is not "fully managed" - but knowledge of interop is not required to use it.

## Example: Reading archive contents

This will open an archive, print out its headers, and list all files in the archive. Don't forget to `Dispose` (or use `using`).

```c#
using var archive = Archive.FromFile(@"C:\path\to\archive.bsa");
Console.WriteLine($"Archive Type: {archive.Type}");
Console.WriteLine($"Archive Version: {archive.Version}");
Console.WriteLine($"Archive Flags: 0x{archive.ArchiveFlags:X4}");
Console.WriteLine($"File Flags: 0x{archive.FileFlags:X4}");
Console.WriteLine($"Compress: {archive.IsCompressionEnabled}");
Console.WriteLine($"Share Data: {archive.IsDataSharingEnabled}");
Console.WriteLine($"File Count: {archive.FileCount}");
Console.WriteLine();
Console.WriteLine("--- File List ---");
foreach (var pathInArchive in archive.GetFileNames())
    Console.WriteLine(pathInArchive);
```

## Example: Creating a new archive

Bethesda archives are immutable (see [[BSA Facts]], so a separate API is provided to create them, instead of adding these methods to the `Archive` itself.

```c#
using var archive = new ArchiveBuilder(ArchiveType.SSE)
    .AddFile(@"C:\path\to\file1.xyz", @"path\in\bsa\f1.xyz")
    .AddFile(@"C:\path\to\file2.xyz", @"path\in\bsa\f2.xyz")
    .AddDirectory(@"C:\path\to\another\directory", @"directory\in\bsa")
    .Compress(true)
    .ShareData(true)
    .Build(@"C:\output\myarchive.bsa");
// Do something with the archive. Or don't.
```

Some important facts about the archive builder:

- Nothing actually gets created until `Build` is called, so builder instances can be reused, passed around, etc. Unlike the `Archive`, these objects don't require any special care.
- The order that the non-`Build` methods (`AddFile`, `AddDirectory`, `Compress`, `ShareData`, etc.) are called in does not matter.
- You can't get around the immutability constraint by holding onto a builder instance, adding one more file and then building it again. It will just build an entirely new archive.
- File compression is highly parallelizable, other operations are generally not (see [[BSA Facts]]. This means that creating an archive with many files and `Compress(true)` will generally use all available cores, but creating an uncompressed archive will generally only use one.

## Archive Splitting

BSA archives over 2 GB in size will cause crashes and instability (BA2 archives used by Fallout 4 do not have this problem), so the `ArchiveBuilder` will split your output into multiple archives in a similar fashion to [Cathedral Assets Optimizer](https://gitlab.com/G_ka/Cathedral_Assets_Optimizer). Specifically, if you specify `myarchive.bsa` as the output file, you may also get `myarchive0.bsa`, `myarchive1.bsa`, etc. However, it does **not** create the dummy plugins that are required for the game to load these archives.

A dummy plugin is just an empty, ESL-flagged ESP that can be created manually in xEdit or using the patching library of your choice.

Because the compressed size is impossible to predict in advance, and Bethesda archives are immutable (again), the `ArchiveBuilder` defaults to a maximum **uncompressed** size of 2 GB, which can be changed using the `MaxUncompressedSize` builder method. For example, if you anticipate a 2:1 compression ratio, you can specify `4L * 1024 * 1024 * 1024` (4 GB) as the maximum uncompressed size, but there is no way to correct for a wrong guess that generates a file more than 2 GB in size, so be conservative with your estimates.

## Archive Extraction

The source library, `libbsarch`, includes functions for extracting BSA files to memory or disk. These are not currently exposed in any of the .NET APIs. There is no reason why they can't be; it just has not been implemented yet, and there are already other .NET libraries capable of _reading_ BSAs.