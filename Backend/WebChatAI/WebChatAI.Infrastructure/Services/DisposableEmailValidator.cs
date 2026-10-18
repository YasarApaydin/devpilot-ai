using WebChatAI.Application.Interfaces;

namespace WebChatAI.Infrastructure.Services;

public class DisposableEmailValidator : IDisposableEmailValidator
{
    private static readonly HashSet<string> DisposableDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        // Major known temp/burner email domains
        "mailinator.com", "yopmail.com", "guerrillamail.com", "guerrillamail.net", "guerrillamail.org",
        "guerrillamailblock.com", "sharklasers.com", "grr.la", "dispostable.com", "10minutemail.com",
        "10minutemail.net", "tempmail.com", "temp-mail.org", "temp-mail.io", "trashmail.com",
        "trashmail.net", "trashmail.me", "nada.ltd", "nada.email", "getnada.com",
        "getairmail.com", "mohmal.com", "generator.email", "throwawaymail.com", "inboxkitten.com",
        "tempail.com", "emailondeck.com", "crazymailing.com", "maildrop.cc", "fakemailgenerator.com",
        "armyspy.com", "cuvox.de", "dayrep.com", "fleckens.hu", "gustr.com",
        "jourrapide.com", "rhyta.com", "superrito.com", "teleworm.us", "einrot.com",
        "dropmail.me", "burnermail.io", "minutemail.com", "mytemp.email", "mytempemail.com",
        "tempmailaddress.com", "fakeinbox.com", "spambog.com", "mailcatch.com", "throwaway.email",
        "zillamail.com", "trash-mail.at", "wegwerfmail.de", "wegwerfmail.net", "yopmail.fr",
        "yopmail.net", "cool.fr.nf", "jetable.fr.nf", "courriel.fr.nf", "moncourrier.fr.nf",
        "monemail.fr.nf", "monmail.fr.nf", "hideaddress.net", "anonymbox.com", "binkmail.com",
        "bobmail.info", "chammy.info", "devnullmail.com", "dontsendmespam.de", "drdrb.net",
        "ephemail.net", "fastacura.com", "filzmail.com", "harakirimail.com", "incognitomail.org",
        "kasmail.com", "meltmail.com", "nobulk.com", "noclickemail.com", "no-spam.ws",
        "pookmail.com", "safetymail.info", "sandvikcoromant.email", "sofort-mail.de", "spamavert.com",
        "spambob.com", "spamex.com", "spamfree24.org", "spamgourmet.com", "spamhole.com",
        "spaml.com", "spamspot.com", "spoofmail.de", "tempemail.net", "temporaryemail.net",
        "trbvm.com", "trash-mail.com", "trashmailer.com", "zoemail.org", "whyspam.me",
        "disposablemail.com", "trashymail.com", "mailnesia.com", "chacuo.net", "0-mail.com",
        "0815.ru", "0clickemail.com", "10mail.org", "20minutemail.com", "33mail.com"
    };

    public bool IsDisposableEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;

        var domain = ExtractDomain(email);
        if (string.IsNullOrWhiteSpace(domain)) return false;

        if (DisposableDomains.Contains(domain))
        {
            return true;
        }

        // Subdomain checking (e.g. *.mailinator.com)
        var parts = domain.Split('.');
        if (parts.Length > 2)
        {
            var baseDomain = string.Join('.', parts.Skip(parts.Length - 2));
            if (DisposableDomains.Contains(baseDomain))
            {
                return true;
            }
        }

        return false;
    }

    public string? ExtractDomain(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;

        var atIndex = email.LastIndexOf('@');
        if (atIndex < 0 || atIndex >= email.Length - 1) return null;

        return email[(atIndex + 1)..].Trim().ToLowerInvariant();
    }
}
