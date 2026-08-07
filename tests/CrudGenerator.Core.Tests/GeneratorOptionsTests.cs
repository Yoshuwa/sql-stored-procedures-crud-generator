using CrudGenerator.Core;

namespace CrudGenerator.Core.Tests;

public sealed class GeneratorOptionsTests
{
    [Fact]
    public void Defaults_SelectTheTenStandardProcedureTypes()
    {
        var subject = new GeneratorOptions();

        Assert.True(subject.HasSelection);
        Assert.False(subject.GenerateIndate);
    }

    [Fact]
    public void HasSelection_IsFalseWhenEverythingIsDisabled()
    {
        var subject = new GeneratorOptions
        {
            GenerateCreate = false,
            GenerateCreateMultiple = false,
            GenerateRead = false,
            GenerateReadEager = false,
            GenerateUpdate = false,
            GenerateUpdateMultiple = false,
            GenerateUpsert = false,
            GenerateIndate = false,
            GenerateDelete = false,
            GenerateDeleteMultiple = false,
            GenerateSearch = false
        };

        Assert.False(subject.HasSelection);
    }

    [Fact]
    public void SelectedProcedureSuffixes_ContainsOnlyEnabledTypes()
    {
        var subject = new GeneratorOptions
        {
            GenerateCreate = true,
            GenerateCreateMultiple = false,
            GenerateRead = true,
            GenerateReadEager = false,
            GenerateUpdate = false,
            GenerateUpdateMultiple = false,
            GenerateUpsert = false,
            GenerateIndate = false,
            GenerateDelete = false,
            GenerateDeleteMultiple = false,
            GenerateSearch = false
        };

        Assert.Equal(["Create", "Read"], subject.SelectedProcedureSuffixes);
    }
}
