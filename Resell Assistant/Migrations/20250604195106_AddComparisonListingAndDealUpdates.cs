using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Resell_Assistant.Migrations
{
    /// <inheritdoc />
    public partial class AddComparisonListingAndDealUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiscoveryMethod",
                table: "Deals",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalListingsAnalyzed",
                table: "Deals",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ComparisonListings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DealId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ShippingCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Marketplace = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Condition = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Location = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Url = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ImageUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    DateListed = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsSelectedDeal = table.Column<bool>(type: "INTEGER", nullable: false),
                    RankingPosition = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComparisonListings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComparisonListings_Deals_DealId",
                        column: x => x.DealId,
                        principalTable: "Deals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComparisonListings_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComparisonListings_CreatedAt",
                table: "ComparisonListings",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ComparisonListings_DealId",
                table: "ComparisonListings",
                column: "DealId");

            migrationBuilder.CreateIndex(
                name: "IX_ComparisonListings_IsSelectedDeal",
                table: "ComparisonListings",
                column: "IsSelectedDeal");

            migrationBuilder.CreateIndex(
                name: "IX_ComparisonListings_Marketplace",
                table: "ComparisonListings",
                column: "Marketplace");

            migrationBuilder.CreateIndex(
                name: "IX_ComparisonListings_Price",
                table: "ComparisonListings",
                column: "Price");

            migrationBuilder.CreateIndex(
                name: "IX_ComparisonListings_ProductId",
                table: "ComparisonListings",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ComparisonListings_RankingPosition",
                table: "ComparisonListings",
                column: "RankingPosition");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComparisonListings");

            migrationBuilder.DropColumn(
                name: "DiscoveryMethod",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "TotalListingsAnalyzed",
                table: "Deals");
        }
    }
}
