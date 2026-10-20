using ExpenseTracker.Infrastructure.Migrations;

namespace ExpenseTracker.Tests.Unit;

public class SqlMigrationRunnerTests
{
    [Fact]
    public void LoadScripts_reads_the_numbered_sql_files_from_Domain_in_order()
    {
        var scripts = SqlMigrationRunner.LoadScripts();

        Assert.Equal([0, 1, 2, 3], scripts.Select(script => script.Version).ToArray());
        Assert.Equal("001_create_users", scripts[1].Name);
        Assert.Equal("002_create_categories", scripts[2].Name);
        Assert.Equal("003_create_transactions", scripts[3].Name);
        Assert.Contains("CREATE TABLE users", scripts[1].Sql);
        Assert.Contains("ck_transactions_amount_positive", scripts[3].Sql);
        Assert.Contains("ix_categories_user_id_kind_name", scripts[2].Sql);
    }
}
