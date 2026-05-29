using Il2CppInterop.Runtime.InteropTypes;
using System;
using System.Diagnostics.CodeAnalysis;

namespace EWC.CustomWeapon.ObjectWrappers
{
    public class ObjectWrapper<T> where T : Il2CppObjectBase
    {
        public static readonly ObjectWrapper<T> SharedInstance = new(null, IntPtr.Zero);

        private T? _object;
        private UnityEngine.Object? _instance;
        [MemberNotNullWhen(false, nameof(Object))]
        public bool IsNull => _instance == null;
        public T? Object
        {
            get => _object;
            private set
            { 
                _object = value;
                _instance = value != null ? value.TryCast<UnityEngine.Object>() : null;
            }
        }
        public IntPtr Pointer { get; protected set; }

        public ObjectWrapper(ObjectWrapper<T> wrapper)
        {
            Pointer = wrapper.Pointer;
            _object = wrapper._object;
            _instance = wrapper._instance;
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
