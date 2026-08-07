namespace CrudGenerator.Core;

public interface ICrudGeneratorService
{
    Task<IReadOnlyList<string>> GetDatabasesAsync(ConnectionProfile profile, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StoredProcedureInfo>> GetGeneratedProceduresAsync(ConnectionProfile profile, CancellationToken cancellationToken = default);
    Task<bool> IsGeneratorInstalledAsync(ConnectionProfile profile, CancellationToken cancellationToken = default);
    Task InstallGeneratorAsync(ConnectionProfile profile, string installerScript, CancellationToken cancellationToken = default);
    Task<GenerationResult> GenerateAsync(ConnectionProfile profile, GeneratorOptions options,
        bool createProcedures, CancellationToken cancellationToken = default);
}
