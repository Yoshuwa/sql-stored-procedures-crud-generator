namespace CrudGenerator.Core;

public interface ICrudGeneratorService
{
    Task<IReadOnlyList<string>> GetDatabasesAsync(ConnectionProfile profile, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DatabaseTable>> GetTablesAsync(ConnectionProfile profile, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StoredProcedureInfo>> GetGeneratedProceduresAsync(ConnectionProfile profile, CancellationToken cancellationToken = default);
    Task<StoredProcedureDetails> GetStoredProcedureDetailsAsync(ConnectionProfile profile,
        StoredProcedureInfo procedure, CancellationToken cancellationToken = default);
    Task<bool> IsGeneratorInstalledAsync(ConnectionProfile profile, CancellationToken cancellationToken = default);
    Task InstallGeneratorAsync(ConnectionProfile profile, string installerScript, CancellationToken cancellationToken = default);
    Task<GenerationResult> GenerateAsync(ConnectionProfile profile, DatabaseTable table, GeneratorOptions options,
        bool createProcedures, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProcedureTestResult>> TestGeneratedProceduresAsync(ConnectionProfile profile,
        DatabaseTable table, GeneratorOptions options, CancellationToken cancellationToken = default);
    Task<ProcedureTestResult> TestGeneratedProcedureAsync(ConnectionProfile profile,
        StoredProcedureInfo procedure, CancellationToken cancellationToken = default);
}
