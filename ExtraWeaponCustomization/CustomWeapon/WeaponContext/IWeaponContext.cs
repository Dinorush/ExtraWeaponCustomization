using Gear;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext
{
    public interface IWeaponContext
    {
        BulletWeapon Weapon { get; }
    }
}
