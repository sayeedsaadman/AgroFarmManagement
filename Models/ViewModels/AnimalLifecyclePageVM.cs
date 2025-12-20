namespace AgroManagement.Models.ViewModels
{
    public class AnimalLifecyclePageVM
    {
        public List<string> StatusOptions { get; set; } = new();
        public List<AnimalLifecycleRowVM> Rows { get; set; } = new();
    }
}
