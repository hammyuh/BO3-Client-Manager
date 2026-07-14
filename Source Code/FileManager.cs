using System.IO;
using System.Text.Json;

namespace T7_Hub
{
    public class FileManager
    {
        public static readonly string standbyPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "T7 Hub",
            "Standby"
        );


        public static readonly string[] Clients =
        {
            "Stock BO3",
            "T7 Patch",
            "BOIII Community",
            "Ezz BOIII",
            "T7x",
            "CleanOps T7",
            "Old BO3 Exe"
        };


        public static readonly Dictionary<string, string[]> RequiredFiles = new()
        {
            {
                "BOIII Community",
                new[]
                {
                    "boiii.exe"
                }
            },

            {
                "T7 Patch",
                new[]
                {
                    "dsound.dll",
                    "t7patch.conf",
                    "t7patch.dll",
                    "t7patchloader.dll"
                }
            },

            {
                "Ezz BOIII",
                new[]
                {
                    "boiii.exe"
                }
            },

            {
                "CleanOps T7",
                new[]
                {
                    "d3d11.dll"
                }
            },

            {
                "T7x",
                new[]
                {
                    "t7x.exe"
                }
            }
        };


        public static void Load()
        {
            Directory.CreateDirectory(standbyPath);
        }


        public static void CreateStandbyFolders()
        {
            Directory.CreateDirectory(standbyPath);

            foreach (string client in Clients)
            {
                Directory.CreateDirectory(
                    Path.Combine(standbyPath, client)
                );
            }
        }


        public static bool CheckClient(string client)
        {
            if (!RequiredFiles.ContainsKey(client))
                return false;


            string clientPath = Path.Combine(
                standbyPath,
                client
            );


            foreach (string file in RequiredFiles[client])
            {
                if (!File.Exists(Path.Combine(clientPath, file)))
                    return false;
            }


            return true;
        }


        public static bool CheckStockBO3()
        {
            string path = Path.Combine(
                standbyPath,
                "Stock BO3",
                "BlackOps3.exe"
            );

            return File.Exists(path);
        }


        public static bool CheckOldExe()
        {
            string path = Path.Combine(
                standbyPath,
                "Old BO3 Exe",
                "BlackOps3.exe"
            );

            return File.Exists(path);
        }


        public static void SwapClient(string client, string gamePath)
        {
            Config config = ConfigManager.Load();


            if (config.appliedClient != "Stock BO3")
            {
                RestoreClient(config.appliedClient, gamePath);
            }


            if (RequiresOldExe(client))
            {
                ExeManager.UseOldExe(gamePath);
            }
            else
            {
                ExeManager.UseNewExe(gamePath);
            }


            if (client == "Stock BO3")
            {
                config.appliedClient = client;
                ConfigManager.Save(config);
                return;
            }


            string clientPath = Path.Combine(
                standbyPath,
                client
            );


            foreach (string file in RequiredFiles[client])
            {
                string source = Path.Combine(clientPath, file);
                string destination = Path.Combine(gamePath, file);

                if (File.Exists(source))
                {
                    File.Copy(source, destination, true);
                }
            }


            config.appliedClient = client;
            ConfigManager.Save(config);
        }


        public static void RestoreClient(string client, string gamePath)
        {
            if (!RequiredFiles.ContainsKey(client))
                return;


            foreach (string file in RequiredFiles[client])
            {
                string gameFile = Path.Combine(gamePath, file);

                if (File.Exists(gameFile))
                {
                    File.Delete(gameFile);
                }
            }
        }


        public static bool RequiresOldExe(string client)
        {
            string? json = HashUpdater.GetHashes();

            if (json == null)
                return false;


            using JsonDocument doc = JsonDocument.Parse(json);


            if (!doc.RootElement.TryGetProperty("clients", out JsonElement clients))
                return false;


            if (!clients.TryGetProperty(client, out JsonElement data))
                return false;


            if (!data.TryGetProperty("requiresOldExe", out JsonElement requiresOldExe))
                return false;


            return requiresOldExe.GetBoolean();
        }


        public static string DetectAppliedClient(string gamePath)
        {
            foreach (string client in Clients)
            {
                if (client == "Stock BO3" || client == "Old BO3 Exe")
                    continue;


                if (IsClientApplied(client, gamePath))
                    return client;
            }


            string exePath = Path.Combine(
                gamePath,
                "BlackOps3.exe"
            );


            if (ExeChecker.CheckBO3(exePath) == "New")
                return "Stock BO3";


            return "Unknown";
        }


        public static bool IsClientApplied(string client, string gamePath)
        {
            if (!RequiredFiles.ContainsKey(client))
                return false;


            foreach (string file in RequiredFiles[client])
            {
                if (!File.Exists(Path.Combine(gamePath, file)))
                    return false;
            }


            string exePath = Path.Combine(
                gamePath,
                "BlackOps3.exe"
            );


            if (!File.Exists(exePath))
                return false;


            if (RequiresOldExe(client))
            {
                return ExeChecker.CheckBO3(exePath) == "Old";
            }


            return ExeChecker.CheckBO3(exePath) == "New";
        }
    }
}