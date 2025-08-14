using Il2CppInterop.Runtime.InteropTypes;
using System;

namespace EWC.CustomWeapon.ObjectWrappers
{
    public class ObjectWrapper<T> where T : Il2CppObjectBase
    {
        public static readonly ObjectWrapper<T> SharedInstance = new(null, IntPtr.Zero);

        public T? Object { get; private set; }
        public IntPtr Pointer { get; protected set; }

        public ObjectWrapper(ObjectWrapper<T> wrapper)
        {
            Pointer = wrapper.Pointer;
            Object = wrapper.Object;
        }

        public ObjectWrapper(T? obj, IntPtr ptr)
        {
            if (ptr == IntPtr.Zero && obj != null)
                Pointer = obj.Pointer;
            else
                Pointer = ptr;
            Object = obj;
        }

        public ObjectWrapper(T obj)
        {
            Pointer = obj.Pointer;
            Object = obj;
        }

        public virtual ObjectWrapper<T> Set(T obj)
        {
            Pointer = obj.Pointer;
            Object = obj;
            return this;
        }

        public override int GetHashCode()
        {
            return Pointer.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            return obj is ObjectWrapper<T> wrapper && wrapper.Pointer == Pointer;
        }
    }
}
