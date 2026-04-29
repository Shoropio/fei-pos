using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FeiPos.Infrastructure.Migrations
{
    [DbContext(typeof(Persistence.AppDbContext))]
    [Migration("20260429001000_AddSaleCustomerFiscalFields")]
    public partial class AddSaleCustomerFiscalFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                table: "Sales",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerTaxId",
                table: "Sales",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CustomerName", table: "Sales");
            migrationBuilder.DropColumn(name: "CustomerTaxId", table: "Sales");
        }
    }
}
