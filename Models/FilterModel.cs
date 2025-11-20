using System.Collections.Generic;

namespace AgroManagement.Models
{
    public class FilterItem
    {
        public string field { get; set; } = string.Empty;
        public string value { get; set; } = string.Empty;
    }

    public class SearchQueryModel
    {
        public int page { get; set; } = 1;
        public decimal size { get; set; } = 5;
        public List<FilterItem> filter { get; set; } = new List<FilterItem>();
    }
}
