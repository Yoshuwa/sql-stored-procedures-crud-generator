namespace CrudGenerator.Core;

public sealed record StoredProcedureParameter(
    string Name,
    string TypeName,
    short MaxLength,
    byte Precision,
    byte Scale,
    bool IsOutput)
{
    public string DisplayType => TypeName.ToLowerInvariant() switch
    {
        "nvarchar" or "nchar" => $"{TypeName}({FormatLength(MaxLength < 0 ? -1 : MaxLength / 2)})",
        "varchar" or "char" or "varbinary" or "binary" => $"{TypeName}({FormatLength(MaxLength)})",
        "decimal" or "numeric" => $"{TypeName}({Precision}, {Scale})",
        "datetime2" or "datetimeoffset" or "time" => $"{TypeName}({Scale})",
        _ => TypeName
    };

    public string Direction => IsOutput ? "Input / output" : "Input";

    private static string FormatLength(int length) => length < 0 ? "max" : length.ToString();
}
