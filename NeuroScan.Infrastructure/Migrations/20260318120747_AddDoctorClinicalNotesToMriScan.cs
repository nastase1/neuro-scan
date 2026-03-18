using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeuroScan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorClinicalNotesToMriScan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DoctorClinicalNotes",
                table: "MriScans",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DoctorClinicalNotes",
                table: "MriScans");
        }
    }
}
