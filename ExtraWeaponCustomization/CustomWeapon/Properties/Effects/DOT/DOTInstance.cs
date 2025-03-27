using EWC.CustomWeapon.CustomShot;
using System;

namespace EWC.CustomWeapon.Properties.Effects.Hit.DOT
{
    public sealed class DOTInstance
    {
        public DamageOverTime DotBase { get; }

        private int _tick = 0;
        private float _damagePerTick = 0f;
        private float _lastTickTime = 0f;

        private readonly int _totalTicks;
        private readonly float _totalDamage;
        private readonly float _precisionMulti;
        private readonly float _staggerMulti;
        private readonly bool _bypassTumor;
        private readonly float _backstabMulti;
        private readonly float _falloff;
        private readonly float _tickDelay;
        private readonly ShotInfo _shotInfo;

        // Stored math constants
        private readonly float _expoPlusOne;
        private readonly double _expoModifier;
        private readonly double _expoDivisor;

        public DOTInstance(float totalDamage, float falloff, float precision, float stagger, bool bypassTumor, float backstab, ShotInfo info, DamageOverTime dotBase)
        {
            DotBase = dotBase;
            _totalDamage = totalDamage;
            _precisionMulti = precision;
            _staggerMulti = stagger;
            _bypassTumor = bypassTumor;
            _backstabMulti = backstab;
            _falloff = falloff;
            _tickDelay = 1f / dotBase.TickRate;
            _lastTickTime = Clock.Time;
            _totalTicks = (int)(dotBase.Duration * dotBase.TickRate);
            _shotInfo = info;

            _expoPlusOne = dotBase.Exponent + 1;
            _expoModifier = _expoPlusOne / ((dotBase.EndDamageFrac * dotBase.Exponent + 1) * _totalTicks);
            _expoDivisor = _expoPlusOne * Math.Pow(_totalTicks, dotBase.Exponent);
            AddInstance(this);
        }

        public bool Expired => _tick >= _totalTicks;
        public bool CanTick => Clock.Time - _lastTickTime >= _tickDelay;
        public float NextTickTime => _lastTickTime + _tickDelay;
        public bool Started => _tick != 0;

        public bool CanAddInstance(DamageOverTime dotBase)
        {
            // Technically, shotgun pellets may have different backstab bonuses... but it's too much effort to fix
            return DotBase.BatchStacks && DotBase.StackLimit == 0 && DotBase == dotBase && !Started;
        }

        // This should only be called before the first damage tick.
        public void AddInstance(DOTInstance instance)
        {
            _damagePerTick += (float)(instance._totalDamage * _expoModifier);
        }

        public void StartWithTargetTime(float nextTickTime)
        {
            _lastTickTime = nextTickTime - _tickDelay;
        }

        public void Destroy()
        {
            _tick = _totalTicks;
        }

        private double ComputeDamageMod(int tick)
        {
            return DotBase.EndDamageFrac * tick + (DotBase.EndDamageFrac - 1) * Math.Pow(_totalTicks - tick, _expoPlusOne) / _expoDivisor;
        }

        public void DoDamage(IDamageable damageable)
        {
            if (!CanTick || Expired) return;

            int damageTicks = Math.Min(_totalTicks - _tick, (int)((Clock.Time - _lastTickTime) * DotBase.TickRate));
            float damage;
            if (DotBase.EndDamageFrac == 1f)
                damage = _damagePerTick * damageTicks;
            else
                damage = (float)(_damagePerTick * (ComputeDamageMod(_tick + damageTicks) - ComputeDamageMod(_tick)));

            _lastTickTime += damageTicks * _tickDelay;
            _tick += damageTicks;

            DOTDamageManager.DoDOTDamage(damageable, damage, _falloff, _precisionMulti, _staggerMulti, _bypassTumor, _backstabMulti, damageTicks, _shotInfo, DotBase);
        }
    }
}
