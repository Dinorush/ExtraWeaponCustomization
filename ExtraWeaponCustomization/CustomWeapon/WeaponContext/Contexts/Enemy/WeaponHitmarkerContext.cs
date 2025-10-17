using Enemies;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Attributes;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(requiredOwnerType: OwnerType.Managed, validOwnerType: OwnerType.Local | OwnerType.Sentry)]
    public sealed class WeaponHitmarkerContext : IWeaponContext
    {
        public bool Result { get; set; } = true;
        public EnemyAgent Enemy { get; }

        public WeaponHitmarkerContext(EnemyAgent enemy)
        {
            Enemy = enemy;
        }
    }
}
