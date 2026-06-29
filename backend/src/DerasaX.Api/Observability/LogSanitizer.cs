using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace DerasaX.Api.Observability
{
    /// <summary>
    /// Phase 19 — masks sensitive values before they reach logs. The signed-download
    /// path carries the download token in the query string (?token=...); request logs
    /// must never persist it verbatim. Also masks any key/secret/password/signature
    /// style query parameter. Backend never logs Authorization headers or bodies.
    /// </summary>
    public static class LogSanitizer
    {
        private static readonly HashSet<string> SensitiveParams = new(StringComparer.OrdinalIgnoreCase)
        {
            "token", "key", "secret", "password", "pwd", "signature", "sig",
            "access_token", "refresh_token", "code", "apikey", "api_key"
        };

        /// <summary>Returns "path?param=value&token=***" with sensitive query values masked.</summary>
        public static string MaskQuery(PathString path, QueryString query)
        {
            var p = path.HasValue ? path.Value! : "/";
            if (!query.HasValue || string.IsNullOrEmpty(query.Value)) return p;

            var raw = query.Value!.TrimStart('?');
            var sb = new StringBuilder(p);
            sb.Append('?');
            var first = true;
            foreach (var pair in raw.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                if (!first) sb.Append('&');
                first = false;
                var eq = pair.IndexOf('=');
                if (eq <= 0) { sb.Append(pair); continue; }
                var name = pair.Substring(0, eq);
                sb.Append(name).Append('=');
                sb.Append(SensitiveParams.Contains(Uri.UnescapeDataString(name)) ? "***" : pair.Substring(eq + 1));
            }
            return sb.ToString();
        }

        /// <summary>Masks a token/secret to a short, non-reversible hint for diagnostics.</summary>
        public static string MaskToken(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "(empty)";
            if (value.Length <= 4) return "***";
            return value.Substring(0, 2) + "***" + "(len=" + value.Length + ")";
        }
    }
}
