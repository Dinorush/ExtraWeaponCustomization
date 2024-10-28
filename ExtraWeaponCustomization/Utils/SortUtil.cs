using Enemies;
using System.Collections.Generic;
using UnityEngine;

namespace EWC.Utils
{
    internal static class SortUtil
    {
        private static List<(RaycastHit hit, float distance)> s_limbCache = new();
        private static List<(EnemyAgent enemy, float value)> s_enemyTupleCache = new();

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
            foreach (RaycastHit hit in list)
            {
                bool weakspot = DamageableUtil.GetDamageableFromRayHit(hit)?.TryCast<Dam_EnemyDamageLimb>()?.m_type == eLimbDamageType.Weakspot;
                s_limbCache.Add((hit, weakspot ? Mathf.Max(hit.distance - SearchUtil.WeakspotBufferDist, 0) : hit.distance));
            }
            s_limbCache.Sort(FloatTuple);
            CopySortedList(s_limbCache, list);
            s_limbCache.Clear();
        }

        public static void CopySortedList<T>(IList<(T, float)> sortedList, IList<T> list)
        {
            for (int i = 0; i < list.Count; i++)
                list[i] = sortedList[i].Item1!;
        }

        public static void CopySortedList<T>(IList<(T, float, float)> sortedList, IList<T> list)
        {
            for (int i = 0; i < list.Count; i++)
                list[i] = sortedList[i].Item1!;
        }

        public static int FloatTuple<T>((T, float val) x, (T, float val) y)
        {
            if (x.val == y.val) return 0;
            return x.val < y.val ? -1 : 1;
        }

        public static int FloatTriple<T>((T, float val1, float val2) x, (T, float val1, float val2) y)
        {
            if (x.val1 == y.val1)
            {
                if (x.val2 == y.val2) return 0;
                return x.val2 < y.val2 ? -1 : 1;
            }
            return x.val1 < y.val1 ? -1 : 1;
        }
    }
}
