using CrudGenerator.Core;

namespace CrudGenerator.Core.Tests;

public sealed class DatabaseTableTests
{
    [Fact]
    public void DisplayName_UsesSchemaQualifiedName()
    {
        var subject = new DatabaseTable("sales", "Order");

        Assert.Equal("sales.Order", subject.DisplayName);
    }
}
