using System.Collections.Generic;

namespace AgroManagement.Models.ViewModels
{
    public class SalesAnalyticsVM
    {
        public decimal TodayTotal { get; set; }
        public decimal WeekTotal { get; set; }
        public decimal MonthTotal { get; set; }

        public List<ProductSalesVM> TopProducts { get; set; } = new();
    }
}
