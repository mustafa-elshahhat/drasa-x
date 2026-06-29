using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Captures the RUNTIME OpenAPI document from the live in-process API and reconciles the
/// implemented Phase 5 route families against it (so the ENDPOINT_INVENTORY is generated from the
/// real swagger document, not a hand-written list). Also writes the document to the phase-5 logs
/// for evidence. Runs in the WDAC-trusted test host (the EF/swagger CLI design host is blocked).
/// </summary>
public class OpenApiCaptureTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public OpenApiCaptureTests(IntegrationFactory factory) => _factory = factory;

    [Fact]
    public async Task Capture_openapi_and_assert_phase5_routes_present()
    {
        var client = TestClient.NewClient(_factory);
        var resp = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.Content.ReadAsStringAsync();

        // Persist the runtime OpenAPI document next to the other phase-5 logs.
        // From tests/DerasaX.Tests/bin/Debug/net9.0 up to the workspace root, then docs/phase5/logs.
        var logsDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "docs", "phase5", "logs"));
        try
        {
            Directory.CreateDirectory(logsDir);
            File.WriteAllText(Path.Combine(logsDir, "openapi-v1-phase5.json"), json);

            using var doc = JsonDocument.Parse(json);
            var paths = doc.RootElement.GetProperty("paths").EnumerateObject().Select(p => p.Name).OrderBy(x => x).ToList();
            File.WriteAllText(Path.Combine(logsDir, "08-openapi-endpoint-inventory.txt"),
                $"Runtime OpenAPI endpoint inventory ({paths.Count} paths)\n\n" + string.Join("\n", paths));
        }
        catch (IOException) { /* read-only FS in some CI sandboxes — assertions below still validate */ }

        // Reconcile: every implemented Phase 5 route family must appear in the runtime document.
        foreach (var required in new[]
        {
            "/api/v1/quizzes", "/api/v1/assigned-quizzes", "/api/v1/submissions/{attemptId}/grade",
            "/api/v1/students/{studentId}/lesson-progress", "/api/v1/performance/class/{classId}",
            "/api/v1/conversations", "/api/v1/parent-requests", "/api/v1/announcements", "/api/v1/suggestions",
            "/api/v1/communities", "/api/v1/competitions", "/api/v1/badges", "/api/v1/office-hours",
            "/api/v1/tenants", "/api/v1/my-tenant", "/api/v1/support-requests", "/api/v1/audit",
            "/api/v1/ai-usage", "/api/v1/system-settings", "/api/v1/feature-flags", "/api/v1/files", "/api/v1/reports/tenant-users",
            // Phase 5 closure additions:
            "/api/v1/homework", "/api/v1/homework/assigned", "/api/v1/homework/{id}/submit",
            "/api/v1/tenant-users", "/api/v1/lesson-materials/{materialId}/comments", "/api/v1/notifications"
        })
            Assert.Contains(required, json);
    }
}
