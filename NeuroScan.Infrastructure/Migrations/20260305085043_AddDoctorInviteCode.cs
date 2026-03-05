using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeuroScan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorInviteCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignedDoctorId",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InviteCode",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_AssignedDoctorId",
                table: "Users",
                column: "AssignedDoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_InviteCode",
                table: "Users",
                column: "InviteCode",
                unique: true,
                filter: "[InviteCode] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_AssignedDoctorId",
                table: "Users",
                column: "AssignedDoctorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_AssignedDoctorId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_AssignedDoctorId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_InviteCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AssignedDoctorId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "InviteCode",
                table: "Users");
        }
    }
}
