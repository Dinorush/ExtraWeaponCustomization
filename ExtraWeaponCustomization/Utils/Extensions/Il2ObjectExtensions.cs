using System.Diagnostics.CodeAnalysis;

namespace EWC.Utils.Extensions
{
    internal static class Il2ObjectExtensions
    {
        public static bool TryCastOut<T>(this Il2CppSystem.Object obj, [MaybeNullWhen(false)] out T result) where T : Il2CppSystem.Object
        {
            result = obj.TryCast<T>();
            return result != null;
        }

        public static bool TryGetComp<T>(this UnityEngine.GameObject obj, [MaybeNullWhen(false)] out T result)
        {
            result = obj.GetComponent<T>();
            return result != null;
        }

        public static bool TryGetComp<T>(this UnityEngine.Component obj, [MaybeNullWhen(false)] out T result)
        {
            result = obj.GetComponent<T>();
            return result != null;
        }
    }
}
