using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diary.Migrations
{
    /// <inheritdoc />
    public partial class AddIdsForGenres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
            table: "Genres",
            columns: new[] { "Id", "Name" },
            values: new object[,]
            {
            { 1, "Akční" },
            { 2, "Komedie" },
            { 3, "Drama" },
            { 4, "Sci-Fi" },
            { 5, "Fantasy" },
            { 6, "Horor" },
            { 7, "Thriller" },
            { 8, "Romantický" },
            { 9, "Dokumentární" },
            { 10, "Ostatní" }
       });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
