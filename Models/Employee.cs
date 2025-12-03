using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace AgroManagement.Models
{
    public class Employee
    {
        [Key]
        [Required, StringLength(50)]
        public string EmployeeCode { get; set; }   // PRIMARY KEY (Employee ID)

        [Required, StringLength(100)]
        public string EmployeeName { get; set; }

        [Required, StringLength(20)]
        public string EmployeeNumber { get; set; } // phone / contact

        [Range(0, 100000000)]
        public decimal Salary { get; set; }

        [Required, StringLength(250)]
        public string Address { get; set; }

        public ICollection<EmployeeTask> Tasks { get; set; } = new List<EmployeeTask>();
    }
}
