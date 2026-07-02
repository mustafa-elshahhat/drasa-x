using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Services.Abstractions.TeacherPortal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DerasaX.Api.Controllers
{
    /// <summary>
    /// Phase 9 — Teacher Portal summary + assignment-scoped reads. Teacher-only:
    /// this is the Teacher's own personal dashboard/assignment surface, not a
    /// school-administration endpoint (SchoolAdmin Teacher-portal removal —
    /// SchoolAdmin uses the school-admin endpoints under /api/v1/school-admin/*
    /// instead). The service layer additionally enforces the teacher's ACTIVE
    /// assignment scope and tenant on every read, so a teacher only ever sees
    /// classes/subjects/students they are assigned to.
    /// </summary>
    [ApiController]
    [Route("api/v1/teacher")]
    [Authorize(Policy = Policies.TeacherOnly)]
    public class TeacherController : ControllerBase
    {
        private readonly ITeacherPortalService _service;
        public TeacherController(ITeacherPortalService service) => _service = service;

        /// <summary>Teacher dashboard summary (assignment-scoped counts).</summary>
        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard(CancellationToken ct)
            => R(await _service.DashboardAsync(ct));

        /// <summary>Classes the teacher is actively assigned to.</summary>
        [HttpGet("classes")]
        public async Task<IActionResult> MyClasses(CancellationToken ct)
            => R(await _service.MyClassesAsync(ct));

        /// <summary>Subjects the teacher is actively assigned to.</summary>
        [HttpGet("subjects")]
        public async Task<IActionResult> MySubjects(CancellationToken ct)
            => R(await _service.MySubjectsAsync(ct));

        /// <summary>Students actively enrolled in an assigned class (403 if unassigned, 404 cross-tenant).</summary>
        [HttpGet("classes/{classId}/students")]
        public async Task<IActionResult> ClassStudents(string classId, CancellationToken ct)
            => R(await _service.ClassStudentsAsync(classId, ct));

        private IActionResult R<T>(Domain.Common.ApiResponse<T> r) => StatusCode(r.StatusCode, r);
    }
}
