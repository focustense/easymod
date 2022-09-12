using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;

namespace Focus.Tools.EasyFollower
{
    static class FormLinks
    {
        // Classes
        public static readonly IFormLink<IClassGetter> CombatWarrior1H = FormLink<IClassGetter>("Skyrim.esm", 0x013176);

        // Factions
        public static readonly IFormLink<IFactionGetter> CurrentFollowerFaction = FormLink<IFactionGetter>("Skyrim.esm", 0x05c84e);
        public static readonly IFormLink<IFactionGetter> PotentialMarriageFaction = FormLink<IFactionGetter>("Skyrim.esm", 0x019809);
        public static readonly IFormLink<IFactionGetter> PotentialFollowerFaction = FormLink<IFactionGetter>("Skyrim.esm", 0x05c84d);

        // Packages
        public static readonly IFormLink<IPackageGetter> DefaultSandboxEditorLocation512 = FormLink<IPackageGetter>("Skyrim.esm", 0x01b217);

        // Spells
        public static readonly IFormLink<ISpellGetter> PCHealRateCombat = FormLink<ISpellGetter>("Skyrim.esm", 0x1031d3);

        // Voice types
        public static readonly IFormLink<IVoiceTypeGetter> FemaleCommonerVoiceType = FormLink<IVoiceTypeGetter>("Skyrim.esm", 0x013ade);
        public static readonly IFormLink<IVoiceTypeGetter> MaleCommonerVoiceType = FormLink<IVoiceTypeGetter>("Skyrim.esm", 0x013ad3);

        private static IFormLink<T> FormLink<T>(string modName, uint id)
            where T : class, IMajorRecordGetter
        {
            return new FormKey(modName, id).ToLink<T>();
        }
    }
}
