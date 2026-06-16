using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VISSTA.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDynamicSizes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sizes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sizes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductSizeStocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    SizeId = table.Column<int>(type: "int", nullable: false),
                    Stock = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSizeStocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductSizeStocks_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductSizeStocks_Sizes_SizeId",
                        column: x => x.SizeId,
                        principalTable: "Sizes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductSizeStocks_ProductId_SizeId",
                table: "ProductSizeStocks",
                columns: new[] { "ProductId", "SizeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductSizeStocks_SizeId",
                table: "ProductSizeStocks",
                column: "SizeId");

            migrationBuilder.CreateIndex(
                name: "IX_Sizes_Name",
                table: "Sizes",
                column: "Name",
                unique: true);

            // Seed initial sizes
            migrationBuilder.Sql("INSERT INTO Sizes (Name, DisplayOrder) VALUES ('S', 1), ('M', 2), ('L', 3), ('XL', 4), ('2XL', 5), ('3XL', 6);");

            // Copy S stock
            migrationBuilder.Sql(@"
                INSERT INTO ProductSizeStocks (ProductId, SizeId, Stock, IsAvailable)
                SELECT p.Id, s.Id, p.StockS, p.HasSizeS
                FROM Products p
                CROSS JOIN Sizes s
                WHERE s.Name = 'S';
            ");

            // Copy M stock
            migrationBuilder.Sql(@"
                INSERT INTO ProductSizeStocks (ProductId, SizeId, Stock, IsAvailable)
                SELECT p.Id, s.Id, p.StockM, p.HasSizeM
                FROM Products p
                CROSS JOIN Sizes s
                WHERE s.Name = 'M';
            ");

            // Copy L stock
            migrationBuilder.Sql(@"
                INSERT INTO ProductSizeStocks (ProductId, SizeId, Stock, IsAvailable)
                SELECT p.Id, s.Id, p.StockL, p.HasSizeL
                FROM Products p
                CROSS JOIN Sizes s
                WHERE s.Name = 'L';
            ");

            // Copy XL stock
            migrationBuilder.Sql(@"
                INSERT INTO ProductSizeStocks (ProductId, SizeId, Stock, IsAvailable)
                SELECT p.Id, s.Id, p.StockXL, p.HasSizeXL
                FROM Products p
                CROSS JOIN Sizes s
                WHERE s.Name = 'XL';
            ");

            // Copy 2XL stock
            migrationBuilder.Sql(@"
                INSERT INTO ProductSizeStocks (ProductId, SizeId, Stock, IsAvailable)
                SELECT p.Id, s.Id, 0, 0
                FROM Products p
                CROSS JOIN Sizes s
                WHERE s.Name = '2XL';
            ");

            // Copy 3XL stock
            migrationBuilder.Sql(@"
                INSERT INTO ProductSizeStocks (ProductId, SizeId, Stock, IsAvailable)
                SELECT p.Id, s.Id, 0, 0
                FROM Products p
                CROSS JOIN Sizes s
                WHERE s.Name = '3XL';
            ");

            // Drop old columns from Products
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

            migrationBuilder.DropColumn(
                name: "StockL",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "StockM",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "StockS",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "StockXL",
                table: "Products");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductSizeStocks");

            migrationBuilder.DropTable(
                name: "Sizes");

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

            migrationBuilder.AddColumn<int>(
                name: "StockL",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StockM",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StockS",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StockXL",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "HasSizeL", "HasSizeM", "HasSizeS", "HasSizeXL", "StockS" },
                values: new object[] { true, true, true, true, 34 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "HasSizeL", "HasSizeM", "HasSizeS", "HasSizeXL", "StockS" },
                values: new object[] { true, true, true, true, 28 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "HasSizeL", "HasSizeM", "HasSizeS", "HasSizeXL", "StockS" },
                values: new object[] { true, true, true, true, 41 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "HasSizeL", "HasSizeM", "HasSizeS", "HasSizeXL", "StockS" },
                values: new object[] { true, true, true, true, 22 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "HasSizeL", "HasSizeM", "HasSizeS", "HasSizeXL", "StockS" },
                values: new object[] { true, true, true, true, 18 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "HasSizeL", "HasSizeM", "HasSizeS", "HasSizeXL", "StockS" },
                values: new object[] { true, true, true, true, 16 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "HasSizeL", "HasSizeM", "HasSizeS", "HasSizeXL", "StockS" },
                values: new object[] { true, true, true, true, 30 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "HasSizeL", "HasSizeM", "HasSizeS", "HasSizeXL", "StockS" },
                values: new object[] { true, true, true, true, 24 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "HasSizeL", "HasSizeM", "HasSizeS", "HasSizeXL", "StockS" },
                values: new object[] { true, true, true, true, 14 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "HasSizeL", "HasSizeM", "HasSizeS", "HasSizeXL", "StockS" },
                values: new object[] { true, true, true, true, 50 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "HasSizeL", "HasSizeM", "HasSizeS", "HasSizeXL", "StockS" },
                values: new object[] { true, true, true, true, 60 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "HasSizeL", "HasSizeM", "HasSizeS", "HasSizeXL", "StockS" },
                values: new object[] { true, true, true, true, 12 });
        }
    }
}
