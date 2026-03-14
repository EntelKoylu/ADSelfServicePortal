using System.Text.RegularExpressions;

namespace AdSelfServicePortal.Helpers
{
    public static class UsernameValidator
    {
        private static readonly Regex UsernameRegex = new Regex(@"^[a-zA-Z0-9._\-]{1,64}$", RegexOptions.Compiled);

        public static bool IsValid(string username)
        {
            return !string.IsNullOrWhiteSpace(username) && UsernameRegex.IsMatch(username);
        }
    }
}
