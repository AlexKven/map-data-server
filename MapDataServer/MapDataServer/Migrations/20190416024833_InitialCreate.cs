using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MapDataServer.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Model1",
                columns: table => new
                {
                    Name = table.Column<string>(nullable: false),
                    NextName = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Model1", x => x.Name);
                    table.ForeignKey(
                        name: "FK_Model1_Model1_NextName",
                        column: x => x.NextName,
                        principalTable: "Model1",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Model2",
                columns: table => new
                {
                    Detail1 = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Detail2 = table.Column<double>(nullable: false),
                    ThatsTrue = table.Column<bool>(nullable: false),
                    TestModel1Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Model2", x => x.Detail1);
                    table.ForeignKey(
                        name: "FK_Model2_Model1_TestModel1Name",
                        column: x => x.TestModel1Name,
                        principalTable: "Model1",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Model1_NextName",
                table: "Model1",
                column: "NextName");

            migrationBuilder.CreateIndex(
                name: "IX_Model2_TestModel1Name",
                table: "Model2",
                column: "TestModel1Name");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Model2");

            migrationBuilder.DropTable(
                name: "Model1");
        }
    }
}
