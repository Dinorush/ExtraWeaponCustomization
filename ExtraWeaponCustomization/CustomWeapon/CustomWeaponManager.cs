using ExtraWeaponCustomization.JSON;
using ExtraWeaponCustomization.Utils;
using Gear;
using GTFO.API.Utilities;
using MTFO.API;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon
{
    public sealed class CustomWeaponManager
    {
        public static readonly CustomWeaponManager Current = new();

        private readonly Dictionary<string, HashSet<uint>> _fileToIDs = new();
        private readonly Dictionary<uint, CustomWeaponData> _customData = new();
        private readonly List<BulletWeapon> _listenCWs = new();

        private readonly LiveEditListener _liveEditListener;

        public string DEFINITION_PATH { get; private set; }

        public override string ToString()
        {
            return "Printing manager: " + _customData.ToString();
        }

        public void AddCustomWeaponData(CustomWeaponData? data, string file)
        {
            if (data == null) return;

            _fileToIDs[file].Add(data.ArchetypeID);
            _customData[data.ArchetypeID] = data;
        }

        private void FileChanged(LiveEditEventArgs e)
        {
            EWCLogger.Warning($"LiveEdit File Changed: {e.FullPath}");
            LiveEdit.TryReadFileContent(e.FullPath, (content) =>
            {
                ReadFileContent(e.FullPath, content);
                PrintCustomIDs();
            });
        }

        private void FileDeleted(LiveEditEventArgs e)
        {
            EWCLogger.Warning($"LiveEdit File Removed: {e.FullPath}");
            foreach (uint id in _fileToIDs[e.FullPath])
                _customData.Remove(id);

            for (int i = _listenCWs.Count - 1; i >= 0; i--)
            {
                if (_listenCWs[i] != null)
                    _listenCWs[i].GetComponent<CustomWeaponComponent>()?.Clear();
                else
                    _listenCWs.RemoveAt(i);
            }

            _fileToIDs.Remove(e.FullPath);

            PrintCustomIDs();
        }

        private void FileCreated(LiveEditEventArgs e)
        {
            EWCLogger.Warning($"LiveEdit File Created: {e.FullPath}");
            LiveEdit.TryReadFileContent(e.FullPath, (content) =>
            {
                ReadFileContent(e.FullPath, content);
                PrintCustomIDs();
            });
        }

        private void ReadFileContent(string file, string content)
        {
            if (!_fileToIDs.ContainsKey(file))
                _fileToIDs[file] = new HashSet<uint>();

            HashSet<uint> fileIDs = _fileToIDs[file];
            foreach (uint id in fileIDs)
                _customData.Remove(id);
            fileIDs.Clear();

            List<CustomWeaponData?>? dataList = null;
            try
            {
                dataList = EWCJson.Deserialize<List<CustomWeaponData?>>(content);
            }
            catch (JsonException ex)
            {
                EWCLogger.Error("Error parsing custom weapon json " + file);
                EWCLogger.Error(ex.Message);
            }

            if (dataList == null) return;

            foreach (CustomWeaponData? data in dataList)
            {
                if (data != null && _customData.ContainsKey(data.ArchetypeID))
                    EWCLogger.Warning("Duplicate archetype ID " + data.ArchetypeID + " found. Previous name: " + _customData[data.ArchetypeID].Name + ", new name: " + data.Name);
                AddCustomWeaponData(data, file);
            }

            // Re-apply changes to listening CWCs
            for (int i = _listenCWs.Count - 1; i >= 0; i--)
            {
                if (_listenCWs[i] != null)
                {
                    CustomWeaponComponent cwc = _listenCWs[i].GetComponent<CustomWeaponComponent>();
                    cwc?.Clear();

                    CustomWeaponData? data = GetCustomWeaponData(_listenCWs[i].ArchetypeID);
                    if (data != null)
                    {
                        if (cwc == null)
                            cwc = _listenCWs[i].gameObject.AddComponent<CustomWeaponComponent>();
                        
                        cwc.Register(data);
                    }
                }
                else
                    _listenCWs.RemoveAt(i);
            }
        }

        public CustomWeaponData? GetCustomWeaponData(uint ArchetypeID) => _customData.ContainsKey(ArchetypeID) ? _customData[ArchetypeID] : null;

        public void AddWeaponListener(BulletWeapon weapon)
        {
            // Prevent duplicates (not using IL2CPP list so don't trust Contains)
            if (_listenCWs.Any(listener => listener.GetInstanceID() == weapon.GetInstanceID())) return;

            _listenCWs.Add(weapon);
        }

        internal void ResetCWCs()
        {
            // Resets CWCs by removing and re-adding all custom data.
            // Not as efficient as implementing a reset function on each property,
            // but that's a pain and this isn't gonna run often.
            for (int i = _listenCWs.Count - 1; i >= 0; i--)
            {
                if (_listenCWs[i] != null)
                {
                    CustomWeaponComponent cwc = _listenCWs[i].GetComponent<CustomWeaponComponent>();
                    cwc?.Clear();

                    CustomWeaponData? data = GetCustomWeaponData(_listenCWs[i].ArchetypeID);
                    if (data != null)
                    {
                        if (cwc == null)
                            cwc = _listenCWs[i].gameObject.AddComponent<CustomWeaponComponent>();

                        cwc.Register(data);
                    }
                }
                else
                    _listenCWs.RemoveAt(i);
            }
        }

        private void PrintCustomIDs()
        {
            StringBuilder builder = new("Found custom blocks for archetype IDs: ");
            builder.AppendJoin(", ", _customData.Keys.ToImmutableSortedSet());
            EWCLogger.Log(builder.ToString());
        }

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
                ReadFileContent(confFile, content);
            }
            PrintCustomIDs();

            _liveEditListener = LiveEdit.CreateListener(DEFINITION_PATH, "*.json", true);
            _liveEditListener.FileCreated += FileCreated;
            _liveEditListener.FileChanged += FileChanged;
            _liveEditListener.FileDeleted += FileDeleted;
        }
    }
}
