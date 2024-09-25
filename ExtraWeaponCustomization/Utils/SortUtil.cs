using Enemies;
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

        public static int SearchDistance((EnemyAgent, RaycastHit hit) x, (EnemyAgent, RaycastHit hit) y)
        {
            if (x.hit.distance == y.hit.distance) return 0;
            return x.hit.distance < y.hit.distance ? -1 : 1;
        }
    }
}
