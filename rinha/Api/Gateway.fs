namespace Api

open System
open System.Threading.Channels
open Api.Model
open Microsoft.Extensions.Logging

type IGateway =
    abstract member ProcessPayment: PaymentRequest -> unit
    abstract member StartHealthChecks: unit -> unit
    abstract member Summary: unit -> PaymentsSummaryResponse

type Gateway(logger: ILogger<Gateway>, defaultBaseUrl: string, fallbackBaseUrl: string, persistence: IPersistence) =
    let defaultClient = Client(logger, defaultBaseUrl, "default")
    let fallbackClient = Client(logger, fallbackBaseUrl, "fallback")

    let getRoute () =
        logger.LogInformation("Default client health: {Health}", defaultClient.Health())
        logger.LogInformation("Fallback client health: {Health}", fallbackClient.Health())

        match defaultClient.Health().failing, fallbackClient.Health().failing with
        | false, _
        | _, true -> Some defaultClient
        | true, false -> Some fallbackClient

    let channel = Channel.CreateUnbounded<GatewayPaymentRequest>()

    let paymentAgent =
        MailboxProcessor.Start(fun inbox ->
            let rec loop () =
                async {
                    let! paymentRequest = inbox.Receive()

                    match getRoute () with
                    | Some client ->
                        let! response = client.Payment(paymentRequest) |> Async.Catch

                        match response with
                        | Choice1Of2 _ ->
                            do! persistence.SavePayment(client.Name(), paymentRequest)
                            return! loop () // Continue processing next payment
                        | Choice2Of2 exn ->
                            logger.LogError(
                                exn,
                                "Payment processing failed for {CorrelationId}",
                                paymentRequest.correlationId
                            )

                            do! Async.Sleep 1000 // Wait for 1 second before retrying
                            inbox.Post paymentRequest // Retry the same payment request
                    | None ->
                        do! Async.Sleep 1000 // Wait for 1 second before retrying
                        inbox.Post paymentRequest
                }
            loop ())

    interface IGateway with
        member _.ProcessPayment(paymentRequest: PaymentRequest) =
            let gatewayRequest: GatewayPaymentRequest =
                { correlationId = paymentRequest.correlationId
                  amount = paymentRequest.amount
                  requestedAt = DateTime.UtcNow }

            paymentAgent.Post(gatewayRequest)

        member _.StartHealthChecks() =
            defaultClient.StartHealthCheck() |> Async.Start
            fallbackClient.StartHealthCheck() |> Async.Start

        member _.Summary() : PaymentsSummaryResponse =
            { ``default`` = defaultClient.Summary()
              fallback = fallbackClient.Summary() }
