module SuaveMusicStore.Db

open FSharp.Data.Sql
open System.Runtime.InteropServices

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
type Artist = DbContext.``public.artistsEntity``
type User = DbContext.``public.usersEntity``
type Cart = DbContext.``public.cartsEntity``
type CartDetails = DbContext.``public.cartdetailsEntity``

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

let getAlbum id (ctx: DbContext): Album option =
    query {
        for album in ctx.Public.Albums do
            where (album.Albumid = id)
            select album
    } |> Seq.tryHead

let createAlbum (artistId, genreId, price, title) (ctx: DbContext) =
    ctx.Public.Albums.Create(artistId, genreId, price, title) |> ignore
    ctx.SubmitUpdates()

let updateAlbum (album: Album) (artistId, genreId, price, title) (ctx: DbContext) =
    album.Artistid <- artistId
    album.Genreid <- genreId
    album.Price <- price
    album.Title <- title
    ctx.SubmitUpdates()

let deleteAlbum (album: Album) (ctx: DbContext) =
    album.Delete()
    ctx.SubmitUpdates()

let getArtists (ctx: DbContext): Artist list =
    ctx.Public.Artists |> Seq.toList

let validateUser (username, password) (ctx: DbContext): User option =
    query {
        for user in ctx.Public.Users do
            where (user.Username = username && user.Password = password)
            select user
    } |> Seq.tryHead

let getCart cartId albumId (ctx: DbContext): Cart option =
    query {
        for cart in ctx.Public.Carts do
            where (cart.Cartid = cartId && cart.Albumid = albumId)
            select cart
    } |> Seq.tryHead

let addToCart cartId albumId (ctx: DbContext)  =
    match getCart cartId albumId ctx with
    | Some cart ->
        cart.Count <- cart.Count + 1
    | None ->
        ctx.Public.Carts.Create(albumId, cartId, 1, System.DateTime.UtcNow) |> ignore
    ctx.SubmitUpdates()

let removeFromCart (cart: Cart) albumId (ctx: DbContext) = 
    cart.Count <- cart.Count - 1
    if cart.Count = 0 then cart.Delete()
    ctx.SubmitUpdates()

let getCartsDetails cartId (ctx: DbContext): CartDetails list =
    query {
        for cart in ctx.Public.Cartdetails do
            where (cart.Cartid = cartId)
            select cart
    } |> Seq.toList

let getCarts cartId (ctx: DbContext): Cart list =
    query {
        for cart in ctx.Public.Carts do
            where (cart.Cartid = cartId)
            select cart
    } |> Seq.toList

let upgradeCarts (cartId: string, username: string) (ctx: DbContext) =
    for cart in getCarts cartId ctx do
        match getCart username cart.Albumid ctx with
        | Some existing ->
            existing.Count <- existing.Count +  cart.Count
            cart.Delete()
        | None ->
            cart.Cartid <- username
    ctx.SubmitUpdates()

let getUser username (ctx: DbContext): User option = 
    query {
        for user in ctx.Public.Users do
        where (user.Username = username)
        select user
    } |> Seq.tryHead

let newUser (username, password, email) (ctx: DbContext) =
    let role = "user"
    let user = ctx.Public.Users.Create(email, password, role, username)
    ctx.SubmitUpdates()
    user

let placeOrder (username: string) (ctx: DbContext) =
    let carts = (username, ctx) ||> getCartsDetails 
    let total = carts |> List.sumBy (fun c -> (decimal) c.Count * c.Price)
    let order = ctx.Public.Orders.Create(System.DateTime.UtcNow, total)
    order.Username <- username
    ctx.SubmitUpdates()
    for cart in carts do
        ctx.Public.Orderdetails.Create(
                cart.Albumid, 
                order.Orderid, 
                cart.Count, 
                cart.Price) |> ignore
        (cart.Cartid, cart.Albumid, ctx) |||> getCart
        |> Option.iter (fun cart -> cart.Delete())
    ctx.SubmitUpdates()