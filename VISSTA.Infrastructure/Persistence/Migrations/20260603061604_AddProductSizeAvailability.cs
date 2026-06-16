using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VISSTA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductSizeAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasSizeL",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasSizeM",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasSizeS",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasSizeXL",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "HasSizeL", "HasSizeM", "HasSizeS", "HasSizeXL" },
                values: new object[] { true, true, true, true });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "HasSizeL", "HasSizeM", "HasSizeS", "HasSizeXL" },
                values: new object[] { true, true, true, true });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "HasSizeL", "HasSizeM", "HasSizeS", "HasSizeXL" },
                values: new object[] { true, true, true, true });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "HasSizeL", "HasSizeM", "HasSizeS", "HasSizeXL" },
                values: new object[] { true, true, true, true });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "HasSizeL", "HasSizeM", "HasSizeS", "HasSizeXL" },
                values: new object[] { true, true, true, true });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "HasSizeL", "HasSizeM", "HasSizeS", "HasSizeXL" },
                values: new object[] { true, true, true, true });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "HasSizeL", "HasSizeM", "HasSizeS", "HasSizeXL" },
                values: new object[] { true, true, true, true });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "HasSizeL", "HasSizeM", "HasSizeS", "HasSizeXL" },
                values: new object[] { true, true, true, true });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "HasSizeL", "HasSizeM", "HasSizeS", "HasSizeXL" },
                values: new object[] { true, true, true, true });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "HasSizeL", "HasSizeM", "HasSizeS", "HasSizeXL" },
                values: new object[] { true, true, true, true });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "HasSizeL", "HasSizeM", "HasSizeS", "HasSizeXL" },
                values: new object[] { true, true, true, true });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "HasSizeL", "HasSizeM", "HasSizeS", "HasSizeXL" },
                values: new object[] { true, true, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasSizeL",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "HasSizeM",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "HasSizeS",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "HasSizeXL",
                table: "Products");
        }
    }
}
