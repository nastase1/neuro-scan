using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeuroScan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DoctorApproved",
                table: "AnalysisResults",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DoctorReviewNotes",
                table: "AnalysisResults",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DoctorApproved",
                table: "AnalysisResults");

            migrationBuilder.DropColumn(
                name: "DoctorReviewNotes",
                table: "AnalysisResults");
        }
    }
}
