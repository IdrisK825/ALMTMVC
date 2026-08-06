using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ALMTMVC.Migrations
{
    /// <inheritdoc />
    public partial class AddEnquiryActivities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EnquiryActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContactEnquiryId = table.Column<int>(type: "int", nullable: false),
                    ActivityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnquiryActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnquiryActivities_ContactEnquiries_ContactEnquiryId",
                        column: x => x.ContactEnquiryId,
                        principalTable: "ContactEnquiries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EnquiryActivities_ContactEnquiryId",
                table: "EnquiryActivities",
                column: "ContactEnquiryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnquiryActivities");
        }
    }
}
