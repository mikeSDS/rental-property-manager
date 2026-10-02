using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalPropertyManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyAndUnitEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Properties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StreetAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ZipCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Properties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Units",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropertyID = table.Column<int>(type: "int", nullable: false),
                    UnitNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Bedrooms = table.Column<int>(type: "int", nullable: false),
                    MonthlyRent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitTypeID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Units_Properties_PropertyID",
                        column: x => x.PropertyID,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Units_UnitTypes_UnitTypeID",
                        column: x => x.UnitTypeID,
                        principalTable: "UnitTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Units_PropertyID",
                table: "Units",
                column: "PropertyID");

            migrationBuilder.CreateIndex(
                name: "IX_Units_UnitTypeID",
                table: "Units",
                column: "UnitTypeID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Units");

            migrationBuilder.DropTable(
                name: "Properties");
        }
    }
}
