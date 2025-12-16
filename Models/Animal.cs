using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AgroManagement.Models
{
    public class Animal
    {
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string TagNumber { get; set; }

        [Required]
        [StringLength(50)]
        public string Breed { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }

        [Range(0, 9999)]
        public double Weight { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Purchase Price")]
        public decimal PurchasePrice { get; set; }
        

        [NotMapped]
        public string Age
        {
            get
            {
                var years = DateTime.Now.Year - DateOfBirth.Year;
                return $"{years} Years";
            }
        } 
        public int TOTALCOUNT { get; set; } = 0;

    }
}
