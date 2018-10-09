# Query Builder

Queries may be written by composing functions or using query expressions. Thus the two examples below are equivalent:

```fs
Seq.toList "TIME FLIES LIKE AN ARROW"
|> Seq.mapi (fun i c -> i, c)
|> ofSeq
|> flatMap (fun (i, c) ->
    fromMouseMoves ()
    |> delay (100 * i)
    |> map (fun m -> Letter (i, string c, int m.clientX, int m.clientY)))
```

The above query may be written in query expression style:

```fs
reaction {
    let! i, c = Seq.toList "TIME FLIES LIKE AN ARROW"
                |> Seq.mapi (fun i c -> i, c)
                |> ofSeq
    let ms = fromMouseMoves () |> delay (100 * i)
    for m in ms do
        yield Letter (i, string c, int m.clientX, int m.clientY)
}
```