using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Focus.Apps.EasyNpc.Build
{
    public class SamplePluginGroup : IEnumerable<SamplePluginDetails>
    {
        private readonly List<SamplePluginDetails> details = new();

        public SamplePluginGroup()
        {
            details.AddRange(new[]
            {
                new SamplePluginDetails("Skyrim.esm", "", "Base game"),
                new SamplePluginDetails("Update.esm", "", "Base game"),
                new SamplePluginDetails("Dawnguard.esm", "", "DLC"),
                new SamplePluginDetails("HearthFires.esm", "", "DLC"),
                new SamplePluginDetails("Dragonborn.esm", "", "DLC"),
                new SamplePluginDetails("Unofficial Skyrim Special Edition Patch.esp", "Unofficial Skyrim Special Edition Patch", "Foundation"),
                new SamplePluginDetails("Unofficial Skyrim Modder's Patch.esp", "Unofficial Skyrim Modder's Patch - USMP SE", "Foundation"),
                new SamplePluginDetails("3DNPC.esp", "Interesting NPCs SE", "New NPCs/followers"),
                new SamplePluginDetails("ApachiiHair.esm", "ApachiiSkyHair SSE", "New head parts"),
                new SamplePluginDetails("Serana.esp", "Seranaholic", "NPC visual replacer", true),
                new SamplePluginDetails("SeranaFooPatch.esp", "My Serana Patch", "NPC visual patch", true),
            });
        }

        public IEnumerator<SamplePluginDetails> GetEnumerator()
        {
            return details.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return details.GetEnumerator();
        }
    }

    public class SamplePluginDetails
    {
        public string PluginName { get; set; }
        public string ModName { get; set; }
        public string Purpose { get; set; }
        public bool IsSuspicious { get; set; }

        public SamplePluginDetails()
        {
        }

        public SamplePluginDetails(string pluginName, string modName, string purpose, bool isSuspicious = false)
        {
            PluginName = pluginName;
            ModName = modName;
            Purpose = purpose;
            IsSuspicious = isSuspicious;
        }
    }
}
