namespace CrudGenerator.Core;

public interface ICrudGeneratorService
{
    Task TestConnectionAsync(ConnectionProfile profile, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DatabaseObject>> GetObjectsAsync(ConnectionProfile profile, CancellationToken cancellationToken = default);
    Task<bool> IsGeneratorInstalledAsync(ConnectionProfile profile, CancellationToken cancellationToken = default);
    Task InstallGeneratorAsync(ConnectionProfile profile, string installerScript, CancellationToken cancellationToken = default);
    Task<GenerationResult> GenerateAsync(ConnectionProfile profile, DatabaseObject databaseObject,
        GeneratorOptions options, bool createProcedures, CancellationToken cancellationToken = default);
}
