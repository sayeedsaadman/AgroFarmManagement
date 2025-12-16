using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AgroManagement.Models;
using AgroManagement.Models.ViewModels;

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
                    File.WriteAllText(f, "[]");
            }
        }

        // ✅ SINGLE SOURCE OF TRUTH (NO DUPLICATE METHODS)
        public static List<SaleRecord> GetAllSales(string root)
        {
            lock (_lock)
            {
                EnsureInitialized(root);

                var f = GetSalesFile(root);
                if (!File.Exists(f)) return new List<SaleRecord>();

                var json = File.ReadAllText(f);
                if (string.IsNullOrWhiteSpace(json)) return new List<SaleRecord>();

                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<List<SaleRecord>>(json, opts) ?? new List<SaleRecord>();
            }
        }

        public static void RecordSale(string contentRootPath, string username, List<CartItem> items)
        {
            if (items == null || items.Count == 0) return;

            lock (_lock)
            {
                EnsureInitialized(contentRootPath);

                var sales = GetAllSales(contentRootPath);

                var total = items.Sum(i => i.Price * i.Quantity);

                sales.Add(new SaleRecord
                {
                    OrderId = "INV-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
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

                var f = GetSalesFile(contentRootPath);
                File.WriteAllText(f, JsonSerializer.Serialize(sales, new JsonSerializerOptions { WriteIndented = true }));
            }
        }

        // ✅ Dashboard: ALL-TIME income
        public static decimal GetTotalIncome(string root)
        {
            return GetAllSales(root).Sum(s => s.TotalAmount);
        }

        // ✅ Dashboard: MONTHLY income (resets automatically each month)
        public static decimal GetMonthIncome(string root, int year, int month)
        {
            return GetAllSales(root)
                .Where(s => s.OrderDateUtc.Year == year && s.OrderDateUtc.Month == month)
                .Sum(s => s.TotalAmount);
        }

        // ✅ Analytics for your Sales Report page
        public static SalesAnalyticsVM GetSalesAnalytics(string root)
        {
            var sales = GetAllSales(root);

            var now = DateTime.UtcNow;

            var today = sales.Where(s => s.OrderDateUtc.Date == now.Date).ToList();
            var week = sales.Where(s => s.OrderDateUtc >= now.AddDays(-7)).ToList();
            var month = sales.Where(s => s.OrderDateUtc.Year == now.Year && s.OrderDateUtc.Month == now.Month).ToList();

            return new SalesAnalyticsVM
            {
                TodayTotal = today.Sum(s => s.TotalAmount),
                WeekTotal = week.Sum(s => s.TotalAmount),
                MonthTotal = month.Sum(s => s.TotalAmount),

                TopProducts = sales
    .SelectMany(s => s.Items)
    .GroupBy(i => i.Name)
    .Select(g => new ProductSalesVM
    {
        ProductName = g.Key,
        Quantity = g.Sum(x => x.Quantity),
        Revenue = g.Sum(x => x.Quantity * x.Price)
    })
    .OrderByDescending(x => x.Quantity)
    .ToList()

            };
        }
    }
}
