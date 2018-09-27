namespace Reaction

#if !FABLE_COMPILER
open FSharp.Control
#endif

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module AsyncObservable =
    type IAsyncObservable<'a> with
        /// Repeat each element of the sequence n times
        /// Subscribes the async observer to the async observable,
        /// ignores the disposable
        member this.RunAsync (obv: IAsyncObserver<'a>) = async {
            let! _ = this.SubscribeAsync obv
            return ()
        }

        /// Subscribes the observer function (`Notification{'a} -> Async{unit}`)
        /// to the AsyncObservable, ignores the disposable.
        member this.RunAsync<'a> (obv: Notification<'a> -> Async<unit>) = async {
            do! this.SubscribeAsync (AsyncObserver obv) |> Async.Ignore
        }

        /// Subscribes the async observer function (`Notification{'a} -> Async{unit}`)
        /// to the AsyncObservable
        member this.SubscribeAsync<'a> (obv: Notification<'a> -> Async<unit>) = async {
            let! disposable = this.SubscribeAsync (AsyncObserver obv)
            return disposable
        }

    // Aggregate
    let groupBy = Aggregatation.groupBy
    /// Applies an accumulator function over an observable sequence and
    /// returns each intermediate result. The seed value is used as the
    /// initial accumulator value. Returns an observable sequence
    /// containing the accumulated values.
    let scan = Aggregatation.scan

    /// Applies an async accumulator function over an observable
    /// sequence and returns each intermediate result. The seed value is
    /// used as the initial accumulator value. Returns an observable
    /// sequence containing the accumulated values.
    let scanAsync = Aggregatation.scanAsync

    // Combine

    /// Merges the specified observable sequences into one observable
    /// sequence by combining elements of the sources into tuples.
    /// Returns an observable sequence containing the combined results.
    let combineLatest = Combine.combineLatest

    /// Returns an observable sequence that contains the elements of
    /// each given sequences, in sequential order.
    let concat = Combine.concat

    /// Merges an observable sequence with another observable sequences.
    let merge = Combine.merge

    /// Merges an observable sequence of observable sequences into an
    /// observable sequence.
    let mergeInner = Combine.mergeInner

    /// Prepends a sequence of values to an observable sequence.
    /// Returns the source sequence prepended with the specified values.
    let startWith = Combine.startWith

    /// Merges the specified observable sequences into one observable
    /// sequence by combining the values into tuples only when the first
    /// observable sequence produces an element. Returns the combined
    /// observable sequence.
    let withLatestFrom = Combine.withLatestFrom
    let zipSeq = Combine.zipSeq

    // Creation

    /// Creates an async observable (`AsyncObservable{'a}`) from the
    /// given subscribe function.
    let create = Create.create

    let defer = Create.defer

    /// Returns an observable sequence with no elements.
    let empty<'a> = Create.empty<'a>

    /// Returns the observable sequence that terminates exceptionally
    /// with the specified exception.
    let fail<'a> = Create.fail<'a>

    /// Returns an observable sequence that triggers the increasing
    /// sequence starting with 0 after the given period.
    let interval = Create.interval
    let ofAsync = Create.ofAsync
#if !FABLE_COMPILER
    let ofAsyncSeq = Create.ofAsyncSeq
#endif
    /// Returns the async observable sequence whose elements are pulled
    /// from the given enumerable sequence.
    let ofSeq = Create.ofSeq
    /// Returns an observable sequence containing the single specified
    /// element.
    let single = Create.single

    /// Returns an observable sequence that triggers the value 0
    /// after the given duetime.
    let timer = Create.timer

    // Filter

    /// Applies the given function to each element of the stream and
    /// returns the stream comprised of the results for each element
    /// where the function returns Some with some value.
    let choose = Filter.choose

    /// Applies the given async function to each element of the stream and
    /// returns the stream comprised of the results for each element
    /// where the function returns Some with some value.
    let chooseAsync = Filter.chooseAsync

    /// Return an observable sequence only containing the distinct
    /// contiguous elementsfrom the source sequence.
    let distinctUntilChanged =Filter.distinctUntilChanged

    /// Filters the elements of an observable sequence based on a
    /// predicate. Returns an observable sequence that contains elements
    /// from the input sequence that satisfy the condition.
    let filter = Filter.filter

    /// Filters the elements of an observable sequence based on an async
    /// predicate. Returns an observable sequence that contains elements
    /// from the input sequence that satisfy the condition.
    let filterAsync = Filter.filterAsync

    /// Returns the values from the source observable sequence until the
    /// other observable sequence produces a value.
    let takeUntil = Filter.takeUntil

    // Leave
#if !FABLE_COMPILER
    /// Convert async observable to async sequence, non-blocking.
    /// Producer will be awaited until item is consumed by the async
    /// enumerator.
    let toAsyncSeq = Leave.toAsyncSeq
#endif

    // Timeshift

    /// Ignores values from an observable sequence which are followed by
    /// another value before the given timeout.
    let debounce = Timeshift.debounce

    /// Time shifts the observable sequence by the given timeout. The
    /// relative time intervals between the values are preserved.
    let delay = Timeshift.delay

    // Transform

    /// Returns an observable sequence containing the first sequence's
    /// elements, followed by the elements of the handler sequence in
    /// case an exception occurred.
    let catch = Transformation.catch

    /// Projects each element of an observable sequence into an
    /// observable sequence and merges the resulting observable
    /// sequences back into one observable sequence.
    let flatMap = Transformation.flatMap

    /// Asynchronously projects each element of an observable sequence
    /// into an observable sequence and merges the resulting observable
    /// sequences back into one observable sequence.
    let flatMapAsync = Transformation.flatMapAsync

    /// Projects each element of an observable sequence into an
    /// observable sequence by incorporating the element's
    /// index on each element of the source. Merges the resulting
    /// observable sequences back into one observable sequence.
    let flatMapi = Transformation.flatMapi

    /// Asynchronously projects each element of an observable sequence
    /// into an observable sequence by incorporating the element's
    /// index on each element of the source. Merges the resulting
    /// observable sequences back into one observable sequence.
    let flatMapiAsync = Transformation.flatMapiAsync

    /// Transforms the items emitted by an source sequence into
    /// observable streams, and mirror those items emitted by the
    /// most-recently transformed observable sequence.
    let flatMapLatest = Transformation.flatMapLatest

    /// Asynchronosly transforms the items emitted by an source sequence
    /// into observable streams, and mirror those items emitted by the
    /// most-recently transformed observable sequence.
    let flatMapLatestAsync = Transformation.flatMapLatestAsync

    /// Returns an observable sequence whose elements are the result of
    /// invoking the mapper function on each element of the source.
    let map = Transformation.map

    /// Returns an observable sequence whose elements are the result of
    /// invoking the async mapper function on each element of the source.
    let mapAsync = Transformation.mapAsync

    /// Returns an observable sequence whose elements are the result of
    /// invoking the mapper function and incorporating the element's
    /// index on each element of the source.
    let mapi = Transformation.mapi

    /// Returns an observable sequence whose elements are the result of
    /// invoking the async mapper function by incorporating the element's
    /// index on each element of the source.
    let mapiAsync = Transformation.mapiAsync

    /// Transforms an observable sequence of observable sequences into
    /// an observable sequence producing values only from the most
    /// recent observable sequence.
    let switchLatest = Transformation.switchLatest

