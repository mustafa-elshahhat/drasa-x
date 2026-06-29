using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 22 Step 9 (XL-03, backend side) — checks the live backend OpenAPI route surface against a
/// checked-in snapshot (<c>docs/contracts/backend-openapi-v1-paths.json</c>) so any added/removed route
/// is a deliberate, reviewed change rather than silent contract drift. Mirrors the AI-side
/// <c>docs/contracts/ai-internal-v1.json</c> drift test. The snapshot self-bootstraps on first run; to
/// accept an intentional route change, delete the snapshot file and re-run to regenerate it.
/// </summary>
public class BackendOpenApiContractTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public BackendOpenApiContractTests(IntegrationFactory factory) => _factory = factory;

    private static string SnapshotPath() => Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "docs", "contracts", "backend-openapi-v1-paths.json"));

    [Fact]
    public async Task Backend_openapi_route_surface_matches_the_checked_in_snapshot()
    {
        var client = TestClient.NewClient(_factory);
        var resp = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(System.Net.HttpStatusCode.OK, resp.StatusCode);

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var livePaths = doc.RootElement.GetProperty("paths").EnumerateObject()
            .Select(p => p.Name).OrderBy(x => x, StringComparer.Ordinal).ToList();

        var snapshotPath = SnapshotPath();
        var opts = new JsonSerializerOptions { WriteIndented = true };
        if (!File.Exists(snapshotPath))
        {
            // Self-bootstrap the baseline on first run (or after a deliberate regeneration).
            Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath)!);
            File.WriteAllText(snapshotPath, JsonSerializer.Serialize(livePaths, opts));
        }

        var snapshot = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(snapshotPath)) ?? new List<string>();
        var added = livePaths.Except(snapshot).OrderBy(x => x, StringComparer.Ordinal).ToList();
        var removed = snapshot.Except(livePaths).OrderBy(x => x, StringComparer.Ordinal).ToList();

        Assert.True(added.Count == 0 && removed.Count == 0,
            $"Backend OpenAPI route drift detected.\n  ADDED:   [{string.Join(", ", added)}]\n  REMOVED: [{string.Join(", ", removed)}]\n" +
            "If this change is intentional, delete docs/contracts/backend-openapi-v1-paths.json and re-run to regenerate the snapshot.");
    }

    [Fact]
    public async Task Canonical_api_v1_academic_routes_are_present_in_the_openapi_document()
    {
        // Phase 22 Step 6 converged the legacy academic controllers onto /api/v1; lock their presence.
        var client = TestClient.NewClient(_factory);
        var json = await (await client.GetAsync("/swagger/v1/swagger.json")).Content.ReadAsStringAsync();
        foreach (var required in new[]
        {
            "/api/v1/Subjects/GetSubjects", "/api/v1/Grades/GetAllGrades", "/api/v1/Units/GetUnitsBySubjectId",
            "/api/v1/Lessons/GetLessonsByUnitId", "/api/v1/Quiz/GetAllQuizzes", "/api/v1/ai/tutor",
        })
            Assert.Contains(required, json);
    }
}
