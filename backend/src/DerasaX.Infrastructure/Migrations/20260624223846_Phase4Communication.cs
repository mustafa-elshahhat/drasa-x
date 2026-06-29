using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DerasaX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase4Communication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "conversations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsClosed = table.Column<bool>(type: "boolean", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversations", x => x.Id);
                    table.UniqueConstraint("AK_conversations_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_conversations_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "parentRequests",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ParentId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    StudentId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Body = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parentRequests", x => x.Id);
                    table.UniqueConstraint("AK_parentRequests_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_parentRequests_Parent_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Parent",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_parentRequests_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_parentRequests_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "suggestions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    SubmittedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Body = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewNotes = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_suggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_suggestions_AspNetUsers_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_suggestions_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "conversationParticipants",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ConversationId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LeftAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversationParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_conversationParticipants_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_conversationParticipants_conversations_TenantId_Conversatio~",
                        columns: x => new { x.TenantId, x.ConversationId },
                        principalTable: "conversations",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_conversationParticipants_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "messages",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ConversationId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    SenderId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EditedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_messages", x => x.Id);
                    table.UniqueConstraint("AK_messages_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_messages_AspNetUsers_SenderId",
                        column: x => x.SenderId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_messages_conversations_TenantId_ConversationId",
                        columns: x => new { x.TenantId, x.ConversationId },
                        principalTable: "conversations",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_messages_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "parentRequestResponses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ParentRequestId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ResponderId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Body = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parentRequestResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_parentRequestResponses_AspNetUsers_ResponderId",
                        column: x => x.ResponderId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_parentRequestResponses_parentRequests_TenantId_ParentReques~",
                        columns: x => new { x.TenantId, x.ParentRequestId },
                        principalTable: "parentRequests",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_parentRequestResponses_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "messageAttachments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    MessageId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    FileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_messageAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_messageAttachments_messages_TenantId_MessageId",
                        columns: x => new { x.TenantId, x.MessageId },
                        principalTable: "messages",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_messageAttachments_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "messageReadReceipts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    MessageId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_messageReadReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_messageReadReceipts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_messageReadReceipts_messages_TenantId_MessageId",
                        columns: x => new { x.TenantId, x.MessageId },
                        principalTable: "messages",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_messageReadReceipts_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_conversationParticipants_TenantId",
                table: "conversationParticipants",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_conversationParticipants_TenantId_ConversationId_UserId",
                table: "conversationParticipants",
                columns: new[] { "TenantId", "ConversationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_conversationParticipants_UserId",
                table: "conversationParticipants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_conversations_TenantId",
                table: "conversations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_conversations_TenantId_Type_IsClosed",
                table: "conversations",
                columns: new[] { "TenantId", "Type", "IsClosed" });

            migrationBuilder.CreateIndex(
                name: "IX_messageAttachments_TenantId",
                table: "messageAttachments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_messageAttachments_TenantId_MessageId",
                table: "messageAttachments",
                columns: new[] { "TenantId", "MessageId" });

            migrationBuilder.CreateIndex(
                name: "IX_messageReadReceipts_TenantId",
                table: "messageReadReceipts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_messageReadReceipts_TenantId_MessageId_UserId",
                table: "messageReadReceipts",
                columns: new[] { "TenantId", "MessageId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_messageReadReceipts_UserId",
                table: "messageReadReceipts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_messages_SenderId",
                table: "messages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_messages_TenantId",
                table: "messages",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_messages_TenantId_ConversationId_SentAt",
                table: "messages",
                columns: new[] { "TenantId", "ConversationId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_parentRequestResponses_ResponderId",
                table: "parentRequestResponses",
                column: "ResponderId");

            migrationBuilder.CreateIndex(
                name: "IX_parentRequestResponses_TenantId",
                table: "parentRequestResponses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_parentRequestResponses_TenantId_ParentRequestId",
                table: "parentRequestResponses",
                columns: new[] { "TenantId", "ParentRequestId" });

            migrationBuilder.CreateIndex(
                name: "IX_parentRequests_ParentId",
                table: "parentRequests",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_parentRequests_StudentId",
                table: "parentRequests",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_parentRequests_TenantId",
                table: "parentRequests",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_parentRequests_TenantId_StudentId_Status",
                table: "parentRequests",
                columns: new[] { "TenantId", "StudentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_suggestions_SubmittedByUserId",
                table: "suggestions",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_suggestions_TenantId",
                table: "suggestions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_suggestions_TenantId_Status_SubmittedAt",
                table: "suggestions",
                columns: new[] { "TenantId", "Status", "SubmittedAt" });

            migrationBuilder.Sql(@"
CREATE TRIGGER trg_conversation_participant_user_tenant
    BEFORE INSERT OR UPDATE ON ""conversationParticipants""
    FOR EACH ROW EXECUTE FUNCTION derasax_assert_user_tenant('UserId');

CREATE TRIGGER trg_message_sender_tenant
    BEFORE INSERT OR UPDATE ON ""messages""
    FOR EACH ROW EXECUTE FUNCTION derasax_assert_user_tenant('SenderId');

CREATE TRIGGER trg_message_receipt_user_tenant
    BEFORE INSERT OR UPDATE ON ""messageReadReceipts""
    FOR EACH ROW EXECUTE FUNCTION derasax_assert_user_tenant('UserId');

CREATE TRIGGER trg_parent_request_parent_tenant
    BEFORE INSERT OR UPDATE ON ""parentRequests""
    FOR EACH ROW EXECUTE FUNCTION derasax_assert_user_tenant('ParentId');

CREATE TRIGGER trg_parent_request_student_tenant
    BEFORE INSERT OR UPDATE ON ""parentRequests""
    FOR EACH ROW EXECUTE FUNCTION derasax_assert_user_tenant('StudentId');

CREATE TRIGGER trg_parent_request_response_responder_tenant
    BEFORE INSERT OR UPDATE ON ""parentRequestResponses""
    FOR EACH ROW EXECUTE FUNCTION derasax_assert_user_tenant('ResponderId');

CREATE TRIGGER trg_suggestion_submitter_tenant
    BEFORE INSERT OR UPDATE ON ""suggestions""
    FOR EACH ROW EXECUTE FUNCTION derasax_assert_user_tenant('SubmittedByUserId');
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TRIGGER IF EXISTS trg_suggestion_submitter_tenant ON ""suggestions"";
DROP TRIGGER IF EXISTS trg_parent_request_response_responder_tenant ON ""parentRequestResponses"";
DROP TRIGGER IF EXISTS trg_parent_request_student_tenant ON ""parentRequests"";
DROP TRIGGER IF EXISTS trg_parent_request_parent_tenant ON ""parentRequests"";
DROP TRIGGER IF EXISTS trg_message_receipt_user_tenant ON ""messageReadReceipts"";
DROP TRIGGER IF EXISTS trg_message_sender_tenant ON ""messages"";
DROP TRIGGER IF EXISTS trg_conversation_participant_user_tenant ON ""conversationParticipants"";
");

            migrationBuilder.DropTable(
                name: "conversationParticipants");

            migrationBuilder.DropTable(
                name: "messageAttachments");

            migrationBuilder.DropTable(
                name: "messageReadReceipts");

            migrationBuilder.DropTable(
                name: "parentRequestResponses");

            migrationBuilder.DropTable(
                name: "suggestions");

            migrationBuilder.DropTable(
                name: "messages");

            migrationBuilder.DropTable(
                name: "parentRequests");

            migrationBuilder.DropTable(
                name: "conversations");
        }
    }
}
