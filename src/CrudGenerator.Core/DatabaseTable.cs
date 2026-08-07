namespace CrudGenerator.Core;

public sealed record DatabaseTable(string Schema, string Name)
{
    public string DisplayName => $"{Schema}.{Name}";
}
