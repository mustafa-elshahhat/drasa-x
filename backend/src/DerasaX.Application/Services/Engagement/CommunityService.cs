using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.EngagementDto;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Audit;
using DerasaX.Application.Services.Abstractions.Engagement;
using DerasaX.Domain.Common;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Specification.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DerasaX.Application.Services.Engagement
{
    public class CommunityService : EngagementServiceBase, ICommunityService
    {
        private readonly UserManager<ApplicationUser> _users;

        public CommunityService(IUnitOfWork unitOfWork, ITenantContext tenant, IAuditWriter audit,
            UserManager<ApplicationUser> users)
            : base(unitOfWork, tenant, audit) => _users = users;

        public async Task<PaginationResponse<IEnumerable<CommunityDto>>> ListAsync(CommunityParameters p, CancellationToken ct = default)
        {
            RequireTenant();
            Expression<Func<Community, bool>> criteria = c => true;
            var repo = UnitOfWork.Repository<Community, string>();
            var total = await repo.CountAsync(new CriteriaSpecification<Community, string>(criteria));
            var items = (await repo.GetAllWithSpecAsync(
                new PagedSpecification<Community, string>(criteria, c => c.CreatedAt, p.PageNumber, p.PageSize, descending: true))).ToList();
            var counts = await MemberCounts(items.Select(c => c.Id).ToList());
            var dto = items.Select(c => Map(c, counts.GetValueOrDefault(c.Id))).ToList();
            return new PaginationResponse<IEnumerable<CommunityDto>>(dto, total, p.PageNumber, p.PageSize)
            { Success = true, StatusCode = 200, Message = "Communities retrieved." };
        }

        public async Task<ApiResponse<CommunityDto>> GetAsync(string id, CancellationToken ct = default)
        {
            var community = await LoadAsync(id);
            var count = await UnitOfWork.Repository<CommunityMembership, string>().CountAsync(
                new CriteriaSpecification<CommunityMembership, string>(m => m.CommunityId == id));
            return Ok(Map(community, count), 200, "Community retrieved.");
        }

        public async Task<ApiResponse<CommunityDto>> CreateAsync(CreateCommunityDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var caller = RequireUser();
            if (!IsTeacher && !IsSchoolAdmin) throw new ForbiddenException("Only a teacher or school administrator may create a community.");
            if (string.IsNullOrWhiteSpace(dto.Name)) throw new BadRequestException("Name is required.");

            var community = new Community
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                Name = dto.Name,
                Description = dto.Description,
                Visibility = dto.Visibility,
                SchoolClassId = dto.SchoolClassId,
                EligibleGradeId = await ResolveEligibleGradeIdAsync(dto.EligibleGradeId, tenantId)
            };
            await UnitOfWork.Repository<Community, string>().AddAsync(community);
            await AddMembership(tenantId, community.Id, caller, CommunityMemberRole.Owner);
            await Audit.StageAsync(AuditActionType.Create, nameof(Community), community.Id, ct: ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(Map(community, 1), 201, "Community created.");
        }

        public async Task<ApiResponse<CommunityDto>> UpdateAsync(string id, UpdateCommunityDto dto, CancellationToken ct = default)
        {
            var community = await LoadAsync(id);
            await RequireManagerAsync(community.Id);
            if (string.IsNullOrWhiteSpace(dto.Name)) throw new BadRequestException("Name is required.");
            community.Name = dto.Name;
            community.Description = dto.Description;
            community.Visibility = dto.Visibility;
            community.EligibleGradeId = await ResolveEligibleGradeIdAsync(dto.EligibleGradeId, RequireTenant());
            UnitOfWork.Repository<Community, string>().Update(community);
            await Audit.StageAsync(AuditActionType.Update, nameof(Community), community.Id, ct: ct);
            await UnitOfWork.SaveChangesAsync(ct);
            var count = await UnitOfWork.Repository<CommunityMembership, string>().CountAsync(
                new CriteriaSpecification<CommunityMembership, string>(m => m.CommunityId == id));
            return Ok(Map(community, count), 200, "Community updated.");
        }

        public async Task<ApiResponse<bool>> ArchiveAsync(string id, CancellationToken ct = default)
        {
            var community = await LoadAsync(id);
            await RequireManagerAsync(community.Id, ownerOrAdminOnly: true);
            community.IsDeleted = true; // safe soft-delete archive
            UnitOfWork.Repository<Community, string>().Update(community);
            await Audit.StageAsync(AuditActionType.Delete, nameof(Community), community.Id, "{\"action\":\"archive\"}", ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(true, 200, "Community archived.");
        }

        public async Task<ApiResponse<IEnumerable<CommunityMemberDto>>> MembersAsync(string id, CancellationToken ct = default)
        {
            await LoadAsync(id);
            await RequireMemberAsync(id);
            var members = await LoadMemberships(id);
            return Ok<IEnumerable<CommunityMemberDto>>(members.Select(MapMember).ToList(), 200, "Members retrieved.");
        }

        public async Task<ApiResponse<bool>> JoinAsync(string id, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var caller = RequireUser();
            var community = await LoadAsync(id);
            if (community.Visibility == CommunityVisibility.ClassOnly)
                throw new ForbiddenException("This community is managed; ask a moderator to add you.");
            // Phase 14 (closure) — grade-eligibility gate. When a community is grade-restricted, only a
            // student whose grade matches may self-join. Staff/admins are unaffected (no academic grade).
            if (!string.IsNullOrWhiteSpace(community.EligibleGradeId) && IsStudent)
            {
                var gradeId = await StudentGradeIdAsync(caller, tenantId);
                if (!string.Equals(gradeId, community.EligibleGradeId, StringComparison.Ordinal))
                    throw new ForbiddenException("This community is restricted to a different grade.");
            }
            var existing = await MembershipOf(id, caller);
            if (existing is not null) throw new ConflictException("You are already a member.");
            await AddMembership(tenantId, id, caller, CommunityMemberRole.Member);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(true, 200, "Joined community.");
        }

        public async Task<ApiResponse<bool>> LeaveAsync(string id, CancellationToken ct = default)
        {
            RequireTenant();
            var caller = RequireUser();
            await LoadAsync(id);
            var membership = await MembershipOf(id, caller) ?? throw new ConflictException("You are not a member.");
            if (membership.Role == CommunityMemberRole.Owner)
                throw new ConflictException("An owner cannot leave the community; transfer ownership or archive it.");
            UnitOfWork.Repository<CommunityMembership, string>().Delete(membership);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(true, 200, "Left community.");
        }

        public async Task<ApiResponse<CommunityMemberDto>> AddMemberAsync(string id, AddMemberDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var community = await LoadAsync(id);
            await RequireManagerAsync(community.Id);
            if (string.IsNullOrWhiteSpace(dto.UserId)) throw new BadRequestException("UserId is required.");
            if (await MembershipOf(id, dto.UserId) is not null) throw new ConflictException("User is already a member.");
            var membership = await AddMembership(tenantId, id, dto.UserId, dto.Role);
            await Audit.StageAsync(AuditActionType.Create, nameof(CommunityMembership), membership.Id, ct: ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(MapMember(membership), 201, "Member added.");
        }

        public async Task<ApiResponse<PostDto>> CreatePostAsync(string id, CreatePostDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var caller = RequireUser();
            await LoadAsync(id);
            await RequireMemberAsync(id);
            if (string.IsNullOrWhiteSpace(dto.Content)) throw new BadRequestException("Content is required.");

            var post = new Post
            {
                Id = Guid.NewGuid().ToString(), TenantId = tenantId, CommunityId = id, UserId = caller,
                Content = dto.Content, PhotoUrl = dto.PhotoUrl, CreatedAt = DateTime.UtcNow
            };
            await UnitOfWork.Repository<Post, string>().AddAsync(post);
            await Audit.StageAsync(AuditActionType.Create, nameof(Post), post.Id, ct: ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(MapPost(post), 201, "Post created.");
        }

        public async Task<PaginationResponse<IEnumerable<PostDto>>> ListPostsAsync(string id, CommunityParameters p, CancellationToken ct = default)
        {
            await LoadAsync(id);
            await RequireMemberAsync(id);
            Expression<Func<Post, bool>> criteria = x => x.CommunityId == id;
            var repo = UnitOfWork.Repository<Post, string>();
            var total = await repo.CountAsync(new CriteriaSpecification<Post, string>(criteria));
            var items = await repo.GetAllWithSpecAsync(
                new PagedSpecification<Post, string>(criteria, x => x.CreatedAt, p.PageNumber, p.PageSize, descending: true));
            var dto = items.Select(MapPost).ToList();
            return new PaginationResponse<IEnumerable<PostDto>>(dto, total, p.PageNumber, p.PageSize)
            { Success = true, StatusCode = 200, Message = "Posts retrieved." };
        }

        public async Task<ApiResponse<bool>> DeletePostAsync(string postId, CancellationToken ct = default)
        {
            RequireTenant();
            var caller = RequireUser();
            var post = await UnitOfWork.Repository<Post, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<Post, string>(x => x.Id == postId)) ?? throw new NotFoundException("Post not found.");
            // Author may delete own post; a moderator/owner/admin may delete any post in the community.
            var canModerate = post.CommunityId != null && await IsManagerAsync(post.CommunityId);
            if (post.UserId != caller && !canModerate)
                throw new ForbiddenException("You may only delete your own post.");
            post.IsDeleted = true;
            UnitOfWork.Repository<Post, string>().Update(post);
            await Audit.StageAsync(AuditActionType.Delete, nameof(Post), post.Id, ct: ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(true, 200, "Post deleted.");
        }

        public async Task<ApiResponse<CommentDto>> CommentAsync(string postId, CreateCommentDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var caller = RequireUser();
            var post = await UnitOfWork.Repository<Post, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<Post, string>(x => x.Id == postId)) ?? throw new NotFoundException("Post not found.");
            if (post.CommunityId != null) await RequireMemberAsync(post.CommunityId);
            if (string.IsNullOrWhiteSpace(dto.Body)) throw new BadRequestException("Comment body is required.");

            var comment = new PostComment
            {
                Id = Guid.NewGuid().ToString(), TenantId = tenantId, PostId = postId, UserId = caller,
                Body = dto.Body, CreatedAtUtc = DateTime.UtcNow
            };
            await UnitOfWork.Repository<PostComment, string>().AddAsync(comment);
            post.CommentsCount += 1;
            UnitOfWork.Repository<Post, string>().Update(post);
            // Notify the post author of a new comment (not when commenting on your own post).
            if (post.UserId != caller)
                await StageNotificationAsync(tenantId, post.UserId, "New comment on your post",
                    "Someone commented on your community post.");
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(new CommentDto { Id = comment.Id, PostId = postId, UserId = caller, Body = comment.Body, CreatedAt = comment.CreatedAtUtc }, 201, "Comment added.");
        }

        public async Task<ApiResponse<bool>> DeleteCommentAsync(string commentId, CancellationToken ct = default)
        {
            RequireTenant();
            var caller = RequireUser();
            var comment = await UnitOfWork.Repository<PostComment, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<PostComment, string>(x => x.Id == commentId)) ?? throw new NotFoundException("Comment not found.");
            var post = await UnitOfWork.Repository<Post, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<Post, string>(x => x.Id == comment.PostId));
            var canModerate = post?.CommunityId != null && await IsManagerAsync(post.CommunityId);
            if (comment.UserId != caller && !canModerate)
                throw new ForbiddenException("You may only delete your own comment.");
            comment.IsDeleted = true;
            UnitOfWork.Repository<PostComment, string>().Update(comment);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(true, 200, "Comment deleted.");
        }

        public async Task<ApiResponse<bool>> ReportPostAsync(string postId, ReportPostDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var caller = RequireUser();
            var post = await UnitOfWork.Repository<Post, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<Post, string>(x => x.Id == postId)) ?? throw new NotFoundException("Post not found.");
            if (post.CommunityId != null) await RequireMemberAsync(post.CommunityId);
            if (string.IsNullOrWhiteSpace(dto.Reason)) throw new BadRequestException("Reason is required.");
            await UnitOfWork.Repository<PostReport, string>().AddAsync(new PostReport
            {
                Id = Guid.NewGuid().ToString(), TenantId = tenantId, PostId = postId,
                ReportedByUserId = caller, Reason = dto.Reason, Status = ReportStatus.Open
            });
            // Notify community moderators/owners that content was reported (bounded to managers).
            if (post.CommunityId != null)
            {
                var managers = (await LoadMemberships(post.CommunityId))
                    .Where(m => m.Role is CommunityMemberRole.Owner or CommunityMemberRole.Moderator)
                    .Select(m => m.UserId).Distinct();
                foreach (var managerId in managers)
                    await StageNotificationAsync(tenantId, managerId, "Post reported",
                        "A post in your community was reported and awaits moderation.", NotificationCategory.Warning);
            }
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(true, 201, "Post reported.");
        }

        public async Task<ApiResponse<bool>> ModeratePostAsync(string postId, ModeratePostDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var post = await UnitOfWork.Repository<Post, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<Post, string>(x => x.Id == postId)) ?? throw new NotFoundException("Post not found.");
            if (post.CommunityId == null || !await IsManagerAsync(post.CommunityId))
                throw new ForbiddenException("Only a community moderator/owner or school administrator may moderate posts.");

            var reports = await UnitOfWork.Repository<PostReport, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<PostReport, string>(r => r.PostId == postId && r.Status == ReportStatus.Open));
            foreach (var r in reports)
            {
                r.Status = dto.Status;
                UnitOfWork.Repository<PostReport, string>().Update(r);
            }
            if (dto.RemovePost)
            {
                post.IsDeleted = true;
                UnitOfWork.Repository<Post, string>().Update(post);
                // Notify the author their content was removed by moderation.
                await StageNotificationAsync(tenantId, post.UserId, "Your post was removed",
                    "A post you made in a community was removed by a moderator.", NotificationCategory.Warning);
            }
            await Audit.StageAsync(AuditActionType.Update, nameof(Post), post.Id, "{\"action\":\"moderate\"}", ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(true, 200, "Post moderated.");
        }

        // ---- helpers ----

        /// <summary>
        /// Validates a requested eligibility grade belongs to the tenant (or clears it when blank), so a
        /// community can never be gated on a foreign/non-existent grade.
        /// </summary>
        private async Task<string?> ResolveEligibleGradeIdAsync(string? gradeId, string tenantId)
        {
            if (string.IsNullOrWhiteSpace(gradeId)) return null;
            var trimmed = gradeId.Trim();
            var exists = await UnitOfWork.Repository<Grade, string>().CountAsync(
                new CriteriaSpecification<Grade, string>(g => g.Id == trimmed && g.TenantId == tenantId));
            if (exists == 0) throw new BadRequestException("The selected eligibility grade does not exist.");
            return trimmed;
        }

        private async Task<string?> StudentGradeIdAsync(string studentId, string tenantId) =>
            await _users.Users.OfType<Student>()
                .Where(s => s.Id == studentId && s.TenantId == tenantId && !s.IsDeleted)
                .Select(s => s.GradeId)
                .FirstOrDefaultAsync();

        private async Task<Community> LoadAsync(string id) =>
            await UnitOfWork.Repository<Community, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<Community, string>(c => c.Id == id)) ?? throw new NotFoundException("Community not found.");

        private async Task<CommunityMembership?> MembershipOf(string communityId, string userId) =>
            (await UnitOfWork.Repository<CommunityMembership, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<CommunityMembership, string>(m => m.CommunityId == communityId && m.UserId == userId))).FirstOrDefault();

        private async Task RequireMemberAsync(string communityId)
        {
            if (IsSchoolAdmin) return;
            if (await MembershipOf(communityId, RequireUser()) is null)
                throw new ForbiddenException("You must be a member of this community.");
        }

        private async Task<bool> IsManagerAsync(string communityId)
        {
            if (IsSchoolAdmin) return true;
            var m = await MembershipOf(communityId, RequireUser());
            return m is not null && (m.Role == CommunityMemberRole.Owner || m.Role == CommunityMemberRole.Moderator);
        }

        private async Task RequireManagerAsync(string communityId, bool ownerOrAdminOnly = false)
        {
            if (IsSchoolAdmin) return;
            var m = await MembershipOf(communityId, RequireUser());
            var ok = ownerOrAdminOnly
                ? m is { Role: CommunityMemberRole.Owner }
                : m is { Role: CommunityMemberRole.Owner } or { Role: CommunityMemberRole.Moderator };
            if (!ok) throw new ForbiddenException("You do not have permission to manage this community.");
        }

        private async Task<CommunityMembership> AddMembership(string tenantId, string communityId, string userId, CommunityMemberRole role)
        {
            var membership = new CommunityMembership
            {
                Id = Guid.NewGuid().ToString(), TenantId = tenantId, CommunityId = communityId,
                UserId = userId, Role = role, JoinedAt = DateTime.UtcNow
            };
            await UnitOfWork.Repository<CommunityMembership, string>().AddAsync(membership);
            return membership;
        }

        private async Task<List<CommunityMembership>> LoadMemberships(string communityId) =>
            (await UnitOfWork.Repository<CommunityMembership, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<CommunityMembership, string>(m => m.CommunityId == communityId))).ToList();

        private async Task<Dictionary<string, int>> MemberCounts(List<string> communityIds)
        {
            if (communityIds.Count == 0) return new();
            var memberships = await UnitOfWork.Repository<CommunityMembership, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<CommunityMembership, string>(m => communityIds.Contains(m.CommunityId)));
            return memberships.GroupBy(m => m.CommunityId).ToDictionary(g => g.Key, g => g.Count());
        }

        private static CommunityDto Map(Community c, int memberCount) => new()
        {
            Id = c.Id, Name = c.Name, Description = c.Description, Visibility = c.Visibility,
            SchoolClassId = c.SchoolClassId, EligibleGradeId = c.EligibleGradeId, MemberCount = memberCount
        };

        private static CommunityMemberDto MapMember(CommunityMembership m) => new()
        {
            UserId = m.UserId, Role = m.Role, JoinedAt = m.JoinedAt
        };

        private static PostDto MapPost(Post p) => new()
        {
            Id = p.Id, CommunityId = p.CommunityId ?? string.Empty, UserId = p.UserId, Content = p.Content,
            PhotoUrl = p.PhotoUrl, CommentsCount = p.CommentsCount, CreatedAt = p.CreatedAt
        };
    }
}
