using CrudGenerator.Core;

namespace CrudGenerator.Core.Tests;

public sealed class StoredProcedureParameterTests
{
    [Theory]
    [InlineData("nvarchar", -1, 0, 0, "nvarchar(max)")]
    [InlineData("nvarchar", 200, 0, 0, "nvarchar(100)")]
    [InlineData("varchar", 80, 0, 0, "varchar(80)")]
    [InlineData("decimal", 17, 18, 2, "decimal(18, 2)")]
    [InlineData("datetime2", 8, 0, 7, "datetime2(7)")]
    [InlineData("int", 4, 10, 0, "int")]
    public void DisplayType_FormatsSqlMetadata(
        string typeName, short maxLength, byte precision, byte scale, string expected)
    {
        var parameter = new StoredProcedureParameter("@Value", typeName, maxLength, precision, scale, false);

        Assert.Equal(expected, parameter.DisplayType);
        Assert.Equal("Input", parameter.Direction);
    }

    [Fact]
    public void Direction_ReportsOutputParameters()
    {
        var parameter = new StoredProcedureParameter("@Value", "int", 4, 10, 0, true);

        Assert.Equal("Input / output", parameter.Direction);
    }
}
