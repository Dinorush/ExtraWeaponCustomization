using Enemies;
using System;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
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
