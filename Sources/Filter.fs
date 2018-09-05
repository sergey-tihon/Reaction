namespace Reaction

open Types
open Core

module Filter =
    let chooseAsync (chooser: 'a -> Async<'b option>) (source : AsyncObservable<'a>) : AsyncObservable<'b> =
        let subscribe (obvAsync : Types.AsyncObserver<'b>) =
            async {
                let _obv n =
                    async {
                        match n with
                        | OnNext x ->
                            // Let exceptions bubble to the top
                            match! chooser x with
                            | Some b ->
                                do! OnNext b |> obvAsync
                            | None -> ()
                        | OnError ex -> do! OnError ex |> obvAsync
                        | OnCompleted -> do! OnCompleted |> obvAsync

                    }
                return! _obv |>source
            }
        subscribe

    // The classic filter (where) operator with an async predicate
    let filterAsync (predicate : 'a -> Async<bool>) (source : AsyncObservable<'a>) : AsyncObservable<'a> =
        let predicate' a = async {
            match! predicate a with
            | true -> return Some a
            | _ -> return None
        }
        chooseAsync predicate' source

    let distinctUntilChanged (source : AsyncObservable<'a>) : AsyncObservable<'a> =
        let subscribe (aobv : AsyncObserver<'a>) =
            let safeObserver = safeObserver aobv
            let agent = MailboxProcessor.Start(fun inbox ->
                let rec messageLoop (latest : Notification<'a>) = async {
                    let! n = inbox.Receive()

                    let! latest' = async {
                        match n with
                        | OnNext x ->
                            if n <> latest then
                                try
                                    do! OnNext x |> safeObserver
                                with
                                | ex -> do! OnError ex |> safeObserver
                        | _ ->
                            do! safeObserver n
                        return n
                    }

                    return! messageLoop latest'
                }

                messageLoop OnCompleted // Use as sentinel value as it will not match any OnNext value
            )

            async {
                let obv n =
                    async {
                        agent.Post n
                    }
                return! source obv
            }
        subscribe

    let takeUntil (other: AsyncObservable<'b>) (source : AsyncObservable<'a>) : AsyncObservable<'a> =
        let subscribe (obvAsync : Types.AsyncObserver<'a>) =
            let safeObv = safeObserver obvAsync

            async {
                let _obv n =
                    async {
                        match n with
                        | OnCompleted -> do! OnCompleted |> safeObv
                        | OnError ex -> do! OnError ex |> safeObv
                        | _ -> ()
                    }

                let! sub1 = source safeObv
                let! sub2 = other _obv

                return compositeDisposable [sub1; sub2 ]
            }
        subscribe