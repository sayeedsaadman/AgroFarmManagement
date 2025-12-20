using AgroManagement.Models;
using System.Text.Json;

namespace AgroManagement.Helper
{
    public static class AnimalLifecycleHelper
    {
        private const string FileName = "animal_status.json";

        private static string GetPath(string contentRoot)
            => Path.Combine(contentRoot, "App_Data", FileName);

        public static void EnsureInitialized(string contentRoot)
        {
            var appDataDir = Path.Combine(contentRoot, "App_Data");
            if (!Directory.Exists(appDataDir))
                Directory.CreateDirectory(appDataDir);

            var path = GetPath(contentRoot);
            if (!File.Exists(path))
                File.WriteAllText(path, "[]");
        }

        public static List<AnimalLifecycleStatusEntry> GetAll(string contentRoot)
        {
            EnsureInitialized(contentRoot);

            var path = GetPath(contentRoot);
            var json = File.ReadAllText(path);

            return JsonSerializer.Deserialize<List<AnimalLifecycleStatusEntry>>(json)
                   ?? new List<AnimalLifecycleStatusEntry>();
        }

        public static AnimalLifecycleStatusEntry? GetByAnimalId(string contentRoot, int animalId)
        {
            var all = GetAll(contentRoot);
            return all.FirstOrDefault(x => x.AnimalId == animalId);
        }

        public static void UpsertMany(string contentRoot, List<AnimalLifecycleStatusEntry> entries)
        {
            EnsureInitialized(contentRoot);

            var all = GetAll(contentRoot);

            foreach (var e in entries)
            {
                // normalize
                e.Status = string.IsNullOrWhiteSpace(e.Status) ? "Active" : e.Status.Trim();
                e.Notes = e.Notes ?? "";
                e.TagNumber = e.TagNumber ?? "";
                e.UpdatedOn = string.IsNullOrWhiteSpace(e.UpdatedOn)
                    ? DateTime.Today.ToString("yyyy-MM-dd")
                    : e.UpdatedOn;

                var existing = all.FirstOrDefault(x => x.AnimalId == e.AnimalId);
                if (existing == null)
                {
                    all.Add(e);
                }
                else
                {
                    existing.TagNumber = e.TagNumber;
                    existing.Status = e.Status;
                    existing.Notes = e.Notes;
                    existing.UpdatedOn = e.UpdatedOn;
                }
            }

            var path = GetPath(contentRoot);
            var json = JsonSerializer.Serialize(all.OrderBy(x => x.TagNumber), new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(path, json);
        }
    }
}
