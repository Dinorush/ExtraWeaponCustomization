using Gear;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPostStartFireContext : IWeaponContext
    {
        public BulletWeapon Weapon { get; }

        public WeaponPostStartFireContext(BulletWeapon weapon)
        {
            Weapon = weapon;
        }
    }
}
