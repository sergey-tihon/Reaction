# Getting Started

Below is a simple example of a single value stream being transformed and subscribed using an observer function.

```fs
open Reaction

let main = async {
    let mapper x =
        x * 10

    let xs =
        AsyncObservable.single 42
        |> AsyncObservable.map mapper

    let obv n =
        async {
            match n with
            | OnNext x -> printfn "OnNext: %d" x
            | OnError ex -> printfn "OnError: %s" (ex.ToString())
            | OnCompleted -> printfn "OnCompleted"
        }

    let! disposable = xs.SubscribeAsync obv
    ()
}

Async.Start main
```