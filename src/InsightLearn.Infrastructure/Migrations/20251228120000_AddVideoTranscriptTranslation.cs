using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsightLearn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoTranscriptTranslation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create VideoTranscriptTranslations table
            migrationBuilder.CreateTable(
                name: "VideoTranscriptTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceLanguage = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "en"),
                    TargetLanguage = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    MongoDocumentId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Pending"),
                    QualityTier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Auto/Ollama"),
                    SegmentCount = table.Column<int>(type: "int", nullable: true),
                    TotalCharacters = table.Column<int>(type: "int", nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "decimal(10,4)", nullable: false, defaultValue: 0.0m),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoTranscriptTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoTranscriptTranslations_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create unique index: one translation per lesson per target language
            migrationBuilder.CreateIndex(
                name: "IX_VideoTranscriptTranslations_LessonId_TargetLanguage_Unique",
                table: "VideoTranscriptTranslations",
                columns: new[] { "LessonId", "TargetLanguage" },
                unique: true);

            // Create performance indexes
            migrationBuilder.CreateIndex(
                name: "IX_VideoTranscriptTranslations_Status",
                table: "VideoTranscriptTranslations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VideoTranscriptTranslations_QualityTier",
                table: "VideoTranscriptTranslations",
                column: "QualityTier");

            migrationBuilder.CreateIndex(
                name: "IX_VideoTranscriptTranslations_LessonId_Status",
                table: "VideoTranscriptTranslations",
                columns: new[] { "LessonId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VideoTranscriptTranslations");
        }
    }
}
