using System.Text.Json;
using AgroManagement.Models;

namespace AgroManagement.Helper
{
    public static class HealthLogHelper
    {
        private const string FolderName = "App_Data";
        private const string FileName = "health_logs.json";

        private static string GetPath(string contentRoot)
            => Path.Combine(contentRoot, FolderName, FileName);

        public static void EnsureInitialized(string contentRoot)
        {
            var folder = Path.Combine(contentRoot, FolderName);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            var path = GetPath(contentRoot);
            if (!File.Exists(path))
            {
                File.WriteAllText(path, "[]");
            }
        }

        public static List<HealthLogEntry> GetAll(string contentRoot)
        {
            EnsureInitialized(contentRoot);
            var path = GetPath(contentRoot);
            var json = File.ReadAllText(path);

            return JsonSerializer.Deserialize<List<HealthLogEntry>>(json)
                   ?? new List<HealthLogEntry>();
        }

        public static List<HealthLogEntry> GetByDate(string contentRoot, string yyyyMMdd)
        {
            return GetAll(contentRoot)
                .Where(x => x.Date == yyyyMMdd)
                .ToList();
        }

        public static List<HealthLogEntry> GetByMonth(string contentRoot, int year, int month)
        {
            var prefix = $"{year:D4}-{month:D2}-";
            return GetAll(contentRoot)
                .Where(x => x.Date.StartsWith(prefix))
                .ToList();
        }

        public static void UpsertMany(string contentRoot, IEnumerable<HealthLogEntry> entries)
        {
            EnsureInitialized(contentRoot);
            var all = GetAll(contentRoot);

            foreach (var e in entries)
            {
                // Normalize
                e.TagNumber ??= "";
                e.MedicineName ??= "None";
                e.Dose ??= "";
                e.Notes ??= "";

                // Rule: if everything empty/false and milk is 0 -> remove the existing record (cleanup)
                var isEmpty =
                    e.MilkLiters <= 0 &&
                    !e.HealthChecked &&
                    !e.MedicineGiven &&
                    string.IsNullOrWhiteSpace(e.Notes);

                var idx = all.FindIndex(x => x.Date == e.Date && x.AnimalId == e.AnimalId);

                if (isEmpty)
                {
                    if (idx >= 0) all.RemoveAt(idx);
                    continue;
                }

                if (idx >= 0)
                {
                    all[idx] = e; // update
                }
                else
                {
                    all.Add(e);   // insert
                }
            }

            SaveAll(contentRoot, all);
        }

        private static void SaveAll(string contentRoot, List<HealthLogEntry> all)
        {
            var path = GetPath(contentRoot);
            var json = JsonSerializer.Serialize(all, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(path, json);
        }
    }
}
