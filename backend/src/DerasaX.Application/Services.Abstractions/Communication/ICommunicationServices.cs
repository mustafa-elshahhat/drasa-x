using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Dto.CommunicationDto;
using DerasaX.Domain.Common;

namespace DerasaX.Application.Services.Abstractions.Communication
{
    public interface IConversationService
    {
        Task<ApiResponse<ConversationDto>> StartAsync(StartConversationDto dto, CancellationToken ct = default);
        Task<ApiResponse<IEnumerable<ConversationDto>>> ListAsync(CancellationToken ct = default);
        Task<ApiResponse<ConversationDto>> GetAsync(string conversationId, CancellationToken ct = default);
        Task<ApiResponse<IEnumerable<ParticipantDto>>> ParticipantsAsync(string conversationId, CancellationToken ct = default);
        Task<ApiResponse<MessageDto>> PostMessageAsync(string conversationId, PostMessageDto dto, CancellationToken ct = default);
        Task<PaginationResponse<IEnumerable<MessageDto>>> ListMessagesAsync(string conversationId, MessageParameters p, CancellationToken ct = default);
        Task<ApiResponse<bool>> MarkReadAsync(string conversationId, string messageId, CancellationToken ct = default);
    }

    public interface IParentRequestService
    {
        Task<ApiResponse<ParentRequestDto>> CreateAsync(CreateParentRequestDto dto, CancellationToken ct = default);
        Task<PaginationResponse<IEnumerable<ParentRequestDto>>> ListAsync(ParentRequestParameters p, CancellationToken ct = default);
        Task<ApiResponse<ParentRequestDto>> GetAsync(string id, CancellationToken ct = default);
        Task<ApiResponse<ParentRequestResponseDto>> RespondAsync(string id, RespondParentRequestDto dto, CancellationToken ct = default);
        Task<ApiResponse<ParentRequestDto>> TransitionAsync(string id, TransitionParentRequestDto dto, CancellationToken ct = default);

        // ---- Phase 16: sensitive document attachments (linked-parent / staff only) ----
        /// <summary>Links a parent-uploaded document to the request (request owner only).</summary>
        Task<ApiResponse<bool>> AttachRequestDocumentAsync(string id, string fileRecordId, CancellationToken ct = default);
        /// <summary>Adds a staff response carrying a document (SchoolAdmin only).</summary>
        Task<ApiResponse<ParentRequestResponseDto>> AttachResponseDocumentAsync(string id, string fileRecordId, string? body, CancellationToken ct = default);
        /// <summary>Returns the request's attached document id after authorizing the caller (owner/staff).</summary>
        Task<string> GetAuthorizedRequestDocumentIdAsync(string id, CancellationToken ct = default);
        /// <summary>Returns a response's document id after authorizing the caller (owner/staff).</summary>
        Task<string> GetAuthorizedResponseDocumentIdAsync(string id, string responseId, CancellationToken ct = default);
    }

    public interface IAnnouncementService
    {
        Task<ApiResponse<AnnouncementDto>> CreateAsync(CreateAnnouncementDto dto, CancellationToken ct = default);
        Task<PaginationResponse<IEnumerable<AnnouncementDto>>> ListAsync(AnnouncementParameters p, CancellationToken ct = default);
        Task<ApiResponse<AnnouncementDto>> PublishAsync(string id, bool publish, CancellationToken ct = default);
    }

    public interface ISuggestionService
    {
        Task<ApiResponse<SuggestionDto>> SubmitAsync(SubmitSuggestionDto dto, CancellationToken ct = default);
        Task<PaginationResponse<IEnumerable<SuggestionDto>>> ListAsync(SuggestionParameters p, CancellationToken ct = default);
        Task<ApiResponse<SuggestionDto>> ModerateAsync(string id, ModerateSuggestionDto dto, CancellationToken ct = default);
    }
}
