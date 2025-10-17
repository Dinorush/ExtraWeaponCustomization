using GameData;

namespace EWC.CustomWeapon.ComponentWrapper.WeaponComps
{
    public abstract class GunComp<T> : WeaponComp<T>, IGunComp where T : ItemEquippable
    {
        private readonly bool _isShotgun;

        public GunComp(T value, bool isShotgun, eWeaponFireMode fireMode) : base(value)
        {
            _isShotgun = isShotgun;
            FireMode = fireMode;
        }

        public eWeaponFireMode FireMode { get; protected set; }
        public bool IsShotgun => _isShotgun;
        public abstract ArchetypeDataBlock ArchetypeData { get; set; }
        public abstract WeaponAudioDataBlock AudioData { get; set; }
        public abstract Weapon.WeaponHitData VanillaHitData { get; set; }
        public abstract float MaxRayDist { get; set; }
        public abstract bool IsAiming { get; }
        public abstract int GetCurrentClip();
        public abstract int GetMaxClip();
        public abstract void SetCurrentClip(int clip);
        public abstract float ModifyFireRate(float lastFireTime, float shotDelay, float burstDelay, float cooldownDelay);
    }

    public interface IGunComp : IArchComp
    {
        public Weapon.WeaponHitData VanillaHitData { get; set; }
        public WeaponAudioDataBlock AudioData { get; set; }
        public eWeaponFireMode FireMode { get; }
        public float MaxRayDist { get; set; }
        public bool IsShotgun { get; }
        public bool IsAiming { get; }
        public float ModifyFireRate(float lastFireTime, float shotDelay, float burstDelay, float cooldownDelay);
    }
}
