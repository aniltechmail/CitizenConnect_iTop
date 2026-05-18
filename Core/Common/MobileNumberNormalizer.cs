namespace Core.Common
{
    public static class MobileNumberNormalizer
    {
        public static string Normalize(string mobile)
        {
            if (string.IsNullOrWhiteSpace(mobile))
                throw new ArgumentException("Mobile number is required");

            mobile = mobile.Trim().Replace(" ", "").Replace("-", "");

            if (mobile.StartsWith("+"))
                mobile = mobile[1..];

            if (mobile.StartsWith("91") && mobile.Length > 10)
                mobile = mobile[^10..];

            if (mobile.Length != 10 || !mobile.All(char.IsDigit))
                throw new ArgumentException("Invalid mobile number format");

            return $"+91{mobile}";
        }

        public static string ToSmsProviderMobile(string mobile)
        {
            var normalized = Normalize(mobile);
            return normalized.StartsWith("+91") ? normalized[3..] : normalized;
        }
    }
}
