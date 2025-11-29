namespace AgroManagement.Models.ViewModels
{
    public class ProfileVM
    {
        public int Id { get; set; }

        public string Username { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
    }
}
