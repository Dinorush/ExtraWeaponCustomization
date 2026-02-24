using EWC.CustomWeapon.WeaponContext.Attributes;
using UnityEngine;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(Enums.OwnerType.Local)]
    public sealed class WeaponKeyContext : WeaponTriggerContext
    {
        public KeyCode Key { get; }
        public bool IsDown { get; }

        public WeaponKeyContext(KeyCode key, bool isDown) : base()
        {
            Key = key;
            IsDown = isDown;
        }
    }
}
