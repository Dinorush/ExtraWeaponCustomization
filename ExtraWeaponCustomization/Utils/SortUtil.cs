using UnityEngine;

namespace ExtraWeaponCustomization.Utils
{
    internal static class SortUtil
    {
        public static int RaycastDistance(RaycastHit x, RaycastHit y)
        {
            if (x.distance == y.distance) return 0;
            return x.distance < y.distance ? -1 : 1;
        }
    }
}
