using System;

namespace EWC.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class InvokeOnCheckpointReloadedAttribute : Attribute
    {
        public InvokeOnCheckpointReloadedAttribute() { }
    }
}
