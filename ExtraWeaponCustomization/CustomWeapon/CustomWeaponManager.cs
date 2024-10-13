using EWC.JSON;
using EWC.Utils.Log;
using Gear;
using GTFO.API.Utilities;
using MTFO.API;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace EWC.CustomWeapon
{
    public sealed class CustomWeaponManager
    {
        public static readonly CustomWeaponManager Current = new();

        private readonly Dictionary<string, HashSet<uint>> _fileToGuns = new();
        private readonly Dictionary<string, HashSet<uint>> _fileToMelees = new();
        private readonly Dictionary<uint, CustomWeaponData> _customGunData = new();
        private readonly Dictionary<uint, CustomWeaponData> _customMeleeData = new();
        private readonly List<ItemEquippable> _listenCWs = new();

        private readonly LiveEditListener _liveEditListener;

        public string DEFINITION_PATH { get; private set; }

        public override string ToString()
        {
            return "Printing manager: " + _customGunData.ToString();
        }

        public void AddCustomWeaponData(CustomWeaponData? data, string file)
        {
            if (data == null) return;

            if (data.ArchetypeID != 0)
            {
                _fileToGuns[file].Add(data.ArchetypeID);
                _customGunData[data.ArchetypeID] = data;
            }

            if (data.MeleeArchetypeID != 0)
            {
                _fileToMelees[file].Add(data.MeleeArchetypeID);
                _customMeleeData[data.MeleeArchetypeID] = data;
            }
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
            if (!_fileToGuns.ContainsKey(e.FullPath))
            {
                PrintCustomIDs();
                return;
            }

            foreach (uint id in _fileToGuns[e.FullPath])
                _customGunData.Remove(id);
            _fileToGuns.Remove(e.FullPath);

            foreach (uint id in _fileToMelees[e.FullPath])
                _customMeleeData.Remove(id);
            _fileToMelees.Remove(e.FullPath);
            
            ResetCWCs();
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
            if (!_fileToGuns.ContainsKey(file))
            {
                _fileToGuns[file] = new HashSet<uint>();
                _fileToMelees[file] = new HashSet<uint>();
            }

            HashSet<uint> fileIDs = _fileToGuns[file];
            foreach (uint id in fileIDs)
                _customGunData.Remove(id);
            fileIDs.Clear();

            fileIDs = _fileToMelees[file];
            foreach (uint id in fileIDs)
                _customMeleeData.Remove(id);
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
                if (data != null)
                {
                    if (data.ArchetypeID != 0 && _customGunData.ContainsKey(data.ArchetypeID))
                        EWCLogger.Warning("Duplicate archetype ID " + data.ArchetypeID + " found. Previous name: " + _customGunData[data.ArchetypeID].Name + ", new name: " + data.Name);
                    if (data.MeleeArchetypeID != 0 && _customMeleeData.ContainsKey(data.MeleeArchetypeID))
                        EWCLogger.Warning("Duplicate melee archetype ID " + data.MeleeArchetypeID + " found. Previous name: " + _customMeleeData[data.MeleeArchetypeID].Name + ", new name: " + data.Name);
                    
                    AddCustomWeaponData(data, file);
                }
            }

            // Re-apply changes to listening CWCs
            ResetCWCs();
        }

        public CustomWeaponData? GetCustomGunData(uint id) => _customGunData.GetValueOrDefault(id);
        public CustomWeaponData? GetCustomMeleeData(uint id) => _customMeleeData.GetValueOrDefault(id);

        public void AddWeaponListener(ItemEquippable weapon)
        {
            // Prevent duplicates (not using IL2CPP list so don't trust Contains)
            if (_listenCWs.Any(listener => listener.Pointer == weapon.Pointer)) return;

            _listenCWs.Add(weapon);
        }

        internal void ResetCWCs(bool activate = true)
        {
            // Resets CWCs by removing and re-adding all custom data.
            // Not as efficient as implementing a reset function on each property,
            // but that's a pain and this isn't gonna run often.
            for (int i = _listenCWs.Count - 1; i >= 0; i--)
            {
                if (_listenCWs[i] != null)
                {
                    CustomWeaponComponent? cwc = _listenCWs[i].GetComponent<CustomWeaponComponent>();
                    cwc?.Clear();

                    bool isGun = _listenCWs[i].TryCast<BulletWeapon>() != null;
                    CustomWeaponData? data;
                    if (isGun)
                        data = GetCustomGunData(_listenCWs[i].ArchetypeID);
                    else
                        data = GetCustomMeleeData(_listenCWs[i].MeleeArchetypeData.persistentID);

                    if (data != null)
                    {
                        if (cwc == null)
                            cwc = _listenCWs[i].gameObject.AddComponent<CustomWeaponComponent>();

                        if (activate)
                            cwc.Register(data);
                    }
                }
                else
                    _listenCWs.RemoveAt(i);
            }
        }

        internal void ActivateCWCs()
        {
            for (int i = _listenCWs.Count - 1; i >= 0; i--)
            {
                if (_listenCWs[i] != null)
                    _listenCWs[i].GetComponent<CustomWeaponComponent>()?.Register();
                else
                    _listenCWs.RemoveAt(i);
            }
        }

        internal void CreateTemplate()
        {
            string path = Path.Combine(DEFINITION_PATH, "Template.json");
            if (!Directory.Exists(DEFINITION_PATH))
            {
                EWCLogger.Log("No directory detected. Creating " + path);
                Directory.CreateDirectory(DEFINITION_PATH);
            }

            if (File.Exists(path))
            {
                EWCLogger.Error("Can't create template file (already exists)!");
                return;
            }

            var file = File.CreateText(path);
            file.WriteLine(EWCJson.Serialize(new List<CustomWeaponData>() { CustomWeaponTemplate.CreateTemplate() }));
            file.Flush();
            file.Close();
        }

        private void PrintCustomIDs()
        {
            StringBuilder builder = new("Found custom blocks for archetype IDs: ");
            builder.AppendJoin(", ", _customGunData.Keys.ToImmutableSortedSet());
            EWCLogger.Log(builder.ToString());
            builder.Clear();
            builder.Append("Found custom blocks for melee archetype IDs: ");
            builder.AppendJoin(", ", _customMeleeData.Keys.ToImmutableSortedSet());
            EWCLogger.Log(builder.ToString());
        }

        private CustomWeaponManager()
        {
            DEFINITION_PATH = Path.Combine(MTFOPathAPI.CustomPath, EntryPoint.MODNAME);
            if (!Directory.Exists(DEFINITION_PATH))
                CreateTemplate();
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
