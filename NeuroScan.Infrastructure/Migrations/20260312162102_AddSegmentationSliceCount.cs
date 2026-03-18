using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeuroScan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSegmentationSliceCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SegmentationSliceCount",
                table: "AnalysisResults",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SegmentationSliceCount",
                table: "AnalysisResults");
        }
    }
}
