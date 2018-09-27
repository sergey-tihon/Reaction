module Tests.Query

open System.Threading.Tasks

open Reaction
open Reaction.AsyncObservable

open NUnit.Framework
open FsUnit

open Tests.Utils

let toTask computation : Task = Async.StartAsTask computation :> _

[<Test>]
let ``test empty query`` () = toTask <| async {
    // Arrange
    let xs = reaction {
        ()
    }
    let obv = TestObserver<unit>()

    // Act
    let! dispose = xs.SubscribeAsync obv

    // Assert
    try
        let! latest = obv.Await ()
        ()
    with
        | :? TaskCanceledException -> ()

    let actual = obv.Notifications |> Seq.toList
    let expected : Notification<unit> list = [ OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}

[<Test>]
let ``test query let!`` () = toTask <| async {
    // Arrange
    let obv = TestObserver<int>()

    let xs = reaction {
        let! a = seq [1; 2] |> ofSeq
        let! b = seq [3; 4] |> ofSeq

        yield a + b
    }

    // Act
    let! subscription = xs.SubscribeAsync obv
    let! latest = obv.Await ()

    // Assert
    let actual = obv.Notifications |> Seq.toList
    let expected : Notification<int> list = [ OnNext 4; OnNext 5; OnNext 5; OnNext 6; OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}

[<Test>]
let ``test query yield!`` () = toTask <| async {
    // Arrange
    let obv = TestObserver<int>()

    let xs = reaction {
        yield! single 42
    }

    // Act
    let! subscription = xs.SubscribeAsync obv
    let! latest = obv.Await ()

    // Assert
    let actual = obv.Notifications |> Seq.toList
    let expected : Notification<int> list = [ OnNext 42;OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}

[<Test>]
let ``test query yield`` () = toTask <| async {
    // Arrange
    let obv = TestObserver<int>()

    let xs = reaction {
        yield 42
    }

    // Act
    let! subscription = xs.SubscribeAsync obv
    let! latest = obv.Await ()

    // Assert
    let actual = obv.Notifications |> Seq.toList
    let expected : Notification<int> list = [ OnNext 42; OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}

[<Test>]
let ``test query combine`` () = toTask <| async {
    // Arrange
    let obv = TestObserver<int>()

    let xs = reaction {
        yield 42
        yield 43
    }

    // Act
    let! subscription = xs.SubscribeAsync obv
    let! latest = obv.Await ()

    // Assert
    let actual = obv.Notifications |> Seq.toList
    let expected : Notification<int> list = [ OnNext 42; OnNext 43; OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}

[<Test>]
let ``test query for in observable`` () = toTask <| async {
    // Arrange
    let obv = TestObserver<int>()

    let xs = reaction {
        let xs = ofSeq [1; 2; 3]
        for x in xs do
            yield x * 10
    }

    // Act
    let! subscription = xs.SubscribeAsync obv
    let! latest = obv.Await ()

    // Assert
    let actual = obv.Notifications |> Seq.toList
    let expected : Notification<int> list = [ OnNext 10; OnNext 20; OnNext 30; OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}

[<Test>]
let ``test query for in seq`` () = toTask <| async {
    // Arrange
    let obv = TestObserver<int>()

    let xs = reaction {
        for x in [1; 2; 3] do
            yield x * 10
    }

    // Act
    let! subscription = xs.SubscribeAsync obv
    let! latest = obv.Await ()

    // Assert
    let actual = obv.Notifications |> Seq.toList
    let expected : Notification<int> list = [ OnNext 10; OnNext 20; OnNext 30; OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}

[<Test>]
let ``test query async`` () = toTask <| async {
    // Arrange
    let obv = TestObserver<int>()

    let xs = reaction {
        let! b = async { return 42 }
        yield b + 2
    }

    // Act
    let! subscription = xs.SubscribeAsync obv
    let! latest = obv.Await ()

    // Assert
    let actual = obv.Notifications |> Seq.toList
    let expected : Notification<int> list = [ OnNext 44; OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}