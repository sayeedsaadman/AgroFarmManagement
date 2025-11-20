using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueTagNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Animals_TagNumber",
                table: "Animals",
                column: "TagNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Animals_TagNumber",
                table: "Animals");
        }
    }
}
