namespace CrudGenerator.Core;

public sealed record GeneratorOptions
{
    public bool GenerateCreate { get; init; } = true;
    public bool GenerateCreateMultiple { get; init; } = true;
    public bool GenerateRead { get; init; } = true;
    public bool GenerateReadEager { get; init; } = true;
    public bool GenerateUpdate { get; init; } = true;
    public bool GenerateUpdateMultiple { get; init; } = true;
    public bool GenerateUpsert { get; init; } = true;
    public bool GenerateIndate { get; init; }
    public bool GenerateDelete { get; init; } = true;
    public bool GenerateDeleteMultiple { get; init; } = true;
    public bool GenerateSearch { get; init; } = true;

    public string SearchSeparatorString { get; init; } = " to ";
    public string CreatePersonColumnName { get; init; } = "CreatePersonId";
    public bool CreatePersonInclude { get; init; }
    public string CreateTimeColumnName { get; init; } = "CreateTime";
    public string CreateTimeFunction { get; init; } = "SYSDATETIMEOFFSET()";
    public string ModifyPersonColumnName { get; init; } = "ModifyPersonId";
    public bool ModifyPersonInclude { get; init; }
    public string ModifyTimeColumnName { get; init; } = "ModifyTime";
    public string ModifyTimeFunction { get; init; } = "SYSDATETIMEOFFSET()";
    public string VersionStampColumnName { get; init; } = "VersionStamp";
    public string ValidFromTimeColumnName { get; init; } = "ValidFromTime";
    public string ValidToTimeColumnName { get; init; } = "ValidToTime";

    public bool HasSelection => GenerateCreate || GenerateCreateMultiple || GenerateRead ||
        GenerateReadEager || GenerateUpdate || GenerateUpdateMultiple || GenerateUpsert ||
        GenerateIndate || GenerateDelete || GenerateDeleteMultiple || GenerateSearch;

    public IReadOnlyList<string> SelectedProcedureSuffixes
    {
        get
        {
            var suffixes = new List<string>();
            Add(GenerateCreate, "Create");
            Add(GenerateCreateMultiple, "CreateMultiple");
            Add(GenerateRead, "Read");
            Add(GenerateReadEager, "ReadEager");
            Add(GenerateUpdate, "Update");
            Add(GenerateUpdateMultiple, "UpdateMultiple");
            Add(GenerateUpsert, "Upsert");
            Add(GenerateIndate, "Indate");
            Add(GenerateDelete, "Delete");
            Add(GenerateDeleteMultiple, "DeleteMultiple");
            Add(GenerateSearch, "Search");
            return suffixes;

            void Add(bool selected, string suffix)
            {
                if (selected) suffixes.Add(suffix);
            }
        }
    }
}
