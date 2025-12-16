namespace AgroManagement.Models.ViewModels
{
    public class ExpenseReportVM
    {
        public int Year { get; set; }
        public int Month { get; set; }

        public List<AnimalExpenseVM> AnimalExpenses { get; set; } = new();
        public List<EmployeeSalaryVM> EmployeeSalaries { get; set; } = new();

        public decimal TotalAnimalExpense =>
            AnimalExpenses.Sum(x => x.TotalExpense);

        public decimal TotalSalaryExpense =>
            EmployeeSalaries.Sum(x => x.Salary);

        public decimal GrandTotalExpense =>
            TotalAnimalExpense + TotalSalaryExpense;
    }
}
