using System.Threading.Tasks;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Infrastructure.DbHelper.Context;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DerasaX.Tests;

/// <summary>Phase 4 §6.6 — communication domain/database integrity.</summary>
public class CommunicationDomainTests : IClassFixture<IntegrationFactory>
{
    private readonly IntegrationFactory _factory;
    public CommunicationDomainTests(IntegrationFactory factory) => _factory = factory;

    private static async Task<string> UserId(DerasaXDbContext db, string loginCode) =>
        (await db.applicationUsers.IgnoreQueryFilters().FirstAsync(u => u.LoginCode == loginCode)).Id;

    [Fact]
    public async Task Conversation_message_attachment_and_read_receipt_succeed_same_tenant()
    {
        await using var setup = Phase4Db.Platform(_factory);
        var parent = await UserId(setup, "PARENT-T1");
        var teacher = await UserId(setup, "TEACH-T1");
        var convId = Phase4Db.NewId("conv");
        var p1 = Phase4Db.NewId("part");
        var p2 = Phase4Db.NewId("part");
        var msgId = Phase4Db.NewId("msg");
        var attId = Phase4Db.NewId("att");
        var readId = Phase4Db.NewId("read");

        try
        {
            await using var db = Phase4Db.AsTenant(_factory, "tenant-1");
            db.conversations.Add(new Conversation { Id = convId, TenantId = "tenant-1", Type = ConversationType.ParentTeacher, Subject = "Progress" });
            db.conversationParticipants.Add(new ConversationParticipant { Id = p1, TenantId = "tenant-1", ConversationId = convId, UserId = parent, Role = ConversationParticipantRole.Parent });
            db.conversationParticipants.Add(new ConversationParticipant { Id = p2, TenantId = "tenant-1", ConversationId = convId, UserId = teacher, Role = ConversationParticipantRole.Teacher });
            db.messages.Add(new Message { Id = msgId, TenantId = "tenant-1", ConversationId = convId, SenderId = parent, Body = "Can we discuss progress?" });
            db.messageAttachments.Add(new MessageAttachment { Id = attId, TenantId = "tenant-1", MessageId = msgId, FileName = "note.pdf", Url = "https://files.local/note.pdf", Type = AttachmentType.Document });
            db.messageReadReceipts.Add(new MessageReadReceipt { Id = readId, TenantId = "tenant-1", MessageId = msgId, UserId = teacher });
            await db.SaveChangesAsync();

            Assert.True(await db.messages.AnyAsync(x => x.ConversationId == convId));
            Assert.True(await db.messageReadReceipts.AnyAsync(x => x.MessageId == msgId));
        }
        finally
        {
            await CleanupAsync("messageReadReceipts", readId);
            await CleanupAsync("messageAttachments", attId);
            await CleanupAsync("messages", msgId);
            await CleanupAsync("conversationParticipants", p1);
            await CleanupAsync("conversationParticipants", p2);
            await CleanupAsync("conversations", convId);
        }
    }

    [Fact]
    public async Task Parent_request_rejects_cross_tenant_student_and_accepts_response_and_suggestion()
    {
        await using var setup = Phase4Db.Platform(_factory);
        var parentT1 = await UserId(setup, "PARENT-T1");
        var studentT1 = await UserId(setup, "STU-T1");
        var studentT2 = await UserId(setup, "STU-T2");
        var teacherT1 = await UserId(setup, "TEACH-T1");
        var requestId = Phase4Db.NewId("preq");
        var responseId = Phase4Db.NewId("pres");
        var suggestionId = Phase4Db.NewId("sug");

        try
        {
            await using (var bad = Phase4Db.AsTenant(_factory, "tenant-1"))
            {
                bad.parentRequests.Add(new ParentRequest { Id = Phase4Db.NewId("preq"), TenantId = "tenant-1", ParentId = parentT1, StudentId = studentT2, Type = ParentRequestType.ProgressFollowUp, Title = "x", Body = "x" });
                await Assert.ThrowsAnyAsync<DbUpdateException>(() => bad.SaveChangesAsync());
            }

            await using var db = Phase4Db.AsTenant(_factory, "tenant-1");
            db.parentRequests.Add(new ParentRequest { Id = requestId, TenantId = "tenant-1", ParentId = parentT1, StudentId = studentT1, Type = ParentRequestType.ProgressFollowUp, Title = "Progress", Body = "Please review." });
            db.parentRequestResponses.Add(new ParentRequestResponse { Id = responseId, TenantId = "tenant-1", ParentRequestId = requestId, ResponderId = teacherT1, Body = "Scheduled." });
            db.suggestions.Add(new Suggestion { Id = suggestionId, TenantId = "tenant-1", SubmittedByUserId = studentT1, Title = "More quizzes", Body = "Add more practice quizzes." });
            await db.SaveChangesAsync();

            Assert.True(await db.parentRequestResponses.AnyAsync(x => x.ParentRequestId == requestId));
            Assert.True(await db.suggestions.AnyAsync(x => x.Id == suggestionId));
        }
        finally
        {
            await CleanupAsync("suggestions", suggestionId);
            await CleanupAsync("parentRequestResponses", responseId);
            await CleanupAsync("parentRequests", requestId);
        }
    }

    private async Task CleanupAsync(string set, string id)
    {
        await using var db = Phase4Db.Platform(_factory);
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"" + set + "\" WHERE \"Id\" = {0}", id);
    }
}
