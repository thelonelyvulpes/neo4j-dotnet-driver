using Neo4j.Driver;

class program
{
    static async Task Main()
    {
        var driver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic("neo4j", "pass"));

        await using var session = driver.Session();

        var write = await session.ExecuteAsync(@"CREATE (n: Example { field: 'example', found: false }) return n.field");

        var theRest = await session.QueryAsync<Example>("MATCH (n: Example {field: $field}) RETURN properties(n) SKIP 1", new { field = "example" });

        Console.WriteLine(string.Join(", ", theRest.Select(x => x.Field)));
    }
}

class Example
{
    public string Field { get; set; }
    public bool Found { get; set; }
}