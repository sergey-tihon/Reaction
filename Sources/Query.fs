namespace Reaction

type QueryBuilder() =
    member this.Zero () : AsyncObservable<_> = empty ()
    member this.Yield (x: 'a) : AsyncObservable<'a> = single x
    member this.YieldFrom (xs: AsyncObservable<'a>) : AsyncObservable<'a> = xs
    member this.Combine (xs: AsyncObservable<'a>, ys: AsyncObservable<'a>) = xs + ys
    member this.Delay (fn) = fn ()
    member this.Bind(source: AsyncObservable<'a>, fn: 'a -> AsyncObservable<'b>) : AsyncObservable<'b> = flatMap fn source
    member x.For(source: AsyncObservable<_>, func) : AsyncObservable<'b> = flatMap func source

    // Async to AsyncObservable conversion
    member this.Bind (source: Async<'a>, fn: 'a -> AsyncObservable<'b>) = ofAsync source |> flatMap fn
    member this.YieldFrom (xs: Async<'x>) = ofAsync xs

    // Sequence to AsyncObservable conversion
    member x.For(source: seq<_>, func) : AsyncObservable<'b> = ofSeq source |> flatMap func

[<AutoOpen>]
module Query =
    /// Query builder for an async reactive event source
    let reaction = new QueryBuilder()
    let rx = reaction // Shorter alias
