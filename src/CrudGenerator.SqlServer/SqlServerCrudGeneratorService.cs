using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using CrudGenerator.Core;
using Microsoft.Data.SqlClient;

namespace CrudGenerator.SqlServer;

public sealed partial class SqlServerCrudGeneratorService : ICrudGeneratorService
{
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

    public async Task<GenerationResult> GenerateAsync(ConnectionProfile profile, GeneratorOptions options,
        bool createProcedures, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!options.HasSelection) throw new InvalidOperationException("Select at least one stored procedure type.");

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
        command.Parameters.Add("@SchemaTableOrViewName", SqlDbType.NVarChar, 200).Value = DBNull.Value;
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

    [GeneratedRegex(@"(?im)^\s*GO\s*(?:--.*)?$")]
    private static partial Regex GoLineRegex();
}
