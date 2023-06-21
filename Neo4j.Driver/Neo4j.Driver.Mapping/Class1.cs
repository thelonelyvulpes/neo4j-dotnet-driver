namespace Neo4j.Driver.Mapping;

public class MapRule
{
}

public class ExampleMapping
{
    public void Setup()
    {
        var mapping = new Mapping<ExampleRecord>();
        mapping.Map("name", (record, value) => record.Name = value);
        mapping.Map("age", (ExampleRecord record, long value) => record.Age = (int)value);

        var driver = GraphDatabase.Driver(
            "bolt://localhost:7687",
            AuthTokens.Basic("neo4j", "password"),
            cfg => cfg.WithMapping(
                new MappingBuilder<ExampleRecord>()
                    .Map("name", (r, v) => r.Name = v)
                    .Map("age", (ExampleRecord r, long v) => r.Age = (int)v)
                    .Build()));

        var task = driver
            .ExecutableQuery("MATCH (n) RETURN n")
            .Mapping<ExampleRecord>("n")
            .ExecuteAsync();
    }
}

public class ExampleRecord{
    public string Name { get; set; }
    public int Age { get; set; }
};