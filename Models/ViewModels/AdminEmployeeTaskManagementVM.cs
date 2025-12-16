namespace AgroManagement.Models.ViewModels
{
    public class AdminEmployeeTaskManagementVM
    {
        public string EmployeeCode { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public int TaskCount { get; set; }
        public bool IsDone => TaskCount == 0;
    }
}
