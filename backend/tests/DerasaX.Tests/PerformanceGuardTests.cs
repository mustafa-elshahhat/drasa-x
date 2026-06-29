using DerasaX.Domain.Specification.Subjects;
using Xunit;

namespace DerasaX.Tests;

/// <summary>
/// Phase 22 Step 8 (PERF-02) — list endpoints must enforce a capped page size so a caller can never
/// request an unbounded result set. The subjects-list parameters previously accepted an unbounded
/// PageSize (the rest of the API already clamps via PaginationParameters). This locks the clamp at
/// <see cref="SubjectsParameters.MaxPageSize"/> (100), the floor at PageSize=default / PageNumber=1, and
/// pass-through for in-range values, so the SubjectsSpecification can only ever issue a bounded Take().
/// The model binder applies the same setter on every request; the end-to-end browse path is exercised by
/// the existing GetSubjects integration tests and the Playwright curriculum specs.
/// </summary>
public class PerformanceGuardTests
{
    [Fact]
    public void SubjectsParameters_clamps_an_abusive_page_size_to_the_safe_maximum()
    {
        Assert.Equal(SubjectsParameters.MaxPageSize, new SubjectsParameters { PageSize = 100_000 }.PageSize);
    }

    [Fact]
    public void SubjectsParameters_floors_invalid_paging_inputs()
    {
        Assert.Equal(1, new SubjectsParameters { PageNumber = 0 }.PageNumber);
        Assert.Equal(1, new SubjectsParameters { PageNumber = -7 }.PageNumber);
        // A non-positive page size falls back to the default (never 0/negative -> never an empty/odd page).
        Assert.True(new SubjectsParameters { PageSize = 0 }.PageSize > 0);
        Assert.True(new SubjectsParameters { PageSize = -3 }.PageSize > 0);
    }

    [Fact]
    public void SubjectsParameters_passes_through_in_range_values()
    {
        var p = new SubjectsParameters { PageNumber = 3, PageSize = 50 };
        Assert.Equal(3, p.PageNumber);
        Assert.Equal(50, p.PageSize);
    }
}
