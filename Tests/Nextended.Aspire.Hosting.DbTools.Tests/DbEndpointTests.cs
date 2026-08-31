using Aspire.Hosting.ApplicationModel;
using Xunit;

namespace Nextended.Aspire.Hosting.DbTools.Tests;

/// A connection string as the five things every engine's command line asks for.
///
/// The same field is spelled three ways depending on whose documentation somebody had open, and two
/// of those spellings contain a space — so this is where "any string somebody actually has" is
/// turned into something a script can use.
public class DbEndpointTests
{
    private static (string Host, string Port, string User, string Password, string Database)
        Parts(string connectionString, int defaultPort = 5432)
    {
        var end = DbEndpoint.Parse(connectionString, defaultPort);

        Assert.False(end.IsWhole, "a string known now is taken apart now");

        return (Text(end.Host), Text(end.Port), Text(end.User), Text(end.Password), Text(end.Database));

        // A literal reference expression is its own format string.
        static string Text(ReferenceExpression? expression) =>
            expression?.Format ?? "";
    }

    [Fact]
    public void The_form_Aspire_hands_out()
    {
        Assert.Equal(("pg", "5432", "postgres", "secret", "shop"),
            Parts("Host=pg;Port=5432;Username=postgres;Password=secret;Database=shop"));
    }

    [Fact]
    public void The_form_SQL_Server_documentation_uses()
    {
        // Including the port written with a comma, which is SQL Server's own way.
        Assert.Equal(("localhost", "1433", "sa", "P@ssw0rd", "orders"),
            Parts("Server=localhost,1433;Database=orders;User Id=sa;Password=P@ssw0rd;"
                  + "TrustServerCertificate=True", 1433));
    }

    [Fact]
    public void The_spellings_the_other_providers_use()
    {
        Assert.Equal(("box", "5432", "app", "hunter2", "erp"),
            Parts("Data Source=box;Initial Catalog=erp;Uid=app;Pwd=hunter2"));

        Assert.Equal(("box", "5432", "app", "hunter2", "erp"),
            Parts("Address=box;Db=erp;User=app;Password=hunter2"));
    }

    [Fact]
    public void The_uri_form_every_engines_own_client_takes()
    {
        Assert.Equal(("db.example.org", "6543", "ada", "l0velace", "shop"),
            Parts("postgres://ada:l0velace@db.example.org:6543/shop?sslmode=require"));
    }

    [Fact]
    public void A_uri_with_no_credentials_and_no_port()
    {
        Assert.Equal(("mongo", "27017", "", "", "events"),
            Parts("mongodb://mongo/events", 27017));
    }

    [Fact]
    public void A_uri_with_nothing_but_a_host()
    {
        Assert.Equal(("cache", "6379", "", "", ""), Parts("redis://cache", 6379));
    }

    [Fact]
    public void An_escaped_password_comes_back_unescaped()
    {
        // A password with an @ in it has to be escaped in a URI, and the tools want the real one.
        Assert.Equal(("host", "5432", "ada", "p@ss word", "shop"),
            Parts("postgres://ada:p%40ss%20word@host/shop"));
    }

    [Fact]
    public void The_port_falls_back_to_the_engines_usual_one()
    {
        Assert.Equal("3306", Parts("Server=box;Database=shop;Uid=root;Pwd=x", 3306).Port);
    }

    [Fact]
    public void A_host_with_a_port_stuck_to_it()
    {
        Assert.Equal(("box", "5433", "", "", ""), Parts("Host=box:5433"));
    }

    [Fact]
    public void Nothing_is_not_a_connection_string()
    {
        Assert.Throws<ArgumentException>(() => DbEndpoint.Parse("", 5432));
        Assert.Throws<ArgumentException>(() => DbEndpoint.Parse("   ", 5432));
    }

    [Fact]
    public void A_whole_string_stays_whole()
    {
        // A parameter's value is not known until the stack runs, so it goes to the container as it
        // is and the script takes it apart there.
        var end = DbEndpoint.Whole(
            ReferenceExpression.Create($"whatever"));

        Assert.True(end.IsWhole);
        Assert.Null(end.Host);
    }
}
