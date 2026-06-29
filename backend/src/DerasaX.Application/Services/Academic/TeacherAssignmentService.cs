using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.AcademicDto;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Academic;
using DerasaX.Application.Services.Abstractions.Audit;
using DerasaX.Domain.Common;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Specification.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DerasaX.Application.Services.Academic
{
    public class TeacherAssignmentService : AcademicServiceBase, ITeacherAssignmentService
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly ILogger<TeacherAssignmentService> _logger;

        public TeacherAssignmentService(IUnitOfWork unitOfWork, ITenantContext tenant, IAuditWriter audit,
            UserManager<ApplicationUser> users, ILogger<TeacherAssignmentService> logger) : base(unitOfWork, tenant, audit)
        {
            _users = users;
            _logger = logger;
        }

        public async Task<PaginationResponse<IEnumerable<TeacherSubjectAssignmentDto>>> ListAsync(TeacherSubjectAssignmentParameters p, CancellationToken ct = default)
        {
            RequireTenant();
            System.Linq.Expressions.Expression<Func<TeacherSubjectAssignment, bool>> criteria = a =>
                (string.IsNullOrEmpty(p.TeacherId) || a.TeacherId == p.TeacherId) &&
                (string.IsNullOrEmpty(p.SubjectId) || a.SubjectId == p.SubjectId) &&
                (!p.IsActive.HasValue || a.IsActive == p.IsActive.Value);

            var repo = UnitOfWork.Repository<TeacherSubjectAssignment, string>();
            var total = await repo.CountAsync(new CriteriaSpecification<TeacherSubjectAssignment, string>(criteria));
            var items = await repo.GetAllWithSpecAsync(
                new PagedSpecification<TeacherSubjectAssignment, string>(criteria, a => a.ActiveFrom, p.PageNumber, p.PageSize, descending: true));

            var dto = items.Select(Map).ToList();
            return new PaginationResponse<IEnumerable<TeacherSubjectAssignmentDto>>(dto, total, p.PageNumber, p.PageSize)
            { Success = true, StatusCode = 200, Message = "Teacher assignments retrieved successfully." };
        }

        public async Task<ApiResponse<TeacherSubjectAssignmentDto>> AssignAsync(AddTeacherSubjectAssignmentDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            if (string.IsNullOrWhiteSpace(dto.TeacherId)) throw new BadRequestException("TeacherId is required.");
            if (string.IsNullOrWhiteSpace(dto.SubjectId)) throw new BadRequestException("SubjectId is required.");

            // Same-tenant subject integrity (global filter scopes to caller's tenant).
            _ = await UnitOfWork.Repository<Subject, string>()
                .GetByIdWithSpecAsync(new CriteriaSpecification<Subject, string>(s => s.Id == dto.SubjectId))
                ?? throw new NotFoundException("Subject not found.");

            // Same-tenant teacher integrity (explicit — ApplicationUser has no tenant filter).
            var teacher = await _users.Users.FirstOrDefaultAsync(u => u.Id == dto.TeacherId, ct);
            if (teacher is null || teacher.TenantId != tenantId || teacher.IsDeleted || teacher is not Teacher)
                throw new NotFoundException("Teacher not found.");

            var duplicate = await UnitOfWork.Repository<TeacherSubjectAssignment, string>()
                .CountAsync(new CriteriaSpecification<TeacherSubjectAssignment, string>(a =>
                    a.TeacherId == dto.TeacherId && a.SubjectId == dto.SubjectId && a.IsActive));
            if (duplicate > 0)
                throw new ConflictException("Teacher is already actively assigned to this subject.");

            var entity = new TeacherSubjectAssignment
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                TeacherId = dto.TeacherId,
                SubjectId = dto.SubjectId,
                AcademicYearId = dto.AcademicYearId,
                IsActive = true,
                ActiveFrom = DateTime.UtcNow
            };

            await UnitOfWork.Repository<TeacherSubjectAssignment, string>().AddAsync(entity);
            await Audit.StageAsync(AuditActionType.Create, nameof(TeacherSubjectAssignment), entity.Id, ct: ct);
            await UnitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("TeacherSubjectAssignment {Id} created (teacher {Teacher}, subject {Subject})", entity.Id, dto.TeacherId, dto.SubjectId);
            return Ok(Map(entity), 201, "Teacher assigned to subject successfully.");
        }

        public async Task<ApiResponse<bool>> DeactivateAsync(string id, CancellationToken ct = default)
        {
            RequireTenant();
            var entity = await UnitOfWork.Repository<TeacherSubjectAssignment, string>()
                .GetByIdWithSpecAsync(new CriteriaSpecification<TeacherSubjectAssignment, string>(a => a.Id == id))
                ?? throw new NotFoundException("Teacher assignment not found.");

            if (!entity.IsActive)
                throw new ConflictException("Assignment is already inactive.");

            entity.IsActive = false;
            entity.ActiveTo = DateTime.UtcNow;
            await Audit.StageAsync(AuditActionType.Update, nameof(TeacherSubjectAssignment), entity.Id, ct: ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return new ApiResponse<bool>(true) { Success = true, StatusCode = 200, Message = "Teacher assignment deactivated successfully." };
        }

        private static ApiResponse<TeacherSubjectAssignmentDto> Ok(TeacherSubjectAssignmentDto data, int status, string message) =>
            new(data) { Success = true, StatusCode = status, Message = message };

        private static TeacherSubjectAssignmentDto Map(TeacherSubjectAssignment a) => new()
        {
            Id = a.Id, TeacherId = a.TeacherId, SubjectId = a.SubjectId, AcademicYearId = a.AcademicYearId,
            IsActive = a.IsActive, ActiveFrom = a.ActiveFrom, ActiveTo = a.ActiveTo
        };
    }
}
