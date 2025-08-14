using EWC.CustomWeapon.WeaponContext.Contexts;
using System;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public interface ITriggerEvent
    {
        public int GetCallbackID(string callbackName);
    }

    public class TriggerEventHelper
    {
        private bool _isActive = false;
        private readonly Func<string, int>? _callbackMap;
        public TriggerEventHelper(Func<string, int>? callbackMap = null) => _callbackMap = callbackMap;

        public int GetCallbackID(string callbackName)
        {
            _isActive = true;
            callbackName = callbackName.Replace(" ", null).ToLowerInvariant();
            return _callbackMap?.Invoke(callbackName) ?? 0;
        }

        public void Invoke(CustomWeaponComponent cwc, WeaponReferenceContext context)
        {
            if (_isActive)
                cwc.Invoke(context);
        }
    }
}
