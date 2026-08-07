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

    public bool HasSelection => GenerateCreate || GenerateCreateMultiple || GenerateRead ||
        GenerateReadEager || GenerateUpdate || GenerateUpdateMultiple || GenerateUpsert ||
        GenerateIndate || GenerateDelete || GenerateDeleteMultiple || GenerateSearch;
}
