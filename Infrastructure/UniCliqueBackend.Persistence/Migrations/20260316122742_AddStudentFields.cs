using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniCliqueBackend.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsStudent",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "StudentDocumentUrl",
                table: "Users",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentVerificationNote",
                table: "Users",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StudentVerificationStatus",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StudentVerifiedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsStudent",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StudentDocumentUrl",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StudentVerificationNote",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StudentVerificationStatus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StudentVerifiedAt",
                table: "Users");
        }
    }
}
