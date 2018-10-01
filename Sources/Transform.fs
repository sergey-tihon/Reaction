namespace Reaction

open Types
open Core

module Transformation =
    /// Returns an observable sequence whose elements are the result of
    /// invoking the async mapper function on each element of the source.
    let mapAsync (mapper: 'a -> Async<'b>) (source: IAsyncObservable<'a>) : IAsyncObservable<'b> =
        let subscribeAsync (aobv : IAsyncObserver<'b>) : Async<IAsyncDisposable> =
            async {
                let _obv =
                    { new IAsyncObserver<'a> with
                        member this.OnNextAsync x = async {
                            let! b =  mapper x
                            do! aobv.OnNextAsync b
                        }
                        member this.OnErrorAsync err = async {
                            do! aobv.OnErrorAsync err
                        }
                        member this.OnCompletedAsync () = async {
                            do! aobv.OnCompletedAsync ()
                        }
                    }
                return! source.SubscribeAsync _obv
            }
        { new IAsyncObservable<'b> with member __.SubscribeAsync o = subscribeAsync o }

    /// Returns an observable sequence whose elements are the result of
    /// invoking the mapper function on each element of the source.
    let map (mapper:'a -> 'b) (source: IAsyncObservable<'a>) : IAsyncObservable<'b> =
        mapAsync (fun x -> async { return mapper x }) source

    /// Returns an observable sequence whose elements are the result of
    /// invoking the async mapper function by incorporating the element's
    /// index on each element of the source.
    let mapiAsync (mapper:'a*int -> Async<'b>) (source: IAsyncObservable<'a>) : IAsyncObservable<'b> =
        source
        |> Combine.zipSeq Core.infinite
        |> mapAsync mapper

    /// Returns an observable sequence whose elements are the result of
    /// invoking the mapper function and incorporating the element's
    /// index on each element of the source.
    let mapi (mapper:'a*int -> 'b) (source: IAsyncObservable<'a>) : IAsyncObservable<'b> =
        mapiAsync (fun (x, i) -> async { return mapper (x, i) }) source

    /// Projects each element of an observable sequence into an
    /// observable sequence and merges the resulting observable
    /// sequences back into one observable sequence.
    let flatMap (mapper:'a -> IAsyncObservable<'b>) (source: IAsyncObservable<'a>) : IAsyncObservable<'b> =
        source
        |> map mapper
        |> Combine.mergeInner

    /// Projects each element of an observable sequence into an
    /// observable sequence by incorporating the element's
    /// index on each element of the source. Merges the resulting
    /// observable sequences back into one observable sequence.
    let flatMapi (mapper:'a*int -> IAsyncObservable<'b>) (source: IAsyncObservable<'a>) : IAsyncObservable<'b> =
        source
        |> mapi mapper
        |> Combine.mergeInner

    /// Asynchronously projects each element of an observable sequence
    /// into an observable sequence and merges the resulting observable
    /// sequences back into one observable sequence.
    let flatMapAsync (mapper:'a -> Async<IAsyncObservable<'b>>) (source: IAsyncObservable<'a>) : IAsyncObservable<'b> =
        source
        |> mapAsync mapper
        |> Combine.mergeInner

    /// Asynchronously projects each element of an observable sequence
    /// into an observable sequence by incorporating the element's
    /// index on each element of the source. Merges the resulting
    /// observable sequences back into one observable sequence.
    let flatMapiAsync (mapper:'a*int -> Async<IAsyncObservable<'b>>) (source: IAsyncObservable<'a>) : IAsyncObservable<'b> =
        source
        |> mapiAsync mapper
        |> Combine.mergeInner

    /// Transforms an observable sequence of observable sequences into
    /// an observable sequence producing values only from the most
    /// recent observable sequence.
    let switchLatest (source: IAsyncObservable<IAsyncObservable<'a>>) : IAsyncObservable<'a> =
        let subscribeAsync (aobv : IAsyncObserver<'a>) =
            let safeObserver = safeObserver aobv
            let innerAgent =
                let obv (mb: MailboxProcessor<InnerSubscriptionCmd<'a>>) (id: int) = {
                    new IAsyncObserver<'a> with
                        member this.OnNextAsync x = async {
                            do! safeObserver.OnNextAsync x
                        }
                        member this.OnErrorAsync err = async {
                            do! safeObserver.OnErrorAsync err
                        }
                        member this.OnCompletedAsync () = async {
                            mb.Post (InnerCompleted id)
                        }
                    }

                MailboxProcessor.Start(fun inbox ->
                    let rec messageLoop (current: IAsyncDisposable option, isStopped, currentId) = async {
                        let! cmd = inbox.Receive()

                        let! (current', isStopped', currentId') = async {
                            match cmd with
                            | InnerObservable xs ->
                                let nextId = currentId + 1
                                if current.IsSome then
                                    do! current.Value.DisposeAsync ()
                                let! inner = xs.SubscribeAsync (obv inbox nextId)
                                return Some inner, isStopped, nextId
                            | InnerCompleted idx ->
                                if isStopped && idx = currentId then
                                    do! safeObserver.OnCompletedAsync ()
                                    return (None, true, currentId)
                                else
                                    return (current, isStopped, currentId)
                            | Completed ->
                                if current.IsNone then
                                    do! safeObserver.OnCompletedAsync ()
                                return (current, true, currentId)
                            | Dispose ->
                                if current.IsSome then
                                    do! current.Value.DisposeAsync ()
                                return (None, true, currentId)
                        }

                        return! messageLoop (current', isStopped', currentId')
                    }

                    messageLoop (None, false, 0)
                )

            async {
                let obv (ns: Notification<IAsyncObservable<'a>>) =
                    async {
                        match ns with
                        | OnNext xs -> InnerObservable xs |> innerAgent.Post
                        | OnError e -> do! safeObserver.OnErrorAsync e
                        | OnCompleted -> innerAgent.Post Completed
                    }

                let! dispose = AsyncObserver obv |> source.SubscribeAsync
                let cancel () =
                    async {
                        do! dispose.DisposeAsync ()
                        innerAgent.Post Dispose
                    }
                return AsyncDisposable.Create cancel
            }
        { new IAsyncObservable<'a> with member __.SubscribeAsync o = subscribeAsync o }

    /// Asynchronosly transforms the items emitted by an source sequence
    /// into observable streams, and mirror those items emitted by the
    /// most-recently transformed observable sequence.
    let flatMapLatestAsync (mapper: 'a -> Async<IAsyncObservable<'b>>) (source: IAsyncObservable<'a>) : IAsyncObservable<'b> =
        source
        |> mapAsync mapper
        |> switchLatest

    /// Transforms the items emitted by an source sequence into
    /// observable streams, and mirror those items emitted by the
    /// most-recently transformed observable sequence.
    let flatMapLatest (mapper: 'a -> IAsyncObservable<'b>) (source: IAsyncObservable<'a>) : IAsyncObservable<'b> =
        source
        |> map mapper
        |> switchLatest


    /// Returns an observable sequence containing the first sequence's
    /// elements, followed by the elements of the handler sequence in
    /// case an exception occurred.
    let catch (handler: exn -> IAsyncObservable<'a>) (source: IAsyncObservable<'a>) : IAsyncObservable<'a> =
        let subscribeAsync (aobv: IAsyncObserver<'a>) =
            async {
                let mutable disposable = AsyncDisposable.Empty

                let rec action (source: IAsyncObservable<_>) = async {
                    let _obv = {
                        new IAsyncObserver<'a> with
                        member this.OnNextAsync x = async {
                            do! aobv.OnNextAsync x
                        }
                        member this.OnErrorAsync err = async {
                            let nextSource = handler err
                            do! action nextSource
                        }
                        member this.OnCompletedAsync () = async {
                            do! aobv.OnCompletedAsync ()
                        }
                    }
                    do! disposable.DisposeAsync ()
                    let! subscription = source.SubscribeAsync _obv
                    disposable <- subscription
                }
                do! action source

                let cancel () =
                    async {
                        do! disposable.DisposeAsync ()
                    }
                return AsyncDisposable.Create cancel
            }
        { new IAsyncObservable<'a> with member __.SubscribeAsync o = subscribeAsync o }