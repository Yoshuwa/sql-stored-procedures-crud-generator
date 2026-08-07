namespace CrudGenerator.Core;

public sealed record DatabaseObject(string Schema, string Name, DatabaseObjectType Type)
{
    public string QualifiedName => $"{Schema}.{Name}";
    public string DisplayName => $"{Schema}.{Name}";
}

public enum DatabaseObjectType { Table, View }
