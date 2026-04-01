using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace To_doList.Migrations
{
    /// <inheritdoc />
    public partial class AddSubTaskDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "SubTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "SubTasks",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "SubTasks");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "SubTasks");
        }
    }
}
