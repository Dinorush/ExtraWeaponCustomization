using Agents;
using System;

namespace EWC.CustomWeapon.ObjectWrappers
{
    internal class AgentWrapper : KeyWrapper
    {
        // Used so a new wrapper need not be created when looking for an existing one.
        // Should ONLY be used by first calling SetAgent (i.e. assume its current state is garbage)
        public static readonly AgentWrapper SharedInstance = new(null, IntPtr.Zero);
        public Agent? Agent { get; private set; }

        public AgentWrapper(Agent? agent, IntPtr ptr = default) : base(ptr)
        {
            if (ptr == IntPtr.Zero && agent != null)
                Pointer = agent.Pointer;
            Agent = agent;
        }

        public AgentWrapper(AgentWrapper wrapper) : base(wrapper.Pointer)
        {
            Agent = wrapper.Agent;
        }

        public void SetAgent(Agent agent)
        {
            Pointer = agent.Pointer;
            Agent = agent;
        }
    }
}
