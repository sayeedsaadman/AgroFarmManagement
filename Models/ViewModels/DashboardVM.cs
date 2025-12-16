using System.Collections.Generic;

namespace AgroManagement.Models.ViewModels
{
    public class DashboardVM
    {
        public int TasksDone { get; set; }

        public int TotalEmployees { get; set; }
        public int TotalAnimals { get; set; }
        public int TotalUsers { get; set; }

        public int TotalTasksAssigned { get; set; }
        public int TasksRemaining { get; set; }

        // Bar chart
        public List<string> EmployeeNames { get; set; } = new();
        public List<int> TasksPerEmployee { get; set; } = new();

        // Pie chart
        public int TotalTasksPossible { get; set; }
        public int TotalTasksUnassigned { get; set; }

    }
}
