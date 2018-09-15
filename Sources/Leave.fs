namespace Reaction

#if !FABLE_COMPILER
open System.Threading
open FSharp.Control
open Types

module Leave =
    let toAsyncSeq (source: AsyncObservable<'a>) : AsyncSeq<'a> =
        let ping = new AutoResetEvent false
        let pong = new AutoResetEvent false
        let mutable latest : Notification<'a> = OnCompleted

        let _obv n =
            async {
                latest <- n
                ping.Set () |> ignore
                do! Async.AwaitWaitHandle pong |> Async.Ignore
            }

        let result = asyncSeq {
            let! dispose = source _obv
            let mutable running = true

            while running do
                do! Async.AwaitWaitHandle ping |> Async.Ignore
                match latest with
                | OnNext x ->
                    yield x
                | OnError ex ->
                    running <- false
                    raise ex
                | OnCompleted ->
                    running <- false
                pong.Set () |> ignore

            do! dispose ()
        }
        result
#endif