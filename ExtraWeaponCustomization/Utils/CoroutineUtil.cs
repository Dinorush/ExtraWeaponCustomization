using UnityEngine;

namespace EWC.Utils
{
    internal static class CoroutineUtil
    {
        public static bool Stop(ref Coroutine? routine, MonoBehaviour? parent = null)
        {
            if (routine != null)
            {
                if (parent == null)
                    CoroutineManager.StopCoroutine(routine);
                else
                    parent.StopCoroutine(routine);
                routine = null;
                return true;
            }
            return false;
        }
    }
}
