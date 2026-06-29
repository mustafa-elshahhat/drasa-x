using DerasaX.Application.Common;
using DerasaX.Application.Dto.SubjectDto;
using DerasaX.Application.Services.Abstractions.Subject;
using DerasaX.Domain.Specification.Subjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DerasaX.Api.Controllers
{
    [Route("api/v1/[controller]")]   // canonical versioned route (Phase 22 Step 6)
    [Route("api/[controller]")]      // legacy alias — retained for backwards compatibility during /api/v1 convergence
    [ApiController]
    [Authorize(Policy = Policies.TenantMember)]
    public class SubjectsController : ControllerBase
    {
        private readonly ISubjectServices _subjectServices;
        private readonly ILogger<SubjectsController> _logger;
        public SubjectsController(ISubjectServices subjectServices, ILogger<SubjectsController> logger)
        {
            _subjectServices=subjectServices;
            _logger=logger;
        }
        [HttpGet("GetSubjects")]
        public async Task<IActionResult> GetSubjects([FromQuery] SubjectsParameters parameters)
        {
            _logger.LogInformation("Getting subjects with parameters: PageNumber={PageNumber}, PageSize={PageSize}",
                parameters.PageNumber, parameters.PageSize);

            var subjects = await _subjectServices.GetSubjectsAsync(parameters);

            _logger.LogInformation("Successfully retrieved {Count} subjects", subjects.Data?.Count() ?? 0);

            return Ok(subjects);
        }
        [HttpGet("GetSubjectById/{id}")]
        public async Task<IActionResult> GetSubjectById(string id)
        {
            _logger.LogInformation("Getting subject by ID: {SubjectId}", id);

            var subject = await _subjectServices.GetSubjectByIdAsync(id);

            _logger.LogInformation("Successfully retrieved subject with ID: {SubjectId}", id);

            return Ok(subject);
        }
        [HttpGet("GetSubjectsByGradeIdAsync")]
        //[AllowAnonymous] // Public endpoint 
        public async Task<IActionResult> GetSubjectsByGradeIdAsync(string id)
        {
            _logger.LogInformation("Geting Subjects By Grade Id : {GradeId}", id);

            var result = await _subjectServices.GetSubjectsByGradeIdAsync(id);

            _logger.LogInformation("Successfully retrieved Subjects for Grade ID: {GradeId}", id);
            return Ok(result);
        }
        [HttpPost("AddSubject")]
        [Authorize(Policy = Policies.TeacherOrSchoolAdmin)]
        public async Task<IActionResult> AddSubject([FromForm] AddSubjectDto addSubjectDto)
        {
            _logger.LogInformation("Adding new subject: {SubjectName}", addSubjectDto.Name);

            var result = await _subjectServices.AddSubjectAsync(addSubjectDto);

            _logger.LogInformation("Successfully added subject: {SubjectName} with ID: {SubjectId}", addSubjectDto.Name, result.Data?.Id);

            return Ok(result); 
        }
        [HttpPut("UpdateSubject")]
        [Authorize(Policy = Policies.TeacherOrSchoolAdmin)]
        public async Task<IActionResult> UpdateSubject([FromForm] UpdateSubjectDto updateSubjectDto)
        {
            _logger.LogInformation("Updating subject with ID: {SubjectId}", updateSubjectDto.Id);

            var result = await _subjectServices.UpdateSubjectAsync(updateSubjectDto);

            _logger.LogInformation("Successfully updated subject with ID: {SubjectId}", updateSubjectDto.Id);

            return Ok(result);
        }
        [HttpDelete("DeleteSubject/{id}")]
        [Authorize(Policy = Policies.TeacherOrSchoolAdmin)]
        public async Task<IActionResult> DeleteSubject(string id)
        {
            _logger.LogInformation("Deleting subject with ID: {SubjectId}", id);

            var result = await _subjectServices.DeleteSubject(id);

            _logger.LogInformation("Successfully deleted subject with ID: {SubjectId}", id);

            return Ok(result);
        }
    }
}
