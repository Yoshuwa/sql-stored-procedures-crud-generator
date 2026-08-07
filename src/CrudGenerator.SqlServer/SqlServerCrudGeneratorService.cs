using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using CrudGenerator.Core;
using Microsoft.Data.SqlClient;

namespace CrudGenerator.SqlServer;

public sealed partial class SqlServerCrudGeneratorService : ICrudGeneratorService
{
    public async Task TestConnectionAsync(ConnectionProfile profile, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection(profile);
        await connection.OpenAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DatabaseObject>> GetObjectsAsync(ConnectionProfile profile, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection(profile);
        await connection.OpenAsync(cancellationToken);
        const string sql = """
            SELECT s.name, o.name, o.type
            FROM sys.objects AS o
            INNER JOIN sys.schemas AS s ON s.schema_id = o.schema_id
            WHERE o.type IN ('U', 'V') AND o.is_ms_shipped = 0
            ORDER BY s.name, o.name;
            """;
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var objects = new List<DatabaseObject>();
        while (await reader.ReadAsync(cancellationToken))
        {
            objects.Add(new DatabaseObject(reader.GetString(0), reader.GetString(1),
                reader.GetString(2) == "V" ? DatabaseObjectType.View : DatabaseObjectType.Table));
        }
        return objects;
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

    public async Task<GenerationResult> GenerateAsync(ConnectionProfile profile, DatabaseObject databaseObject,
        GeneratorOptions options, bool createProcedures, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(databaseObject);
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
        command.Parameters.Add("@SchemaTableOrViewName", SqlDbType.NVarChar, 200).Value = databaseObject.QualifiedName;
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

    private static SqlConnection CreateConnection(ConnectionProfile profile)
    {
        profile.Validate();
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = profile.Server.Trim(),
            InitialCatalog = profile.Database.Trim(),
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
