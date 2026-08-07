using CrudGenerator.Core;
using CrudGenerator.SqlServer;
using Microsoft.Data.SqlClient;

namespace CrudGenerator.Core.Tests;

public sealed class SingleTableGenerationIntegrationTests
{
    [Fact]
    public async Task SelectedTable_CanBePreviewedCreatedAndTested()
    {
        var server = Environment.GetEnvironmentVariable("CRUDGEN_TEST_SERVER");
        if (string.IsNullOrWhiteSpace(server)) return;

        var database = "CrudGenIntegration_" + Guid.NewGuid().ToString("N")[..12];
        var masterConnectionString = new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = "master",
            IntegratedSecurity = true,
            Encrypt = true,
            TrustServerCertificate = true
        }.ConnectionString;

        await using var master = new SqlConnection(masterConnectionString);
        await master.OpenAsync();
        await ExecuteAsync(master, $"CREATE DATABASE [{database}];");
        try
        {
            var profile = new ConnectionProfile(server, database, true, "", "");
            var service = new SqlServerCrudGeneratorService();
            var table = new DatabaseTable("dbo", "Widget");
            var options = CreateTwoProcedureOptions();

            await using (var target = new SqlConnection(new SqlConnectionStringBuilder(masterConnectionString)
            {
                InitialCatalog = database
            }.ConnectionString))
            {
                await target.OpenAsync();
                await ExecuteAsync(target, "CREATE TABLE dbo.Widget (WidgetId int IDENTITY PRIMARY KEY, Name nvarchar(100) NOT NULL);");
            }

            var installer = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "sql", "sp_CRUDGen.sql"));
            await service.InstallGeneratorAsync(profile, installer);
            Assert.Contains(table, await service.GetTablesAsync(profile));

            var preview = await service.GenerateAsync(profile, table, options, false);
            Assert.Contains("[dbo].[WidgetCreate]", preview.Sql, StringComparison.OrdinalIgnoreCase);
            await service.GenerateAsync(profile, table, options, true);

            var procedures = await service.GetGeneratedProceduresAsync(profile);
            Assert.Contains(procedures, item => item.DisplayName == "dbo.WidgetCreate");
            Assert.Contains(procedures, item => item.DisplayName == "dbo.WidgetRead");
            Assert.DoesNotContain(procedures, item => item.DisplayName == "dbo.sp_CRUDGen");

            var tests = await service.TestGeneratedProceduresAsync(profile, table, options);
            Assert.Equal(2, tests.Count);
            Assert.All(tests, result => Assert.True(result.Passed, result.Message));
        }
        finally
        {
            SqlConnection.ClearAllPools();
            await ExecuteAsync(master, $"ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}];");
        }
    }

    private static GeneratorOptions CreateTwoProcedureOptions() => new()
    {
        GenerateCreate = true,
        GenerateCreateMultiple = false,
        GenerateRead = true,
        GenerateReadEager = false,
        GenerateUpdate = false,
        GenerateUpdateMultiple = false,
        GenerateUpsert = false,
        GenerateIndate = false,
        GenerateDelete = false,
        GenerateDeleteMultiple = false,
        GenerateSearch = false
    };

    private static async Task ExecuteAsync(SqlConnection connection, string sql)
    {
        await using var command = new SqlCommand(sql, connection) { CommandTimeout = 120 };
        await command.ExecuteNonQueryAsync();
    }
}
