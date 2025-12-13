using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace AgroManagement.Helper
{
    public static class StockHelper
    {
        private static readonly object _lock = new();

        private static string GetDataFolder(string contentRootPath)
            => Path.Combine(contentRootPath, "App_Data");

        private static string GetStockFile(string contentRootPath)
            => Path.Combine(GetDataFolder(contentRootPath), "stock.json");

        public static void EnsureInitialized(string contentRootPath, int defaultStockPerProduct = 10)
        {
            lock (_lock)
            {
                var folder = GetDataFolder(contentRootPath);
                Directory.CreateDirectory(folder);

                var stockFile = GetStockFile(contentRootPath);

                if (!File.Exists(stockFile))
                {
                    // first time create: all products with default stock
                    var initial = ProductCatalog.All.ToDictionary(p => p.Key, _ => defaultStockPerProduct);
                    WriteStock(contentRootPath, initial);
                    return;
                }

                // Ensure any new product keys added later also exist in JSON
                var current = ReadStock(contentRootPath);
                var changed = false;

                foreach (var p in ProductCatalog.All)
                {
                    if (!current.ContainsKey(p.Key))
                    {
                        current[p.Key] = defaultStockPerProduct;
                        changed = true;
                    }
                }

                if (changed)
                    WriteStock(contentRootPath, current);
            }
        }

        public static Dictionary<string, int> GetAllStocks(string contentRootPath)
        {
            lock (_lock)
            {
                return ReadStock(contentRootPath);
            }
        }

        public static int GetStock(string contentRootPath, string productKey)
        {
            lock (_lock)
            {
                var stock = ReadStock(contentRootPath);
                return stock.TryGetValue(productKey, out var qty) ? qty : 0;
            }
        }

        public static void SetStock(string contentRootPath, string productKey, int newStock)
        {
            if (newStock < 0) newStock = 0;

            lock (_lock)
            {
                var stock = ReadStock(contentRootPath);
                stock[productKey] = newStock;
                WriteStock(contentRootPath, stock);
            }
        }

        public static bool TryDecreaseStock(string contentRootPath, string productKey, int amount, out string error)
        {
            error = "";

            if (amount <= 0)
            {
                error = "Invalid quantity.";
                return false;
            }

            lock (_lock)
            {
                var stock = ReadStock(contentRootPath);

                var current = stock.TryGetValue(productKey, out var qty) ? qty : 0;
                if (current < amount)
                {
                    error = $"Not enough stock. Only {current} left.";
                    return false;
                }

                stock[productKey] = current - amount;
                WriteStock(contentRootPath, stock);
                return true;
            }
        }

        public static void IncreaseStock(string contentRootPath, string productKey, int amount)
        {
            if (amount <= 0) return;

            lock (_lock)
            {
                var stock = ReadStock(contentRootPath);
                var current = stock.TryGetValue(productKey, out var qty) ? qty : 0;
                stock[productKey] = current + amount;
                WriteStock(contentRootPath, stock);
            }
        }
        public static bool TryDecreaseStockBulk(string contentRootPath, Dictionary<string, int> requests, out string error)
        {
            error = "";

            if (requests == null || requests.Count == 0)
            {
                error = "Cart is empty.";
                return false;
            }

            // normalize
            foreach (var kv in requests)
            {
                if (string.IsNullOrWhiteSpace(kv.Key) || kv.Value <= 0)
                {
                    error = "Invalid cart quantity.";
                    return false;
                }
            }

            lock (_lock)
            {
                var stock = ReadStock(contentRootPath);

                // 1) Validate all first
                foreach (var kv in requests)
                {
                    var key = kv.Key;
                    var need = kv.Value;

                    var current = stock.TryGetValue(key, out var qty) ? qty : 0;
                    if (current < need)
                    {
                        error = $"Not enough stock for {key}. Only {current} left.";
                        return false;
                    }
                }

                // 2) Then decrease all (atomic)
                foreach (var kv in requests)
                {
                    stock[kv.Key] = stock[kv.Key] - kv.Value;
                }

                WriteStock(contentRootPath, stock);
                return true;
            }
        }

        private static Dictionary<string, int> ReadStock(string contentRootPath)
        {
            var stockFile = GetStockFile(contentRootPath);
            if (!File.Exists(stockFile))
                return new Dictionary<string, int>();

            var json = File.ReadAllText(stockFile);
            if (string.IsNullOrWhiteSpace(json))
                return new Dictionary<string, int>();

            return JsonSerializer.Deserialize<Dictionary<string, int>>(json)
                   ?? new Dictionary<string, int>();
        }

        private static void WriteStock(string contentRootPath, Dictionary<string, int> stock)
        {
            var stockFile = GetStockFile(contentRootPath);

            var json = JsonSerializer.Serialize(stock, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(stockFile, json);
        }
    }
}
