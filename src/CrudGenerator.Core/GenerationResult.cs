namespace CrudGenerator.Core;

public sealed record GenerationResult(string Sql, IReadOnlyList<string> Messages)
{
    public bool HasSql => !string.IsNullOrWhiteSpace(Sql);
}
