using ExtraWeaponCustomization.JSON;
using ExtraWeaponCustomization.Utils;
using GTFO.API.Utilities;
using MTFO.API;
using System.Collections.Generic;
using System.IO;

namespace ExtraWeaponCustomization.CustomWeapon
{
    public sealed class CustomWeaponManager
    {
        public static readonly CustomWeaponManager Current = new();

        private readonly Dictionary<uint, CustomWeaponData> customData = new();

        private readonly LiveEditListener liveEditListener;

        public string DEFINITION_PATH { get; private set; }

        public override string ToString()
        {
            return "Printing manager: " + customData.ToString();
        }

        public void AddCustomWeaponData(CustomWeaponData? data)
        {
            if (data == null) return;

            if (customData.ContainsKey(data.ArchetypeID))
                EWCLogger.Warning("Replaced custom weapon behavior for ArchetypeID " + data.ArchetypeID);

            customData[data.ArchetypeID] = data;
        }

        private void FileChanged(LiveEditEventArgs e)
        {
            EWCLogger.Warning($"LiveEdit File Changed: {e.FullPath}");
            LiveEdit.TryReadFileContent(e.FullPath, (content) =>
            {
                List<CustomWeaponData>? dataList = EWCJson.Deserialize<List<CustomWeaponData>>(content);

                if (dataList == null) return;

                foreach (CustomWeaponData data in dataList)
                    AddCustomWeaponData(data);
            });
        }

        public CustomWeaponData? GetCustomWeaponData(uint RecoilID) => customData.ContainsKey(RecoilID) ? customData[RecoilID] : null;
        
        private CustomWeaponManager()
        {
            DEFINITION_PATH = Path.Combine(MTFOPathAPI.CustomPath, EntryPoint.MODNAME);
            if (!Directory.Exists(DEFINITION_PATH))
            {
                Directory.CreateDirectory(DEFINITION_PATH);
                var file = File.CreateText(Path.Combine(DEFINITION_PATH, "Template.json"));
                file.WriteLine(EWCJson.Serialize(new List<CustomWeaponData>() { CustomWeaponTemplate.CreateTemplate() }));
                file.Flush();
                file.Close();
            }

            foreach (string confFile in Directory.EnumerateFiles(DEFINITION_PATH, "*.json", SearchOption.AllDirectories))
            {
                string content = File.ReadAllText(confFile);
                List<CustomWeaponData>? dataList = EWCJson.Deserialize<List<CustomWeaponData>>(content);

                if (dataList == null) continue;

                foreach (CustomWeaponData data in dataList)
                    AddCustomWeaponData(data);
            }

            liveEditListener = LiveEdit.CreateListener(DEFINITION_PATH, "*.json", true);
            liveEditListener.FileChanged += FileChanged;
        }
    }
}
