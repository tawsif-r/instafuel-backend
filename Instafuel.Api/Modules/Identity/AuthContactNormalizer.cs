namespace Instafuel.Api.Modules.Identity;

public static class AuthContactNormalizer
{
    public static string? NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

    public static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;
        var value = phone.Trim();
        var digits = new string(value.Where(character => character is >= '0' and <= '9').ToArray());
        if (digits.Length == 0) return null;
        return value.StartsWith('+') ? $"+{digits}" : digits;
    }
}
