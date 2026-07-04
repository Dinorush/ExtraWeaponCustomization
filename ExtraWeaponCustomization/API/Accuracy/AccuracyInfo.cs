using Player;

namespace EWC.API.Accuracy
{
    public struct WeaponDelta
    {
        public static readonly WeaponDelta Zero = new();

        public StatDelta Shots;
        public StatDelta FullShots;
        public StatDelta Groups;

        public WeaponDelta()
        {
            Shots = StatDelta.Zero;
            FullShots = StatDelta.Zero;
            Groups = StatDelta.Zero;
        }

        public WeaponDelta(StatDelta shots, StatDelta fullShots, StatDelta groups)
        {
            Shots = shots;
            FullShots = fullShots;
            Groups = groups;
        }

        public static WeaponDelta operator +(WeaponDelta left, WeaponDelta right) =>
            new(
                left.Shots + right.Shots,
                left.FullShots + right.FullShots,
                left.Groups + right.Groups);
    }

    public struct StatDelta
    {
        public static readonly StatDelta Zero = new();

        public int Hits;
        public int Crits;
        public int Count;

        public StatDelta()
        {
            Hits = 0;
            Crits = 0;
            Count = 0;
        }

        public StatDelta(int hits, int crits, int count)
        {
            Hits = hits;
            Crits = crits;
            Count = count;
        }

        public static StatDelta operator +(StatDelta left, StatDelta right) =>
            new(
                left.Hits + right.Hits,
                left.Crits + right.Crits,
                left.Count + right.Count);
    }

    public class WeaponAccuracy
    {
        public readonly StatInfo Shots;
        public readonly StatInfo FullShots;
        public readonly StatInfo Groups;
        public readonly InventorySlot Slot;
        public readonly AccuracyStats ParentStats;

        public WeaponAccuracy(InventorySlot slot, AccuracyStats parent)
        {
            Shots = new(this);
            FullShots = new(this);
            Groups = new(this);
            Slot = slot;
            ParentStats = parent;
        }

        public void Add(WeaponDelta delta)
        {
            Shots.Add(delta.Shots);
            FullShots.Add(delta.FullShots);
            Groups.Add(delta.Groups);
        }
    }

    public class StatInfo
    {
        public int Hits { get; private set; }
        public int Crits { get; private set; }
        public int Count { get; private set; }
        public readonly WeaponAccuracy ParentWeapon;

        public StatInfo(WeaponAccuracy parent)
        {
            ParentWeapon = parent;
        }

        public void Add(StatDelta delta)
        {
            Hits += delta.Hits;
            Crits += delta.Crits;
            Count += delta.Count;
        }
    }
}
