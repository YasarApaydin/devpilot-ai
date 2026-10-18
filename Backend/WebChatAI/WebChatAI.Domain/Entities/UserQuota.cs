using WebChatAI.Domain.Common;

namespace WebChatAI.Domain.Entities;

public class UserQuota : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;

    public int DailyCreditLimit { get; set; } = 15;
    public int CreditsUsedToday { get; set; } = 0;
    public DateTime CycleStartDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime? LastConsumedAt { get; set; }

    public string SubscriptionTier { get; set; } = "free";
    public bool IsExemptFromAbuseLimits { get; set; } = false;
    public string? AllocatedByDeviceHash { get; set; }

    public int RemainingCredits => Math.Max(0, DailyCreditLimit - CreditsUsedToday);

    public void CheckAndResetCycle()
    {
        var today = DateTime.UtcNow.Date;
        if (today > CycleStartDate.Date)
        {
            CreditsUsedToday = 0;
            CycleStartDate = today;
        }
    }
}
