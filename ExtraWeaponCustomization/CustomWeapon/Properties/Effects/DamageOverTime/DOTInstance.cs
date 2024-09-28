using System;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class DOTInstance
    {
        public DamageOverTime DotBase { get; }

        public float NextTickTime { get { return _lastTickTime + TickDelay; } }
        private float _lastTickTime = 0f;
        private int _ticks = 0;
        private float _damagePerTick = 0f;
        private float _precisionMulti = 1f;
        private float _backstabMulti = 1f;
        private float _falloff = 1f;

        public DOTInstance(float totalDamage, float falloff, float precision, float backstab, DamageOverTime dotBase)
        {
            DotBase = dotBase;
            _precisionMulti = precision;
            _backstabMulti = backstab;
            _falloff = falloff;
            _lastTickTime = Clock.Time;
            _ticks = (int)(dotBase.Duration * dotBase.TickRate);
            AddInstance(totalDamage);
        }

        private float TickDelay => 1f / DotBase.TickRate;
        public bool Expired => _ticks <= 0;
        public bool CanTick => Clock.Time - _lastTickTime >= TickDelay;
        public bool Started => _ticks < (int)(DotBase.Duration * DotBase.TickRate);

        public bool CanAddInstance(DamageOverTime dotBase)
        {
            // Technically, shotgun pellets may have different backstab bonuses... but it's too much effort to fix
            return DotBase.Stacks && DotBase == dotBase && !Started;
        }

        // This should only be called before the first damage tick.
        public void AddInstance(float totalDamage)
        {
            _damagePerTick += totalDamage / _ticks;
        }

        public void StartWithTargetTime(float nextTickTime)
        {
            _lastTickTime = nextTickTime - TickDelay;
        }

        public void Destroy()
        {
            _ticks = 0;
        }

        public void DoDamage(IDamageable damageable)
        {
            if (!CanTick || Expired) return;

            float damage = 0f;
            int damageTicks = Math.Min(_ticks, (int) ((Clock.Time - _lastTickTime) * DotBase.TickRate));
            damage += damageTicks * _damagePerTick;
            _lastTickTime += damageTicks * TickDelay;
            _ticks -= damageTicks;

            DOTDamageManager.DoDOTDamage(damageable, damage, _falloff, _precisionMulti, _backstabMulti, DotBase);
        }
    }
}
