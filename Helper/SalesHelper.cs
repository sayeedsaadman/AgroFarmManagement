using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AgroManagement.Models;

namespace AgroManagement.Helper
{
    public class SaleRecord
    {
        public string OrderId { get; set; } = Guid.NewGuid().ToString("N");
        public string Username { get; set; } = "";
        public DateTime OrderDateUtc { get; set; } = DateTime.UtcNow;
        public decimal TotalAmount { get; set; }
        public List<CartItem> Items { get; set; } = new();
    }

    public static class SalesHelper
    {
        private static readonly object _lock = new();

        private static string GetDataFolder(string contentRootPath)
            => Path.Combine(contentRootPath, "App_Data");

        private static string GetSalesFile(string contentRootPath)
            => Path.Combine(GetDataFolder(contentRootPath), "sales.json");

        public static void EnsureInitialized(string contentRootPath)
        {
            lock (_lock)
            {
                Directory.CreateDirectory(GetDataFolder(contentRootPath));
                var f = GetSalesFile(contentRootPath);

                if (!File.Exists(f))
                {
                    File.WriteAllText(f, "[]");
                }
            }
        }

        public static void RecordSale(string contentRootPath, string username, List<CartItem> items)
        {
            if (items == null || items.Count == 0) return;

            lock (_lock)
            {
                EnsureInitialized(contentRootPath);

                var sales = ReadAll(contentRootPath);

                var total = items.Sum(i => i.Price * i.Quantity);

                sales.Add(new SaleRecord
                {
                    Username = username ?? "",
                    Items = items.Select(i => new CartItem
                    {
                        ProductKey = i.ProductKey,
                        Name = i.Name,
                        UnitLabel = i.UnitLabel,
                        Price = i.Price,
                        Quantity = i.Quantity
                    }).ToList(),
                    TotalAmount = total,
                    OrderDateUtc = DateTime.UtcNow
                });

                WriteAll(contentRootPath, sales);
            }
        }

        public static decimal GetTotalIncome(string contentRootPath)
        {
            lock (_lock)
            {
                EnsureInitialized(contentRootPath);
                return ReadAll(contentRootPath).Sum(s => s.TotalAmount);
            }
        }

        public static List<SaleRecord> GetRecentSales(string contentRootPath, int take = 10)
        {
            lock (_lock)
            {
                EnsureInitialized(contentRootPath);
                return ReadAll(contentRootPath)
                    .OrderByDescending(x => x.OrderDateUtc)
                    .Take(take)
                    .ToList();
            }
        }

        private static List<SaleRecord> ReadAll(string contentRootPath)
        {
            var f = GetSalesFile(contentRootPath);
            if (!File.Exists(f)) return new List<SaleRecord>();

            var json = File.ReadAllText(f);
            if (string.IsNullOrWhiteSpace(json)) return new List<SaleRecord>();

            return JsonSerializer.Deserialize<List<SaleRecord>>(json) ?? new List<SaleRecord>();
        }

        private static void WriteAll(string contentRootPath, List<SaleRecord> sales)
        {
            var f = GetSalesFile(contentRootPath);

            var json = JsonSerializer.Serialize(sales, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(f, json);
        }
    }
}
