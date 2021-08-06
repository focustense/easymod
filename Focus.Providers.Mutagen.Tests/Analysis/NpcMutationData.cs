using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Skyrim;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Focus.Providers.Mutagen.Tests.Analysis
{
    class NpcMutationData : TheoryData
    {
        public NpcMutationData(IEnumerable<Action<Npc>> mutations)
        {
            foreach (var mutation in mutations)
                Add(new object[] { mutation });
        }
    }

    class NpcMutations
    {
        // For many types of mutations, the analyzer is only going to look at what is referenced, and not actually
        // follow the reference. For these, we can use an arbitrary (invalid) key.
        private static readonly FormKey dummyKey = FormKey.Factory("123456:dummy.esp");

        public static readonly NpcMutationData Behavior = new(new Action<Npc>[]
        {
            x => x.AIData.Aggression = GetAlternateValue(x.AIData.Aggression),
            x => x.AIData.AggroRadiusBehavior = !x.AIData.AggroRadiusBehavior,
            x => x.AIData.Assistance = GetAlternateValue(x.AIData.Assistance),
            x => x.AIData.Attack++,
            x => x.AIData.Confidence = GetAlternateValue(x.AIData.Confidence),
            x => x.AIData.EnergyLevel--,
            x => x.AIData.Mood = GetAlternateValue(x.AIData.Mood),
            x => x.AIData.Responsibility = GetAlternateValue(x.AIData.Responsibility),
            x => x.AIData.Warn++,
            x => x.AIData.WarnOrAttack++,
            x => x.AttackRace.SetTo(dummyKey),
            x => x.Attacks.Add(new Attack()),
            x => x.Class.SetTo(dummyKey),
            x => x.CombatOverridePackageList.SetTo(dummyKey),
            x => x.CombatStyle.SetTo(dummyKey),
            x => x.Configuration.BleedoutOverride++,
            x => x.Configuration.CalcMaxLevel--,
            x => x.Configuration.CalcMinLevel++,
            x => x.Configuration.DispositionBase++,
            x => x.Configuration.Flags ^= NpcConfiguration.Flag.Essential,
            // Sex changes ("female") flag would be considered behavior AND face. For now, not included.
            x => x.Configuration.Flags ^= NpcConfiguration.Flag.Invulnerable,
            x => x.Configuration.HealthOffset++,
            x => ((NpcLevel)x.Configuration.Level).Level++,
            x => x.Configuration.MagickaOffset++,
            x => x.Configuration.SpeedMultiplier--,
            x => x.Configuration.StaminaOffset++,
            x => x.Configuration.TemplateFlags = NpcConfiguration.TemplateFlag.Inventory,
            x => x.CrimeFaction.SetTo(dummyKey),
            x => x.DeathItem.SetTo(dummyKey),
            x => x.DefaultPackageList.SetTo(dummyKey),
            x => x.Destructible = new() { Stages = { new() {  Model = new() { File = "destruct1.nif" } } } },
            x => x.Factions.RemoveAt(1),
            // TODO: FarAwayModel - is this "behavior"?
            x => x.FormVersion++,
            x => x.GiftFilter.SetTo(dummyKey),
            x => x.GuardWarnOverridePackageList.SetTo(dummyKey),
            x => x.IsDeleted = true,
            x => x.Items.RemoveAt(0),
            x => x.Keywords.RemoveAt(0),
            x => x.NAM5 = 23,
            x => x.ObjectBounds.First = new Noggog.P3Int16(1, 2, 3),
            x => x.ObjectBounds.Second = new Noggog.P3Int16(1, 2, 3),
            x => x.ObserveDeadBodyOverridePackageList.SetTo(dummyKey),
            x => x.Packages.RemoveAt(1),
            x => x.Perks.RemoveAt(x.Perks.Count - 1),
            x => x.PlayerSkills.FarAwayModelDistance += 0.123f,
            x => x.PlayerSkills.Health += 5,
            x => x.PlayerSkills.Magicka += 8,
            x => x.PlayerSkills.Stamina -= 4,
            x => x.PlayerSkills.SkillOffsets[Skill.Enchanting] += 2,
            x => x.PlayerSkills.SkillValues[Skill.Conjuration] -= 3,
            // Race is behavior but also other things; needs its own test.
            x => x.Sound = new NpcSoundTypes { Types = { new() { Type = NpcSoundType.SoundType.LeftFoot } } },
            x => x.SoundLevel = GetAlternateValue(x.SoundLevel),
            x => x.SpectatorOverridePackageList.SetTo(dummyKey),
            x => x.Template.SetTo(dummyKey),
            x => x.VirtualMachineAdapter.Clear(),
            x => x.VirtualMachineAdapter.ObjectFormat++,
            x => x.VirtualMachineAdapter.Scripts[0].Flags |= ScriptEntry.Flag.InheritedAndRemoved,
            x => x.VirtualMachineAdapter.Scripts[0].Properties[0] = new ScriptFloatProperty { Name = "Prop0", Data = 0.32f },
            x => x.VirtualMachineAdapter.Version++,
            x => x.Voice.SetTo(dummyKey),
        });

        public static readonly NpcMutationData Face = new(new Action<Npc>[]
        {
            x => x.FaceMorph.JawNarrowVsWide += 0.1f,
            x => x.FaceMorph.NoseLongVsShort += 0.1f,
            x => x.FaceParts.Eyes++,
            x => x.FaceParts.Mouth++,
            x => x.FaceParts.Nose++,
            x => x.HairColor.SetTo(dummyKey),
            x => x.HeadTexture.SetTo(dummyKey),
            x => x.TextureLighting = Color.Goldenrod,
            x => x.TintLayers.RemoveAt(0),
            x => x.TintLayers[0].Color = Color.CadetBlue,
            x => x.TintLayers[1].Index++,
            x => x.TintLayers[2].InterpolationValue -= 0.15f,
        });

        // "Ignored" means it's a record change with known, but currently benign, effect, i.e. not included in any
        // specific part of the analysis.
        public static readonly NpcMutationData Ignored = new(new Action<Npc>[]
        {
            x => x.Configuration.Flags ^= NpcConfiguration.Flag.OppositeGenderAnims,
            x => x.TintLayers[1].Preset = 4,
        });

        public static readonly NpcMutationData Outfits = new(new Action<Npc>[]
        {
            x => x.DefaultOutfit.SetTo(dummyKey),
            x => x.SleepingOutfit.SetTo(dummyKey),
        });

        public static readonly NpcMutationData Scales = new(new Action<Npc>[]
        {
            x => x.Height += 0.04f,
            x => x.Weight += 7,
        });

        // "Unused", unlike "ignored", means that the change is to some part of the record that is believed to have no
        // effect whatsoever on the game, and therefore should be excluded from ITM/ITPO comparisons.
        // This group is also used for comparisons deemed to be unreliable, such as floating-point comparisons with
        // extremely minute differences.
        public static readonly NpcMutationData Unused = new(new Action<Npc>[]
        {
            x => x.AIData.Unused++,
            x => x.FaceMorph.BrowsUpVsDown += float.Epsilon * 2,
            x => x.Height += float.Epsilon * 2,
            x => x.Weight -= float.Epsilon * 2,
            x => x.PlayerSkills.Unused += 5,
            x => x.PlayerSkills.Unused2 = new byte[] { 1, 2, 3 },
            x => x.Version2++,
            x => x.VersionControl++,
        });

        private static TEnum GetAlternateValue<TEnum>(TEnum currentValue)
            where TEnum : struct, Enum
        {
            return Enum.GetValues<TEnum>()
                .Where(v => !Equals(v, currentValue))
                .FirstOrDefault();
        }
    }
}
