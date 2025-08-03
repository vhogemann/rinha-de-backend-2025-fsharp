namespace Api

open System
open Microsoft.Extensions.Logging
open Model

module Client =
    open FsHttp
    let postPayment baseUrl (paymentRequest: GatewayPaymentRequest) : Async<PaymentResponse> = async {
        let! response =
            http {
                POST $"{ baseUrl }/payments"
                body
                jsonSerialize
                    paymentRequest
            }
            |> Request.sendAsync
        return response |> Response.deserializeJson<PaymentResponse>
    }
    
    let healthCheck baseUrl : Async<Health> = async {
        let! response =
            http {
                GET $"{ baseUrl }/payments/service-health"
            }
            |> Request.sendAsync
            
        return response |> Response.deserializeJson<Health>
    }

type Client (logger: ILogger, baseUrl:string, name: string) =

    let mutable _health: Health = {
        failing = false
        minResponseTime = 0L
    }
    
    let mutable _summary: PaymentSummary = {
        totalRequests = 0L
        totalAmount = 0M
    }
    
    member this.Name() = name
    member this.Summary() = _summary
    member this.Health() = _health
    
    member _.Payment(request:GatewayPaymentRequest) = async {
        logger.LogDebug("Sending payment request: {CorrelationId} with amount: {Amount}", request.correlationId, request.amount)
        let! result = Client.postPayment baseUrl request |> Async.Catch
        match result with
        | Choice1Of2 response ->
            _summary <- {
                totalRequests = _summary.totalRequests + 1L
                totalAmount = _summary.totalAmount + request.amount
            }
            return Result.Ok response
        | Choice2Of2 ex ->
            logger.LogError(ex, "Payment request failed for {CorrelationId}", request.correlationId)
            return Result.Error ex
    }
    
    member this.StartHealthCheck() = async {
        let rec loop() = async {
            let! result = Client.healthCheck baseUrl |> Async.Catch
            match result with
            | Choice1Of2 health ->
                logger.LogDebug("Client {Name} health check: {Health}", name, health)
                _health <- health
            | Choice2Of2 exn ->
                logger.LogError(exn, "Health check failed for {Name}", name)
                _health <- { failing = true; minResponseTime = Int64.MaxValue }
            do! Async.Sleep 5000 // Wait for 5 seconds before the next check    
            return! loop()
        }
        loop() |> Async.Start
    }