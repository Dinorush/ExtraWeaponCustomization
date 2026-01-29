using EWC.CustomWeapon;
using System.IO;

namespace EWC.API
{
    public static class BlockLoaderAPI
    {
        public static void LoadCustomData(string filePath)
        {
            if (filePath != null)
            {
                EWCLogger.Log("Loading file with path: " + filePath);
                string content = File.ReadAllText(filePath);
                CustomDataManager.Current.ReadFileContent(filePath, content);
            }
        }
    }
}
