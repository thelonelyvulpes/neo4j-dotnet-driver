using Neo4j.Driver;

namespace Neo4j.Sandbox;

class Program
{
    static async Task Main()
    {
        var driver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic("neo4j", "pass"));

        var write = await driver.WriteAsync(@"CREATE (n: Example { field: 'example', found: false }) return n.field");
        var theRest = await driver.ReadAsync<Example>(@"
            MATCH (n: Example {field: $field}) 
            RETURN n.field as field, n.found as found SKIP 1",
            new { field = "example" });
    }
}

class Example
{
    public string Field { get; set; }
    public bool Found { get; set; }
}