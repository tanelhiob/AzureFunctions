module Http

open Microsoft.Azure.WebJobs
open System.Net.Http
open System.Text
open System.Net
open System.Threading.Tasks
open System
open FSharp.Control.Tasks
open Microsoft.Azure.WebJobs.Host
open Newtonsoft.Json
open Newtonsoft.Json.Linq

let getAsyncTask (url:string) = task {
    use httpClient = new HttpClient()
    use! response = httpClient.GetAsync(url)
    response.EnsureSuccessStatusCode() |> ignore
    return! response.Content.ReadAsStringAsync()
}

let getAsync = getAsyncTask >> Async.AwaitTask

let createResponse (contentType:string) (content:string) : HttpResponseMessage =
    let response = new HttpResponseMessage(HttpStatusCode.OK)
    response.Content <- new StringContent(content, Encoding.UTF8, contentType)
    response

let createJsonResponse = createResponse "application/json"

[<Literal>]
let functionNameHello = "Hello"
[<FunctionName(functionNameHello)>]
let hello ([<HttpTrigger>] req: HttpRequestMessage) =
    createResponse "application/text" "Hello World"

[<Literal>]
let functionNameGetInfo = "getInfo"
[<FunctionName(functionNameGetInfo)>]
let getInfo ([<ActivityTrigger>] id: int, log: TraceWriter) = task {
    let url = "https://api.coinmarketcap.com/v2/ticker/" + (string id) + "/?convert=EUR"
    use httpClient = new HttpClient()
    use! response = httpClient.GetAsync(url)
    response.EnsureSuccessStatusCode() |> ignore
    return! response.Content.ReadAsStringAsync()
}

[<Literal>]
let functionNameList = "list"
[<FunctionName(functionNameList)>]
let list ([<HttpTrigger>] req: HttpRequestMessage) =
    getAsync("https://api.coinmarketcap.com/v2/listings")
    |> Async.RunSynchronously
    |> createJsonResponse

(*
    BTC 1
    LTC 2
    XRP 52
    MIOTA 1720
    NANO 1567
    XMR 328
    ETH 1027
*)
[<Literal>]
let functionNameGetFavouritePrices = "getFavouritePrices"
[<FunctionName(functionNameGetFavouritePrices)>]
let getFavouritePrices ([<OrchestrationTrigger>] context: DurableOrchestrationContext) = task {
    let favouriteCurrencyIds = [1;2;52;328;1027;1567;1720]
    let createCryptoPriceTask id = context.CallActivityAsync<string>(functionNameGetInfo, id)
    let tasks = favouriteCurrencyIds |> List.map createCryptoPriceTask
    let! results = Task.WhenAll(tasks)

    let jsonArray =
        results
        |> Array.map JObject.Parse 
        |> Array.fold (fun (state:JArray) (element:JObject) -> state.Add(element); state) (new JArray())

    return jsonArray.ToString()
}

[<Literal>]
let functionNameFavouritePlaces = "FavouritePlaces"
[<FunctionName(functionNameFavouritePlaces)>]
let favouritePlaces ([<HttpTrigger>] req: HttpRequestMessage, [<OrchestrationClient>] client: DurableOrchestrationClient, log: TraceWriter) =
    async {
        let! instanceId = client.StartNewAsync(functionNameGetFavouritePrices, null) |> Async.AwaitTask
        return client.CreateCheckStatusResponse(req, instanceId)
    } |> Async.StartAsTask



