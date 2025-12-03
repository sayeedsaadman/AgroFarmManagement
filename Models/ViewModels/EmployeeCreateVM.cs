using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace AgroManagement.Models.ViewModels
{
    public class EmployeeCreateVM
    {
        [Required, StringLength(100)]
        public string EmployeeName { get; set; }

        [Required, StringLength(50)]
        public string EmployeeCode { get; set; }

        [Required, StringLength(20)]
        public string EmployeeNumber { get; set; }

        [Range(0, 100000000)]
        public decimal Salary { get; set; }

        [Required, StringLength(250)]
        public string Address { get; set; }

        public int? SelectedAnimalId { get; set; }

        // multiple tasks
        public List<string> SelectedTaskNames { get; set; } = new();
    }
}
