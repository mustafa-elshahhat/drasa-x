using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DerasaX.Api.Observability;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 22 Step 9 (SEC-06, backend side) — PII-in-logs regression coverage.
/// (1) Locks the Phase 19 <see cref="LogSanitizer"/> masking (sensitive query params + tokens) so the
///     request-log path can never persist a download token / secret / password verbatim.
/// (2) A source guard that fails if any backend <c>Log*</c> statement is added that interpolates a raw
///     PII/secret value (email, password, raw bearer token, an AI prompt/answer/question, or an
///     Authorization header). Non-sensitive operational context (correlation ids, entity ids, category,
///     status) is intentionally NOT restricted — the sweep sanitizes sensitive values, it does not
///     weaken logging.
/// </summary>
public class BackendLogPiiSweepTests
{
    [Theory]
    [InlineData("token", true)]
    [InlineData("access_token", true)]
    [InlineData("refresh_token", true)]
    [InlineData("key", true)]
    [InlineData("secret", true)]
    [InlineData("password", true)]
    [InlineData("sig", true)]
    [InlineData("apikey", true)]
    [InlineData("page", false)]
    [InlineData("id", false)]
    public void MaskQuery_masks_only_sensitive_query_params(string param, bool masked)
    {
        var result = LogSanitizer.MaskQuery(new PathString("/api/v1/files/download"), new QueryString($"?{param}=SECRET-VALUE-123"));
        if (masked)
        {
            Assert.Contains($"{param}=***", result);
            Assert.DoesNotContain("SECRET-VALUE-123", result);
        }
        else
        {
            Assert.Contains($"{param}=SECRET-VALUE-123", result); // non-sensitive value preserved for diagnostics
        }
    }

    [Fact]
    public void MaskToken_never_reveals_the_token_value()
    {
        var masked = LogSanitizer.MaskToken("super-secret-bearer-token-abcdef");
        Assert.DoesNotContain("secret", masked);
        Assert.DoesNotContain("abcdef", masked);
        Assert.Equal("(empty)", LogSanitizer.MaskToken(null));
    }

    [Fact]
    public void No_backend_log_statement_interpolates_a_raw_pii_or_secret_value()
    {
        var srcRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "backend", "src"));
        Assert.True(Directory.Exists(srcRoot), $"backend src not found at {srcRoot}");

        // Structured-logging placeholders (or an Authorization header) that would persist a raw value.
        var forbidden = new Regex(
            @"Log(Information|Warning|Error|Debug|Critical|Trace)\s*\([^;]*" +
            @"(\{Email\}|\{Password\}|\{PasswordHash\}|\{RawToken\}|\{AccessToken\}|\{RefreshToken\}|\{Prompt\}|\{Answer\}|\{Question\}|Authorization)",
            RegexOptions.Singleline);

        var offenders = new List<string>();
        foreach (var file in Directory.EnumerateFiles(srcRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") ||
                file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")) continue;
            if (forbidden.IsMatch(File.ReadAllText(file))) offenders.Add(Path.GetFileName(file));
        }

        Assert.True(offenders.Count == 0,
            $"Backend log statement(s) may leak a raw PII/secret value: {string.Join(", ", offenders.Distinct())}");
    }
}
