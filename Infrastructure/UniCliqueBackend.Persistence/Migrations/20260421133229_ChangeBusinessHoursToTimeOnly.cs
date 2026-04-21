using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniCliqueBackend.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChangeBusinessHoursToTimeOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"BusinessDetails\" ALTER COLUMN \"OpeningHours\" TYPE time without time zone USING \"OpeningHours\"::time without time zone;");
            migrationBuilder.Sql("ALTER TABLE \"BusinessDetails\" ALTER COLUMN \"ClosingHours\" TYPE time without time zone USING \"ClosingHours\"::time without time zone;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "OpeningHours",
                table: "BusinessDetails",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(TimeOnly),
                oldType: "time without time zone");

            migrationBuilder.AlterColumn<string>(
                name: "ClosingHours",
                table: "BusinessDetails",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(TimeOnly),
                oldType: "time without time zone");
        }
    }
}
