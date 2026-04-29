using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FeiPos.Infrastructure.Migrations
{
    [DbContext(typeof(Persistence.AppDbContext))]
    [Migration("20260429003000_AddSaleComment")]
    public partial class AddSaleComment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "Sales",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Comment", table: "Sales");
        }
    }
}
