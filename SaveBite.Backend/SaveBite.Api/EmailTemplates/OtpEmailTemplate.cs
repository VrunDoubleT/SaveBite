using System.Net;

namespace SaveBite.Backend.EmailTemplates;

// Builds reusable HTML content for emails that deliver a one-time password.
public static class OtpEmailTemplate
{
    public static string Build(string otp, int lifetimeMinutes)
        => $"""
           <p>Your SaveBite verification code is:</p>
           <p style="font-size:28px;font-weight:bold;letter-spacing:6px">{WebUtility.HtmlEncode(otp)}</p>
           <p>This code expires in {lifetimeMinutes} minutes. Never share it with anyone.</p>
           """;
}
