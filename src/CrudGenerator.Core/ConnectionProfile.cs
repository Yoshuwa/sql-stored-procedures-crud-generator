namespace CrudGenerator.Core;

public sealed record ConnectionProfile(
    string Server,
    string Database,
    bool UseIntegratedSecurity,
    string? UserName = null,
    string? Password = null,
    bool TrustServerCertificate = true)
{
    public void ValidateServer()
    {
        if (string.IsNullOrWhiteSpace(Server))
            throw new ArgumentException("A SQL Server name is required.", nameof(Server));
        if (!UseIntegratedSecurity && string.IsNullOrWhiteSpace(UserName))
            throw new ArgumentException("A user name is required for SQL authentication.", nameof(UserName));
    }

    public void Validate()
    {
        ValidateServer();
        if (string.IsNullOrWhiteSpace(Database))
            throw new ArgumentException("A database name is required.", nameof(Database));
    }
}
