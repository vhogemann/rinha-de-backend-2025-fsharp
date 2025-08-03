module Api.Program
open System
open System.Data
open Api
open Api.Model
open Falco
open Falco.Routing
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Data.Sqlite
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging

let payments : HttpHandler = fun ctx -> task {
    let! request =
        ctx
        |> Request.getJson<PaymentRequest>

    let gateway = ctx.Plug<IGateway>()

    gateway.ProcessPayment {   
        correlationId = request.correlationId
        amount = request.amount
    }

    return (Response.withStatusCode 200 >> Response.ofPlainText "OK") ctx
}

let summary : HttpHandler = fun ctx -> task {
    let persistence = ctx.Plug<IPersistence>()
    let start = ctx.Request.Query.["from"] |> DateTime.Parse
    let finish = ctx.Request.Query.["to"] |> DateTime.Parse
    let! result = persistence.GetPaymentsSummary (start, finish)
    return Response.ofJson result ctx
}
    

[<EntryPoint>]
let main args =

    let routes = [
        post "/payments" payments
        get "/payments-summary" summary
    ]
    
    let builder = WebApplication.CreateBuilder(args)
    
    let conf =
        builder
            .Configuration
            .AddJsonFile("appsettings.json", optional = false, reloadOnChange = true)
            .AddEnvironmentVariables()
            .Build()
    
    builder.Logging.AddConsole() |> ignore
    
    builder
        .Services
        .AddLogging()
        .AddTransient<IDbConnection>(fun _ -> new SqliteConnection(conf.GetConnectionString("DefaultConnection")))
        .AddSingleton<IPersistence, Persistence>()
        .AddSingleton<IGateway>(fun ctx ->
            // Having this here is a hack... but whatever
            ctx.GetService<IPersistence>().Bootstrap() |> Async.RunSynchronously
            let gateway =
                Gateway(
                    ctx.GetService<Microsoft.Extensions.Logging.ILogger<Gateway>>(),
                    conf.GetValue("DefaultGatewayUrl"),
                    conf.GetValue("FallbackGatewayUrl"),
                    ctx.GetService<IPersistence>()) :> IGateway
            gateway.StartHealthChecks()
            gateway
            )
            
    |> ignore

    let wapp = builder.Build()
    
    wapp
        .UseRouting()
        .UseFalco(routes)
        .Run(Response.ofPlainText "Not Found")
    0