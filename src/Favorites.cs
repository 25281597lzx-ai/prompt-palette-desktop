using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace PromptPalette
{
    [DataContract]
    internal sealed class FavoriteRecord
    {
        public FavoriteRecord()
        {
            Chinese = string.Empty;
            English = string.Empty;
        }

        [DataMember(Order = 1)]
        public string Chinese { get; set; }

        [DataMember(Order = 2)]
        public string English { get; set; }

        [DataMember(Order = 3)]
        public long AddedTicks { get; set; }
    }

    [DataContract]
    internal sealed class FavoriteCollection
    {
        public FavoriteCollection()
        {
            Version = 1;
            Tags = new List<FavoriteRecord>();
        }

        [DataMember(Order = 1)]
        public int Version { get; set; }

        [DataMember(Order = 2)]
        public List<FavoriteRecord> Tags { get; set; }

        public bool IsFavorite(PromptTag tag)
        {
            if (tag == null || Tags == null) return false;
            string key = PresetExchangeIO.TagIdentity(tag.Chinese, tag.English);
            for (int i = 0; i < Tags.Count; i++)
            {
                FavoriteRecord record = Tags[i];
                if (string.Equals(PresetExchangeIO.TagIdentity(record.Chinese, record.English),
                    key, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        public bool Toggle(PromptTag tag)
        {
            if (tag == null) return false;
            string key = PresetExchangeIO.TagIdentity(tag.Chinese, tag.English);
            for (int i = 0; i < Tags.Count; i++)
            {
                FavoriteRecord record = Tags[i];
                if (string.Equals(PresetExchangeIO.TagIdentity(record.Chinese, record.English),
                    key, StringComparison.Ordinal))
                {
                    Tags.RemoveAt(i);
                    return false;
                }
            }
            FavoriteRecord added = new FavoriteRecord();
            added.Chinese = tag.Chinese ?? string.Empty;
            added.English = tag.English ?? string.Empty;
            added.AddedTicks = DateTime.UtcNow.Ticks;
            Tags.Insert(0, added);
            return true;
        }

        public void Normalize()
        {
            if (Tags == null) Tags = new List<FavoriteRecord>();
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < Tags.Count; i++)
            {
                FavoriteRecord record = Tags[i];
                if (record == null
                    || (string.IsNullOrWhiteSpace(record.Chinese)
                        && string.IsNullOrWhiteSpace(record.English)))
                {
                    Tags.RemoveAt(i);
                    i--;
                    continue;
                }
                string key = PresetExchangeIO.TagIdentity(record.Chinese, record.English);
                if (!seen.Add(key))
                {
                    Tags.RemoveAt(i);
                    i--;
                }
            }
        }
    }

    internal static class FavoritesStore
    {
        public static string DataFile
        {
            get
            {
                string directory = Path.GetDirectoryName(PromptStore.DataFile);
                return Path.Combine(directory, "favorites.json");
            }
        }

        public static FavoriteCollection Load()
        {
            try
            {
                if (!File.Exists(DataFile)) return new FavoriteCollection();
                using (FileStream stream = File.OpenRead(DataFile))
                {
                    DataContractJsonSerializer serializer =
                        new DataContractJsonSerializer(typeof(FavoriteCollection));
                    FavoriteCollection collection =
                        (FavoriteCollection)serializer.ReadObject(stream);
                    if (collection == null) return new FavoriteCollection();
                    collection.Normalize();
                    return collection;
                }
            }
            catch
            {
                return new FavoriteCollection();
            }
        }

        public static void Save(FavoriteCollection collection)
        {
            if (collection == null) return;
            collection.Normalize();
            string directory = Path.GetDirectoryName(DataFile);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            string temp = DataFile + ".tmp";
            using (FileStream stream = File.Create(temp))
            {
                DataContractJsonSerializer serializer =
                    new DataContractJsonSerializer(typeof(FavoriteCollection));
                serializer.WriteObject(stream, collection);
            }
            File.Copy(temp, DataFile, true);
            File.Delete(temp);
        }
    }
}
