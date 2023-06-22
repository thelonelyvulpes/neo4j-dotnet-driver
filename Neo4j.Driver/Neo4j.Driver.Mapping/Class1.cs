namespace Neo4j.Driver.Mapping;

public class MapRule
{
}

public class ExampleMapping
{
    public class ExampleRecord
    {
        public string Name { get; set; }
        public int Age { get; set; }
    };
    
    public void Setup()
    {
        var destFirst = new MappingBuilder<ExampleRecord>(unspecifiedNameKeyStrategy: NameStrategy.CamelCase )
            .Map(destination: x => x.Age, sourceKey: "age", converter: (long x) => (int)x)
            .Map(x => x.Name, "name")
            .Build();

        // An example of how to use the mapping
        var driver = GraphDatabase.Driver(
            "bolt://localhost:7687",
            AuthTokens.Basic("neo4j", "password"),
            cfg => cfg.WithMapping(mapping));

        var task = driver
            .ExecutableQuery("MATCH (n) RETURN n")
            .Mapping<ExampleRecord>("n")
            .ExecuteAsync();
    }
}

public class MappingSettings : MapValidationRules
{
}

