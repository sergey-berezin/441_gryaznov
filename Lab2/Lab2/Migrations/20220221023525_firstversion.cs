using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lab2.Migrations
{
    public partial class firstversion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DetectedObjects",
                columns: table => new
                {
                    DetectedObjectId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    x1 = table.Column<float>(type: "REAL", nullable: false),
                    y1 = table.Column<float>(type: "REAL", nullable: false),
                    x2 = table.Column<float>(type: "REAL", nullable: false),
                    y2 = table.Column<float>(type: "REAL", nullable: false),
                    BitmapImageObj = table.Column<byte[]>(type: "BLOB", nullable: false),
                    BitmapImageFull = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetectedObjects", x => x.DetectedObjectId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetectedObjects");
        }
    }
}
