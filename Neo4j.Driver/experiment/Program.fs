module blah
open Neo4j.Driver
open Neo4j.Driver.FSharp


type demo =
    static member ex () =
            async {
                use driver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic("neo4j", "password"))
                let! _ = driver.ExecutableQuery("MATCH (n) RETURN n LIMIT 5")
                               .WithParameters({| param = "value" |})
                               .WithConfig(QueryConfig(RoutingControl.Readers, enableBookmarkManager = false))
                               |> QueryExtensions.execute
                use session = driver.Session()
                let! cursor = session.AsyncRun("MATCH (n) RETURN n LIMIT 5")
                let! records = cursor.ToListAsync() |> Async.AwaitTask
                printfn "%A" records 
                return ()
            }

demo.ex() |> Async.RunSynchronously
