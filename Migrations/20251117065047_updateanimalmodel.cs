using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroManagement.Migrations
{
    /// <inheritdoc />
    public partial class updateanimalmodel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TOTALCOUNT",
                table: "Animals",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TOTALCOUNT",
                table: "Animals");
        }
    }
}
