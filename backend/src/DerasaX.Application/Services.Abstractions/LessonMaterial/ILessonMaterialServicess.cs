using DerasaX.Application.Dto.LessonDto;
using DerasaX.Application.Dto.LessonMaterialDto;
using DerasaX.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DerasaX.Application.Services.Abstractions.LessonMaterial
{
    public interface ILessonMaterialServicess
    {
        Task<ApiResponse<IEnumerable<GetLessonMaterialDto>>> GetMaterialByLessonIdAsync(string lessonId);
        /// <summary>
        /// Fetches a single lesson material by its own id (tenant-scoped). Used by detail pages that
        /// only know the material id — not its parent lesson — such as the student material detail
        /// page (P1-6). Cross-tenant ids resolve to <see cref="DerasaX.Domain.Exceptions.NotFoundException"/>.
        /// </summary>
        Task<ApiResponse<GetLessonMaterialDto>> GetMaterialByIdAsync(string id);
        Task<ApiResponse<GetLessonMaterialDto>> AddMaterialAsync(AddLessonMaterialDto addLessonMaterialDto);
        /// <summary>
        /// Phase 16 — registers a lesson material backed by a durable uploaded file. The file bytes
        /// have already been validated/stored by the file-storage service; this links the resulting
        /// FileRecord to a tenant-scoped lesson material whose Url is the backend download path.
        /// </summary>
        Task<ApiResponse<GetLessonMaterialDto>> AddUploadedMaterialAsync(
            string lessonId, string title, DerasaX.Domain.Enums.AttachmentType type, string fileRecordId, string downloadUrl);
        Task<ApiResponse<GetLessonMaterialDto>> UpdateMaterialAsync(GetLessonMaterialDto getLessonMaterialDto);
        Task<ApiResponse<bool>> DeleteMaterial(string id);
    }
}
