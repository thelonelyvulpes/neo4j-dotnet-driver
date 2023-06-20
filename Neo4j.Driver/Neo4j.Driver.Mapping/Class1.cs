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
            cfg => cfg.WithMapping(new MappingBuilder<ExampleRecord>()));
    }
}

public class ExampleRecord{
    public string Name { get; set; }
    public int Age { get; set; }
};