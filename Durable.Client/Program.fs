open System
open FSharp.Data
open System.Threading

type StartFavouritePrice = JsonProvider<"JsonExamples/FavouritePricesStart.json">
type StatusFavouritePrice = JsonProvider<"JsonExamples/FavouritePricesStatus.json">
type FavouritePrices = JsonProvider<"JsonExamples/FavouritePrices.json">

let getStatus url =
    Http.RequestString(url) |> StatusFavouritePrice.Parse

let rec pollGetStatus url =
    printfn "getting status..."
    let statusFavouritePrice = Http.RequestString(url) |> StatusFavouritePrice.Parse
    printfn "status %s" statusFavouritePrice.RuntimeStatus

    if statusFavouritePrice.RuntimeStatus = "Completed" then
        statusFavouritePrice.Output
    else
        Thread.Sleep(3000)
        pollGetStatus url


[<EntryPoint>]
let main argv =
    let url = "http://localhost:7071/api/FavouritePlaces"
    let startFavouritePrice = Http.RequestString(url) |> StartFavouritePrice.Parse
    
    let favouritePrices = pollGetStatus startFavouritePrice.StatusQueryGetUri |> FavouritePrices.Parse
    favouritePrices |> Array.iter (fun favouritePrice -> printfn "%s (%s) %f€ %f%%" favouritePrice.Data.Name favouritePrice.Data.Symbol favouritePrice.Data.Quotes.Eur.Price favouritePrice.Data.Quotes.Eur.PercentChange24h)

    0 // return an integer exit code
