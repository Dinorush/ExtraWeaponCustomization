using Enemies;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
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
