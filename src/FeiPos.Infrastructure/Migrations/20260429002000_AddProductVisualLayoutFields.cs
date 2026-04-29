using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FeiPos.Infrastructure.Migrations
{
    [DbContext(typeof(Persistence.AppDbContext))]
    [Migration("20260429002000_AddProductVisualLayoutFields")]
    public partial class AddProductVisualLayoutFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ColorHex",
                table: "Products",
                type: "TEXT",
                nullable: false,
                defaultValue: "#455A64");

            migrationBuilder.AddColumn<string>(
                name: "Comments",
                table: "Products",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultQuantity",
                table: "Products",
                type: "TEXT",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<string>(
                name: "GroupName",
                table: "Products",
                type: "TEXT",
                nullable: false,
                defaultValue: "General");

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Products",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsService",
                table: "Products",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UnitOfMeasure",
                table: "Products",
                type: "TEXT",
                nullable: false,
                defaultValue: "Unid");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ColorHex", table: "Products");
            migrationBuilder.DropColumn(name: "Comments", table: "Products");
            migrationBuilder.DropColumn(name: "DefaultQuantity", table: "Products");
            migrationBuilder.DropColumn(name: "GroupName", table: "Products");
            migrationBuilder.DropColumn(name: "ImagePath", table: "Products");
            migrationBuilder.DropColumn(name: "IsService", table: "Products");
            migrationBuilder.DropColumn(name: "UnitOfMeasure", table: "Products");
        }
    }
}
