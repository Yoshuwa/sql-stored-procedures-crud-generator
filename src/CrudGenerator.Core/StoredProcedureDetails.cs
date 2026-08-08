namespace CrudGenerator.Core;

public sealed record StoredProcedureDetails(
    StoredProcedureInfo Procedure,
    string Definition,
    IReadOnlyList<StoredProcedureParameter> Parameters);
