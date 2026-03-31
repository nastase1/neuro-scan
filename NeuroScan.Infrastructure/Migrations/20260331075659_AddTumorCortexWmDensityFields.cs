using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeuroScan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTumorCortexWmDensityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "CortexThicknessAvg",
                table: "AnalysisResults",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "CortexThicknessMax",
                table: "AnalysisResults",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "CortexThicknessMin",
                table: "AnalysisResults",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<bool>(
                name: "TumorDetected",
                table: "AnalysisResults",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "TumorSurfaceArea",
                table: "AnalysisResults",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "TumorVolume",
                table: "AnalysisResults",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "WmCoefficientOfVariation",
                table: "AnalysisResults",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "WmDensityScore",
                table: "AnalysisResults",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "WmMeanIntensity",
                table: "AnalysisResults",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CortexThicknessAvg",
                table: "AnalysisResults");

            migrationBuilder.DropColumn(
                name: "CortexThicknessMax",
                table: "AnalysisResults");

            migrationBuilder.DropColumn(
                name: "CortexThicknessMin",
                table: "AnalysisResults");

            migrationBuilder.DropColumn(
                name: "TumorDetected",
                table: "AnalysisResults");

            migrationBuilder.DropColumn(
                name: "TumorSurfaceArea",
                table: "AnalysisResults");

            migrationBuilder.DropColumn(
                name: "TumorVolume",
                table: "AnalysisResults");

            migrationBuilder.DropColumn(
                name: "WmCoefficientOfVariation",
                table: "AnalysisResults");

            migrationBuilder.DropColumn(
                name: "WmDensityScore",
                table: "AnalysisResults");

            migrationBuilder.DropColumn(
                name: "WmMeanIntensity",
                table: "AnalysisResults");
        }
    }
}
