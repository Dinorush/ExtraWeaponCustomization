using EWC.CustomWeapon.Enums;
using GameData;
using Gear;
using Player;

namespace EWC.CustomWeapon.ComponentWrapper.WeaponComps
{
    public abstract class BulletWeaponComp<T> : GunComp<T>, IBulletWeaponComp where T : BulletWeapon
    {
        private WeaponAudioDataBlock _audioData;
        private ArchetypeDataBlock _archetypeData;

        public BulletWeaponComp(T value, bool isShotgun) : base(value, isShotgun, value.ArchetypeData.FireMode)
        {
            _archetypeData = Value.ArchetypeData;
            _audioData = Value.AudioData;
        }

        public override ArchetypeDataBlock ArchetypeData
        {
            get => _archetypeData;
            set
            {
                _archetypeData = value;
                Value.ArchetypeData = value;
                FireMode = value.FireMode;
            }
        }

        public override WeaponAudioDataBlock AudioData
        {
            get => _audioData;
            set
            {
                _audioData = value;
                Value.AudioData = value;
                Value.SetupAudioEvents();
            }
        }

        public override Weapon.WeaponHitData VanillaHitData
        {
            get => Weapon.s_weaponRayData;
            set => Weapon.s_weaponRayData = value;
        }

        public override float MaxRayDist
        {
            get => Value.MaxRayDist;
            set => Value.MaxRayDist = value;
        }

        public BulletWeapon BulletWeapon => Value;
        public override WeaponType Type => WeaponType.BulletWeapon | WeaponType.Gun;
        public override AmmoType AmmoType => Value.AmmoType;
        public override CellSoundPlayer Sound => Value.Sound;

        public override int GetCurrentClip() => Value.GetCurrentClip();
        public override int GetMaxClip() => Value.GetMaxClip();
        public override void SetCurrentClip(int clip) => Value.SetCurrentClip(clip);
        public override bool AllowBackstab => true;
    }

    public interface IBulletWeaponComp : IGunComp
    {
        public BulletWeapon BulletWeapon { get; }
    }
}
