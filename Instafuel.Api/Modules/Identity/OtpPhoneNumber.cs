using Instafuel.Api.Exceptions;

namespace Instafuel.Api.Modules.Identity;

public static class OtpPhoneNumber
{
    public static string NormalizeBangladeshPhone(string? value)
    {
        var normalized = AuthContactNormalizer.NormalizePhone(value);
        var digits = normalized is null
            ? string.Empty
            : new string(normalized.Where(character => character is >= '0' and <= '9').ToArray());

        if (digits.Length == 11 && digits.StartsWith("01", StringComparison.Ordinal)
            && digits[2] is >= '3' and <= '9')
            digits = "88" + digits;

        if (digits.Length != 13 || !digits.StartsWith("8801", StringComparison.Ordinal)
            || digits[4] is < '3' or > '9')
            throw new AppValidationException("Phone", "OTP delivery currently supports Bangladesh mobile numbers only.");

        return "+" + digits;
    }

    public static string[] GetStoredPhoneVariants(string canonicalPhone)
    {
        var digits = canonicalPhone[1..];
        return [canonicalPhone, "0" + digits[3..]];
    }

    public static string ToSslWirelessMsisdn(string canonicalPhone) => canonicalPhone[1..];
}
