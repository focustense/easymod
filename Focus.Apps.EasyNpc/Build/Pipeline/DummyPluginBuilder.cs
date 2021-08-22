using Focus.Providers.Mutagen;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using System.IO;

namespace Focus.Apps.EasyNpc.Build.Pipeline
{
    // TODO: This doesn't quite seem to fit in any of our upstream libs. However, it's not really EasyNPC specific,
    // either. Find a better home for it.

    public interface IDummyPluginBuilder
    {
        void CreateDummyPlugin(string path);
    }

    public class DummyPluginBuilder : IDummyPluginBuilder
    {
        private readonly GameSelection game;

        public DummyPluginBuilder(GameSelection game)
        {
            this.game = game;
        }

        public void CreateDummyPlugin(string path)
        {
            var modKey = ModKey.FromNameAndExtension(Path.GetFileName(path));
            var dummyMod = new SkyrimMod(modKey, game.GameRelease.ToSkyrimRelease());
            dummyMod.ModHeader.Flags |= SkyrimModHeader.HeaderFlag.LightMaster;
            dummyMod.WriteToBinary(path);
        }
    }
}
