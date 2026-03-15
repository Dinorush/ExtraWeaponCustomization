using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Attributes;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(OwnerType.Player)]
    public sealed class WeaponPlayerArmorContext : WeaponStackModContext
    {
        private readonly float _damage;
        public float Damage => Immune ? 0 : (_stackMod.Value > 0 ? _damage / _stackMod.Value : float.PositiveInfinity);
        public PlayerDamageType DamageType { get; }
        public bool Immune { get; set; } = false;

        public WeaponPlayerArmorContext(float damage, PlayerDamageType damageType) : base(1f)
        {
            _damage = damage;
            DamageType = damageType;
        }
    }
}
