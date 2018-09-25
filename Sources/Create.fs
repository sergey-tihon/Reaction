namespace Reaction

open System.Threading
open System

#if !FABLE_COMPILER
open FSharp.Control
#endif

open Types

module Creation =
    // Create async observable from async worker function
    let ofAsyncWorker (worker: AsyncObserver<'a> -> CancellationToken -> Async<unit>) : AsyncObservable<_> =
        let subscribe (aobv : AsyncObserver<_>) : Async<AsyncDisposable> =
            let cancel, token = Core.canceller ()
            let obv = Core.safeObserver aobv

            async {
                Async.StartImmediate (worker obv token)
                return cancel
            }
        subscribe

    let inline ofAsync(workflow : Async<'a>)  : AsyncObservable<'a> =
        ofAsyncWorker (fun obv _ -> async {
            let! result = workflow
            do! OnNext result |> obv
            do! OnCompleted |> obv
        })

    // An async observervable that just completes when subscribed.
    let inline empty () : AsyncObservable<'a> =
        ofAsyncWorker (fun obv _ -> async {
            do! OnCompleted |> obv
        })

    // An async observervable that just fails with an error when subscribed.
    let inline fail (exn) : AsyncObservable<'a> =
        ofAsyncWorker (fun obv _ -> async {
            do! OnError exn |> obv
        })

    let ofSeq (xs: seq<'a>) : AsyncObservable<'a> =
        ofAsyncWorker (fun obv token -> async {
            for x in xs do
                if token.IsCancellationRequested then
                    raise <| OperationCanceledException("Operation cancelled")

                try
                    do! OnNext x |> obv
                with ex ->
                    do! OnError ex |> obv

            do! OnCompleted |> obv
        })

#if !FABLE_COMPILER
    let ofAsyncSeq (xs: AsyncSeq<'a>) : AsyncObservable<'a> =
        let subscribe  (aobv : AsyncObserver<'a>) : Async<AsyncDisposable> =
            let cancel, token = Core.canceller ()
            let mutable running = true

            async {
                let ie = xs.GetEnumerator ()

                let rec loop () =
                    async {
                        let! result =
                            async {
                                try
                                    let! value = ie.MoveNext()
                                    return Ok value
                                with
                                | ex -> return Error ex
                            }

                        match result with
                        | Ok notification ->
                            match notification with
                            | Some x ->
                                do! OnNext x |> aobv
                                do! loop ()
                            | None ->
                                do! OnCompleted |> aobv
                        | Error err ->
                            do! OnError err |> aobv
                    }

                Async.StartImmediate (loop (), token)
                return cancel
            }
        subscribe
#endif
    let inline single (x : 'a) : AsyncObservable<'a> =
        ofSeq [ x ]

    let defer (factory: unit -> AsyncObservable<'a>) : AsyncObservable<'a> =
        let subscribe  (aobv : AsyncObserver<'a>) : Async<AsyncDisposable> =
            async {
                let result =
                    try
                        factory ()
                    with
                    | ex ->
                        fail ex

                return! result aobv
            }
        subscribe

    let timer (msecs: int) (period: int): AsyncObservable<int> =
        let subscribe  (aobv : AsyncObserver<int>) : Async<AsyncDisposable> =
            let cancel, token = Core.canceller ()
            async {
                let rec handler msecs next = async {
                    do! Async.Sleep msecs
                    do! OnNext next |> aobv

                    if period > 0 then
                        return! handler period (next + 1)
                    else
                        do! aobv OnCompleted
                }

                Async.StartImmediate ((handler msecs 0), token)
                return cancel
            }

        subscribe