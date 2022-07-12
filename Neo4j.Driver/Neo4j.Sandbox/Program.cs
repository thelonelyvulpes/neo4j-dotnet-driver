using Neo4j.Driver;

namespace Neo4j.Sandbox;

class Program
{
    static async Task Main()
    {
        var driver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic("neo4j", "pass"));

        var write = await driver.QueryAsync(@"CREATE (n: Example { field: 'example', found: false }) return n.field");

        await driver.ExecuteAsync(async x =>
        {
            await x.QueryAsync("");
        }, TransactionClusterMemberAccess.Writers).ConfigureAwait(false);
    }
}

class Example
{
    public string Field { get; set; }
    public bool Found { get; set; }
}