namespace dagangOnline.Domain.Chat;

public enum ConversationPriority
{
    Low,
    Normal,
    High,
    Urgent
}

public enum IntentType
{
    product_information,
    product_recommendation,
    service_information,
    order_status,
    payment,
    shipping,
    return_intent,
    refund,
    warranty,
    complaint,
    technical_support,
    promotion,
    human_request,
    unknown
}
