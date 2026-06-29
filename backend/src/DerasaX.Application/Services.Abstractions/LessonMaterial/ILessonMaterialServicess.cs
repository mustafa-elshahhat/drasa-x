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
