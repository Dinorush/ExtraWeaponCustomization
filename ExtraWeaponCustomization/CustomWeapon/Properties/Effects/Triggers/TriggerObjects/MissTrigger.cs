using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Utils.Log;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class MissTrigger : IDamageTypeTrigger
    {
        public TriggerName Name { get; } = TriggerName.Miss;

        public DamageType BaseType { get; }
        public DamageType[] DamageTypes { get; }
        public DamageType BlacklistType { get; set; } = DamageType.Dead;
        public float Amount { get; private set; } = 1f;
        public int RequiredHits { get; private set; } = 1;

        public MissTrigger(params DamageType[] types)
        {
            BaseType = types.GetBaseType();
            DamageTypes = types.Length == 0 ? DamageTypeConst.Any : types;
        }

        public bool Invoke(WeaponTriggerContext context, out float amount)
        {
            if (context is WeaponShotEndContext text)
                EWCLogger.Log($"Miss trigger received ShotEnd context! Miss trigger: {BaseType} base type, [{string.Join(",", DamageTypes)}] types, {BlacklistType} blacklist. ShotEnd: {text.DamageType} type, {text.ShotInfo.Hits} hits, {text.OldInfo?.Hits ?? 0} oldHits, {text.DiffHits()} diffHits, {text.ShotInfo.TypeHits(DamageTypes, BlacklistType)} typeHits, {text.OldInfo?.TypeHits(DamageTypes, BlacklistType) ?? 0} oldTypeHits, {text.DiffTypeHits(DamageTypes, BlacklistType)} diffTypeHits");
            amount = 0f;
            if (context is WeaponShotEndContext shotEnd
                && !shotEnd.DamageType.HasAnyFlag(BlacklistType)
                && shotEnd.DamageType.HasFlag(BaseType)
                && shotEnd.DiffTypeHits(DamageTypes, BlacklistType) < RequiredHits)
            {
                EWCLogger.Log("Passed!");
                amount = Amount;
                return true;
            }
            return false;
        }

        public void Reset() { }

        public ITrigger Clone() => this;

        public void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property)
            {
                case "triggeramount":
                case "amount":
                    Amount = reader.GetSingle();
                    break;
                case "requiredhits":
                    RequiredHits = reader.GetInt32();
                    break;
            }
        }
    }
}
