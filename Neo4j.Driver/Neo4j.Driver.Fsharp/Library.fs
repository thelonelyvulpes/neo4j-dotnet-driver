namespace Neo4j.Driver.FSharp

open System
open System.Collections.Generic
open System.Runtime.CompilerServices
open Neo4j.Driver

type FSession(session: IAsyncSession) =
    let inner = session
    interface IAsyncDisposable with
        member this.DisposeAsync() =
            printf "disposing async"
            inner.DisposeAsync()
    interface IDisposable with
        member this.Dispose() =
            printf "disposing"
            inner.Dispose()
    member this.AsyncRun (query:string, ?parameters:Map<string, obj>) =
        Async.AwaitTask(inner.RunAsync(query, parameters)) 

[<Extension>]
type DriverExtensions =
    [<Extension>]
    static member Session(driver:IDriver): FSession = new FSession(driver.AsyncSession())
    
[<Extension>]
type QueryExtensions =
    [<Extension>]
    static member execute<'T, 'TOut>(query: IConfiguredQuery<'T, 'TOut>):
        Async<Result<EagerResult<IReadOnlyList<'TOut>>, Exception>> =
            async {
                try 
                    let! r = query.ExecuteAsync() |> Async.AwaitTask
                    return Result.Ok r
                with
                | e -> return Result.Error e
            }
    [<Extension>]
    static member execute<'TOut>(query: IReducedExecutableQuery<'TOut>): Async<Result<EagerResult<'TOut>, Exception>> = 
        async {
            try 
                let! r = query.ExecuteAsync() |> Async.AwaitTask
                return Result.Ok r
            with
            | e -> return Result.Error e
        }
