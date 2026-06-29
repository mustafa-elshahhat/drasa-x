using DerasaX.Application.Common;
using DerasaX.Application.Dto.LessonDto;
using DerasaX.Application.Dto.UnitDto;
using DerasaX.Application.Services.Abstractions.Lesson;
using DerasaX.Application.Services.Abstractions.Unit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DerasaX.Api.Controllers
{
    [Route("api/v1/[controller]")]   // canonical versioned route (Phase 22 Step 6)
    [Route("api/[controller]")]      // legacy alias — retained for backwards compatibility during /api/v1 convergence
    [ApiController]
    [Authorize(Policy = Policies.TenantMember)]
    public class LessonsController : ControllerBase
    {
        private readonly ILessonServices _lessonServices;
        private readonly ILogger<LessonsController> _logger;
        public LessonsController(ILessonServices lessonServices, ILogger<LessonsController> logger)
        {
            _lessonServices=lessonServices;
            _logger=logger;
        }
        [HttpGet("GetLessonsByUnitId")]
        //[AllowAnonymous] // Public endpoint 
        public async Task<IActionResult> GetLessonsByUnitId(string id)
        {
            _logger.LogInformation("Getting Lessons by Unit ID: {UnitId}", id);

            var result = await _lessonServices.GetLessonByUnitIdAsync(id);

            _logger.LogInformation("Successfully retrieved Lessons for unit ID: {unitId}", id);
            return Ok(result);
        }

        [HttpPost("AddLesson")]
        [Authorize(Policy = Policies.TeacherOrSchoolAdmin)]
        public async Task<IActionResult> AddLesson([FromForm] AddLessonDto lessonDto)
        {
            _logger.LogInformation("Adding new Lesson");

            var result = await _lessonServices.AddLessonAsync(lessonDto);

            _logger.LogInformation("Successfully added Lesson with ID: {LessonId}", result.Data?.Id);
            return Ok(result);
        }

        [HttpPut("UpdateLesson")]
        [Authorize(Policy = Policies.TeacherOrSchoolAdmin)]
        public async Task<IActionResult> UpdateLesson([FromForm] GetLessonDto getLessonDto)
        {
            _logger.LogInformation("Updating lesson with ID: {lessonId}", getLessonDto.Id);

            var result = await _lessonServices.UpdateLessonAsync(getLessonDto);

            _logger.LogInformation("Successfully updated Lessson with ID: {LessonId}", getLessonDto.Id);
            return Ok(result);
        }

        [HttpDelete("DeleteLesson")]
        [Authorize(Policy = Policies.TeacherOrSchoolAdmin)]
        public async Task<IActionResult> DeleteLesson(string id)
        {
            _logger.LogInformation("Deleting Lesson with ID: {LessonId}", id);

            var result = await _lessonServices.DeleteLesson(id);

            _logger.LogInformation("Successfully deleted Lesson with ID: {LessonId}", id);
            return Ok(result);
        }
    }
}
