namespace DerasaX.Api.Security
{
    /// <summary>
    /// Phase 18 — configurable HTTP security headers (<c>SecurityHeaders</c> section). Safe defaults
    /// suit a JSON API: a strict CSP that forbids subresources/framing, nosniff, frame denial,
    /// a strict referrer policy and a locked-down permissions policy. HSTS is emitted only over
    /// HTTPS outside Development (browsers ignore it on plain HTTP anyway).
    /// </summary>
    public class SecurityHeadersOptions
    {
        public const string SectionName = "SecurityHeaders";

        /// <summary>Master switch (default true). When false, no headers are added.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary><c>X-Content-Type-Options</c>. Default "nosniff".</summary>
        public string ContentTypeOptions { get; set; } = "nosniff";

        /// <summary><c>X-Frame-Options</c>. Default "DENY" (CSP frame-ancestors is the modern equivalent).</summary>
        public string XFrameOptions { get; set; } = "DENY";

        /// <summary><c>Referrer-Policy</c>. Default "no-referrer".</summary>
        public string ReferrerPolicy { get; set; } = "no-referrer";

        /// <summary><c>Permissions-Policy</c>. Default disables camera/mic/geolocation/etc.</summary>
        public string PermissionsPolicy { get; set; } =
            "camera=(), microphone=(), geolocation=(), payment=(), usb=(), browsing-topics=()";

        /// <summary><c>Cross-Origin-Opener-Policy</c>. Default "same-origin".</summary>
        public string CrossOriginOpenerPolicy { get; set; } = "same-origin";

        /// <summary>
        /// <c>Content-Security-Policy</c>. Default is API-appropriate: deny everything + no framing.
        /// Skipped for the Development Swagger UI so the interactive docs keep working.
        /// </summary>
        public string ContentSecurityPolicy { get; set; } =
            "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";

        /// <summary>Emit <c>Strict-Transport-Security</c> over HTTPS outside Development.</summary>
        public bool EnableHsts { get; set; } = true;

        /// <summary>HSTS value. Default 1 year + subdomains (add <c>; preload</c> only when ready).</summary>
        public string HstsValue { get; set; } = "max-age=31536000; includeSubDomains";
    }
}
