module SuaveMusicStore.Db

open FSharp.Data.Sql

[<Literal>]
let ConnectionString =
    "Server=127.0.0.1;" +
    "Database=suavemusicstore;" +
    "User Id=suave;" +
    "Password=1234;"

type Sql =
    SqlDataProvider<
        ConnectionString = ConnectionString,
        DatabaseVendor = Common.DatabaseProviderTypes.POSTGRESQL,
        CaseSensitivityChange = Common.CaseSensitivityChange.ORIGINAL>

type DbContext = Sql.dataContext
type Album = DbContext.``public.albumsEntity``
type Genre = DbContext.``public.genresEntity``
type AlbumsDetails = DbContext.``public.albumdetailsEntity``

let getContext() = Sql.GetDataContext()

let getGenres (ctx: DbContext): Genre list =
    ctx.Public.Genres |> Seq.toList

let getAlbumsForGenre genreName (ctx: DbContext): Album list =
    query {
        for album in ctx.Public.Albums do
            join genre in ctx.Public.Genres on (album.Genreid = genre.Genreid)
            where (genre.Name = genreName)
            select album
    } |> Seq.toList

let getAlbumDetails id (ctx: DbContext): AlbumsDetails option =
    query {
        for album in ctx.Public.Albumdetails do
            where (album.Albumid = id)
            select album
    } |> Seq.tryHead

let getAlbumsDetails (ctx: DbContext): AlbumsDetails list =
    ctx.Public.Albumdetails
    |> Seq.toList
    |> List.sortBy (fun a -> a.Artist)