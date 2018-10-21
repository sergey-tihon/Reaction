module Tests.Scan

open System.Threading.Tasks

open Reaction
open Reaction.AsyncObservable

open NUnit.Framework
open FsUnit
open Tests.Utils
open Tests.Observer

exception MyError of string

let toTask computation : Task = Async.StartAsTask computation :> _

[<Test>]
let ``Test scanAsync``() = toTask <| async {
    // Arrange
    let scanner a x =
        async {
            return a + x
        }

    let xs = ofSeq <| seq { 1..5 } |> scanAsync 0 scanner
    let obv = TestObserver<int>()

    // Act
    let! sub = xs.SubscribeAsync obv
    let! result = obv.Await ()

    // Assert
    result |> should equal 15
    obv.Notifications |> should haveCount 6
    let actual = obv.Notifications |> Seq.toList
    let expected = [ OnNext 1; OnNext 3; OnNext 6; OnNext 10; OnNext 15; OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}

[<Test>]
let ``Test scan``() = toTask <| async {
    // Arrange
    let scanner a x =
            a + x

    let xs = ofSeq <| seq { 1..5 } |> scan 0 scanner
    let obv = TestObserver<int>()

    // Act
    let! sub = xs.SubscribeAsync obv
    let! result = obv.Await ()

    // Assert
    result |> should equal 15
    obv.Notifications |> should haveCount 6
    let actual = obv.Notifications |> Seq.toList
    let expected = [ OnNext 1; OnNext 3; OnNext 6; OnNext 10; OnNext 15; OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}

[<Test>]
let ``Test scan accumulator fails``() = toTask <| async {
    // Arrange
    let error = MyError "error"
    let scanner a x =
        raise error
        0

    let xs = ofSeq <| seq { 1..5 } |> scan 0 scanner
    let obv = TestObserver<int>()

    // Act
    let! sub = xs.SubscribeAsync obv

    try
        do! obv.AwaitIgnore ()
    with
    | ex -> ()

    // Assert
    obv.Notifications |> should haveCount 1
}