using DerasaX.Domain.Entities.Base;
using DerasaX.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DerasaX.Domain.Entities.Models
{
    public class LessonMaterial : BaseEntity<string>
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public AttachmentType Type { get; set; }
        [ForeignKey("Lesson")]
        public string LessonId { get; set; }
        public Lesson Lesson { get; set; }

        /// <summary>
        /// Phase 16 — durable file backing this material when it was uploaded (vs. an external
        /// URL). Null for legacy/external-link materials; clients download via the file API.
        /// </summary>
        public string? FileRecordId { get; set; }
    }
}
