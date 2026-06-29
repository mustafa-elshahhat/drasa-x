using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.CommunicationDto;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Audit;
using DerasaX.Application.Services.Abstractions.Communication;
using DerasaX.Domain.Common;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Specification.Common;
using Microsoft.AspNetCore.Identity;

namespace DerasaX.Application.Services.Communication
{
    public class ConversationService : CommunicationServiceBase, IConversationService
    {
        public ConversationService(IUnitOfWork unitOfWork, ITenantContext tenant, IAuditWriter audit,
            UserManager<ApplicationUser> users) : base(unitOfWork, tenant, audit, users) { }

        public async Task<ApiResponse<ConversationDto>> StartAsync(StartConversationDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var caller = RequireUser();
            if (string.IsNullOrWhiteSpace(dto.ParticipantUserId))
                throw new BadRequestException("ParticipantUserId is required.");
            if (dto.ParticipantUserId == caller)
                throw new BadRequestException("You cannot start a conversation with yourself.");

            ConversationType type;
            ConversationParticipantRole callerRole, otherRole;

            if (IsParent)
            {
                // Parent → teacher of their linked child's current class.
                var teacher = await RequireTenantUserAsync(dto.ParticipantUserId, Roles.Teacher, ct);
                if (!await ParentTeacherLinkedAsync(caller, teacher.Id, dto.StudentId))
                    throw new ForbiddenException("You may only contact teachers of your linked child's current classes.");
                type = ConversationType.ParentTeacher;
                callerRole = ConversationParticipantRole.Parent;
                otherRole = ConversationParticipantRole.Teacher;
            }
            else if (IsStudent)
            {
                // Student → a teacher assigned to a class the student is enrolled in.
                var teacher = await RequireTenantUserAsync(dto.ParticipantUserId, Roles.Teacher, ct);
                if (!await TeacherStudentLinkedAsync(teacher.Id, caller))
                    throw new ForbiddenException("You may only contact teachers of your current classes.");
                type = ConversationType.StudentTeacher;
                callerRole = ConversationParticipantRole.Student;
                otherRole = ConversationParticipantRole.Teacher;
            }
            else if (IsTeacher)
            {
                // Teacher → either a parent (parent↔teacher) or an assigned student (student↔teacher),
                // resolved from the participant's actual role. Cross-tenant ids already 404'd above.
                var (participant, isParent) = await LoadTenantUserWithRoleAsync(dto.ParticipantUserId, Roles.Parent, ct);
                if (isParent)
                {
                    if (!await ParentTeacherLinkedAsync(participant.Id, caller, dto.StudentId))
                        throw new ForbiddenException("You may only contact parents of students in your assigned classes.");
                    type = ConversationType.ParentTeacher;
                    callerRole = ConversationParticipantRole.Teacher;
                    otherRole = ConversationParticipantRole.Parent;
                }
                else
                {
                    var student = await RequireTenantUserAsync(dto.ParticipantUserId, Roles.Student, ct);
                    if (!await TeacherStudentLinkedAsync(caller, student.Id))
                        throw new ForbiddenException("You may only contact students in your assigned classes.");
                    type = ConversationType.StudentTeacher;
                    callerRole = ConversationParticipantRole.Teacher;
                    otherRole = ConversationParticipantRole.Student;
                }
            }
            else
            {
                throw new ForbiddenException("Only a parent, student or teacher may start this conversation.");
            }

            var conversation = new Conversation
            {
                Id = Guid.NewGuid().ToString(),
                TenantId = tenantId,
                Type = type,
                Subject = dto.Subject,
                StartedAt = DateTime.UtcNow
            };
            await UnitOfWork.Repository<Conversation, string>().AddAsync(conversation);
            await AddParticipant(tenantId, conversation.Id, caller, callerRole);
            await AddParticipant(tenantId, conversation.Id, dto.ParticipantUserId, otherRole);

            if (!string.IsNullOrWhiteSpace(dto.FirstMessage))
            {
                await AddMessage(tenantId, conversation.Id, caller, dto.FirstMessage!);
                await StageNotificationAsync(tenantId, dto.ParticipantUserId, "New message",
                    "You have a new message.", NotificationCategory.General);
            }

            await Audit.StageAsync(AuditActionType.Create, nameof(Conversation), conversation.Id, ct: ct);
            await UnitOfWork.SaveChangesAsync(ct);

            var participants = await LoadParticipants(conversation.Id);
            return Ok(Map(conversation, participants), 201, "Conversation started.");
        }

        public async Task<ApiResponse<IEnumerable<ConversationDto>>> ListAsync(CancellationToken ct = default)
        {
            RequireTenant();
            var caller = RequireUser();
            var myParts = await UnitOfWork.Repository<ConversationParticipant, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<ConversationParticipant, string>(p => p.UserId == caller));
            var convIds = myParts.Select(p => p.ConversationId).Distinct().ToList();
            if (convIds.Count == 0) return Ok<IEnumerable<ConversationDto>>(new List<ConversationDto>(), 200, "Conversations retrieved.");

            var convs = await UnitOfWork.Repository<Conversation, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<Conversation, string>(c => convIds.Contains(c.Id)));
            var allParts = await UnitOfWork.Repository<ConversationParticipant, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<ConversationParticipant, string>(p => convIds.Contains(p.ConversationId)));
            var dto = convs.Select(c => Map(c, allParts.Where(p => p.ConversationId == c.Id))).ToList();
            return Ok<IEnumerable<ConversationDto>>(dto, 200, "Conversations retrieved.");
        }

        public async Task<ApiResponse<ConversationDto>> GetAsync(string conversationId, CancellationToken ct = default)
        {
            var conv = await RequireParticipantConversationAsync(conversationId);
            var participants = await LoadParticipants(conv.Id);
            return Ok(Map(conv, participants), 200, "Conversation retrieved.");
        }

        public async Task<ApiResponse<IEnumerable<ParticipantDto>>> ParticipantsAsync(string conversationId, CancellationToken ct = default)
        {
            await RequireParticipantConversationAsync(conversationId);
            var participants = await LoadParticipants(conversationId);
            return Ok<IEnumerable<ParticipantDto>>(participants.Select(MapParticipant).ToList(), 200, "Participants retrieved.");
        }

        public async Task<ApiResponse<MessageDto>> PostMessageAsync(string conversationId, PostMessageDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var caller = RequireUser();
            var conv = await RequireParticipantConversationAsync(conversationId);
            if (conv.IsClosed) throw new ConflictException("This conversation is closed.");
            if (string.IsNullOrWhiteSpace(dto.Body)) throw new BadRequestException("Message body is required.");

            var message = await AddMessage(tenantId, conversationId, caller, dto.Body);

            var others = (await LoadParticipants(conversationId)).Where(p => p.UserId != caller).ToList();
            foreach (var o in others)
                await StageNotificationAsync(tenantId, o.UserId, "New message", "You have a new message.", NotificationCategory.General);

            await Audit.StageAsync(AuditActionType.Create, nameof(Message), message.Id, ct: ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(MapMessage(message), 201, "Message sent.");
        }

        public async Task<PaginationResponse<IEnumerable<MessageDto>>> ListMessagesAsync(string conversationId, MessageParameters p, CancellationToken ct = default)
        {
            await RequireParticipantConversationAsync(conversationId);
            var repo = UnitOfWork.Repository<Message, string>();
            System.Linq.Expressions.Expression<Func<Message, bool>> criteria = m => m.ConversationId == conversationId;
            var total = await repo.CountAsync(new CriteriaSpecification<Message, string>(criteria));
            var items = await repo.GetAllWithSpecAsync(
                new PagedSpecification<Message, string>(criteria, m => m.SentAt, p.PageNumber, p.PageSize, descending: true));
            var dto = items.Select(MapMessage).ToList();
            return new PaginationResponse<IEnumerable<MessageDto>>(dto, total, p.PageNumber, p.PageSize)
            { Success = true, StatusCode = 200, Message = "Messages retrieved." };
        }

        public async Task<ApiResponse<bool>> MarkReadAsync(string conversationId, string messageId, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var caller = RequireUser();
            await RequireParticipantConversationAsync(conversationId);

            var message = await UnitOfWork.Repository<Message, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<Message, string>(m => m.Id == messageId && m.ConversationId == conversationId))
                ?? throw new NotFoundException("Message not found.");

            var existing = await UnitOfWork.Repository<MessageReadReceipt, string>().CountAsync(
                new CriteriaSpecification<MessageReadReceipt, string>(r => r.MessageId == message.Id && r.UserId == caller));
            if (existing == 0)
            {
                await UnitOfWork.Repository<MessageReadReceipt, string>().AddAsync(new MessageReadReceipt
                {
                    Id = Guid.NewGuid().ToString(), TenantId = tenantId, MessageId = message.Id, UserId = caller, ReadAt = DateTime.UtcNow
                });
                await UnitOfWork.SaveChangesAsync(ct);
            }
            return Ok(true, 200, "Message marked as read.");
        }

        // ---- helpers ----

        private async Task<Conversation> RequireParticipantConversationAsync(string conversationId)
        {
            RequireTenant();
            var caller = RequireUser();
            var conv = await UnitOfWork.Repository<Conversation, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<Conversation, string>(c => c.Id == conversationId))
                ?? throw new NotFoundException("Conversation not found.");
            var isParticipant = await UnitOfWork.Repository<ConversationParticipant, string>().CountAsync(
                new CriteriaSpecification<ConversationParticipant, string>(pp => pp.ConversationId == conversationId && pp.UserId == caller));
            // Not a participant → 404 (do not leak that the conversation exists).
            if (isParticipant == 0) throw new NotFoundException("Conversation not found.");
            return conv;
        }

        private Task AddParticipant(string tenantId, string conversationId, string userId, ConversationParticipantRole role) =>
            UnitOfWork.Repository<ConversationParticipant, string>().AddAsync(new ConversationParticipant
            {
                Id = Guid.NewGuid().ToString(), TenantId = tenantId, ConversationId = conversationId,
                UserId = userId, Role = role, JoinedAt = DateTime.UtcNow
            });

        private async Task<Message> AddMessage(string tenantId, string conversationId, string senderId, string body)
        {
            var message = new Message
            {
                Id = Guid.NewGuid().ToString(), TenantId = tenantId, ConversationId = conversationId,
                SenderId = senderId, Body = body, Type = MessageType.Text, SentAt = DateTime.UtcNow
            };
            await UnitOfWork.Repository<Message, string>().AddAsync(message);
            return message;
        }

        private async Task<List<ConversationParticipant>> LoadParticipants(string conversationId) =>
            (await UnitOfWork.Repository<ConversationParticipant, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<ConversationParticipant, string>(p => p.ConversationId == conversationId))).ToList();

        private static ConversationDto Map(Conversation c, IEnumerable<ConversationParticipant> parts) => new()
        {
            Id = c.Id, Type = c.Type, Subject = c.Subject, IsClosed = c.IsClosed, StartedAt = c.StartedAt,
            Participants = parts.Select(MapParticipant).ToList()
        };

        private static ParticipantDto MapParticipant(ConversationParticipant p) => new()
        {
            UserId = p.UserId, Role = p.Role, JoinedAt = p.JoinedAt
        };

        private static MessageDto MapMessage(Message m) => new()
        {
            Id = m.Id, ConversationId = m.ConversationId, SenderId = m.SenderId, Body = m.Body, Type = m.Type, SentAt = m.SentAt
        };
    }
}
