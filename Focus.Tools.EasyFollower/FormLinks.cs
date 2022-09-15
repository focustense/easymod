using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Skyrim;

namespace Focus.Tools.EasyFollower
{
    static class FormLinks
    {
        // Armor addons
        public static readonly IFormLink<IArmorAddonGetter> NakedTorso = FormLink<IArmorAddonGetter>("Skyrim.esm", 0x000d67);
        public static readonly IFormLink<IArmorAddonGetter> NakedHands = FormLink<IArmorAddonGetter>("Skyrim.esm", 0x000d6c);
        public static readonly IFormLink<IArmorAddonGetter> NakedFeet = FormLink<IArmorAddonGetter>("Skyrim.esm", 0x000d6e);

        // Classes
        public static readonly IFormLink<IClassGetter> CombatWarrior1H = FormLink<IClassGetter>("Skyrim.esm", 0x013176);

        // Factions
        public static readonly IFormLink<IFactionGetter> CurrentFollowerFaction = FormLink<IFactionGetter>("Skyrim.esm", 0x05c84e);
        public static readonly IFormLink<IFactionGetter> PotentialMarriageFaction = FormLink<IFactionGetter>("Skyrim.esm", 0x019809);
        public static readonly IFormLink<IFactionGetter> PotentialFollowerFaction = FormLink<IFactionGetter>("Skyrim.esm", 0x05c84d);

        // Form lists
        public static readonly IFormLink<IFormListGetter> HeadPartsAllRacesMinusBeast = FormLink<IFormListGetter>("Skyrim.esm", 0x0a803f);

        // NPCs
        public static readonly IFormLink<INpcGetter> Player = FormLink<INpcGetter>("Skyrim.esm", 0x000007);

        // Packages
        public static readonly IFormLink<IPackageGetter> DefaultSandboxEditorLocation512 = FormLink<IPackageGetter>("Skyrim.esm", 0x01b217);

        // Races
        public static readonly IFormLink<IRaceGetter> DefaultRace = FormLink<IRaceGetter>("Skyrim.esm", 0x000019);

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
