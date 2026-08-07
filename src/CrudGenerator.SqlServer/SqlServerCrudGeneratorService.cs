using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using CrudGenerator.Core;
using Microsoft.Data.SqlClient;

namespace CrudGenerator.SqlServer;

public sealed partial class SqlServerCrudGeneratorService : ICrudGeneratorService
{
    private static readonly HashSet<string> AllowedTimeFunctions = new(StringComparer.OrdinalIgnoreCase)
    {
        "SYSDATETIMEOFFSET()", "SYSUTCDATETIME()", "SYSDATETIME()",
        "GETUTCDATE()", "GETDATE()", "CURRENT_TIMESTAMP"
    };

    public async Task<IReadOnlyList<string>> GetDatabasesAsync(ConnectionProfile profile, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection(profile, connectToMaster: true);
        await connection.OpenAsync(cancellationToken);
        const string sql = """
            SELECT name
            FROM sys.databases
            WHERE state = 0 AND HAS_DBACCESS(name) = 1
            ORDER BY name;
            """;
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var databases = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
            databases.Add(reader.GetString(0));
        return databases;
    }

    public async Task<IReadOnlyList<StoredProcedureInfo>> GetGeneratedProceduresAsync(
        ConnectionProfile profile, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection(profile);
        await connection.OpenAsync(cancellationToken);
        const string sql = """
            SELECT s.name, p.name, p.modify_date, m.definition
            FROM sys.procedures AS p
            INNER JOIN sys.schemas AS s ON s.schema_id = p.schema_id
            INNER JOIN sys.sql_modules AS m ON m.object_id = p.object_id
            WHERE p.is_ms_shipped = 0
              AND NOT (s.name = N'dbo' AND p.name = N'sp_CRUDGen')
            ORDER BY s.name, p.name;
            """;
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var procedures = new List<StoredProcedureInfo>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var definition = reader.IsDBNull(3) ? null : reader.GetString(3);
            if (StoredProcedureInfo.IsGeneratedBySpCrudGen(definition))
                procedures.Add(new StoredProcedureInfo(reader.GetString(0), reader.GetString(1), reader.GetDateTime(2)));
        }
        return procedures;
    }

    public async Task<IReadOnlyList<DatabaseTable>> GetTablesAsync(
        ConnectionProfile profile, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection(profile);
        await connection.OpenAsync(cancellationToken);
        const string sql = """
            SELECT s.name, t.name
            FROM sys.tables AS t
            INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
            WHERE t.is_ms_shipped = 0
            ORDER BY s.name, t.name;
            """;
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var tables = new List<DatabaseTable>();
        while (await reader.ReadAsync(cancellationToken))
            tables.Add(new DatabaseTable(reader.GetString(0), reader.GetString(1)));
        return tables;
    }

    public async Task<bool> IsGeneratorInstalledAsync(ConnectionProfile profile, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection(profile);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(
            "SELECT CASE WHEN OBJECT_ID(N'dbo.sp_CRUDGen', N'P') IS NULL THEN 0 ELSE 1 END;", connection);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
    }

    public async Task InstallGeneratorAsync(ConnectionProfile profile, string installerScript, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(installerScript))
            throw new ArgumentException("The installer script is empty.", nameof(installerScript));
        await using var connection = CreateConnection(profile);
        await connection.OpenAsync(cancellationToken);
        foreach (var batch in GoLineRegex().Split(installerScript))
        {
            if (string.IsNullOrWhiteSpace(batch)) continue;
            await using var command = new SqlCommand(batch, connection) { CommandTimeout = 120 };
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task<GenerationResult> GenerateAsync(ConnectionProfile profile, DatabaseTable table, GeneratorOptions options,
        bool createProcedures, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(options);
        if (!options.HasSelection) throw new InvalidOperationException("Select at least one stored procedure type.");
        ValidateGenerationInputs(table, options);

        await using var connection = CreateConnection(profile);
        connection.FireInfoMessageEventOnUserErrors = true;
        var messages = new List<string>();
        connection.InfoMessage += (_, args) => messages.Add(args.Message);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.sp_CRUDGen", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 180
        };
        command.Parameters.Add("@GenerateStoredProcedures", SqlDbType.Bit).Value = createProcedures;
        command.Parameters.Add("@SchemaTableOrViewName", SqlDbType.NVarChar, 200).Value = table.DisplayName;
        AddFlag(command, "@GenerateCreate", options.GenerateCreate);
        AddFlag(command, "@GenerateCreateMultiple", options.GenerateCreateMultiple);
        AddFlag(command, "@GenerateRead", options.GenerateRead);
        AddFlag(command, "@GenerateReadEager", options.GenerateReadEager);
        AddFlag(command, "@GenerateUpdate", options.GenerateUpdate);
        AddFlag(command, "@GenerateUpdateMultiple", options.GenerateUpdateMultiple);
        AddFlag(command, "@GenerateUpsert", options.GenerateUpsert);
        AddFlag(command, "@GenerateIndate", options.GenerateIndate);
        AddFlag(command, "@GenerateDelete", options.GenerateDelete);
        AddFlag(command, "@GenerateDeleteMultiple", options.GenerateDeleteMultiple);
        AddFlag(command, "@GenerateSearch", options.GenerateSearch);
        AddText(command, "@SearchSeparatorString", options.SearchSeparatorString);
        AddText(command, "@CreatePersonColumnName", options.CreatePersonColumnName);
        AddFlag(command, "@CreatePersonInclude", options.CreatePersonInclude);
        AddText(command, "@CreateTimeColumnName", options.CreateTimeColumnName);
        AddText(command, "@CreateTimeFunction", options.CreateTimeFunction, 30, unicode: false);
        AddText(command, "@ModifyPersonColumnName", options.ModifyPersonColumnName);
        AddFlag(command, "@ModifyPersonInclude", options.ModifyPersonInclude);
        AddText(command, "@ModifyTimeColumnName", options.ModifyTimeColumnName);
        AddText(command, "@ModifyTimeFunction", options.ModifyTimeFunction, 30, unicode: false);
        AddText(command, "@VersionStampColumnName", options.VersionStampColumnName);
        AddText(command, "@ValidFromTimeColumName", options.ValidFromTimeColumnName);
        AddText(command, "@ValidToTimeColumName", options.ValidToTimeColumnName);

        var output = new StringBuilder();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        do
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                for (var index = 0; index < reader.FieldCount; index++)
                    if (!reader.IsDBNull(index)) output.AppendLine(reader.GetValue(index).ToString());
            }
        } while (await reader.NextResultAsync(cancellationToken));

        if (output.Length == 0 && messages.Count > 0) output.AppendLine(string.Join(Environment.NewLine, messages));
        return new GenerationResult(output.ToString().Trim(), messages);
    }

    public async Task<IReadOnlyList<ProcedureTestResult>> TestGeneratedProceduresAsync(
        ConnectionProfile profile, DatabaseTable table, GeneratorOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(options);
        if (!options.HasSelection) throw new InvalidOperationException("Select at least one stored procedure type.");
        ValidateGenerationInputs(table, options);

        await using var connection = CreateConnection(profile);
        await connection.OpenAsync(cancellationToken);
        var results = new List<ProcedureTestResult>();
        foreach (var suffix in options.SelectedProcedureSuffixes)
        {
            var procedureName = table.Name + suffix;
            var qualifiedName = $"{table.Schema}.{procedureName}";
            const string definitionSql = """
                SELECT m.definition
                FROM sys.procedures AS p
                INNER JOIN sys.schemas AS s ON s.schema_id = p.schema_id
                INNER JOIN sys.sql_modules AS m ON m.object_id = p.object_id
                WHERE s.name = @SchemaName AND p.name = @ProcedureName;
                """;
            await using var definitionCommand = new SqlCommand(definitionSql, connection);
            definitionCommand.Parameters.Add("@SchemaName", SqlDbType.NVarChar, 128).Value = table.Schema;
            definitionCommand.Parameters.Add("@ProcedureName", SqlDbType.NVarChar, 128).Value = procedureName;
            var definition = await definitionCommand.ExecuteScalarAsync(cancellationToken) as string;
            if (definition is null)
            {
                results.Add(new ProcedureTestResult(qualifiedName, false, "Procedure was not found."));
                continue;
            }
            if (!StoredProcedureInfo.IsGeneratedBySpCrudGen(definition))
            {
                results.Add(new ProcedureTestResult(qualifiedName, false, "Procedure does not contain the sp_CRUDGen marker."));
                continue;
            }

            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                await using var refreshCommand = new SqlCommand("sys.sp_refreshsqlmodule", connection, transaction)
                {
                    CommandType = CommandType.StoredProcedure
                };
                refreshCommand.Parameters.Add("@name", SqlDbType.NVarChar, 776).Value = qualifiedName;
                await refreshCommand.ExecuteNonQueryAsync(cancellationToken);
                await transaction.RollbackAsync(cancellationToken);
                results.Add(new ProcedureTestResult(qualifiedName, true, "Exists, is generated by sp_CRUDGen, and SQL Server rebound it successfully."));
            }
            catch (SqlException exception)
            {
                try { await transaction.RollbackAsync(CancellationToken.None); }
                catch (InvalidOperationException) { }
                results.Add(new ProcedureTestResult(qualifiedName, false, exception.Message));
            }
        }
        return results;
    }

    private static SqlConnection CreateConnection(ConnectionProfile profile, bool connectToMaster = false)
    {
        if (connectToMaster) profile.ValidateServer(); else profile.Validate();
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = profile.Server.Trim(),
            InitialCatalog = connectToMaster ? "master" : profile.Database.Trim(),
            IntegratedSecurity = profile.UseIntegratedSecurity,
            Encrypt = true,
            TrustServerCertificate = profile.TrustServerCertificate,
            ApplicationName = "SQL Stored Procedures CRUD Generator",
            ConnectTimeout = 15
        };
        if (!profile.UseIntegratedSecurity) { builder.UserID = profile.UserName; builder.Password = profile.Password; }
        return new SqlConnection(builder.ConnectionString);
    }

    private static void AddFlag(SqlCommand command, string name, bool value) =>
        command.Parameters.Add(name, SqlDbType.Bit).Value = value;

    private static void AddText(SqlCommand command, string name, string value, int size = -1, bool unicode = true) =>
        command.Parameters.Add(name, unicode ? SqlDbType.NVarChar : SqlDbType.VarChar, size).Value = value ?? string.Empty;

    private static void ValidateGenerationInputs(DatabaseTable table, GeneratorOptions options)
    {
        if (table.DisplayName.Length > 200)
            throw new InvalidOperationException("The selected schema-qualified table name exceeds sp_CRUDGen's 200-character limit.");
        if (!AllowedTimeFunctions.Contains(options.CreateTimeFunction))
            throw new InvalidOperationException("Select a documented created-time function.");
        if (!AllowedTimeFunctions.Contains(options.ModifyTimeFunction))
            throw new InvalidOperationException("Select a documented modified-time function.");
    }

    [GeneratedRegex(@"(?im)^\s*GO\s*(?:--.*)?$")]
    private static partial Regex GoLineRegex();
}
