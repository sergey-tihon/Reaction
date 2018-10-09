# Streams

Streams are a special contruction in Reaction. They are sort of similar to a classic `Subject` in ReactiveX, in that they implement both the `IAsyncObservable<'a>` and `IAsyncObserver<'a>` interfaces. The difference is that a stream in Reaction does not return a single stream object, but two entangled objects where one implements `IAsyncObserver<'a>` and the other implements `IAsyncObservable<'a>`. This solves the problem of having a single object trying to be two things at once.

## Stream

The stream will forward any notification pushed to the `IAsyncObserver<'a>` side to all observers that have subscribed to the `IAsyncObservable<'a>` side. The stream is hot in the sense that if there are no observers, then the notification will be lost.

- **stream** : unit -> IAsyncObserver<'a> * IAsyncObservable<'a>

```fs
let dispatch, obs = stream ()

let main = async {
    let! sub = obs.SubscribeAsync obv
    do! dispatch.OnNextAsync 42
    ...
}

Async.StartImmediate main ()
```

## Mailbox Stream

Same as a `stream` except that the observer is exposed as a `MailboxProcessor<Notification<'a>>`. The stream is hot in the sense that if there are no observers, then the notification will be lost.

- **mbStream** : unit -> MailboxProcessor<Notification<'a>> * IAsyncObservable<'a>

```fs
let dispatch, obs = stream ()

let main = async {
    let! sub = obs.SubscribeAsync obv
    do dispatch.Post (OnNext 42)
    ...
}

Async.StartImmediate main ()
```

## Single Stream

The stream will forward any notification pushed to the `IAsyncObserver<'a>` side to a single observers that have subscribed to the `IAsyncObservable<'a>` side. The stream is cold in the sense that if there is no observer, then the writer will be awaited until there is a subscriber.

- **singleStream** : unit -> IAsyncObserver<'a> * IAsyncObservable<'a>
