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
        public override bool IsShotgun => _isShotgun;
        public abstract WeaponAudioDataBlock AudioData { get; set; }
        public abstract int GetCurrentClip();
        public abstract int GetMaxClip();
        public abstract void SetCurrentClip(int clip);
        public abstract float ModifyFireRate(float lastFireTime, float shotDelay, float burstDelay, float cooldownDelay);
    }

    public interface IGunComp : IAmmoComp
    {
        public WeaponAudioDataBlock AudioData { get; set; }
        public eWeaponFireMode FireMode { get; }
        public float ModifyFireRate(float lastFireTime, float shotDelay, float burstDelay, float cooldownDelay);
    }
}
