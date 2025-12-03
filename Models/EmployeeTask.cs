using System.ComponentModel.DataAnnotations;

namespace AgroManagement.Models
{
    public class EmployeeTask
    {
        public int Id { get; set; }

        [Required]
        public string EmployeeCode { get; set; } // FK -> Employees.EmployeeCode
        public Employee Employee { get; set; }

        [Required]
        public int AnimalId { get; set; }
        public Animal Animal { get; set; }

        [Required, StringLength(100)]
        public string TaskName { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.Now;
    }
}
