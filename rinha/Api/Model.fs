module Api.Model

open System

type PaymentRequest = {
    correlationId: Guid
    amount: decimal
}

type GatewayPaymentRequest = {
    correlationId : Guid
    amount: decimal
    requestedAt: DateTime
}

type PaymentsSummaryRequest = {
    from: DateTime
    ``to``: DateTime
}

type PaymentsSummaryResponse = {
    ``default``: PaymentSummary
    fallback: PaymentSummary
} and PaymentSummary = {
    totalRequests: int64
    totalAmount: decimal
}

type PaymentResponse = {
    message: string
}

type Health = {
    failing: bool
    minResponseTime: int64
}