using EWC.CustomWeapon;

namespace EWC.API
{
    public static class DataAPI
    {
        public static void ReadDirectory(string directory, bool liveEdit = true)
        {
            if (string.IsNullOrEmpty(directory)) return;

            CustomDataManager.Current.ReadDirectory(directory, liveEdit);
        }
    }
}
