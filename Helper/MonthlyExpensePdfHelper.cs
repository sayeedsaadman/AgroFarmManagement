using AgroManagement.Models.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AgroManagement.Helper
{
    public static class MonthlyExpensePdfHelper
    {
        public static byte[] Generate(int year, int month, ExpenseReportVM vm)
        {
            // If you didn't already configure QuestPDF license in Program.cs:
            QuestPDF.Settings.License = LicenseType.Community;

            var title = $"Expense Report - {year:D4}-{month:D2}";

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Column(col =>
                    {
                        col.Item().Text(title).FontSize(18).Bold();
                        col.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}");
                        col.Item().LineHorizontal(1);
                    });

                    page.Content().Column(col =>
                    {
                        // Summary
                        col.Item().PaddingTop(10).Text("Summary").FontSize(14).Bold();

                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });

                            t.Cell().Text("Total Animal Expense").Bold();
                            t.Cell().AlignRight().Text(vm.TotalAnimalExpense.ToString("0.00"));

                            t.Cell().Text("Total Salary Expense").Bold();
                            t.Cell().AlignRight().Text(vm.TotalSalaryExpense.ToString("0.00"));

                            t.Cell().Text("Grand Total Expense").Bold();
                            t.Cell().AlignRight().Text(vm.GrandTotalExpense.ToString("0.00")).Bold();
                        });

                        // Animal Expenses table
                        col.Item().PaddingTop(15).Text("Animal Expenses").FontSize(14).Bold();

                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(); // Tag
                                c.RelativeColumn(); // Food
                                c.RelativeColumn(); // Maint
                                c.RelativeColumn(); // Medical
                                c.RelativeColumn(); // Total
                            });

                            // Header
                            t.Header(h =>
                            {
                                h.Cell().Element(CellHeader).Text("Tag/ID");
                                h.Cell().Element(CellHeader).AlignRight().Text("Food");
                                h.Cell().Element(CellHeader).AlignRight().Text("Maintenance");
                                h.Cell().Element(CellHeader).AlignRight().Text("Medical");
                                h.Cell().Element(CellHeader).AlignRight().Text("Total");
                            });

                            foreach (var a in vm.AnimalExpenses)
                            {
                                t.Cell().Element(CellBody).Text(a.TagNumber);
                                t.Cell().Element(CellBody).AlignRight().Text(a.FoodExpense.ToString("0.00"));
                                t.Cell().Element(CellBody).AlignRight().Text(a.MaintenanceExpense.ToString("0.00"));
                                t.Cell().Element(CellBody).AlignRight().Text(a.MedicalExpense.ToString("0.00"));
                                t.Cell().Element(CellBody).AlignRight().Text(a.TotalExpense.ToString("0.00"));
                            }
                        });

                        // Employee Salaries table
                        col.Item().PaddingTop(15).Text("Employee Salaries").FontSize(14).Bold();

                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2); // Name
                                c.RelativeColumn();  // Salary
                            });

                            t.Header(h =>
                            {
                                h.Cell().Element(CellHeader).Text("Employee");
                                h.Cell().Element(CellHeader).AlignRight().Text("Salary");
                            });

                            foreach (var e in vm.EmployeeSalaries)
                            {
                                t.Cell().Element(CellBody).Text(e.EmployeeName);
                                t.Cell().Element(CellBody).AlignRight().Text(e.Salary.ToString("0.00"));
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });

            return doc.GeneratePdf();

            static IContainer CellHeader(IContainer c) =>
                c.PaddingVertical(5).PaddingHorizontal(4).Background(Colors.Grey.Lighten3).DefaultTextStyle(x => x.Bold());

            static IContainer CellBody(IContainer c) =>
                c.PaddingVertical(4).PaddingHorizontal(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
        }
    }
}
