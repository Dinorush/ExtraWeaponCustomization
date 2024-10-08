﻿using EWC.CustomWeapon.Properties;
using System.Collections.Generic;

namespace EWC.CustomWeapon
{
    public class CustomWeaponData
    {
        public const float MaxFireRate = 512f;
        public const float MinShotDelay = 1/MaxFireRate;
        public uint ArchetypeID { get; set; } = 0;
        public uint MeleeArchetypeID { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public List<IWeaponProperty> Properties { get; set; } = new();
    }
}
