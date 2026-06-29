using EWC.CustomWeapon.ComponentWrapper.WeaponComps;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace EWC.CustomWeapon.CustomShot
{
    public sealed class ShotInfo
    {
        public uint ID { get; private set; }
        public uint OriginID { get; private set; }
        public uint GroupID { get; private set; }
        private readonly List<DamageType> _hits;
        public uint Hits => (uint)_hits.Count;
        public uint TypeHits(DamageType type) => (uint)_hits.Count(hitType => hitType.HasFlag(type));
        public uint TypeHits(DamageType[] types) => (uint)_hits.Count(hitType => hitType.HasFlagIn(types));
        public uint TypeHits(DamageType[] types, DamageType blacklist) => (uint)_hits.Count(hitType => hitType.HasFlagIn(types) && !hitType.HasAnyFlag(blacklist));

        public float OrigDamage { get; private set; }
        public float OrigPrecision { get; private set; }
        public float OrigStagger { get; private set; }
        public float ExternalDamageMod { get; private set; }
        public float InnateDamageMod { get; private set; }
        public float InnateStaggerMod { get; private set; }

        public ShotInfoMod Mod { get; private set; }
        public ShotInfoMod GroupMod { get; private set; }
        public CustomWeaponComponent? CWC { get; private set; }

        private bool _isDirty;
        private Const _state;
        public Const State
        {
            get
            {
                if (_isDirty)
                {
                    _state = new Const(this);
                    _isDirty = false;
                }
                return _state;
            }
        }

        public ShotInfo() : this(0, 0, 0) { }
        public ShotInfo(float origDamage, float origPrecision, float origStagger)
        {
            _hits = new(5);
            Mod = new();
            InnateDamageMod = ShotManager.CurrentDamageMod;
            InnateStaggerMod = ShotManager.CurrentStaggerMod;
            ExternalDamageMod = ShotManager.CurrentExternalDamageMod;
            GroupMod = ShotManager.CurrentGroupMod;
            OrigDamage = origDamage * InnateDamageMod;
            OrigPrecision = origPrecision;
            OrigStagger = origStagger * InnateStaggerMod;
            _state = new(this);
        }

        public ShotInfo(CustomWeaponComponent cwc, bool modOnly = false, bool isTagged = false, bool computeAccuracy = true)
        {
            CWC = cwc;
            if (computeAccuracy)
                GetIDs();
            _hits = new(5);

            RefreshMods(isTagged);
            if (!modOnly && !cwc.Weapon.Type.HasFlag(WeaponType.Melee))
            {
                var archData = ((IAmmoComp)cwc.Weapon).ArchetypeData;
                OrigDamage = archData.Damage * InnateDamageMod;
                OrigPrecision = archData.PrecisionDamageMulti;
                OrigStagger = archData.StaggerDamageMulti * InnateStaggerMod;
            }
            else
            {
                OrigDamage = 0;
                OrigPrecision = 0;
                OrigStagger = 0;
            }
            _state = new(this);
        }

        public ShotInfo(ShotInfo copy, bool modOnly = false, bool useParentMod = true)
        {
            CWC = copy.CWC;
            PullMods(copy, useParentMod);
            GetIDs(copy, modOnly);

            if (modOnly)
            {
                _hits = new(5);
                OrigDamage = 0;
                OrigPrecision = 0;
                OrigStagger = 0;
                _state = new(this);
            }
            else
            {
                _hits = new(copy._hits);
                OrigDamage = copy.OrigDamage;
                OrigPrecision = copy.OrigPrecision;
                OrigStagger = copy.OrigStagger;
                _state = new(this);
            }
        }

        public void Reset(float origDamage, float origPrecision, float origStagger, CustomWeaponComponent cwc, ShotInfo? parent = null, bool useParent = true)
        {
            CWC = cwc;
            if (parent != null)
                PullMods(parent, useParent);
            else
                RefreshMods();

            GetIDs(parent);
            OrigDamage = origDamage * InnateDamageMod;
            OrigPrecision = origPrecision * InnateStaggerMod;
            OrigStagger = origStagger;
            _hits.Clear();
            _isDirty = true;
        }

        public void SetToPush(CustomWeaponComponent cwc)
        {
            CWC = cwc;
            InnateDamageMod = 1f;
            InnateStaggerMod = 1f;
            ExternalDamageMod = 1f;
            OrigDamage = 1f;
            OrigPrecision = 1f;
            OrigStagger = 1f;
            (ID, OriginID, GroupID) = (0, 0, 0);
            _hits.Clear();
            _isDirty = true;
        }

        public void AddHit(DamageType type)
        {
            _hits.Add(type);
            _isDirty = true;
        }

        public void AddHits(DamageType type, int hits)
        {
            for (int i = 0; i < hits; i++)
                _hits.Add(type);
            _isDirty = true;
        }

        [MemberNotNull(nameof(Mod))]
        [MemberNotNull(nameof(GroupMod))]
        private void PullMods(ShotInfo info, bool useOriginal = true)
        {
            Mod = useOriginal ? info.Mod : new(info.Mod);
            GroupMod = useOriginal ? info.GroupMod : new(info.GroupMod);
            ExternalDamageMod = info.ExternalDamageMod;
            InnateDamageMod = info.InnateDamageMod;
            InnateStaggerMod = info.InnateStaggerMod;
        }

        [MemberNotNull(nameof(Mod))]
        [MemberNotNull(nameof(GroupMod))]
        private void RefreshMods(bool isTagged = false)
        {
            ShotManager.AdvanceGroupModIfOld(CWC!, isTagged);
            Mod = new();
            GroupMod = ShotManager.CurrentGroupMod;
            InnateDamageMod = ShotManager.CurrentDamageMod;
            InnateStaggerMod = ShotManager.CurrentStaggerMod;
            ExternalDamageMod = ShotManager.CurrentExternalDamageMod;
            CWC!.Invoke(new WeaponShotInitContext(Mod));
        }

        private void GetIDs(ShotInfo? info = null, bool asNew = true)
        {
            if (CWC!.Weapon.IsType(WeaponType.Melee))
                (ID, OriginID, GroupID) = (0, 0, 0);
            else if (info != null)
                (ID, OriginID, GroupID) = ShotManager.PullIDs(CWC.Owner, info, asNew);
            else
                (ID, OriginID, GroupID) = ShotManager.GetIDs(CWC.Owner);
        }

        // Snapshot of ShotInfo to capture its current state
        // in case the object changes in the future.
        public class Const
        {
            public readonly ShotInfo Orig;
            public readonly float ExternalDamageMod;
            public readonly float InnateDamageMod;
            public readonly float InnateStaggerMod;
            public readonly uint ID;
            public readonly uint OriginID;
            public readonly uint GroupID;
            private readonly DamageType[] _hits;
            public readonly uint Hits;
            public uint TypeHits(DamageType type) => (uint)_hits.Count(hitType => hitType.HasFlag(type));
            public uint TypeHits(DamageType[] types) => (uint)_hits.Count(hitType => hitType.HasFlagIn(types));
            public uint TypeHits(DamageType[] types, DamageType blacklist) => (uint)_hits.Count(hitType => hitType.HasFlagIn(types) && !hitType.HasAnyFlag(blacklist));

            public Const(ShotInfo info)
            {
                Orig = info;
                ExternalDamageMod = info.ExternalDamageMod;
                InnateDamageMod = info.InnateDamageMod;
                InnateStaggerMod = info.InnateStaggerMod;
                ID = info.ID;
                OriginID = info.OriginID;
                GroupID = info.GroupID;
                _hits = info._hits.ToArray();
                Hits = (uint)_hits.Length;
            }
        }

        public static implicit operator Const(ShotInfo info) => info.State;
    }
}
