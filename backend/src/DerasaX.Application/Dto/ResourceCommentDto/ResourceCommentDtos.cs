using System;
using DerasaX.Application.Common;

namespace DerasaX.Application.Dto.ResourceCommentDto
{
    public class ResourceCommentDto
    {
        public string Id { get; set; } = string.Empty;
        public string MaterialId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateResourceCommentDto
    {
        public string Body { get; set; } = string.Empty;
    }

    public class UpdateResourceCommentDto
    {
        public string Body { get; set; } = string.Empty;
    }

    public class ResourceCommentParameters : PaginationParameters { }
}
