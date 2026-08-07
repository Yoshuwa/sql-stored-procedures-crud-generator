using CrudGenerator.Core;

namespace CrudGenerator.Core.Tests;

public sealed class DatabaseObjectTests
{
    [Fact]
    public void QualifiedName_UsesTheFormatExpectedBySpCrudGen()
    {
        var subject = new DatabaseObject("sales", "Order", DatabaseObjectType.Table);

        Assert.Equal("sales.Order", subject.QualifiedName);
        Assert.Equal("sales.Order", subject.DisplayName);
    }
}
