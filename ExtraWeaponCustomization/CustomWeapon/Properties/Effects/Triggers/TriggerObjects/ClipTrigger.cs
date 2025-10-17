using EWC.CustomWeapon.ComponentWrapper.WeaponComps;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Utils.Extensions;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class ClipTrigger : ITrigger
    {
        public TriggerName Name { get; } = TriggerName.Clip;
        public float Amount { get; private set; } = 1f;
        public float AmountAtMin { get; private set; } = 1f;
        public float AmountAtMax { get; private set; } = 1f;
        public int ClipMin { get; private set; } = 0;
        public int ClipMax { get; private set; } = 1;
        public float ClipMinRel { get; private set; } = -1;
        public float ClipMaxRel { get; private set; } = -1;
        public float Exponent { get; private set; } = 1f;
        public bool FlipExponent { get; private set; } = false;

        public ClipTrigger() {}

        public bool StoreZeroAmount => true;

        public bool Invoke(WeaponTriggerContext context, out float amount)
        {
            if (context is WeaponAmmoContext fireContext)
            {
                amount = ClipMaxRel >= 0 ? CalculateAmount(fireContext.ClipRel) : CalculateAmount(fireContext.Clip);
                return true;
            }
            else if (context is WeaponInitContext initContext)
            {
                var gun = (IGunComp)initContext.Weapon;
                amount = ClipMaxRel >= 0 ? CalculateAmount((float)gun.GetCurrentClip() / gun.GetMaxClip()) : CalculateAmount(gun.GetCurrentClip());
                return true;
            }
            amount = 0f;
            return false;
        }

        private float CalculateAmount(int ammo) => CalculateAmount(ammo, ClipMin, ClipMax);
        private float CalculateAmount(float ammoRel) => CalculateAmount(ammoRel, ClipMinRel, ClipMaxRel);
        private float CalculateAmount(float val, float min, float max)
        {
            if (FlipExponent)
                return Amount * val.MapInverted(min, max, AmountAtMin, AmountAtMax, Exponent);
            else
                return Amount * val.Map(min, max, AmountAtMin, AmountAtMax, Exponent);
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
                case "triggeramountatmin":
                case "amountatmin":
                    AmountAtMin = reader.GetSingle();
                    break;
                case "triggeramountatmax":
                case "amountatmax":
                    AmountAtMax = reader.GetSingle();
                    break;
                case "clipmin":
                    ClipMin = reader.GetInt32();
                    break;
                case "clipminrel":
                    ClipMinRel = reader.GetSingle();
                    break;
                case "clipmax":
                    ClipMax = reader.GetInt32();
                    break;
                case "clipmaxrel":
                    ClipMaxRel = reader.GetSingle();
                    break;
                case "exponent":
                    Exponent = reader.GetSingle();
                    break;
                case "flipexponent":
                case "flip":
                    FlipExponent = reader.GetBoolean();
                    break;
            }
        }
    }
}
