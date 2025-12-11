using AIGraph;
using EWC.CustomWeapon.Enums;
using EWC.Utils.Extensions;
using Player;
using UnityEngine;

namespace EWC.CustomWeapon.ComponentWrapper
{
    public abstract class OwnerComp<T> : IOwnerComp where T : Il2CppSystem.Object
    {
        public readonly T Value;

        public OwnerComp(T value, Transform muzzleAlign)
        {
            Value = value;
            MuzzleAlign = muzzleAlign;
        }

        public Transform MuzzleAlign { get; }
        public eDimensionIndex DimensionIndex => Player.DimensionIndex;
        public abstract PlayerAgent? Player { get; }
        public abstract OwnerType Type { get; }
        public abstract AIG_CourseNode CourseNode { get; }
        public abstract Vector3 FirePos { get; }
        public abstract Vector3 FireDir { get; }
    }

    public interface IOwnerComp
    {
        public Transform MuzzleAlign { get; }
        public PlayerAgent? Player { get; }
        public OwnerType Type { get; }
        public bool IsType(OwnerType type) => Type.HasFlag(type);
        public bool IsAnyType(OwnerType type) => Type.HasAnyFlag(type);
        public AIG_CourseNode CourseNode { get; }
        public Vector3 FirePos { get; }
        public Vector3 FireDir { get; }
        public eDimensionIndex DimensionIndex { get; }
    }
}
