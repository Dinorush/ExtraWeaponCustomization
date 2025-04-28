using EWC.Utils;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class ResetHolder : TriggerHolder
    {
        public ResetHolder(TriggerCoordinator parent, params ITrigger[] triggers) : base(parent, triggers) { }

        public void ResetTriggers(bool resetAccumulated = true)
        {
            if (ApplyDelay > 0f)
            {
                var callback = new DelayedCallback(ApplyDelay, () =>
                {
                    DoReset();
                    _delayedApplies!.Dequeue();
                });
                _delayedApplies!.Enqueue(callback);
                StartDelayedCallback(callback);
            }
            else
                DoReset(resetAccumulated);
        }

        public void DoReset(bool resetAccumulated = true)
        {
            Caller?.TriggerReset();
            Parent.Activate.Reset(resetAccumulated);
            Reset(resetAccumulated);
        }
    }
}
