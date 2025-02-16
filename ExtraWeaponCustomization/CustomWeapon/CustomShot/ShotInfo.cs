using EWC.CustomWeapon.Enums;
using System.Collections.Generic;
using System.Linq;

namespace EWC.CustomWeapon.CustomShot
{
    public sealed class ShotInfo
    {
        public uint ID { get; private set; }
        private readonly List<DamageType> _hits = new(5);
        public uint Hits => (uint)_hits.Count;
        public uint TypeHits(DamageType type) => (uint)_hits.Count(hitType => hitType.HasFlag(type));
        public uint TypeHits(DamageType[] types) => (uint)_hits.Count(hitType => hitType.HasFlagIn(types));
        public uint TypeHits(DamageType[] types, DamageType blacklist) => (uint)_hits.Count(hitType => hitType.HasFlagIn(types) && !hitType.HasAnyFlag(blacklist));
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

        public ShotInfo() : this(ShotManager.NextID) { }
        public ShotInfo(ShotInfo copy) : this(copy.ID)
        {
            _hits = new(copy._hits);
        }
        public ShotInfo(uint id)
        {
            ID = id;
        }

        public void Reset() => Reset(ShotManager.NextID);
        public void Reset(uint id)
        {
            ID = id;
            _hits.Clear();
        }

        public void AddHit(DamageType type) => _hits.Add(type);
        public void AddHits(DamageType type, int hits)
        {
            for (int i = 0; i < hits; i++)
                _hits.Add(type);
        }

        public readonly struct Const
        {
            public readonly uint ID;
            private readonly DamageType[] _hits;
            public readonly uint Hits;
            public uint TypeHits(DamageType type) => (uint)_hits.Count(hitType => hitType.HasFlag(type));
            public uint TypeHits(DamageType[] types) => (uint)_hits.Count(hitType => hitType.HasFlagIn(types));
            public uint TypeHits(DamageType[] types, DamageType blacklist) => (uint)_hits.Count(hitType => hitType.HasFlagIn(types) && !hitType.HasAnyFlag(blacklist));

            public Const(ShotInfo info)
            {
                ID = info.ID;
                _hits = info._hits.ToArray();
                Hits = (uint)_hits.Length;
            }
        }

        public static implicit operator Const(ShotInfo info) => info.State;
        public bool Equals(Const state) => ID == state.ID && Hits == state.Hits;
    }
}
