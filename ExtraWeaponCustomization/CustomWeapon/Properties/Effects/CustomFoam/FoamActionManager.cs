using Agents;
using Enemies;
using EWC.Attributes;
using Player;
using SNetwork;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Effects.Hit.CustomFoam
{
    public static class FoamActionManager
    {
        private readonly static FoamStaticSync _staticSync = new();
        private readonly static FoamEnemySync _enemySync = new();
        private readonly static FoamDoorSync _doorSync = new();
        private readonly static FoamDirectSync _directSync = new();
        private readonly static FoamSync _foamSync = new();
        private readonly static FoamActivateSync _activateSync = new();
        private readonly static FoamBubbleSync _bubbleSync = new();

        public const float MaxVolumeMod = 256f;
        public const float MaxDirect = 100f;
        public const float MaxFoam = 32f;
        public const float MaxTime = 32f;

        [InvokeOnAssetLoad]
        private static void Init()
        {
            _staticSync.Setup();
            _enemySync.Setup();
            _doorSync.Setup();
            _directSync.Setup();
            _foamSync.Setup();
            _activateSync.Setup();
            _bubbleSync.Setup();
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
                data.volumeMod.Set(volumeMod + 0.001f, MaxVolumeMod);
                data.propertyID = property.SyncPropertyID;
                _enemySync.Send(data);
            }
            else
                FoamStatic(source, pos, volumeMod, property);
        }

        internal static void Internal_ReceiveFoamEnemy(FoamEnemyData packet)
        {
            if (!TrySpawnNewFoam(packet.source, packet.volumeMod, packet.propertyID, out var proj) || !packet.target.TryGet(out var enemy)) return;

            ProjectileManager.WantToSpawnGlueOnEnemyAgent(proj.SyncID, enemy, packet.limbID, packet.localPosition, proj.m_volumeDesc, proj.EffectMultiplier);
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
            data.volumeMod.Set(volumeMod + 0.001f, MaxVolumeMod);
            data.propertyID = property.SyncPropertyID;
            _doorSync.Send(data);
        }

        internal static void Internal_ReceiveFoamDoor(FoamDoorData packet)
        {
            if (!TrySpawnNewFoam(packet.source, packet.volumeMod, packet.propertyID, out var proj)) return;

            ProjectileManager.WantToSpawnGlueOnDoor(proj.SyncID, packet.door, packet.glueTarget, packet.localPosition, proj.m_volumeDesc);
        }

        public static void FoamStatic(PlayerAgent source, Vector3 pos, float volumeMod, Foam property)
        {
            FoamStaticData data = default;
            data.source.Set(source);
            data.position = pos;
            data.volumeMod.Set(volumeMod + 0.001f, MaxVolumeMod);
            data.propertyID = property.SyncPropertyID;
            _staticSync.Send(data);
        }

        internal static void Internal_ReceiveFoamStatic(FoamStaticData packet)
        {
            if (!TrySpawnNewFoam(packet.source, packet.volumeMod, packet.propertyID, out var proj)) return;

            ProjectileManager.WantToSpawnStaticGlue(proj.SyncID, packet.position, proj.m_volumeDesc);
        }

        private static bool TrySpawnNewFoam(pPlayerAgent owner, UFloat16 volumeMod, ushort propertyID, [MaybeNullWhen(false)] out GlueGunProjectile projectile, uint syncID = 0)
        {
            projectile = null;
            if (!owner.TryGet(out var source) || !CustomWeaponManager.TryGetSyncProperty<Foam>(propertyID, out var property)) return false;

            projectile = SpawnNewFoam(source, volumeMod.Get(MaxVolumeMod), property, syncID);
            return true;
        }

        private static GlueGunProjectile SpawnNewFoam(PlayerAgent owner, float volumeMod, Foam property, uint syncID = 0)
        {
            // Glue code depends on glue having been instanced as a projectile first, but we skip that step, so need to make our own manually.
            // Except in the case where an enemy takes a portion of foam to fully foam.
            if (syncID == 0)
                syncID = ProjectileManager.GetNextSyncID();
            var projectile = ProjectileManager.Current.SpawnGlueGunProjectileIfNeeded(syncID);

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

            FoamManager.AddFoamBubble(projectile, property);
            FoamBubbleSync(owner, syncID, volumeMod, property);

            return projectile;
        }

        public static void FoamDirect(EnemyAgent enemy, float amount, Foam property)
        {
            FoamDirectData data = default;
            data.target.Set(enemy);
            data.amount.Set(amount + 0.001f, MaxDirect);
            data.propertyID = property.SyncPropertyID;
            _directSync.Send(data);
        }

        internal static void Internal_ReceiveFoamDirect(FoamDirectData packet)
        {
            if (!packet.target.TryGet(out var enemy) || !CustomWeaponManager.TryGetSyncProperty<Foam>(packet.propertyID, out var property)) return;

            // If foam time isn't modified, we don't need to do anything
            FoamManager.AddFoam(enemy.Damage, packet.amount.Get(MaxDirect), property);
        }

        public static void FoamSync(EnemyAgent enemy, float amountRel, float timeRel)
        {
            // Only master should send syncs.
            if (!SNet.IsMaster) return;

            FoamSyncData data = default;
            data.target.Set(enemy);
            data.amount.Set(amountRel, MaxFoam);
            data.time.Set(timeRel, MaxTime);
            _foamSync.Send(data);
        }

        internal static void Internal_ReceiveFoamSync(FoamSyncData packet)
        {
            if (!packet.target.TryGet(out var enemy)) return;

            FoamManager.ReceiveSyncFoam(enemy.Damage, packet.amount.Get(MaxFoam), packet.time.Get(MaxTime));
        }

        public static void FoamActivateSync(EnemyAgent enemy, float amountRel, float timeRel, byte animIndex, bool fromHibernate)
        {
            // Only master should send syncs.
            if (!SNet.IsMaster) return;

            FoamSyncData syncData = default;
            syncData.target.Set(enemy);
            syncData.amount.Set(amountRel, MaxFoam);
            syncData.time.Set(timeRel, MaxTime);

            FoamActivateSyncData data = default;
            data.syncData = syncData;
            data.animIndex = animIndex;
            data.fromHibernate = fromHibernate;
            _activateSync.Send(data);
        }

        internal static void Internal_ReceiveFoamActivateSync(FoamActivateSyncData packet)
        {
            if (!packet.syncData.target.TryGet(out var enemy)) return;

            FoamManager.ReceiveActivateSyncFoam(enemy.Damage, packet.syncData.amount.Get(MaxFoam), packet.syncData.time.Get(MaxTime), packet.animIndex, packet.fromHibernate);
        }

        public static void FoamBubbleSync(PlayerAgent owner, uint syncID, float volumeMod, Foam property)
        {
            // Only master should send syncs.
            if (!SNet.IsMaster) return;

            FoamBubbleSyncData data = new() { syncID = syncID, propertyID = property.SyncPropertyID };
            data.source.Set(owner);
            data.volumeMod.Set(volumeMod, MaxVolumeMod);
            _bubbleSync.Send(data);
        }

        internal static void Internal_ReceiveBubbleSync(FoamBubbleSyncData packet)
        {
            // Set the properties on the bubble. Passing in syncID won't create a new bubble if it's already instanced.
            TrySpawnNewFoam(packet.source, packet.volumeMod, packet.propertyID, out _, packet.syncID);
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

    public struct FoamDirectData
    {
        public pEnemyAgent target;
        public UFloat16 amount;
        public ushort propertyID;
    }

    public struct FoamSyncData
    {
        public pEnemyAgent target;
        public UFloat16 amount;
        public UFloat16 time;
    }

    public struct FoamActivateSyncData
    {
        public FoamSyncData syncData;
        public byte animIndex;
        public bool fromHibernate;
    }

    public struct FoamBubbleSyncData
    {
        public pPlayerAgent source;
        public uint syncID;
        public UFloat16 volumeMod;
        public ushort propertyID;
    }
}
