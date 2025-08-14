using System;

namespace EWC.Attributes
{
    // Shamelessly stolen from Flow because I like this
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class InvokeOnBuildDoneAttribute : Attribute
    {
        public InvokeOnBuildDoneAttribute()
        {
        }
    }
}
