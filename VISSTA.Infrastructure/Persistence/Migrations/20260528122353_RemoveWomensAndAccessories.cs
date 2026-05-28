using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace VISSTA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWomensAndAccessories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 1. Re-point all products away from Women's (1) and Accessories (3) FIRST,
            //       so the cascade delete on those categories doesn't wipe them out.

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CategoryId", "SKU" },
                values: new object[] { 4, "VIS-MEN-TOP-001" });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CategoryId", "SKU" },
                values: new object[] { 5, "VIS-MEN-SHIRT-002" });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "CategoryId", "Description", "Name", "SKU", "Slug" },
                values: new object[] { 6, "A clean tapered cut in a refined cream tone.", "Cream Tailored Trouser", "VIS-MEN-PANT-002", "cream-tailored-trouser" });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "CategoryId", "SKU" },
                values: new object[] { 6, "VIS-MEN-BELT-001" });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "CategoryId", "SKU" },
                values: new object[] { 5, "VIS-MEN-SILK-001" });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "CategoryId", "SKU" },
                values: new object[] { 4, "VIS-MEN-TOTE-001" });

            // ── 2. Now safely delete the empty categories (no products reference them anymore).

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3);
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "ImageUrl", "Name", "ParentCategoryId", "Slug" },
                values: new object[,]
                {
                    { 1, null, "Women's", null, "women" },
                    { 3, null, "Accessories", null, "accessories" }
                });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CategoryId", "SKU" },
                values: new object[] { 1, "VIS-WOM-TOP-001" });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CategoryId", "SKU" },
                values: new object[] { 1, "VIS-WOM-SHIRT-001" });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "CategoryId", "Description", "Name", "SKU", "Slug" },
                values: new object[] { 1, "A clean column silhouette for quiet evening dressing.", "Cream Column Skirt", "VIS-WOM-SKIRT-001", "cream-column-skirt" });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "CategoryId", "SKU" },
                values: new object[] { 3, "VIS-ACC-BELT-001" });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "CategoryId", "SKU" },
                values: new object[] { 3, "VIS-ACC-SILK-001" });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "CategoryId", "SKU" },
                values: new object[] { 3, "VIS-ACC-TOTE-001" });
        }
    }
}
