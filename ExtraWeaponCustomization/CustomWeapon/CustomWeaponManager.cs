using ExtraWeaponCustomization.JSON;
using ExtraWeaponCustomization.Utils;
using GTFO.API.Utilities;
using MTFO.API;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon
{
    public sealed class CustomWeaponManager
    {
        public static readonly CustomWeaponManager Current = new();

        private readonly Dictionary<uint, CustomWeaponData> _customData = new();
        private readonly List<CustomWeaponComponent> _listenCWCs = new();

        private readonly LiveEditListener _liveEditListener;

        public string DEFINITION_PATH { get; private set; }

        public override string ToString()
        {
            return "Printing manager: " + _customData.ToString();
        }

        public void AddCustomWeaponData(CustomWeaponData? data)
        {
            if (data == null) return;

            if (_customData.ContainsKey(data.ArchetypeID))
                EWCLogger.Warning("Replaced custom weapon behavior for ArchetypeID " + data.ArchetypeID);

            _customData[data.ArchetypeID] = data;
        }

        private void FileChanged(LiveEditEventArgs e)
        {
            EWCLogger.Warning($"LiveEdit File Changed: {e.FullPath}");
            LiveEdit.TryReadFileContent(e.FullPath, (content) =>
            {
                List<CustomWeaponData>? dataList = null;
                try
                {
                    dataList = EWCJson.Deserialize<List<CustomWeaponData>>(content);
                }
                catch(JsonException ex)
                {
                    EWCLogger.Error("Error parsing custom weapon json " + e.FullPath);
                    EWCLogger.Error(ex.Message);
                }

                if (dataList == null) return;

                foreach (CustomWeaponData data in dataList)
                    AddCustomWeaponData(data);

                // Re-apply changes to listening CWCs
                for (int i = _listenCWCs.Count - 1; i >= 0; i--)
                {
                    if (_listenCWCs[i] != null)
                    {
                        _listenCWCs[i].Clear();
                        CustomWeaponData? data = GetCustomWeaponData(_listenCWCs[i].Weapon.ArchetypeID);
                        if (data != null)
                            _listenCWCs[i].Register(data);
                    }
                    else
                        _listenCWCs.RemoveAt(i);
                }
            });
        }

        public CustomWeaponData? GetCustomWeaponData(uint ArchetypeID) => _customData.ContainsKey(ArchetypeID) ? _customData[ArchetypeID] : null;
        
        public void AddCWCListener(CustomWeaponComponent cwc) => _listenCWCs.Add(cwc);

        private CustomWeaponManager()
        {
            DEFINITION_PATH = Path.Combine(MTFOPathAPI.CustomPath, EntryPoint.MODNAME);
            if (!Directory.Exists(DEFINITION_PATH))
            {
                EWCLogger.Log("No directory detected. Creating " + DEFINITION_PATH + "/Template.json");
                Directory.CreateDirectory(DEFINITION_PATH);
                var file = File.CreateText(Path.Combine(DEFINITION_PATH, "Template.json"));
                file.WriteLine(EWCJson.Serialize(new List<CustomWeaponData>() { CustomWeaponTemplate.CreateTemplate() }));
                file.Flush();
                file.Close();
            }
            else
                EWCLogger.Log("Directory detected. " + DEFINITION_PATH);

            foreach (string confFile in Directory.EnumerateFiles(DEFINITION_PATH, "*.json", SearchOption.AllDirectories))
            {
                string content = File.ReadAllText(confFile);
                List<CustomWeaponData>? dataList = null;
                try
                {
                    dataList = EWCJson.Deserialize<List<CustomWeaponData>>(content);
                }
                catch (JsonException ex)
                {
                    EWCLogger.Error("Error parsing custom weapon json " + confFile);
                    EWCLogger.Error(ex.Message);
                }

                if (dataList == null) continue;

                foreach (CustomWeaponData data in dataList)
                    AddCustomWeaponData(data);
            }

            _liveEditListener = LiveEdit.CreateListener(DEFINITION_PATH, "*.json", true);
            _liveEditListener.FileChanged += FileChanged;
        }
    }
}
