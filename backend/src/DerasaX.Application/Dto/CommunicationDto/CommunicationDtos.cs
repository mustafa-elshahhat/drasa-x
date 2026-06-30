using System;
using System.Collections.Generic;
using DerasaX.Application.Common;
using DerasaX.Domain.Enums;

namespace DerasaX.Application.Dto.CommunicationDto
{
    // ---- Conversations & messages ----

    public class StartConversationDto
    {
        /// <summary>The other participant (a teacher when the caller is a parent, and vice versa).</summary>
        public string ParticipantUserId { get; set; } = string.Empty;
        /// <summary>The linked child the contact concerns (required for parent↔teacher).</summary>
        public string? StudentId { get; set; }
        public string? Subject { get; set; }
        public string? FirstMessage { get; set; }
    }

    public class ConversationDto
    {
        public string Id { get; set; } = string.Empty;
        public ConversationType Type { get; set; }
        public string? Subject { get; set; }
        public bool IsClosed { get; set; }
        public DateTime StartedAt { get; set; }
        public List<ParticipantDto> Participants { get; set; } = new();
        /// <summary>Messages addressed to the caller that the caller has not yet read.</summary>
        public int UnreadCount { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public string? LastMessagePreview { get; set; }
    }

    public class ParticipantDto
    {
        public string UserId { get; set; } = string.Empty;
        public ConversationParticipantRole Role { get; set; }
        public DateTime JoinedAt { get; set; }
    }

    public class PostMessageDto
    {
        public string Body { get; set; } = string.Empty;
    }

    public class MessageDto
    {
        public string Id { get; set; } = string.Empty;
        public string ConversationId { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public MessageType Type { get; set; }
        public DateTime SentAt { get; set; }
        /// <summary>For an incoming message: the caller has read it. For the caller's own message:
        /// at least one other participant has read it (drives the Sent/Read indicator).</summary>
        public bool IsRead { get; set; }
    }

    public class MessageParameters : PaginationParameters { }

    // ---- Parent requests ----

    public class CreateParentRequestDto
    {
        public string StudentId { get; set; } = string.Empty;
        public ParentRequestType Type { get; set; } = ParentRequestType.Document;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }

    public class ParentRequestDto
    {
        public string Id { get; set; } = string.Empty;
        public string ParentId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public ParentRequestType Type { get; set; }
        public ParentRequestStatus Status { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public List<ParentRequestResponseDto> Responses { get; set; } = new();
    }

    public class ParentRequestResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string ResponderId { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime RespondedAt { get; set; }
    }

    public class RespondParentRequestDto
    {
        public string Body { get; set; } = string.Empty;
    }

    public class TransitionParentRequestDto
    {
        public ParentRequestStatus Status { get; set; }
    }

    public class ParentRequestParameters : PaginationParameters
    {
        public ParentRequestStatus? Status { get; set; }
    }

    // ---- Announcements ----

    public class CreateAnnouncementDto
    {
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public TargetAudience TargetAudience { get; set; } = TargetAudience.All;
        public DateTime? ExpiresAt { get; set; }
    }

    public class AnnouncementDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public TargetAudience TargetAudience { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class AnnouncementParameters : PaginationParameters { }

    // ---- Suggestions (anonymous) ----

    public class SubmitSuggestionDto
    {
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }

    /// <summary>Suggestion as seen by school staff — author identity is NEVER included.</summary>
    public class SuggestionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public SuggestionStatus Status { get; set; }
        public DateTime SubmittedAt { get; set; }
        public string? ReviewNotes { get; set; }
    }

    public class ModerateSuggestionDto
    {
        public SuggestionStatus Status { get; set; }
        public string? ReviewNotes { get; set; }
    }

    public class SuggestionParameters : PaginationParameters
    {
        public SuggestionStatus? Status { get; set; }
    }
}
