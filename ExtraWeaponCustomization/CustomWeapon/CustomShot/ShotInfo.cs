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
        private readonly List<DamageType> _hits;
        public uint Hits => (uint)_hits.Count;
        public uint TypeHits(DamageType type) => (uint)_hits.Count(hitType => hitType.HasFlag(type));
        public uint TypeHits(DamageType[] types) => (uint)_hits.Count(hitType => hitType.HasFlagIn(types));
        public uint TypeHits(DamageType[] types, DamageType blacklist) => (uint)_hits.Count(hitType => hitType.HasFlagIn(types) && !hitType.HasAnyFlag(blacklist));

        public float OrigDamage { get; private set; }
        public float OrigPrecision { get; private set; }
        public float OrigStagger { get; private set; }

        public ShotInfoMod Mod { get; set; }
        public ShotInfoMod GroupMod { get; set; }

        private Const _state;
        public Const State
        {
            get
            {
                if (!Equals(_state))
                    _state = new Const(this);
                return _state;
            }
        }

        public ShotInfo() : this(0, 0, 0) { }
        public ShotInfo(float origDamage, float origPrecision, float origStagger)
        {
            ID = ShotManager.NextID;
            _hits = new(5);

            Mod = new();
            GroupMod = ShotManager.CurrentGroupMod;
            OrigDamage = origDamage;
            OrigPrecision = origPrecision;
            OrigStagger = origStagger;
            _state = new(this);
        }

        public ShotInfo(ShotInfo copy, bool modOnly = false, bool useParentMod = true)
        {
            PullMods(copy, useParentMod);

            if (modOnly)
            {
                ID = ShotManager.NextID;
                _hits = new(5);
                OrigDamage = 0;
                OrigPrecision = 0;
                OrigStagger = 0;
                _state = new(this);
            }
            else
            {
                ID = copy.ID;
                _hits = new(copy._hits);
                OrigDamage = copy.OrigDamage;
                OrigPrecision = copy.OrigPrecision;
                OrigStagger = copy.OrigStagger;
                _state = new(this);
            }
        }

        public void Reset(float origDamage, float origPrecision, float origStagger)
        {
            OrigDamage = origDamage;
            OrigPrecision = origPrecision;
            OrigStagger = origStagger;

            GroupMod = ShotManager.CurrentGroupMod;
            ID = ShotManager.NextID;
            _hits.Clear();
        }

        public void NewShot(CustomWeaponComponent cwc)
        {
            Mod = new();
            cwc.Invoke(new WeaponShotInitContext(Mod));
            ShotManager.AdvanceGroupModIfOld(cwc);
            GroupMod = ShotManager.CurrentGroupMod;
        }

        public void AddHit(DamageType type) => _hits.Add(type);
        public void AddHits(DamageType type, int hits)
        {
            for (int i = 0; i < hits; i++)
                _hits.Add(type);
        }

        [MemberNotNull(nameof(Mod))]
        [MemberNotNull(nameof(GroupMod))]
        public void PullMods(ShotInfo info, bool useOriginal = true)
        {
            Mod = useOriginal ? info.Mod : new(info.Mod);
            GroupMod = useOriginal ? info.GroupMod : new(info.GroupMod);
        }

        // Snapshot of ShotInfo to capture its current state
        // in case the object changes in the future.
        public class Const
        {
            public readonly ShotInfo Orig;
            public readonly uint ID;
            private readonly DamageType[] _hits;
            public readonly uint Hits;
            public uint TypeHits(DamageType type) => (uint)_hits.Count(hitType => hitType.HasFlag(type));
            public uint TypeHits(DamageType[] types) => (uint)_hits.Count(hitType => hitType.HasFlagIn(types));
            public uint TypeHits(DamageType[] types, DamageType blacklist) => (uint)_hits.Count(hitType => hitType.HasFlagIn(types) && !hitType.HasAnyFlag(blacklist));

            public Const(ShotInfo info)
            {
                Orig = info;
                ID = info.ID;
                _hits = info._hits.ToArray();
                Hits = (uint)_hits.Length;
            }
        }

        public static implicit operator Const(ShotInfo info) => info.State;
        public bool Equals(Const state) => ID == state.ID && Hits == state.Hits;
    }
}
