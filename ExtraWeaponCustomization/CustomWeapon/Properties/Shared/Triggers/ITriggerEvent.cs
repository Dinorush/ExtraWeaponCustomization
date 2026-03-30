using EWC.CustomWeapon.WeaponContext;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System;

namespace EWC.CustomWeapon.Properties.Shared.Triggers
{
    public interface ITriggerEvent
    {
        public uint GetCallbackID(string callbackName);
    }

    public class TriggerEventHelper
    {
        private bool _isActive = false;
        private readonly Func<string, uint>? _callbackMap;
        public TriggerEventHelper(Func<string, uint>? callbackMap = null) => _callbackMap = callbackMap;

        public uint GetCallbackID(string callbackName)
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

        public void Invoke(ContextController cc, WeaponReferenceContext context)
        {
            if (_isActive)
                cc.Invoke(context);
        }
    }
}
