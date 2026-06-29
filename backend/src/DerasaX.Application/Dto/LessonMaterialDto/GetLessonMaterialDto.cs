using DerasaX.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DerasaX.Application.Dto.LessonMaterialDto
{
    public class GetLessonMaterialDto
    {
        public string Id { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Url { get; set; } = null!;
        public AttachmentType Type { get; set; }
        public string LessonId { get; set; } = null!;
        /// <summary>Phase 16 — set when the material is a durable uploaded file (download via the file API).</summary>
        public string? FileRecordId { get; set; }
    }
}
