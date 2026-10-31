using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace PromptPalette
{
    [DataContract]
    internal sealed class PresetRecord
    {
        public PresetRecord()
        {
            Id = string.Empty;
            Name = string.Empty;
            Library = new PromptLibrary();
        }

        public PresetRecord(string id, string name, PromptLibrary library)
        {
            Id = id;
            Name = name;
            Library = library ?? new PromptLibrary();
        }

        public override string ToString()
        {
            return Name;
        }

        [DataMember(Order = 1)]
        public string Id { get; set; }

        [DataMember(Order = 2)]
        public string Name { get; set; }

        [DataMember(Order = 3)]
        public PromptLibrary Library { get; set; }
    }

    [DataContract]
    internal sealed class PresetCollection
    {
        public PresetCollection()
        {
            Version = 1;
            ActivePresetId = string.Empty;
            Presets = new List<PresetRecord>();
        }

        [DataMember(Order = 1)]
        public int Version { get; set; }

        [DataMember(Order = 2)]
        public string ActivePresetId { get; set; }

        [DataMember(Order = 3)]
        public List<PresetRecord> Presets { get; set; }

        public PresetRecord FindPreset(string id)
        {
            if (Presets == null) return null;
            for (int i = 0; i < Presets.Count; i++)
            {
                if (Presets[i].Id == id) return Presets[i];
            }
            return null;
        }

        public PresetRecord FindPresetByName(string name, string exceptId)
        {
            if (Presets == null) return null;
            for (int i = 0; i < Presets.Count; i++)
            {
                PresetRecord preset = Presets[i];
                if (exceptId != null && preset.Id == exceptId) continue;
                if (string.Equals(preset.Name, name, StringComparison.OrdinalIgnoreCase))
                    return preset;
            }
            return null;
        }

        public PresetRecord GetActive()
        {
            PresetRecord preset = FindPreset(ActivePresetId);
            if (preset == null && Presets.Count > 0) preset = Presets[0];
            if (preset != null) ActivePresetId = preset.Id;
            return preset;
        }

        public string MakeUniqueName(string baseName)
        {
            string root = string.IsNullOrEmpty(baseName) ? "新预设" : baseName.Trim();
            if (FindPresetByName(root, null) == null) return root;
            string candidate = root + " (导入)";
            if (FindPresetByName(candidate, null) == null) return candidate;
            int number = 2;
            while (true)
            {
                candidate = root + " (" + number + ")";
                if (FindPresetByName(candidate, null) == null) return candidate;
                number++;
            }
        }

        public void Normalize()
        {
            if (Version <= 0) Version = 1;
            if (Presets == null) Presets = new List<PresetRecord>();
            List<PresetRecord> clean = new List<PresetRecord>();
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < Presets.Count; i++)
            {
                PresetRecord preset = Presets[i];
                if (preset == null) continue;
                if (string.IsNullOrEmpty(preset.Id) || ids.Contains(preset.Id))
                    preset.Id = PresetStore.NewPresetId();
                preset.Name = (preset.Name ?? string.Empty).Trim();
                if (preset.Name.Length == 0) preset.Name = "未命名预设";
                if (names.Contains(preset.Name))
                    preset.Name = MakeUniqueName(preset.Name);
                if (preset.Library == null) preset.Library = new PromptLibrary();
                preset.Library.Normalize();
                ids.Add(preset.Id);
                names.Add(preset.Name);
                clean.Add(preset);
            }
            Presets = clean;
            if (Presets.Count == 0)
            {
                PresetRecord preset = new PresetRecord(
                    PresetStore.NewPresetId(), "默认模板", PromptTemplate.Create());
                Presets.Add(preset);
                ActivePresetId = preset.Id;
            }
            if (FindPreset(ActivePresetId) == null) ActivePresetId = Presets[0].Id;
        }
    }

    internal static class PresetStore
    {
        public static string DataFile
        {
            get
            {
                string directory = Path.GetDirectoryName(PromptStore.DataFile);
                return Path.Combine(directory, "presets.json");
            }
        }

        public static string NewPresetId()
        {
            return PromptLibrary.NewId("preset");
        }

        public static PresetCollection LoadOrCreate()
        {
            try
            {
                string directory = Path.GetDirectoryName(DataFile);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                if (File.Exists(DataFile))
                {
                    PresetCollection collection;
                    using (FileStream stream = File.OpenRead(DataFile))
                    {
                        DataContractJsonSerializer serializer =
                            new DataContractJsonSerializer(typeof(PresetCollection));
                        collection = (PresetCollection)serializer.ReadObject(stream);
                    }
                    if (collection == null) throw new InvalidDataException("Preset file is empty.");
                    collection.Normalize();
                    return collection;
                }

                // Migrate the previous single-library file, or create the built-in template.
                PromptLibrary legacy = PromptStore.LoadOrCreate();
                PresetCollection created = new PresetCollection();
                PresetRecord preset = new PresetRecord(NewPresetId(), "默认模板", legacy);
                created.Presets.Add(preset);
                created.ActivePresetId = preset.Id;
                created.Normalize();
                try { Save(created); }
                catch { }
                return created;
            }
            catch
            {
                TryBackupBrokenFile();
                PresetCollection collection = new PresetCollection();
                PresetRecord preset = new PresetRecord(NewPresetId(), "默认模板", PromptTemplate.Create());
                collection.Presets.Add(preset);
                collection.ActivePresetId = preset.Id;
                try { Save(collection); }
                catch { }
                return collection;
            }
        }

        public static void Save(PresetCollection collection)
        {
            if (collection == null) throw new ArgumentNullException("collection");
            collection.Normalize();
            string directory = Path.GetDirectoryName(DataFile);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            string temp = DataFile + ".tmp";
            using (FileStream stream = File.Create(temp))
            {
                DataContractJsonSerializer serializer =
                    new DataContractJsonSerializer(typeof(PresetCollection));
                serializer.WriteObject(stream, collection);
            }
            File.Copy(temp, DataFile, true);
            File.Delete(temp);
        }

        private static void TryBackupBrokenFile()
        {
            try
            {
                if (!File.Exists(DataFile)) return;
                string backup = DataFile + ".broken";
                if (File.Exists(backup)) File.Delete(backup);
                File.Move(DataFile, backup);
            }
            catch
            {
            }
        }
    }
}
