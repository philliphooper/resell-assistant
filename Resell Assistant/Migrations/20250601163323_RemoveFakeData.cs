using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Resell_Assistant.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFakeData : Migration
    {        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove all existing fake data to ensure clean start with real data only
            migrationBuilder.Sql("DELETE FROM Deals;");
            migrationBuilder.Sql("DELETE FROM PriceHistory;");
            migrationBuilder.Sql("DELETE FROM UserPortfolio;");
            migrationBuilder.Sql("DELETE FROM SearchAlerts;");
            migrationBuilder.Sql("DELETE FROM Products;");
            
            // Reset identity sequences to start from 1
            migrationBuilder.Sql("DELETE FROM sqlite_sequence WHERE name IN ('Products', 'Deals', 'SearchAlerts', 'PriceHistory', 'UserPortfolio');");
        }        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Note: Down migration intentionally left empty
            // Re-adding fake data would be counterproductive to the goal of using real data only
        }
    }
}
