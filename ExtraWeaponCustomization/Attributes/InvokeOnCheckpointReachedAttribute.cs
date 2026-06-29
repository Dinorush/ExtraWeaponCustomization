using System;

namespace EWC.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class InvokeOnCheckpointReachedAttribute : Attribute
    {
        public InvokeOnCheckpointReachedAttribute() { }
    }
}
