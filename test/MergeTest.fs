module Tests.Merge

open System.Threading.Tasks

open Reaction
open Reaction.AsyncObservable

open NUnit.Framework
open FsUnit
open Tests.Utils

exception  MyError of string

let toTask computation : Task = Async.StartAsTask computation :> _

[<Test>]
let ``Test merge non empty emtpy``() = toTask <| async {
    // Arrange
    let xs = ofSeq <| seq { 1..5 }
    let ys = empty<int> ()
    let zs = ofSeq <| [ xs; ys ] |> mergeInner
    let obv = TestObserver<int>()

    // Act
    let! sub = zs.SubscribeAsync obv
    let! latest= obv.Await ()

    // Assert
    latest |> should equal 5
    obv.Notifications |> should haveCount 6
    let actual = obv.Notifications |> Seq.toList
    let expected : Notification<int> list = [ OnNext 1; OnNext 2; OnNext 3; OnNext 4; OnNext 5; OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}

[<Test>]
let ``Test merge empty non emtpy``() = toTask <| async {
    // Arrange
    let xs = empty<int> ()
    let ys = ofSeq <| seq { 1..5 }
    let zs = ofSeq <| [ xs; ys ] |> mergeInner
    let obv = TestObserver<int>()

    // Act
    let! sub = zs.SubscribeAsync obv
    let! latest= obv.Await ()

    // Assert
    latest |> should equal 5
    obv.Notifications |> should haveCount 6
    let actual = obv.Notifications |> Seq.toList
    let expected : Notification<int> list = [ OnNext 1; OnNext 2; OnNext 3; OnNext 4; OnNext 5; OnCompleted ]
    Assert.That(actual, Is.EquivalentTo(expected))
}

[<Test>]
let ``Test merge error error``() = toTask <| async {
    // Arrange
    let error = MyError "error"
    let xs = fail error
    let ys = fail error
    let zs = ofSeq <| [ xs; ys ] |> mergeInner
    let obv = TestObserver<int>()

    // Act
    let! sub = zs.SubscribeAsync obv

    try
        do! obv.Await () |> Async.Ignore
    with
    | _ -> ()

    // Assert
    obv.Notifications |> should haveCount 1
    let actual = obv.Notifications |> Seq.toList
    let expected : Notification<int> list = [ OnError error ]
    Assert.That(actual, Is.EquivalentTo(expected))
}

[<Test>]
let ``Test merge two``() = toTask <| async {
    // Arrange
    let xs  = ofSeq <| seq { 1..3 }
    let ys = ofSeq <| seq { 4..5 }
    let zs = ofSeq <| [ xs; ys ] |> mergeInner
    let obv = TestObserver<int>()

    // Act
    let! sub = zs.SubscribeAsync obv
    do! obv.AwaitIgnore ()

    // Assert
    //obv.Notifications |> should haveCount 6
    let actual = obv.Notifications |> Seq.toList
    actual|> should contain (OnNext 1)
    actual|> should contain (OnNext 2)
    actual|> should contain (OnNext 3)
    actual|> should contain (OnNext 4)
    actual|> should contain (OnNext 5)
    actual|> should contain (OnCompleted : Notification<int>)
}