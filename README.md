# re·ac·tion

> Reaction is a lightweight Async Reactive library for F#.

Reaction is an F# implementation of Async Observables ([ReactiveX](http://reactivex.io/)) and was designed spesifically for targeting [Fable](http://fable.io/) which means that the code may be [transpiled](https://en.wikipedia.org/wiki/Source-to-source_compiler) to JavaScript, and thus the same F# code may be used both client and server side for full stack software development. The project is heavily inspired by [aioreactive](https://github.com/dbrattli/aioreactive).

See [Fable Reaction](https://github.com/dbrattli/Fable.Reaction) for Elmish-ish use of Reaction.

## Install

```cmd
paket add Reaction --project <project>
```

## Async Observables

Reaction is an implementation of Async Observable. The difference between an "Async Observable" and an "Observable" is that with "Async Observables" you need to await methods such as `Subscribe`, `OnNext`, `OnError`, and `OnCompleted`. In Reaction they are thus called `SubscribeAsync`, `OnNextAsync`, `OnErrorAsync`, and `OnCompletedAsync`. This enables `SubscribeAsync` to await async operations i.e setup network connections, and observers (`OnNext`) may finally await side effects such as writing to disk (observers are all about side-effects right?).

This diagram shows the how Async Observables relates to other collections and values.

|  | Single Value | Multiple Values
| --- | --- | --- |
| Synchronous pull  | unit -> 'a | [seq<'a>](https://msdn.microsoft.com/en-us/visualfsharpdocs/conceptual/collections.seq-module-%5Bfsharp%5D?f=255&MSPPError=-2147217396) |
| Synchronous push  |'a -> unit | [Observable<'a>](http://fsprojects.github.io/FSharp.Control.Reactive/tutorial.html) |
| Asynchronous pull | unit -> [Async<'a>](https://msdn.microsoft.com/en-us/visualfsharpdocs/conceptual/control.async-class-%5Bfsharp%5D) | [AsyncSeq<'a>](http://fsprojects.github.io/FSharp.Control.AsyncSeq/library/AsyncSeq.html) |
| Asynchronous push |'a -> [Async\<unit\>](https://msdn.microsoft.com/en-us/visualfsharpdocs/conceptual/control.async-class-%5Bfsharp%5D) | **AsyncObservable<'a>** |

Here are the core interfaces:

```f#
type IAsyncDisposable =
    abstract member DisposeAsync: unit -> Async<unit>

type IAsyncObserver<'a> =
    abstract member OnNextAsync: 'a -> Async<unit>
    abstract member OnErrorAsync: exn -> Async<unit>
    abstract member OnCompletedAsync: unit -> Async<unit>

type IIAsyncObservable<'a> =
    abstract member SubscribeAsync: IAsyncObserver<'a> -> Async<IAsyncDisposable>
```

This enables a familiar Rx programming syntax, and the relationship between these three interfaces
can be seen in this single line of code:

```f#
let! disposableAsync = observable.SubscribeAsync observerAsync
```

There is also a `SubscribeAsync` overload that takes a notification function so instead of using
`IAsyncObserver<'a>` instances you may subscribe with a single async function taking a
`Notification<'a>`.

```f#
type Notification<'a> =
    | OnNext of 'a
    | OnError of exn
    | OnCompleted
```

Thus observers may be defined either as:

```f#
let _obv =
    { new IAsyncObserver<'a> with
        member this.OnNextAsync x = async {
            printfn "OnNext: %d" x
        }
        member this.OnErrorAsync err = async {
            printfn "OnError: %s" (ex.ToString())
        }
        member this.OnCompletedAsync () = async {
            printfn "OnCompleted"
        }
    }
```

... or using a simple function such as:

```f#
let obv n =
    async {
        match n with
        | OnNext x -> printfn "OnNext: %d" x
        | OnError ex -> printfn "OnError: %s" (ex.ToString())
        | OnCompleted -> printfn "OnCompleted"
    }
```

## Usage

```f#
open Reaction

let main = async {
    let mapper x =
        x * 10

    let xs = IAsyncObservable.single 42
             |> IAsyncObservable.map mapper

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

## Operators

The following parameterized async observerable returning functions (operators) are
currently supported. Other operators may be implemented on-demand, but the goal is to keep it simple
and not make this into a full featured [ReactiveX](http://reactivex.io/) implementation (if possible).

### Create

Functions for creating (`'a -> IAsyncObservable<'a>`) an async observable.

- **empty** : unit -> IAsyncObservable<'a>
- **single** : 'a -> IAsyncObservable<'a>
- **fail** : exn -> IAsyncObservable<'a>
- **defer** : (unit -> IAsyncObservable<'a>) -> IAsyncObservable<'a>
- **create** : (AsyncObserver\<'a\> -> Async\<AsyncDisposable\>) -> IAsyncObservable<'a>
- **ofSeq** : seq<'a> -> IAsyncObservable<'a>
- **ofAsyncSeq** : AsyncSeq<'a> -> IAsyncObservable<'a> *(Not available in Fable)*
- **timer** : int -> IAsyncObservable\<int\>
- **interval** int -> IAsyncObservable\<int\>

### Transform

Functions for transforming (`IAsyncObservable<'a> -> IAsyncObservable<'b>`) an async observable.

- **map** : ('a -> 'b) -> IAsyncObservable<'a> -> IAsyncObservable<'b>
- **mapi** : ('a*int -> 'b) -> IAsyncObservable<'a> -> IAsyncObservable<'b>
- **mapAsync** : ('a -> Async<'b>) -> IAsyncObservable<'a> -> IAsyncObservable<'b>
- **mapiAsync** : ('a*int -> Async<'b>) -> IAsyncObservable<'a> -> IAsyncObservable<'b>
- **flatMap** : ('a -> IAsyncObservable<'b>) -> IAsyncObservable<'a> -> IAsyncObservable<'b>
- **flatMapi** : ('a*int -> IAsyncObservable<'b>) -> IAsyncObservable<'a> -> IAsyncObservable<'b>
- **flatMapAsync** : ('a -> Async\<IAsyncObservable\<'b\>\>) -> IAsyncObservable<'a> -> IAsyncObservable<'b>
- **flatMapiAsync** : ('a*int -> Async\<IAsyncObservable\<'b\>\>) -> IAsyncObservable<'a> -> IAsyncObservable<'b>
- **flatMapLatest** : ('a -> IAsyncObservable<'b>) -> IAsyncObservable<'a> -> IAsyncObservable<'b>
- **flatMapLatestAsync** : ('a -> Async\<IAsyncObservable\<'b\>\>) -> IAsyncObservable<'a> -> IAsyncObservable<'b>
- **catch** : (exn -> IAsyncObservable<'a>) -> IAsyncObservable<'a> -> IAsyncObservable<'a>

### Filter

Functions for filtering (`IAsyncObservable<'a> -> IAsyncObservable<'a>`) an async observable.

- **filter** : ('a -> bool) -> IAsyncObservable<'a> -> IAsyncObservable<'a>
- **filterAsync** : ('a -> Async\<bool\>) -> IAsyncObservable<'a> -> IAsyncObservable<'a>
- **distinctUntilChanged** : IAsyncObservable<'a> -> IAsyncObservable<'a>
- **takeUntil** : IAsyncObservable<'b> -> IAsyncObservable<'a> -> IAsyncObservable<'a>
- **choose** : ('a -> 'b option) -> IAsyncObservable<'a> -> IAsyncObservable<'b>
- **chooseAsync** : ('a -> Async<'b option>) -> IAsyncObservable<'a> -> IAsyncObservable<'b>

### Aggregate

- **scan** : 's -> ('s -> 'a -> 's) -> IAsyncObservable<'a> -> IAsyncObservable<'s>
- **scanAsync** : 's -> ('s -> 'a -> Async<'s>) -> IAsyncObservable<'a> -> IAsyncObservable<'s>
- **groupBy** : ('a -> 'g) -> IAsyncObservable<'a> -> IAsyncObservable\<IAsyncObservable\<'a\>\>

### Combine

Functions for combining multiple async observables into one.

- **merge** : IAsyncObservable<'a> -> IAsyncObservable<'a> -> IAsyncObservable<'a>
- **mergeInner** : IAsyncObservable<IAsyncObservable<'a>> -> IAsyncObservable<'a>
- **switchLatest** : IAsyncObservable<IAsyncObservable<'a>> -> IAsyncObservable<'a>
- **concat** : seq<IAsyncObservable<'a>> -> IAsyncObservable<'a>
- **startWith** : seq<'a> -> IAsyncObservable<'a> -> IAsyncObservable<'a>
- **combineLatest** : IAsyncObservable<'b> -> IAsyncObservable<'a> -> IAsyncObservable<'a*'b>
- **withLatestFrom** : IAsyncObservable<'b> -> IAsyncObservable<'a> -> IAsyncObservable<'a*'b>
- **zipSeq** : seq<'b> -> IAsyncObservable<'a> -> IAsyncObservable<'a*'b>

### Time-shift

Functions for time-shifting (`IAsyncObservable<'a> -> IAsyncObservable<'a>`) an async observable.

- **delay** : int -> IAsyncObservable<'a> -> IAsyncObservable<'a>
- **debounce** : int -> IAsyncObservable<'a> -> IAsyncObservable<'a>

### Leave

Functions for leaving (`IAsyncObservable<'a> -> 'a`) the async observable.

-- **toAsyncSeq** : IAsyncObservable<'a> -> AsyncSeq<'a> *(Not available in Fable)*

### Streams

Functions returning tuples (`IAsyncObserver<'a> * IAsyncObservable<'a>`) of async observers and async
observables (aka Subjects in ReactiveX).

- **stream** : unit -> IAsyncObserver<'a> * IAsyncObservable<'a>
- **mbStream** : unit -> MailboxProcessor<'a> * IAsyncObservable<'a>

## Query Builder

Queries may be written by composing functions or using query expressions. Thus the two examples below are equivalent:

```f#
Seq.toList "TIME FLIES LIKE AN ARROW"
|> Seq.mapi (fun i c -> i, c)
|> ofSeq
|> flatMap (fun (i, c) ->
    fromMouseMoves ()
    |> delay (100 * i)
    |> map (fun m -> Letter (i, string c, int m.clientX, int m.clientY)))
```

The above query may be written in query expression style:

```f#
reaction {
    let! i, c = Seq.toList "TIME FLIES LIKE AN ARROW"
                |> Seq.mapi (fun i c -> i, c)
                |> ofSeq
    let ms = fromMouseMoves () |> delay (100 * i)
    for m in ms do
        yield Letter (i, string c, int m.clientX, int m.clientY)
}
```