using Agents;

namespace EWC.CustomWeapon.ObjectWrappers
{
    internal class AgentWrapper : KeyWrapper
    {
        // Used so a new wrapper need not be created when looking for an existing one.
        // Should ONLY be used by first calling SetAgent (i.e. assume its current state is garbage)
        public static readonly AgentWrapper SharedInstance = new(null, 0);
        public Agent? Agent { get; private set; }

        public AgentWrapper(Agent? agent, int iD = 0) : base(iD)
        {
            if(iD == 0) ID = agent?.GetInstanceID() ?? 0;
            Agent = agent;
        }

        public AgentWrapper(AgentWrapper wrapper) : base(wrapper.ID)
        {
            Agent = wrapper.Agent;
        }

        public void SetAgent(Agent agent)
        {
            ID = agent.GetInstanceID();
            Agent = agent;
        }
    }
}
