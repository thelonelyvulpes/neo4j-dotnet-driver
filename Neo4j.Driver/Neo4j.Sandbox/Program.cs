using Neo4j.Driver;

var driver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic("neo4j", "pass"));

await using var session = driver.ReducedSession();

var write = await session.ApplyAsync(@"CREATE (n: Example { field: 'example', found: false }) return n.field");

var count = await session.ScalarAsync<int>("MATCH (n: Example) RETURN count(*) as count");

var firstExample = await session.SingleAsync(new Query("MATCH (n: Example) RETURN n.field as field LIMIT 1"));

var theRest = await session.QueryAsync<Example>("MATCH (n: Example {field: $field}) RETURN n.field as field, n.found as found SKIP 1", new {field = "example"});

var merged = await session.QueryAsync(@"
    MERGE (n:Example {field: $field})
    ON MATCH
        SET
        n.found = true,
        n.lastAccessed = timestamp()
    RETURN n.found as found", 
    new { field = "example" },
    AccessMode.Write,
    x => new Example
    {
        Field = "ignored",
        Found = x["found"].As<bool>()
    });


class Example
{
    public string Field { get; set; }
    public bool Found { get; set; }
}