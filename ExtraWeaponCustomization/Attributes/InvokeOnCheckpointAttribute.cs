using System;

namespace EWC.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class InvokeOnCheckpointAttribute : Attribute
    {
        public InvokeOnCheckpointAttribute() { }
    }
}
