namespace dagangOnline.Domain.Chat;

public enum ChatSessionStatus
{
    ActiveWithBot = 1,
    Escalated = 2,
    ActiveWithAgent = 3,
    Resolved = 4
}
