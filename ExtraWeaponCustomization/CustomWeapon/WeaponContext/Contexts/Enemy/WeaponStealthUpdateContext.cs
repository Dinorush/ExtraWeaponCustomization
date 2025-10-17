using Enemies;
using EWC.CustomWeapon.WeaponContext.Attributes;
using System;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(Enums.OwnerType.Any)]
    public sealed class WeaponStealthUpdateContext : IWeaponContext
    {
        public EnemyAgent Enemy { get; }
        public bool Detecting { get; }
        private float _output;
        public float Output { get => _output; set => _output = Math.Max(_output, value); }

        public WeaponStealthUpdateContext(EnemyAgent enemy, bool detecting, float output)
        { 
            Enemy = enemy;
            Detecting = detecting;
            _output = output;
        }
    }
}
