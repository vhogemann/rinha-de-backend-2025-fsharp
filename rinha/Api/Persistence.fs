namespace Api

open Microsoft.Extensions.Logging
open Model
open System
open System.Data

module Persistence =
    open Donald
    let savePayment (conn:IDbConnection) (gateway:string) (request: GatewayPaymentRequest) =
        let sql = """
        INSERT INTO transactions (gateway, correlation_id, amount, created_at)
        VALUES (@Gateway, @CorrelationId, @Amount, @CreatedAt);
        """
        
        let parameters = 
            [ "@Gateway", sqlString gateway
              "@CorrelationId", sqlGuid request.correlationId
              "@Amount", sqlInt64 (request.amount * 100M)
              "@CreatedAt", sqlDateTime (request.requestedAt.ToUniversalTime()) ]
        
        conn
        |> Db.newCommand sql
        |> Db.setParams parameters
        |> Db.Async.exec

    type PaymentSummaryDTO = {
        totalRequests: int64
        totalAmount: decimal
        gateway: string
    }

    let resultToSummaryResponse (result: PaymentSummaryDTO list ) : PaymentsSummaryResponse =
        let defaultSummary = 
            result
            |> List.tryFind (fun x -> x.gateway = "default")
            |> Option.defaultValue { totalRequests = 0L; totalAmount = 0M; gateway = "default" }
        
        let fallbackSummary = 
            result
            |> List.tryFind (fun x -> x.gateway = "fallback")
            |> Option.defaultValue { totalRequests = 0L; totalAmount = 0M; gateway = "fallback" }
        
        {
            ``default`` = { totalRequests = defaultSummary.totalRequests; totalAmount = defaultSummary.totalAmount }
            fallback = { totalRequests = fallbackSummary.totalRequests; totalAmount = fallbackSummary.totalAmount }
        }

    let summaryDbReader (rd: IDataReader) : PaymentSummaryDTO=
        {
            totalRequests = rd.GetInt64(0)
            totalAmount = rd.GetDecimal(1) / 100M
            gateway = rd.GetString(2)
        }
    let getPaymentsSummary (conn:IDbConnection) (start: DateTime) (finish: DateTime) =
        let sql = """
        SELECT COUNT(*) AS totalRequests, SUM(amount) AS totalAmount, gateway
        FROM transactions
        WHERE created_at >= @Start AND created_at <= @Finish
        GROUP BY gateway
        """
        
        let parameters = 
            [ "@Start", sqlDateTime (start.ToUniversalTime())
              "@Finish", sqlDateTime (finish.ToUniversalTime()) ]
        task {
            let! result =
                conn
                |> Db.newCommand sql
                |> Db.setParams parameters
                |> Db.Async.query summaryDbReader
            return resultToSummaryResponse result
        }
type IPersistence =
    abstract member SavePayment: string * GatewayPaymentRequest -> Async<unit>
    abstract member GetPaymentsSummary: DateTime * DateTime -> Async<PaymentsSummaryResponse>

type Persistence (logger: ILogger<Persistence>, dbconn: IDbConnection) =
    interface IPersistence with
        member _.SavePayment(gateway: string, request: GatewayPaymentRequest) =
            Persistence.savePayment dbconn gateway request |> Async.AwaitTask
        member _.GetPaymentsSummary(start: DateTime, finish: DateTime) =
            Persistence.getPaymentsSummary dbconn start finish |> Async.AwaitTask