using Enemies;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponStealthUpdateContext : IWeaponContext
    {
        public EnemyAgent Enemy { get; }
        public bool Detecting { get; }
        public float Output { get; set; }

        public WeaponStealthUpdateContext(EnemyAgent enemy, bool detecting, float output)
        { 
            Enemy = enemy;
            Detecting = detecting;
            Output = output;
        }
    }
}
