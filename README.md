# re·ac·tion

> Reaction is a lightweight Async Reactive library for F#.

Reaction is an F# implementation of Async Observables ([ReactiveX](http://reactivex.io/)) and was designed spesifically for targeting [Fable](http://fable.io/) which means that the code may be [transpiled](https://en.wikipedia.org/wiki/Source-to-source_compiler) to JavaScript, and thus the same F# code may be used both client and server side for full stack software development. The project is heavily inspired by [aioreactive](https://github.com/dbrattli/aioreactive).

See [Fable Reaction](https://github.com/dbrattli/Fable.Reaction) for Elmish-ish use of Reaction.

## Install

```cmd
paket add Reaction --project <project>
```

## Async Observables

Reaction is an implementation of Async Observable. The difference between an "Async Observable" and an "Observable" is that with "Async Observables" you need to await operations such as Subscribe, OnNext, OnError, OnCompleted. This enables Subscribe to await async operations i.e setup network connections, and observers may finally await side effects such as writing to disk (observers are all about side-effects right?).

This diagram shows the how Async Observables relates to other collections and values.

| Command | Single Value | Multiple Values
| --- | --- | --- |
| Synchronous pull  | unit -> 'a | [seq<'a>](https://msdn.microsoft.com/en-us/visualfsharpdocs/conceptual/collections.seq-module-%5Bfsharp%5D?f=255&MSPPError=-2147217396) |
| Synchronous push  |'a -> unit | [Observable<'a>](http://fsprojects.github.io/FSharp.Control.Reactive/tutorial.html) |
| Asynchronous pull | unit -> [Async<'a>](https://msdn.microsoft.com/en-us/visualfsharpdocs/conceptual/control.async-class-%5Bfsharp%5D) | [AsyncSeq<'a>](http://fsprojects.github.io/FSharp.Control.AsyncSeq/library/AsyncSeq.html) |
| Asynchronous push |'a -> [Async\<unit\>](https://msdn.microsoft.com/en-us/visualfsharpdocs/conceptual/control.async-class-%5Bfsharp%5D) | **AsyncObservable<'a>** |

Reaction is built upon simple functions instead of classes and the traditional Rx interfaces. Some of the operators uses mailbox processors (actors) to implement the observer pipeline in order to avoid locks and mutables. This makes the code more Fable friendly. Here are the core types:

```f#
type Notification<'a> =
    | OnNext of 'a
    | OnError of exn
    | OnCompleted

type AsyncDisposable = unit -> Async<unit>
type AsyncObserver<'a> = Notification<'a> -> Async<unit>
type AsyncObservable<'a> = AsyncObserver<'a> -> Async<AsyncDisposable>
```

In the API these types have been wrapped in single case union types to enable member methods and a more familiar Rx programming syntax (see usage below).

## Usage

```f#
open Reaction

let main = async {
    let mapper x =
        x * 10

    let xs = single 42 |> map mapper
    let obv n =
        async {
            match n with
            | OnNext x -> printfn "OnNext: %d" x
            | OnError ex -> printfn "OnError: %s" ex.ToString()
            | OnCompleted -> printfn "OnCompleted"
        }

    let! subscription = xs.SubscribeAsync obv
}

Async.Start main
```

## Operators

The following parameterized async observerable returning functions (operators) are
currently supported. Other operators may be implemented on-demand, but the goal is to keep it simple
and not  make this into a full featured [ReactiveX](http://reactivex.io/) implementation.

### Create

Functions for creating (`'a -> AsyncObservable<'a>`) an async observable.

- **empty** : unit -> AsyncObservable<'a>
- **single** : 'a -> AsyncObservable<'a>
- **fail** : exn -> AsyncObservable<'a>
- **defer** : (unit -> AsyncObservable<'a>) -> AsyncObservable<'a>
- **create** : (AsyncObserver\<'a\> -> Async\<AsyncDisposable\>) -> AsyncObservable<'a>
- **ofSeq** : seq<'a> -> AsyncObservable<'a>
- **ofAsyncSeq** : AsyncSeq<'a> -> AsyncObservable<'a> *(Not available in Fable)*

### Transform

Functions for transforming (`AsyncObservable<'a> -> AsyncObservable<'b>`) an async observable.

- **map** : ('a -> 'b) -> AsyncObservable<'a> -> AsyncObservable<'b>
- **mapi** : ('a*int -> 'b) -> AsyncObservable<'a> -> AsyncObservable<'b>
- **mapAsync** : ('a -> Async<'b>) -> AsyncObservable<'a> -> AsyncObservable<'b>
- **mapiAsync** : ('a*int -> Async<'b>) -> AsyncObservable<'a> -> AsyncObservable<'b>
- **flatMap** : ('a -> AsyncObservable<'b>) -> AsyncObservable<'a> -> AsyncObservable<'b>
- **flatMapi** : ('a*int -> AsyncObservable<'b>) -> AsyncObservable<'a> -> AsyncObservable<'b>
- **flatMapAsync** : ('a -> Async\<AsyncObservable\<'b\>\>) -> AsyncObservable<'a> -> AsyncObservable<'b>
- **flatMapiAsync** : ('a*int -> Async\<AsyncObservable\<'b\>\>) -> AsyncObservable<'a> -> AsyncObservable<'b>
- **flatMapLatest** : ('a -> AsyncObservable<'b>) -> AsyncObservable<'a> -> AsyncObservable<'b>
- **flatMapLatestAsync** : ('a -> Async\<AsyncObservable\<'b\>\>) -> AsyncObservable<'a> -> AsyncObservable<'b>
- **catch** : (exn -> AsyncObservable<'a>) -> AsyncObservable<'a> -> AsyncObservable<'a>

### Filter

Functions for filtering (`AsyncObservable<'a> -> AsyncObservable<'a>`) an async observable.

- **filter** : ('a -> bool) -> AsyncObservable<'a> -> AsyncObservable<'a>
- **filterAsync** : ('a -> Async\<bool\>) -> AsyncObservable<'a> -> AsyncObservable<'a>
- **distinctUntilChanged** : AsyncObservable<'a> -> AsyncObservable<'a>
- **takeUntil** : AsyncObservable<'b> -> AsyncObservable<'a> -> AsyncObservable<'a>
- **choose** : ('a -> 'b option) -> AsyncObservable<'a> -> AsyncObservable<'b>
- **chooseAsync** : ('a -> Async<'b option>) -> AsyncObservable<'a> -> AsyncObservable<'b>

### Aggregate

- **scan** : 's -> ('s -> 'a -> 's) -> AsyncObservable<'a> -> AsyncObservable<'s>
- **scanAsync** : 's -> ('s -> 'a -> Async<'s>) -> AsyncObservable<'a> -> AsyncObservable<'s>
- **groupBy** : ('a -> 'g) -> AsyncObservable<'a> -> AsyncObservable\<AsyncObservable\<'a\>\>

### Combine

Functions for combining multiple async observables into one.

- **merge** : AsyncObservable<'a> -> AsyncObservable<'a> -> AsyncObservable<'a>
- **mergeInner** : AsyncObservable<AsyncObservable<'a>> -> AsyncObservable<'a>
- **switchLatest** : AsyncObservable<AsyncObservable<'a>> -> AsyncObservable<'a>
- **concat** : seq<AsyncObservable<'a>> -> AsyncObservable<'a>
- **startWith** : seq<'a> -> AsyncObservable<'a> -> AsyncObservable<'a>
- **combineLatest** : AsyncObservable<'b> -> AsyncObservable<'a> -> AsyncObservable<'a*'b>
- **withLatestFrom** : AsyncObservable<'b> -> AsyncObservable<'a> -> AsyncObservable<'a*'b>
- **zipSeq** : seq<'b> -> AsyncObservable<'a> -> AsyncObservable<'a*'b>

### Time-shift

Functions for time-shifting (`AsyncObservable<'a> -> AsyncObservable<'a>`) an async observable.

- **delay** : int -> AsyncObservable<'a> -> AsyncObservable<'a>
- **debounce** : int -> AsyncObservable<'a> -> AsyncObservable<'a>

### Leave

Functions for leaving (`AsyncObservable<'a> -> 'a`) the async observable.

-- **toAsyncSeq** : AsyncObservable<'a> -> AsyncSeq<'a> *(Not available in Fable)*

### Streams

Functions returning tuples (`AsyncObserver<'a> * AsyncObservable<'a>`) of async observers and async
observables (aka Subjects in ReactiveX).

- **stream** : unit -> AsyncObserver<'a> * AsyncObservable<'a>
- **mbStream** : unit -> MailboxProcessor<'a> * AsyncObservable<'a>

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