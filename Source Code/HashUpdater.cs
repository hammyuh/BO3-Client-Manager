using System.IO;
using System.Net.Http;

namespace T7_Hub
{
    public static class HashUpdater
    {
        private static readonly string hashPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "T7 Hub",
            "hashes.json"
        );

        private const string HashUrl =
            "https://raw.githubusercontent.com/hammyuh/T7-Hub/refs/heads/main/hashes.json";


        public static async Task UpdateHashes()
        {
            try
            {
                using HttpClient client = new();

                client.Timeout = TimeSpan.FromSeconds(5);

                string json = await client.GetStringAsync(HashUrl);

                Directory.CreateDirectory(
                    Path.GetDirectoryName(hashPath)!
                );

                File.WriteAllText(hashPath, json);
            }
            catch
            {
               
            }
        }


        public static string? GetHashes()
        {
            if (!File.Exists(hashPath))
                return null;

            return File.ReadAllText(hashPath);
        }
    }
}