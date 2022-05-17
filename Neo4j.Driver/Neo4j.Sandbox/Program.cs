using Neo4j.Driver;

var driver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic("neo4j", "pass"));

await using var session = driver.ReducedSession();

