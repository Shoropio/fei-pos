using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FeiPos.Infrastructure.Migrations
{
    [DbContext(typeof(Persistence.AppDbContext))]
    [Migration("20260501123000_AddFiscalXmlStorage")]
    public partial class AddFiscalXmlStorage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "XmlRequest",
                table: "HaciendaResponses",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SignedXml",
                table: "HaciendaResponses",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "SignedXml", table: "HaciendaResponses");
            migrationBuilder.DropColumn(name: "XmlRequest", table: "HaciendaResponses");
        }
    }
}
