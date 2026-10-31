using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Windows.Forms;

namespace PromptPalette
{
    internal static class PromptTemplate
    {
        public static PromptLibrary Create()
        {
            PromptLibrary library = new PromptLibrary();
            library.Version = 1;
            return library;
        }
    }

    internal static class PromptStore
    {
        private static string DataDirectory
        {
            get
            {
                string home = Environment.GetEnvironmentVariable("PROMPT_PALETTE_HOME");
                if (string.IsNullOrEmpty(home)) home = Application.StartupPath;
                return Path.Combine(home, "data");
            }
        }

        public static string DataFile
        {
            get { return Path.Combine(DataDirectory, "library.json"); }
        }

        public static PromptLibrary LoadOrCreate()
        {
            try
            {
                if (!Directory.Exists(DataDirectory)) Directory.CreateDirectory(DataDirectory);
                if (!File.Exists(DataFile))
                {
                    PromptLibrary seed = PromptTemplate.Create();
                    Save(seed);
                    return seed;
                }

                PromptLibrary library;
                using (FileStream stream = File.OpenRead(DataFile))
                {
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(PromptLibrary));
                    library = (PromptLibrary)serializer.ReadObject(stream);
                }
                if (library == null) throw new InvalidDataException("The library file is empty.");
                library.Normalize();
                return library;
            }
            catch
            {
                TryBackupBrokenFile();
                PromptLibrary seed = PromptTemplate.Create();
                try { Save(seed); }
                catch { }
                return seed;
            }
        }

        public static void Save(PromptLibrary library)
        {
            if (library == null) throw new ArgumentNullException("library");
            if (!Directory.Exists(DataDirectory)) Directory.CreateDirectory(DataDirectory);
            string temp = DataFile + ".tmp";
            using (FileStream stream = File.Create(temp))
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(PromptLibrary));
                serializer.WriteObject(stream, library);
            }
            File.Copy(temp, DataFile, true);
            File.Delete(temp);
        }

        public static PromptLibrary ResetTemplate()
        {
            PromptLibrary library = PromptTemplate.Create();
            Save(library);
            return library;
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

