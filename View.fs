module SuaveMusicStore.View

open Suave.Html

let cssLink href = link ["href", href; " rel", "stylesheet"; " type", "text/css"]
let h2 s = tag "h2" [] [Text s]
let ul nodes = tag "ul" [] nodes
let li = tag "li" []
let em s = tag "em" [] [Text s]
let table x = tag "table" [] x
let th x = tag "th" [] x
let tr x = tag "tr" [] x
let td x = tag "td" [] x

let home = [
    h2 "Home"
]

let store genres = [
    h2 "Browse genres"
    p [] [
        Text (sprintf "Select from %d genres: " (List.length genres))
    ]
    ul [
        for genre in genres ->
            let url = 
                Path.Store.browse
                |> Path.withParam (Path.Store.browseKey, genre)
            li [
                a url [] [
                    Text genre
                ]
            ]
    ]
]

let browse genre (albums: Db.Album list) = [
    h2 (sprintf "Genre: %s" genre)
    ul [
        for album in albums ->
            li [
                a (sprintf Path.Store.details album.Albumid) [] [
                    Text album.Title
                ]
            ]
    ]
]

let details (album: Db.AlbumsDetails) = [
    h2 album.Title
    p [] [
        img ["src", album.Albumarturl]
    ]
    div ["id", "album-details"] [
        for (caption, t) in ["Genre: ", album.Genre;
                             "Artist: ", album.Artist;
                             "Price: ", album.Price.ToString("0.##")] ->
            p [] [
                em caption
                Text t
            ]
    ]
]

let notFound = [
    h2 "Page not found"
    p [] [
        Text "Could not find the requested resource"
    ]
    p [] [
        Text "Back to "
        a Path.home [] [Text "Home"]
    ]
]

let truncate k (s: string) =
    if s.Length > k then
        s.Substring(0, k - 3) + "..."
    else s

let manage (albums: Db.AlbumsDetails list) = [
    h2 "Index"
    table [
        yield tr [
            for t in ["Artist"; "Title"; "Genre"; "Price"] -> th [Text t]
        ]
        for album in albums ->
            tr [
                for t in [truncate 25 album.Artist;
                          truncate 25 album.Title;
                          album.Genre;
                          album.Price.ToString("0.##")] ->
                    td [Text t]
            ]
    ]
]

let index container = 
    html [] [
        head [] [
            title [] "Suave Music Store"
            cssLink "/Site.css"
        ]

        body [] [
            div ["id", "header"] [
                tag "h1" [] [
                    a Path.home [] [Text "F# Suave Music Store"]
                ]
            ]

            div ["id", "main"] container

            div ["id", "footer"] [
                Text "built with "
                a "http://fsharp.org" [] [Text "F#"]
                Text " and "
                a "http://suave.io" [] [Text "Suave.IO"]
            ]
        ]
    ]
    |> htmlToString