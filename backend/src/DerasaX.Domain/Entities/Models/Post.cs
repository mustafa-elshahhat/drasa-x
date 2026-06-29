using DerasaX.Domain.Entities.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DerasaX.Domain.Entities.Models
{
    public class Post :BaseEntity<string>
    {
        public string? PhotoUrl { get; set; }
        public string Content { get; set; } = null!;
        public int LikesCount { get; set; } = 0;
        public int CommentsCount { get; set; } = 0;
        public int ViewsCount { get; set; } = 0;     
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CommunityId { get; set; }
        public Community? Community { get; set; }
        [ForeignKey("User")]
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
        public ICollection<PostComment> Comments { get; set; } = new HashSet<PostComment>();
        public ICollection<PostReport> Reports { get; set; } = new HashSet<PostReport>();
    }
}
