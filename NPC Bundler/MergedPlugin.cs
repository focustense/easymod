using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XeLib;
using XeLib.API;

namespace NPC_Bundler
{
    static class MergedPlugin
    {
        private static readonly string MergeFileName = "NPC Appearances Merged.esp";

        public static void Build(IEnumerable<NpcConfiguration> npcs)
        {
            var dataPath = Meta.GetGlobal("DataPath");
            var mergeFilePath = $@"{dataPath}\{MergeFileName}";
            if (File.Exists(mergeFilePath))
                File.Move(mergeFilePath, $"{mergeFilePath}.{DateTime.Now.ToString("yyyyMMdd_hhmmss")}.bak", true);

            // Unfortunately, concurrency is (probably?) not possible in this method due to the way in which XEditLib
            // passes data back to the caller, using a single block of shared memory to store the result of the last
            // operation - meaning, if one thread starts a new operation before the previous thread has a chance to
            // read the result of its own operation, it may get incorrect or corrupted data back.
            //
            // It might be possible to work around this with a lock, but seems probable that most threads would then
            // be spending >90% of their time in wait states, actually making the entire process slower than ST.
            //
            // So, at least until more tests can be done and/or some solution is found, this will be single-threaded.

            using var g = new HandleGroup();
            var mergeFile = Files.FileByName(MergeFileName);
            if (mergeFile.Value == 0)
                mergeFile = Files.AddFile(MergeFileName);
            g.AddHandle(mergeFile);
            Files.NukeFile(mergeFile);

            var header = g.AddHandle(Files.GetFileHeader(mergeFile));
            ElementValues.SetFlag(header, @"Record Header\Record Flags", "ESL", true);

            foreach (var npc in npcs)
            {
                // We only care about actors that have actually been overridden, and not just by DLC. Otherwise they're
                // just vanilla NPCs and we can ignore them.
                if ((npc.DefaultPluginName == npc.BasePluginName && npc.FacePluginName == npc.DefaultPluginName) ||
                    (FileStructure.IsDlc(npc.DefaultPluginName) && FileStructure.IsDlc(npc.FacePluginName)))
                    continue;

                var formIdHex = npc.FormId.ToString("X8");
                var defaultFile = g.AddHandle(Files.FileByName(npc.DefaultPluginName));
                var defaultNpcElement = g.AddHandle(Elements.GetElement(defaultFile, formIdHex));
                Masters.AddRequiredMasters(defaultNpcElement, mergeFile);
                var mergedNpcRecord = g.AddHandle(Elements.CopyElement(defaultNpcElement, mergeFile));
            }

            // This doesn't save the file we expect - it will actually have an ".esp.save" extension.
            Files.SaveFile(mergeFile);
            File.Move($"{mergeFilePath}.save", mergeFilePath);
        }
    }
}