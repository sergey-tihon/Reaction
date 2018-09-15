namespace Reaction

type AsyncDisposable = AsyncDisposable of Types.AsyncDisposable with
    static member Unwrap (AsyncDisposable dsp) : Types.AsyncDisposable = dsp

    static member Empty = AsyncDisposable Core.disposableEmpty

    static member Composite seq = AsyncDisposable <| Core.compositeDisposable seq

    member this.DisposeAsync () = AsyncDisposable.Unwrap this ()

