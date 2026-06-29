using System.Threading.Tasks;
using DerasaX.Api.SeedData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace DerasaX.Api.Controllers
{
    /// <summary>
    /// Phase 8 §7 — Development/Test-only deterministic E2E fixture reset. This
    /// endpoint exists ONLY to make the live Playwright acceptance matrix repeatable.
    /// It is hard-gated three ways:
    ///   1. It returns 404 (as if absent) outside Development/Test environments —
    ///      it can never run against a production deployment.
    ///   2. It requires a matching <c>X-E2E-Reset-Key</c> header (config E2E:ResetKey,
    ///      local-only default) so it is NOT a public unauthenticated reset.
    ///   3. It resets ONLY the mutable rows owned by the Phase 8 E2E fixture actors
    ///      (STU-T1 / STU-T2) via <see cref="DataSeederService.ResetE2EAcceptanceStateAsync"/>.
    ///      It never drops the database and never deletes unrelated user data.
    /// </summary>
    [ApiController]
    [AllowAnonymous]
    [Route("api/v1/dev/e2e")]
    public class DevE2EController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly DataSeederService _seeder;

        public DevE2EController(IWebHostEnvironment env, IConfiguration config, DataSeederService seeder)
        {
            _env = env;
            _config = config;
            _seeder = seeder;
        }

        [HttpPost("reset")]
        public async Task<IActionResult> Reset()
        {
            // 1. Environment hard-gate — invisible outside Development/Test.
            if (!_env.IsDevelopment() && !_env.IsEnvironment("Test"))
                return NotFound();

            // 2. Shared-secret header (local-only default; never a production secret).
            var expected = _config["E2E:ResetKey"] ?? "ph8-e2e-local";
            if (!Request.Headers.TryGetValue("X-E2E-Reset-Key", out var provided) || provided != expected)
                return NotFound();

            await _seeder.ResetE2EAcceptanceStateAsync();
            return Ok(new { reset = true });
        }
    }
}
