namespace Reaction

open System.Collections.Generic
open System.Threading

open Types
open Core

module Streams =
    /// A stream is both an observable sequence as well as an observer.
    /// Each notification is broadcasted to all subscribed observers.
    let stream<'a> () : IAsyncObserver<'a> * IAsyncObservable<'a> =
        let obvs = new List<IAsyncObserver<'a>>()

        let subscribeAsync (aobv: IAsyncObserver<'a>) : Async<IAsyncDisposable> =
            let sobv = safeObserver aobv
            obvs.Add sobv

            async {
                let cancel () = async {
                    obvs.Remove sobv |> ignore
                }
                return AsyncDisposable.Create cancel
            }

        let obv (n : Notification<'a>) =
            async {
                for aobv in obvs do
                    match n with
                    | OnNext x ->
                        try
                            do! aobv.OnNextAsync x
                        with ex ->
                            do!  aobv.OnErrorAsync ex
                    | OnError e -> do! aobv.OnErrorAsync e
                    | OnCompleted -> do! aobv.OnCompletedAsync ()
            }
        let obs = { new IAsyncObservable<'a> with member __.SubscribeAsync o = subscribeAsync o }
        AsyncObserver obv :> IAsyncObserver<'a>, obs

    /// A cold stream that only supports a single subscriber
    let singleStream () : IAsyncObserver<'a> * IAsyncObservable<'a> =
        let mutable oobv: IAsyncObserver<'a> option = None
        let waitTokenSource = new CancellationTokenSource ()

        let subscribeAsync (aobv : IAsyncObserver<'a>) : Async<IAsyncDisposable> =
            let sobv = safeObserver aobv
            if Option.isSome oobv then
                failwith "singleStream: Already subscribed"

            oobv <- Some sobv
            waitTokenSource.Cancel ()

            async {
                let cancel () = async {
                    oobv <- None
                }
                return AsyncDisposable.Create cancel
            }

        let obv (n: Notification<'a>) =
            async {
                while oobv.IsNone do
                    // Wait for subscriber
                    Async.StartImmediate (Async.Sleep 100, waitTokenSource.Token)

                match oobv with
                | Some obv ->
                    match n with
                    | OnNext x ->
                        try
                            do! obv.OnNextAsync x
                        with ex ->
                            do! obv.OnErrorAsync ex
                    | OnError e -> do! obv.OnErrorAsync e
                    | OnCompleted -> do! obv.OnCompletedAsync ()
                | None ->
                    printfn "No observer for %A" n
                    ()
            }
        let obs = { new IAsyncObservable<'a> with member __.SubscribeAsync o = subscribeAsync o }
        AsyncObserver obv :> IAsyncObserver<'a>, obs

    /// A mailbox stream is a subscribable mailbox. Each message is
    /// broadcasted to all subscribed observers.
    let mbStream<'a> () : MailboxProcessor<'a>*IAsyncObservable<'a> =
        let obvs = new List<IAsyncObserver<'a>>()

        let mb = MailboxProcessor.Start(fun inbox ->
            let rec messageLoop _ = async {
                let! msg = inbox.Receive ()

                for aobv in obvs do
                    try
                        do! aobv.OnNextAsync msg
                    with ex ->
                        do! aobv.OnErrorAsync ex
                return! messageLoop ()
            }
            messageLoop ()
        )

        let subscribeAsync (aobv: IAsyncObserver<'a>) : Async<IAsyncDisposable> =
            let sobv = safeObserver aobv
            obvs.Add sobv

            async {
                let cancel () = async {
                    obvs.Remove sobv |> ignore
                }
                return AsyncDisposable.Create cancel
            }
        let obs = { new IAsyncObservable<'a> with member __.SubscribeAsync o = subscribeAsync o }
        mb, obs
