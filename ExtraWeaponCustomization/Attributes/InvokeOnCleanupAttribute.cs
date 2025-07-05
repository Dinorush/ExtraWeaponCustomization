using System;

namespace EWC.Attributes
{
    // Shamelessly stolen from Flow because I like this
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class InvokeOnCleanupAttribute : Attribute
    {
        public bool OnCheckpoint { get; set; }
        public InvokeOnCleanupAttribute(bool onCheckpoint = false)
        {
            OnCheckpoint = onCheckpoint;
        }
    }
}
