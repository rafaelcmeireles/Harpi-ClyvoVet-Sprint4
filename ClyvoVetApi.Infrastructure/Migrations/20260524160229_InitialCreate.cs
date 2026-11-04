using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClyvoVetApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "USERS",
                columns: table => new
                {
                    ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NAME = table.Column<string>(type: "NVARCHAR2(120)", maxLength: 120, nullable: false),
                    EMAIL = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: false),
                    ROLE = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: false),
                    PHONE = table.Column<string>(type: "NVARCHAR2(20)", maxLength: 20, nullable: true),
                    CPF = table.Column<string>(type: "NVARCHAR2(14)", maxLength: 14, nullable: true),
                    CRMV = table.Column<string>(type: "NVARCHAR2(10)", maxLength: 10, nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USERS", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "PETS",
                columns: table => new
                {
                    ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NAME = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    SPECIES = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    BREED = table.Column<string>(type: "NVARCHAR2(80)", maxLength: 80, nullable: true),
                    BIRTH_DATE = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    WEIGHT_KG = table.Column<double>(type: "BINARY_DOUBLE", nullable: true),
                    TUTOR_ID = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PETS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PETS_USERS_TUTOR_ID",
                        column: x => x.TUTOR_ID,
                        principalTable: "USERS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CONSULTATIONS",
                columns: table => new
                {
                    ID = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    PET_ID = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    VET_ID = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    CONSULTATION_DATE = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    DESCRIPTION = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    PRESCRIPTION = table.Column<string>(type: "NVARCHAR2(500)", maxLength: 500, nullable: true),
                    STATUS = table.Column<string>(type: "NVARCHAR2(30)", maxLength: 30, nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CONSULTATIONS", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CONSULTATIONS_PETS_PET_ID",
                        column: x => x.PET_ID,
                        principalTable: "PETS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CONSULTATIONS_USERS_VET_ID",
                        column: x => x.VET_ID,
                        principalTable: "USERS",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CONSULTATIONS_PET_ID",
                table: "CONSULTATIONS",
                column: "PET_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CONSULTATIONS_VET_ID",
                table: "CONSULTATIONS",
                column: "VET_ID");

            migrationBuilder.CreateIndex(
                name: "IX_PETS_TUTOR_ID",
                table: "PETS",
                column: "TUTOR_ID");

            migrationBuilder.CreateIndex(
                name: "IX_USERS_EMAIL",
                table: "USERS",
                column: "EMAIL",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CONSULTATIONS");

            migrationBuilder.DropTable(
                name: "PETS");

            migrationBuilder.DropTable(
                name: "USERS");
        }
    }
}
