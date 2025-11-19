using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InsightLearn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentLearningSpaceEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create StudentNotes table
            migrationBuilder.CreateTable(
                name: "StudentNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VideoTimestamp = table.Column<int>(type: "int", nullable: false),
                    NoteText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    IsShared = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsBookmarked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentNotes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentNotes_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes for StudentNotes
            migrationBuilder.CreateIndex(
                name: "IX_StudentNotes_UserId_LessonId",
                table: "StudentNotes",
                columns: new[] { "UserId", "LessonId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentNotes_VideoTimestamp",
                table: "StudentNotes",
                column: "VideoTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_StudentNotes_IsBookmarked",
                table: "StudentNotes",
                column: "IsBookmarked",
                filter: "[IsBookmarked] = 1");

            // Create VideoBookmarks table
            migrationBuilder.CreateTable(
                name: "VideoBookmarks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VideoTimestamp = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BookmarkType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Manual"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoBookmarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoBookmarks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VideoBookmarks_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes for VideoBookmarks
            migrationBuilder.CreateIndex(
                name: "IX_VideoBookmarks_UserId_LessonId",
                table: "VideoBookmarks",
                columns: new[] { "UserId", "LessonId" });

            // Create VideoTranscriptMetadata table
            migrationBuilder.CreateTable(
                name: "VideoTranscriptMetadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MongoDocumentId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "en-US"),
                    ProcessingStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Pending"),
                    WordCount = table.Column<int>(type: "int", nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: true),
                    AverageConfidence = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoTranscriptMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoTranscriptMetadata_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create unique index for VideoTranscriptMetadata
            migrationBuilder.CreateIndex(
                name: "IX_VideoTranscriptMetadata_LessonId_Unique",
                table: "VideoTranscriptMetadata",
                column: "LessonId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VideoTranscriptMetadata_ProcessingStatus",
                table: "VideoTranscriptMetadata",
                column: "ProcessingStatus");

            // Create AIKeyTakeawaysMetadata table
            migrationBuilder.CreateTable(
                name: "AIKeyTakeawaysMetadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MongoDocumentId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TakeawayCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ProcessingStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Pending"),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIKeyTakeawaysMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIKeyTakeawaysMetadata_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create unique index for AIKeyTakeawaysMetadata
            migrationBuilder.CreateIndex(
                name: "IX_AIKeyTakeawaysMetadata_LessonId_Unique",
                table: "AIKeyTakeawaysMetadata",
                column: "LessonId",
                unique: true);

            // Create AIConversations table
            migrationBuilder.CreateTable(
                name: "AIConversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentVideoTimestamp = table.Column<int>(type: "int", nullable: true),
                    MongoDocumentId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MessageCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastMessageAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIConversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIConversations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AIConversations_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            // Create unique index for AIConversations SessionId
            migrationBuilder.CreateIndex(
                name: "IX_AIConversations_SessionId_Unique",
                table: "AIConversations",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AIConversations_UserId",
                table: "AIConversations",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop tables in reverse order
            migrationBuilder.DropTable(name: "AIConversations");
            migrationBuilder.DropTable(name: "AIKeyTakeawaysMetadata");
            migrationBuilder.DropTable(name: "VideoTranscriptMetadata");
            migrationBuilder.DropTable(name: "VideoBookmarks");
            migrationBuilder.DropTable(name: "StudentNotes");
        }
    }
}
