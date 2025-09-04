using EWC.Utils;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class ResetHolder : TriggerHolder
    {
        public float ApplyDelay { get; private set; } = 0f;

        private DelayedCallback? _delayedApply;

        public ResetHolder(TriggerCoordinator parent, params ITrigger[] triggers) : base(parent, triggers) { }

        public override TriggerHolder Clone(TriggerCoordinator parent)
        {
            var copy = (ResetHolder) base.Clone(parent);
            copy._delayedApply = _delayedApply != null ? new(ApplyDelay, copy.ApplyReset) : null;
            return copy;
        }

        public void ResetTriggers(bool resetAccumulated = true)
        {
            if (ApplyDelay > 0f)
                StartDelayedCallback(_delayedApply!, checkEnd: true, refresh: false);
            else
                DoReset(resetAccumulated);
        }

        public void DoReset(bool resetAccumulated = true)
        {
            Caller?.TriggerReset();
            Parent.Activate.Reset(resetAccumulated);
            Reset(resetAccumulated);
        }

        public void ApplyReset() => DoReset(true);

        protected override void OnCancel()
        {
            if (CancelDelay && ApplyDelay > 0)
                _delayedApply!.Cancel();

            base.OnCancel();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "applydelay":
                case "delay":
                    ApplyDelay = reader.GetSingle();
                    if (ApplyDelay > 0f)
                        _delayedApply = new(ApplyDelay, ApplyReset);
                    break;
            }
        }
    }
}
