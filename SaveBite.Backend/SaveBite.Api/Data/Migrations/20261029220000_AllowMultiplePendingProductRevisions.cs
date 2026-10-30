using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SaveBite.Backend.Data;

namespace SaveBite.Backend.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261029220000_AllowMultiplePendingProductRevisions")]
public partial class AllowMultiplePendingProductRevisions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
        => migrationBuilder.Sql("DROP INDEX IF EXISTS ux_product_content_revisions_pending_product;");

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.Sql("CREATE UNIQUE INDEX ux_product_content_revisions_pending_product ON product_content_revisions (product_id) WHERE status = 'Pending';");
}
