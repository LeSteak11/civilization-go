using System;
using System.Security.Cryptography;
using System.Text;

namespace Epoch.Application.Runs
{
    /// <summary>
    /// SHA-256 over canonical UTF-8 bytes (Implementation Lock decision 3).
    ///
    /// Hashing lives here rather than in the Core because System.Security is a forbidden
    /// namespace there (Technical Plan sec.3.1) - the Core produces canonical bytes and
    /// nothing else. That split is enforced by the architecture guard, not by convention.
    ///
    /// The digest is lowercase hex, formatted without a culture-sensitive path so that
    /// two machines with different locales cannot produce different text for one hash.
    /// </summary>
    public static class StateHasher
    {
        private const string HexDigits = "0123456789abcdef";

        public static string Sha256Hex(string canonicalText)
        {
            if (canonicalText is null)
            {
                throw new ArgumentNullException(nameof(canonicalText));
            }

            byte[] bytes = new UTF8Encoding(false).GetBytes(canonicalText);
            using (SHA256 sha = SHA256.Create())
            {
                return ToHex(sha.ComputeHash(bytes));
            }
        }

        public static string ToHex(byte[] digest)
        {
            if (digest is null)
            {
                throw new ArgumentNullException(nameof(digest));
            }

            char[] text = new char[digest.Length * 2];
            for (int i = 0; i < digest.Length; i++)
            {
                text[i * 2] = HexDigits[digest[i] >> 4];
                text[(i * 2) + 1] = HexDigits[digest[i] & 0x0F];
            }

            return new string(text);
        }
    }
}
