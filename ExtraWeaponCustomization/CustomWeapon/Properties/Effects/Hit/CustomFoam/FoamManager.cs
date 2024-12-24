using Agents;
using Player;
using SNetwork;
using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Effects.Hit.CustomFoam
{
    public static class FoamManager
    {
        private readonly static FoamStaticSync _staticSync = new();
        private readonly static FoamEnemySync _enemySync = new();
        private readonly static FoamDoorSync _doorSync = new();

        public const float MaxVolumeMod = 256f;

        internal static void Init()
        {
            _staticSync.Setup();
            _enemySync.Setup();
            _doorSync.Setup();
        }

        public static void FoamEnemy(GameObject go, PlayerAgent source, Vector3 pos, float volumeMod, Foam property)
        {
            IGlueTarget component = go.GetComponent<IGlueTarget>();
            if (component == null || component.GlueTargetEnemyAgent == null) return;

            if (component.GlueTargetEnemyAgent != null)
            {
                FoamEnemyData data = default;
                data.target.Set(component.GlueTargetEnemyAgent);
                data.limbID = (byte)component.GlueTargetSubIndex;
                data.source.Set(source);
                data.localPosition = component.GlueTargetTransform.InverseTransformPoint(pos);
                data.volumeMod.Set(volumeMod, MaxVolumeMod);
                data.propertyID = property.SyncPropertyID;
                _enemySync.Send(data);
            }
            else
                FoamStatic(source, pos, volumeMod, property);
        }

        internal static void Internal_ReceiveFoamEnemy(FoamEnemyData packet)
        {
            if (!TrySpawnNewFoam(packet.source, packet.volumeMod.Get(MaxVolumeMod), packet.propertyID, out var proj)) return;

            ProjectileManager.pSpawnGlueOnEnemyAgent data = default;
            data.syncID = proj.SyncID;
            data.onEnemyAgent = packet.target;
            data.targetSubIndex = packet.limbID;
            data.localPos = packet.localPosition;
            data.volumeDesc = proj.m_volumeDesc;
            data.effectMultiplier = proj.EffectMultiplier;
            ProjectileManager.Current.DoSpawnGlueOnEnemyAgent(data);
        }

        public static void FoamDoor(GameObject go, PlayerAgent source, Vector3 pos, float volumeMod, Foam property)
        {
            IGlueTarget component = go.GetComponent<IGlueTarget>();
            if (component == null) return;

            FoamDoorData data = default;
            data.door = component.GlueTargetDoorSyncStruct;
            data.glueTarget = component.GlueTargetSubIndex;
            data.source.Set(source);
            data.localPosition = component.GlueTargetTransform.InverseTransformPoint(pos);
            data.volumeMod.Set(volumeMod, MaxVolumeMod);
            data.propertyID = property.SyncPropertyID;
            _doorSync.Send(data);
        }

        internal static void Internal_ReceiveFoamDoor(FoamDoorData packet)
        {
            if (!TrySpawnNewFoam(packet.source, packet.volumeMod.Get(MaxVolumeMod), packet.propertyID, out var proj)) return;

            ProjectileManager.pSpawnGlueOnDoor data = default;
            data.syncID = proj.SyncID;
            data.pDoor = packet.door;
            data.targetSubIndex = packet.glueTarget;
            data.localPos = packet.localPosition;
            data.volumeDesc = proj.m_volumeDesc;
            ProjectileManager.Current.DoSpawnGlueOnDoor(data);
        }

        public static void FoamStatic(PlayerAgent source, Vector3 pos, float volumeMod, Foam property)
        {
            FoamStaticData data = default;
            data.source.Set(source);
            data.position = pos;
            data.volumeMod.Set(volumeMod, MaxVolumeMod);
            data.propertyID = property.SyncPropertyID;
            _staticSync.Send(data);
        }

        internal static void Internal_ReceiveFoamStatic(FoamStaticData packet)
        {
            if (!TrySpawnNewFoam(packet.source, packet.volumeMod.Get(MaxVolumeMod), packet.propertyID, out var proj)) return;

            ProjectileManager.pSpawnGlueOnStatic data = default;
            data.syncID = proj.SyncID;
            data.position = packet.position;
            data.volumeDesc = proj.m_volumeDesc;
            ProjectileManager.Current.DoSpawnGlueStatic(data);
        }

        private static bool TrySpawnNewFoam(pPlayerAgent owner, float volumeMod, ushort propertyID, [MaybeNullWhen(false)] out GlueGunProjectile projectile)
        {
            projectile = null;
            if (!owner.TryGet(out var source) || !CustomWeaponManager.TryGetSyncProperty<Foam>(propertyID, out var property)) return false;

            projectile = SpawnNewFoam(source, volumeMod, property);
            return true;
        }

        private static GlueGunProjectile SpawnNewFoam(PlayerAgent owner, float volumeMod, Foam property)
        {
            // Glue code depends on glue having been instanced as a projectile first, but we skip that step, so need to make our own manually.
            uint id = ProjectileManager.GetNextSyncID();
            var projectile = ProjectileManager.Current.SpawnGlueGunProjectileIfNeeded(id);

            float strength = property.BubbleStrength;
            strength *= property.IgnoreBooster ? 1f : AgentModifierManager.ApplyModifier(owner, AgentModifier.GlueStrength, 1f);
            projectile.m_glueStrengthMultiplier = strength;

            // Opting to modify volume by trigger amount/falloff rather than strength for better visual indication
            var volumeDesc = projectile.m_volumeDesc;
            volumeDesc.expandVolume = property.BubbleAmount * volumeMod;
            projectile.m_volumeDesc = volumeDesc;

            projectile.m_expandSpeed = property.BubbleExpandSpeed * volumeMod;

            projectile.m_owner = owner;
            projectile.m_allowSplat = false;

            return projectile;
        }
    }

    public struct FoamEnemyData
    {
        public pEnemyAgent target;
        public byte limbID;
        public pPlayerAgent source;
        public Vector3 localPosition;
        public UFloat16 volumeMod;
        public ushort propertyID;
    }

    public struct FoamStaticData
    {
        public pPlayerAgent source;
        public Vector3 position;
        public UFloat16 volumeMod;
        public ushort propertyID;
    }

    public struct FoamDoorData
    {
        public pStateReplicatorProvider door;
        public int glueTarget;
        public pPlayerAgent source;
        public Vector3 localPosition;
        public UFloat16 volumeMod;
        public ushort propertyID;
    }
}
