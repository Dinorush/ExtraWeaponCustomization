using Enemies;
using System.Collections.Generic;
using UnityEngine;

namespace EWC.Utils
{
    internal static class SortUtil
    {
        private static List<(RaycastHit hit, float distance)> s_limbCache = new();

        public static int Rayhit(RaycastHit x, RaycastHit y)
        {
            if (x.distance == y.distance) return 0;
            return x.distance < y.distance ? -1 : 1;
        }

        public static int EnemyRayhit((EnemyAgent, RaycastHit hit) x, (EnemyAgent, RaycastHit hit) y)
        {
            if (x.hit.distance == y.hit.distance) return 0;
            return x.hit.distance < y.hit.distance ? -1 : 1;
        }

        public static void SortWithWeakspotBuffer(IList<RaycastHit> list)
        {
            s_limbCache.Clear();
            foreach (RaycastHit hit in list)
            {
                bool weakspot = DamageableUtil.GetDamageableFromRayHit(hit)?.TryCast<Dam_EnemyDamageLimb>()?.m_type == eLimbDamageType.Weakspot;
                s_limbCache.Add((hit, weakspot ? Mathf.Max(hit.distance - SearchUtil.WeakspotBufferDist, 0) : hit.distance));
            }
            s_limbCache.Sort(FloatTuple);
            for (int i = 0; i < list.Count; i++)
                list[i] = s_limbCache[i].hit;
        }

        public static int FloatTuple<T>((T, float distance) x, (T, float distance) y)
        {
            if (x.distance == y.distance) return 0;
            return x.distance < y.distance ? -1 : 1;
        }
    }
}
