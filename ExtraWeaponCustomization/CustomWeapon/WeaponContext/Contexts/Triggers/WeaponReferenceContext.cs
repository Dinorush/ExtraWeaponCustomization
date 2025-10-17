namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public class WeaponReferenceContext : WeaponTriggerContext
    {
        public float Mod { get; }
        public uint ID { get; }
        public uint CallbackID { get; }

        public WeaponReferenceContext(uint id, uint callbackID, float mod = 1f) : base()
        {
            Mod = mod;
            ID = id;
            CallbackID = callbackID;
        }
    }
}
