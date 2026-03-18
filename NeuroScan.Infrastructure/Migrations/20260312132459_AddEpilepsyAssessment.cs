using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeuroScan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEpilepsyAssessment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AsymmetryIndexModel2",
                table: "AnalysisResults");

            migrationBuilder.DropColumn(
                name: "CsfVolumeModel2",
                table: "AnalysisResults");

            migrationBuilder.DropColumn(
                name: "DiceScoreCsf",
                table: "AnalysisResults");

            migrationBuilder.DropColumn(
                name: "DiceScoreGm",
                table: "AnalysisResults");

            migrationBuilder.DropColumn(
                name: "DiceScoreWm",
                table: "AnalysisResults");

            migrationBuilder.DropColumn(
                name: "DisagreementPercentage",
                table: "AnalysisResults");

            migrationBuilder.DropColumn(
                name: "GmVolumeModel2",
                table: "AnalysisResults");

            migrationBuilder.DropColumn(
                name: "ModelConfidence",
                table: "AnalysisResults");

            migrationBuilder.RenameColumn(
                name: "WmVolumeModel2",
                table: "AnalysisResults",
                newName: "EpilepsyRiskScore");

            migrationBuilder.RenameColumn(
                name: "RecommendedModel",
                table: "AnalysisResults",
                newName: "SegmentationImagePath");

            migrationBuilder.AddColumn<string>(
                name: "EpilepsyRiskLevel",
                table: "AnalysisResults",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EpilepsyRiskLevel",
                table: "AnalysisResults");

            migrationBuilder.RenameColumn(
                name: "SegmentationImagePath",
                table: "AnalysisResults",
                newName: "RecommendedModel");

            migrationBuilder.RenameColumn(
                name: "EpilepsyRiskScore",
                table: "AnalysisResults",
                newName: "WmVolumeModel2");

            migrationBuilder.AddColumn<double>(
                name: "AsymmetryIndexModel2",
                table: "AnalysisResults",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "CsfVolumeModel2",
                table: "AnalysisResults",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "DiceScoreCsf",
                table: "AnalysisResults",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "DiceScoreGm",
                table: "AnalysisResults",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "DiceScoreWm",
                table: "AnalysisResults",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "DisagreementPercentage",
                table: "AnalysisResults",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GmVolumeModel2",
                table: "AnalysisResults",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ModelConfidence",
                table: "AnalysisResults",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
