namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext
{
    // For simple contexts that hold no data (only their type matters)
    public static class StaticContext<T> where T : IWeaponContext, new()
    {
        public readonly static T Instance = new();
    }
}
