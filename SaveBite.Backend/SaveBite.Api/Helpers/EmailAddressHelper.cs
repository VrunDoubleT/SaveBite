namespace SaveBite.Backend.Helpers;


// Provides reusable operations for canonicalizing email addresses.
public static class EmailAddressHelper
{
    public static string Normalize(string email)
        => email.Trim().ToLowerInvariant();
}
