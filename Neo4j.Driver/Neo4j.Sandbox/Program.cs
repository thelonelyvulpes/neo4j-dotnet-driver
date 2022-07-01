using Neo4j.Driver;

namespace Neo4j.Sandbox;

class Program
{
    static async Task Main()
    {
        var driver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic("neo4j", "pass"));

        var write = await driver.QueryAsync(@"CREATE (n: Example { field: 'example', found: false }) return n.field",
            QueryConfig.Read, );
        var theRest = await driver.QueryAsync<Example>(@"
            MATCH (n: Example {field: $field}) 
            RETURN n.field as field, n.found as found SKIP 1",
            new { field = "example" });

        await using var session = driver.AsyncSession(x => x.WithDatabase("db"));
        var result = await session.QueryAsync(new Query("MERGE (:User {id: $id})", new {id = 10}));
        if (result.Summary.Counters.NodesCreated == 0)
            Console.WriteLine("User Exists");

        await driver.ExecuteAsync(async x =>
        {
            await x.QueryAsync("");
        }).ConfigureAwait(false);

    }
}

class Example
{
    public string Field { get; set; }
    public bool Found { get; set; }
}