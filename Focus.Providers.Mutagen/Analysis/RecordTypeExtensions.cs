﻿using Mutagen.Bethesda.Skyrim;
using System;
using RecordType = Focus.Analysis.Records.RecordType;

namespace Focus.Providers.Mutagen.Analysis
{
    public static class RecordTypeExtensions
    {
        public static Type GetGroupType(this RecordType type) => type switch
        {
            RecordType.AcousticSpace => typeof(IAcousticSpaceGetter),
            RecordType.Action => typeof(IActionRecordGetter),
            RecordType.Activator => typeof(IActivatorGetter),
            RecordType.ActorValueInformation => typeof(IActorValueInformationGetter),
            RecordType.AddonNode => typeof(IAddonNodeGetter),
            RecordType.AlchemicalApparatus => typeof(IAlchemicalApparatusGetter),
            RecordType.Ammunition => typeof(IAmmunitionGetter),
            RecordType.AnimatedObject => typeof(IAnimatedObjectGetter),
            RecordType.Armor => typeof(IArmorGetter),
            RecordType.ArmorAddon => typeof(IArmorAddonGetter),
            RecordType.ArtObject => typeof(IArtObjectGetter),
            RecordType.AssociationType => typeof(IAssociationTypeGetter),
            RecordType.BodyPartData => typeof(IBodyPartDataGetter),
            RecordType.Book => typeof(IBookGetter),
            RecordType.CameraPath => typeof(ICameraPathGetter),
            RecordType.CameraShot => typeof(ICameraShotGetter),
            RecordType.Cell => typeof(ICellGetter),
            RecordType.Climate => typeof(IClimateGetter),
            RecordType.CollisionLayer => typeof(ICollisionLayerGetter),
            RecordType.Color => typeof(IColorRecordGetter),
            RecordType.CombatStyle => typeof(ICombatStyleGetter),
            RecordType.ConstructibleObject => typeof(IConstructibleObjectGetter),
            RecordType.Container => typeof(IContainerGetter),
            RecordType.Debris => typeof(IDebrisGetter),
            RecordType.DefaultObjectManager => typeof(IDefaultObjectManagerGetter),
            RecordType.DialogBranch => typeof(IDialogBranchGetter),
            RecordType.DialogTopic => typeof(IDialogTopicGetter),
            RecordType.DialogView => typeof(IDialogViewGetter),
            RecordType.Door => typeof(IDoorGetter),
            RecordType.DualCastData => typeof(IDualCastDataGetter),
            RecordType.EffectShader => typeof(IEffectShaderGetter),
            RecordType.EncounterZone => typeof(IEncounterZoneGetter),
            RecordType.EquipType => typeof(IEquipTypeGetter),
            RecordType.Explosion => typeof(IExplosionGetter),
            RecordType.Eyes => typeof(IEyesGetter),
            RecordType.Faction => typeof(IFactionGetter),
            RecordType.Flora => typeof(IFloraGetter),
            RecordType.Footstep => typeof(IFootstepGetter),
            RecordType.FootstepSet => typeof(IFootstepSetGetter),
            RecordType.FormIdList => typeof(IFormListGetter),
            RecordType.Furniture => typeof(IFurnitureGetter),
            RecordType.GameSetting => typeof(IGameSettingGetter),
            RecordType.Global => typeof(IGlobalGetter),
            RecordType.Grass => typeof(IGrassGetter),
            RecordType.Hazard => typeof(IHazardGetter),
            RecordType.HeadPart => typeof(IHeadPartGetter),
            RecordType.IdleAnimation => typeof(IIdleAnimationGetter),
            RecordType.IdleMarker => typeof(IIdleMarkerGetter),
            RecordType.ImageSpace => typeof(IImageSpaceGetter),
            RecordType.ImageSpaceAdapter => typeof(IImageSpaceAdapterGetter),
            RecordType.Impact => typeof(IImpactGetter),
            RecordType.ImpactDataSet => typeof(IImpactDataSetGetter),
            RecordType.Ingestible => typeof(IIngestibleGetter),
            RecordType.Ingredient => typeof(IIngredientGetter),
            RecordType.Key => typeof(IKeyGetter),
            RecordType.Keyword => typeof(IKeywordGetter),
            RecordType.LandscapeTexture => typeof(ILandscapeTextureGetter),
            RecordType.LeveledItem => typeof(ILeveledItemGetter),
            RecordType.LeveledNpc => typeof(ILeveledNpcGetter),
            RecordType.LeveledSpell => typeof(ILeveledSpellGetter),
            RecordType.Light => typeof(ILightGetter),
            RecordType.LightingTemplate => typeof(ILightingTemplateGetter),
            RecordType.LoadScreen => typeof(ILoadScreenGetter),
            RecordType.Location => typeof(ILocationGetter),
            RecordType.LocationReferenceType => typeof(ILocationReferenceTypeGetter),
            RecordType.MagicEffect => typeof(IMagicEffectGetter),
            RecordType.MaterialObject => typeof(IMaterialObjectGetter),
            RecordType.MaterialType => typeof(IMaterialTypeGetter),
            RecordType.Message => typeof(IMessageGetter),
            RecordType.MiscItem => typeof(IMiscItemGetter),
            RecordType.MoveableStatic => typeof(IMoveableStaticGetter),
            RecordType.MovementType => typeof(IMovementTypeGetter),
            RecordType.MusicTrack => typeof(IMusicTrackGetter),
            RecordType.MusicType => typeof(IMusicTypeGetter),
            RecordType.NavigationMeshInfoMap => typeof(INavigationMeshInfoMapGetter),
            RecordType.Npc => typeof(INpcGetter),
            RecordType.ObjectEffect => typeof(IObjectEffectGetter),
            RecordType.Outfit => typeof(IOutfitGetter),
            RecordType.Package => typeof(IPackageGetter),
            RecordType.Perk => typeof(IPerkGetter),
            RecordType.Projectile => typeof(IProjectileGetter),
            RecordType.Quest => typeof(IQuestGetter),
            RecordType.Race => typeof(IRaceGetter),
            RecordType.Region => typeof(IRegionGetter),
            RecordType.Relationship => typeof(IRelationshipGetter),
            RecordType.ReverbParameters => typeof(IReverbParametersGetter),
            RecordType.Scene => typeof(ISceneGetter),
            RecordType.Scroll => typeof(IScrollGetter),
            RecordType.ShaderParticleGeometry => typeof(IShaderParticleGeometryGetter),
            RecordType.Shout => typeof(IShoutGetter),
            RecordType.SoulGem => typeof(ISoulGemGetter),
            RecordType.SoundCategory => typeof(ISoundCategoryGetter),
            RecordType.SoundDescriptor => typeof(ISoundDescriptorGetter),
            RecordType.SoundMarker => typeof(ISoundMarkerGetter),
            RecordType.SoundOutputModel => typeof(ISoundOutputModelGetter),
            RecordType.Spell => typeof(ISpellGetter),
            RecordType.Static => typeof(IStaticGetter),
            RecordType.StoryManageBranchNode => typeof(IStoryManagerBranchNodeGetter),
            RecordType.StoryManagerEventNode => typeof(IStoryManagerEventNodeGetter),
            RecordType.StoryManagerQuestNode => typeof(IStoryManagerQuestNodeGetter),
            RecordType.TalkingActivator => typeof(ITalkingActivatorGetter),
            RecordType.TextureSet => typeof(ITextureSetGetter),
            RecordType.Tree => typeof(ITreeGetter),
            RecordType.VisualEffect => typeof(IVisualEffectGetter),
            RecordType.VoiceType => typeof(IVoiceTypeGetter),
            RecordType.Water => typeof(IWaterGetter),
            RecordType.Weapon => typeof(IWeaponGetter),
            RecordType.Weather => typeof(IWeatherGetter),
            RecordType.WordOfPower => typeof(IWordOfPowerGetter),
            RecordType.Worldspace => typeof(IWorldspaceGetter),
            _ => throw new ArgumentException($"Record type {type} is not recognized or not supported.", nameof(type))
        };
    }
}
