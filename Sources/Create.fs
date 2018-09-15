namespace Reaction

open System.Threading
open System

#if !FABLE_COMPILER
open FSharp.Control
#endif

open Types

module Creation =
    // Create async observable from async worker function
    let ofAsync (worker: AsyncObserver<'a> -> CancellationToken -> Async<unit>) : AsyncObservable<_> =
        let subscribe (aobv : AsyncObserver<_>) : Async<AsyncDisposable> =
            let cancel, token = Core.canceller ()
            let obv = Core.safeObserver aobv

            async {
                Async.StartImmediate (worker obv token)
                return cancel
            }
        subscribe

    // An async observervable that just completes when subscribed.
    let inline empty () : AsyncObservable<'a> =
        ofAsync (fun obv _ -> async {
            do! OnCompleted |> obv
        })

    // An async observervable that just fails with an error when subscribed.
    let inline fail (exn) : AsyncObservable<'a> =
        ofAsync (fun obv _ -> async {
            do! OnError exn |> obv
        })

    let ofSeq (xs: seq<'a>) : AsyncObservable<'a> =
        ofAsync (fun obv token -> async {
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

    let timeout (msecs: int) (period: int): AsyncObservable<bool> =
        let subscribe  (aobv : AsyncObserver<bool>) : Async<AsyncDisposable> =
            let cancel, token = Core.canceller ()
            async {
                let rec handler msecs = async {
                    if not token.IsCancellationRequested then
                        do! Async.Sleep msecs
                        do! OnNext true |> aobv

                        return! handler period
                }

                Async.StartImmediate ((handler msecs), token)
                return cancel
            }

        subscribe