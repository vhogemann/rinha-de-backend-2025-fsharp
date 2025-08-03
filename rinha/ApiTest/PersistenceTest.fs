module ApiTest

open System
open NUnit.Framework
open Microsoft.Extensions.Logging.Abstractions

let logger = NullLogger<Api.Persistence>.Instance :> Microsoft.Extensions.Logging.ILogger<Api.Persistence>

[<SetUp>]
let Setup () =
    ()

[<Test>]
let BootstrapTest () =
    let connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:")
    let persistence = Api.Persistence(logger, connection) :> Api.IPersistence

    persistence.Bootstrap()
    |> Async.Catch
    |> Async.RunSynchronously
    |> function
        | Choice1Of2 _ -> Assert.Pass("Bootstrap successful")
        | Choice2Of2 ex -> Assert.Fail($"Bootstrap failed: {ex.Message}")

[<Test>]
let SavePaymentTest () =
    let connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:")
    let persistence = Api.Persistence(logger, connection) :> Api.IPersistence
    
    persistence.Bootstrap() |> Async.RunSynchronously
    
    persistence.SavePayment("default", { amount = 100M; correlationId = Guid.NewGuid(); requestedAt = DateTime.UtcNow })
    |> Async.Catch
    |> Async.RunSynchronously
    |> function
        | Choice1Of2 _ -> Assert.Pass("SavePayment successful")
        | Choice2Of2 ex -> Assert.Fail($"SavePayment failed: {ex.Message}")
        
[<Test>]
let SummaryTest () =
    let connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:")
    let persistence = Api.Persistence(logger, connection) :> Api.IPersistence
    
    persistence.Bootstrap() |> Async.RunSynchronously
    
    // Save a couple of payments
    persistence.SavePayment("default", { amount = 100M; correlationId = Guid.NewGuid(); requestedAt = DateTime.UtcNow }) |> Async.RunSynchronously
    persistence.SavePayment("fallback", { amount = 200M; correlationId = Guid.NewGuid(); requestedAt = DateTime.UtcNow }) |> Async.RunSynchronously
    
    let summary = persistence.GetPaymentsSummary(DateTime.UtcNow.AddDays(-1.0), DateTime.UtcNow) |> Async.RunSynchronously
    
    Assert.That(summary, Is.Not.Null)
    Assert.That(summary.``default``.totalRequests, Is.GreaterThan(0L))
    Assert.That(summary.fallback.totalRequests, Is.GreaterThan(0L))