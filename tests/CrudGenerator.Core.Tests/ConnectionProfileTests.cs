using CrudGenerator.Core;

namespace CrudGenerator.Core.Tests;

public sealed class ConnectionProfileTests
{
    [Fact]
    public void Validate_AcceptsIntegratedSecurityWithoutCredentials()
    {
        var subject = new ConnectionProfile("localhost", "Example", true);

        subject.Validate();
    }

    [Fact]
    public void Validate_RequiresAUserNameForSqlAuthentication()
    {
        var subject = new ConnectionProfile("localhost", "Example", false);

        var exception = Assert.Throws<ArgumentException>(subject.Validate);
        Assert.Contains("user name", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateServer_DoesNotRequireADatabase()
    {
        var subject = new ConnectionProfile("localhost", "", true);

        subject.ValidateServer();
    }

    [Theory]
    [InlineData("", "Example")]
    [InlineData("localhost", "")]
    public void Validate_RejectsMissingConnectionTargets(string server, string database)
    {
        var subject = new ConnectionProfile(server, database, true);

        Assert.Throws<ArgumentException>(subject.Validate);
    }
}
