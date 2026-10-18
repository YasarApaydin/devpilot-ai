using WebChatAI.Domain.Common;

namespace WebChatAI.Domain.Entities;

public class DeviceRegistration : BaseEntity
{
    public string DeviceHash { get; set; } = default!;
    public string? LastIpAddress { get; set; }
    public string? UserAgent { get; set; }
    public int RegistrationCount { get; set; } = 1;
    public DateTime FirstSeenAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    public DateTime LastRegisteredDate { get; set; } = DateTime.UtcNow.Date;
    public int DailyRegistrationsCount { get; set; } = 1;
    public int RiskScore { get; set; } = 0;
    public bool IsFlagged { get; set; } = false;
    public string? FlagReason { get; set; }

    public void RecordNewRegistration(string? ipAddress, string? userAgent)
    {
        var today = DateTime.UtcNow.Date;
        if (today > LastRegisteredDate.Date)
        {
            DailyRegistrationsCount = 1;
            LastRegisteredDate = today;
        }
        else
        {
            DailyRegistrationsCount++;
        }

        RegistrationCount++;
        LastSeenAt = DateTime.UtcNow;
        LastIpAddress = ipAddress;
        UserAgent = userAgent;

        if (DailyRegistrationsCount > 2)
        {
            RiskScore += 30;
            IsFlagged = true;
            FlagReason = $"Aynı cihazdan 24 saat içinde {DailyRegistrationsCount} hesap kaydı tespit edildi.";
        }
    }
}
