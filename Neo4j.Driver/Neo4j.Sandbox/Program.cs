using Neo4j.Driver;

namespace Neo4j.Sandbox;

class Program
{
    static async Task Main()
    {
        var driver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic("neo4j", "pass"));

        await driver.QueryAsync(@"CREATE (n: Example { field: 'example', found: false }) return n.field");

        await driver.ExecuteAsync(async (x, ct) =>
        {
            var blah = await x.QueryAsync("MATCH (n: Example) SET n.found = true", cancellationToken: ct);
            Console.WriteLine(blah.Summary.Counters.ContainsUpdates);
        }, TxAccess.Writers).ConfigureAwait(false);


        await using var session = driver.AsyncSession(x => x.WithDatabase("neo4j"));
        await session.QueryAsync(@"CREATE (n: SessEg { field: 'example', found: false }) return n.field");

        await session.ExecuteAsync(async (x, ct) =>
        {
            var blah = await x.QueryAsync("MATCH (n: SessEg) SET n.found = true", cancellationToken: ct);
            Console.WriteLine(blah.Summary.Counters.ContainsUpdates);
        }, TxAccess.Writers).ConfigureAwait(false);
    }
}

class Example
{
    public string Field { get; set; }
    public bool Found { get; set; }
}