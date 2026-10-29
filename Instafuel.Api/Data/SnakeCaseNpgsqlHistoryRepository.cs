using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations.Internal;

namespace Instafuel.Api.Data;

// EF's base history query uses PascalCase identifiers, while the project's
// naming-convention plugin creates the history table with snake_case columns.
// Keep the runtime query aligned with the actual history table schema.
#pragma warning disable EF1001
public sealed class SnakeCaseNpgsqlHistoryRepository(HistoryRepositoryDependencies dependencies)
    : NpgsqlHistoryRepository(dependencies)
{
    protected override string GetAppliedMigrationsSql => $"""
        SELECT {SqlGenerationHelper.DelimitIdentifier("migration_id")},
               {SqlGenerationHelper.DelimitIdentifier("product_version")}
        FROM {SqlGenerationHelper.DelimitIdentifier(TableName, TableSchema)}
        ORDER BY {SqlGenerationHelper.DelimitIdentifier("migration_id")};
        """;
}
#pragma warning restore EF1001
