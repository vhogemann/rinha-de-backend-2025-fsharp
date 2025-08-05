module Api.Program
open System
open System.Data
open Api
open Api.Model
open Falco
open Falco.Routing
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging


let healthCheck : HttpHandler = fun ctx -> task {
    let persistence = ctx.Plug<IPersistence>()
    let! result = persistence.HealthCheck()
    match result with
    | Some _ -> 
        return (Response.withStatusCode 200 >> Response.ofPlainText "OK") ctx
    | None ->
        return (Response.withStatusCode 503 >> Response.ofPlainText "Service Unavailable") ctx
}

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
    let start =
        ctx.Request.Query.["from"] |> DateTime.TryParse |> function
            | true, date -> date
            | false, _ ->
                DateTime.UtcNow.AddMinutes(-1.0)                  
    let finish = ctx.Request.Query.["to"] |> DateTime.TryParse |> function
        | true, date -> date
        | false, _ ->
            DateTime.UtcNow
    let! result = persistence.GetPaymentsSummary (start, finish)
    return Response.ofJson result ctx
}
    

[<EntryPoint>]
let main args =

    let routes = [
        post "/payments" payments
        get "/payments-summary" summary
        get "/health" healthCheck
    ]
    
    let builder = WebApplication.CreateBuilder(args)
    
    let conf =
        builder
            .Configuration
            .AddJsonFile("appsettings.json", optional = false, reloadOnChange = true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional = true, reloadOnChange = true)
            .AddEnvironmentVariables()
            .Build()
    
    builder.Logging.AddConsole() |> ignore
    
    builder
        .Services
        .AddLogging()
        .AddTransient<IDbConnection>(fun _ ->
            new Npgsql.NpgsqlConnection(conf.GetConnectionString("DefaultConnection")))
        .AddSingleton<IPersistence, Persistence>()
        .AddSingleton<IGateway>(fun ctx ->
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