namespace Reaction

type AsyncObserver<'a> (fn: Notification<'a> -> Async<unit>) =

    interface Types.IAsyncObserver<'a> with
        member this.OnNextAsync (x: 'a) =  OnNext x |> fn
        member this.OnErrorAsync err = OnError err |> fn
        member this.OnCompletedAsync () = OnCompleted |> fn

    static member Create (cancel) : IAsyncObserver<'a> =
        AsyncObserver<'a> cancel :> IAsyncObserver<'a>
