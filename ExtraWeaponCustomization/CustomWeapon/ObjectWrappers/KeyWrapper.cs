namespace ExtraWeaponCustomization.CustomWeapon.ObjectWrappers
{
    internal abstract class KeyWrapper
    {
        public int ID { get; protected set; }

        public KeyWrapper(int id)
        {
            ID = id;
        }

        public override int GetHashCode()
        {
            return ID;
        }

        public override bool Equals(object? obj)
        {
            return obj is KeyWrapper wrapper && wrapper.ID == ID;
        }
    }
}
