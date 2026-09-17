namespace dagangOnline.Domain.Chat;

public enum ConversationStatus
{
    Open,
    WaitingForAgent,
    WaitingForCustomer,
    Resolved,
    Closed,
    Escalated
}
