using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DerasaX.Application.Common;
using DerasaX.Application.Dto.EngagementDto;
using DerasaX.Application.Services.Abstractions;
using DerasaX.Application.Services.Abstractions.Audit;
using DerasaX.Application.Services.Abstractions.Engagement;
using DerasaX.Domain.Common;
using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Domain.Exceptions;
using DerasaX.Domain.Interfaces;
using DerasaX.Domain.Specification.Common;

namespace DerasaX.Application.Services.Engagement
{
    public class OfficeHourService : EngagementServiceBase, IOfficeHourService
    {
        public OfficeHourService(IUnitOfWork unitOfWork, ITenantContext tenant, IAuditWriter audit)
            : base(unitOfWork, tenant, audit) { }

        public async Task<ApiResponse<OfficeHourDto>> CreateAsync(CreateOfficeHourDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var teacherId = RequireUser();
            if (!IsTeacher) throw new ForbiddenException("Only a teacher may create office hours.");
            ValidateSchedule(dto.StartsAt, dto.EndsAt, dto.Capacity);

            var session = new OfficeHourSession
            {
                Id = Guid.NewGuid().ToString(), TenantId = tenantId, TeacherId = teacherId, Title = dto.Title,
                StartsAt = AsUtc(dto.StartsAt), EndsAt = AsUtc(dto.EndsAt), Capacity = dto.Capacity, Status = OfficeHourStatus.Scheduled
            };
            await UnitOfWork.Repository<OfficeHourSession, string>().AddAsync(session);
            await Audit.StageAsync(AuditActionType.Create, nameof(OfficeHourSession), session.Id, ct: ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(Map(session, 0), 201, "Office-hour session created.");
        }

        public async Task<ApiResponse<OfficeHourDto>> UpdateAsync(string id, UpdateOfficeHourDto dto, CancellationToken ct = default)
        {
            RequireTenant();
            var session = await LoadOwnedAsync(id);
            if (session.Status != OfficeHourStatus.Scheduled)
                throw new ConflictException("Only a scheduled session can be edited.");
            ValidateSchedule(dto.StartsAt, dto.EndsAt, dto.Capacity);
            var booked = await BookedCount(id);
            if (dto.Capacity < booked) throw new ConflictException("Capacity cannot be set below the number of existing bookings.");

            session.Title = dto.Title;
            session.StartsAt = AsUtc(dto.StartsAt);
            session.EndsAt = AsUtc(dto.EndsAt);
            session.Capacity = dto.Capacity;
            UnitOfWork.Repository<OfficeHourSession, string>().Update(session);
            await Audit.StageAsync(AuditActionType.Update, nameof(OfficeHourSession), session.Id, ct: ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(Map(session, booked), 200, "Office-hour session updated.");
        }

        public async Task<ApiResponse<bool>> CancelAsync(string id, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var session = await LoadOwnedAsync(id);
            if (session.Status == OfficeHourStatus.Cancelled) throw new ConflictException("Session is already cancelled.");
            session.Status = OfficeHourStatus.Cancelled;
            UnitOfWork.Repository<OfficeHourSession, string>().Update(session);

            // Cancel outstanding bookings and notify the students.
            var bookings = await ActiveBookings(id);
            foreach (var b in bookings)
            {
                b.Status = OfficeHourBookingStatus.Cancelled;
                UnitOfWork.Repository<OfficeHourBooking, string>().Update(b);
                await StageNotificationAsync(tenantId, b.StudentId, "Office hours cancelled", "A session you booked was cancelled.");
            }
            await Audit.StageAsync(AuditActionType.Update, nameof(OfficeHourSession), session.Id, "{\"action\":\"cancel\"}", ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(true, 200, "Session cancelled.");
        }

        public async Task<ApiResponse<IEnumerable<OfficeHourDto>>> MySessionsAsync(CancellationToken ct = default)
        {
            RequireTenant();
            var teacherId = RequireUser();
            var sessions = (await UnitOfWork.Repository<OfficeHourSession, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<OfficeHourSession, string>(s => s.TeacherId == teacherId))).ToList();
            var dto = await MapWithCounts(sessions);
            return Ok<IEnumerable<OfficeHourDto>>(dto, 200, "Sessions retrieved.");
        }

        public async Task<ApiResponse<IEnumerable<OfficeHourDto>>> MineAsync(CancellationToken ct = default)
        {
            RequireTenant();
            // A teacher/admin sees the sessions they own; a student sees the sessions
            // they have an active booking for (their "my bookings" list).
            if (!IsStudent) return await MySessionsAsync(ct);

            var studentId = RequireUser();
            var bookings = (await UnitOfWork.Repository<OfficeHourBooking, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<OfficeHourBooking, string>(b =>
                    b.StudentId == studentId && b.Status != OfficeHourBookingStatus.Cancelled))).ToList();
            var ids = bookings.Select(b => b.OfficeHourSessionId).Distinct().ToList();
            var sessions = ids.Count == 0
                ? new List<OfficeHourSession>()
                : (await UnitOfWork.Repository<OfficeHourSession, string>().GetAllWithSpecAsync(
                    new CriteriaSpecification<OfficeHourSession, string>(s => ids.Contains(s.Id)))).ToList();
            var dto = await MapWithCounts(sessions);
            // The student's own booking id isn't derivable from the session-level DTO otherwise
            // (GET .../bookings is teacher/admin-only) — without this, a student has no way to
            // discover the id they'd need to call POST bookings/{bookingId}/cancel.
            var bookingIdBySession = bookings.GroupBy(b => b.OfficeHourSessionId).ToDictionary(g => g.Key, g => g.First().Id);
            foreach (var d in dto)
                if (bookingIdBySession.TryGetValue(d.Id, out var bookingId)) d.MyBookingId = bookingId;
            return Ok<IEnumerable<OfficeHourDto>>(dto, 200, "Sessions retrieved.");
        }

        public async Task<ApiResponse<IEnumerable<OfficeHourDto>>> AvailableAsync(CancellationToken ct = default)
        {
            RequireTenant();
            var now = DateTime.UtcNow;
            var sessions = (await UnitOfWork.Repository<OfficeHourSession, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<OfficeHourSession, string>(s => s.Status == OfficeHourStatus.Scheduled && s.StartsAt > now))).ToList();
            var dto = (await MapWithCounts(sessions)).Where(d => d.BookedCount < d.Capacity).ToList();
            return Ok<IEnumerable<OfficeHourDto>>(dto, 200, "Available sessions retrieved.");
        }

        public async Task<ApiResponse<BookingDto>> BookAsync(string id, BookOfficeHourDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var studentId = RequireUser();
            if (!IsStudent) throw new ForbiddenException("Only a student may book office hours.");

            var session = await UnitOfWork.Repository<OfficeHourSession, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<OfficeHourSession, string>(s => s.Id == id)) ?? throw new NotFoundException("Session not found.");
            if (session.Status != OfficeHourStatus.Scheduled) throw new ConflictException("This session is not open for bookings.");
            if (session.StartsAt <= DateTime.UtcNow) throw new ConflictException("This session has already started.");

            // Duplicate-booking prevention.
            var mine = await UnitOfWork.Repository<OfficeHourBooking, string>().CountAsync(
                new CriteriaSpecification<OfficeHourBooking, string>(b =>
                    b.OfficeHourSessionId == id && b.StudentId == studentId && b.Status != OfficeHourBookingStatus.Cancelled));
            if (mine > 0) throw new ConflictException("You have already booked this session.");

            // Capacity enforcement.
            if (await BookedCount(id) >= session.Capacity) throw new ConflictException("This session is full.");

            var booking = new OfficeHourBooking
            {
                Id = Guid.NewGuid().ToString(), TenantId = tenantId, OfficeHourSessionId = id, StudentId = studentId,
                Status = OfficeHourBookingStatus.Confirmed, BookedAt = DateTime.UtcNow, Notes = dto.Notes
            };
            await UnitOfWork.Repository<OfficeHourBooking, string>().AddAsync(booking);
            await Audit.StageAsync(AuditActionType.Create, nameof(OfficeHourBooking), booking.Id, ct: ct);
            await StageNotificationAsync(tenantId, session.TeacherId, "New office-hour booking", "A student booked your office hours.");
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(MapBooking(booking), 201, "Session booked.");
        }

        public async Task<ApiResponse<bool>> CancelBookingAsync(string bookingId, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            var caller = RequireUser();
            var booking = await UnitOfWork.Repository<OfficeHourBooking, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<OfficeHourBooking, string>(b => b.Id == bookingId)) ?? throw new NotFoundException("Booking not found.");
            var session = await UnitOfWork.Repository<OfficeHourSession, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<OfficeHourSession, string>(s => s.Id == booking.OfficeHourSessionId));

            // The booking student or the owning teacher may cancel.
            var isOwnerTeacher = session is not null && session.TeacherId == caller;
            if (booking.StudentId != caller && !isOwnerTeacher)
                throw new ForbiddenException("You may only cancel your own booking.");
            if (booking.Status == OfficeHourBookingStatus.Cancelled) throw new ConflictException("Booking is already cancelled.");

            booking.Status = OfficeHourBookingStatus.Cancelled;
            UnitOfWork.Repository<OfficeHourBooking, string>().Update(booking);
            await Audit.StageAsync(AuditActionType.Update, nameof(OfficeHourBooking), booking.Id, "{\"action\":\"cancel\"}", ct);
            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(true, 200, "Booking cancelled.");
        }

        public async Task<ApiResponse<IEnumerable<BookingDto>>> SessionBookingsAsync(string id, CancellationToken ct = default)
        {
            RequireTenant();
            var session = await LoadOwnedAsync(id);
            var bookings = await UnitOfWork.Repository<OfficeHourBooking, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<OfficeHourBooking, string>(b => b.OfficeHourSessionId == session.Id));
            return Ok<IEnumerable<BookingDto>>(bookings.Select(MapBooking).ToList(), 200, "Bookings retrieved.");
        }

        public async Task<ApiResponse<BookingDto>> MarkAttendanceAsync(string bookingId, MarkAttendanceDto dto, CancellationToken ct = default)
        {
            var tenantId = RequireTenant();
            if (dto.Status is not (OfficeHourBookingStatus.Attended or OfficeHourBookingStatus.NoShow))
                throw new BadRequestException("Attendance status must be Attended or NoShow.");

            var booking = await UnitOfWork.Repository<OfficeHourBooking, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<OfficeHourBooking, string>(b => b.Id == bookingId)) ?? throw new NotFoundException("Booking not found.");
            // Attendance is recorded only by the owning teacher or a school administrator.
            var session = await LoadOwnedAsync(booking.OfficeHourSessionId);
            if (booking.Status == OfficeHourBookingStatus.Cancelled)
                throw new ConflictException("A cancelled booking cannot be marked for attendance.");

            booking.Status = dto.Status;
            UnitOfWork.Repository<OfficeHourBooking, string>().Update(booking);
            await Audit.StageAsync(AuditActionType.Update, nameof(OfficeHourBooking), booking.Id,
                $"{{\"action\":\"attendance\",\"status\":\"{dto.Status}\"}}", ct);

            if (dto.Status == OfficeHourBookingStatus.Attended)
            {
                // Idempotent gamification reward: attending the same booking awards points at most once.
                var (points, ruleId) = await GamificationDefaults.ResolveAsync(
                    UnitOfWork, GamificationTrigger.OfficeHourAttended, GamificationDefaults.OfficeHourAttended, ct);
                if (points != 0)
                {
                    var awarded = await PointLedgerStaging.StageAsync(UnitOfWork, tenantId, booking.StudentId, points,
                        "Attended office hours", PointSourceType.OfficeHourAttendance, $"oh-attend:{booking.Id}",
                        sourceId: session.Id, gamificationRuleId: ruleId, ct: ct);
                    if (awarded is not null)
                        await StageNotificationAsync(tenantId, booking.StudentId, "Points awarded",
                            $"You earned {points} point(s) for attending office hours.", NotificationCategory.General);
                }
            }

            await UnitOfWork.SaveChangesAsync(ct);
            return Ok(MapBooking(booking), 200, "Attendance recorded.");
        }

        // ---- helpers ----

        private static void ValidateSchedule(DateTime startsAt, DateTime endsAt, int capacity)
        {
            if (AsUtc(endsAt) <= AsUtc(startsAt)) throw new BadRequestException("EndsAt must be after StartsAt.");
            if (capacity < 1 || capacity > 500) throw new BadRequestException("Capacity must be between 1 and 500.");
        }

        private async Task<OfficeHourSession> LoadOwnedAsync(string id)
        {
            var caller = RequireUser();
            var session = await UnitOfWork.Repository<OfficeHourSession, string>().GetByIdWithSpecAsync(
                new CriteriaSpecification<OfficeHourSession, string>(s => s.Id == id)) ?? throw new NotFoundException("Session not found.");
            if (!IsSchoolAdmin && session.TeacherId != caller)
                throw new ForbiddenException("You may only manage your own office-hour sessions.");
            return session;
        }

        private async Task<int> BookedCount(string sessionId) =>
            await UnitOfWork.Repository<OfficeHourBooking, string>().CountAsync(
                new CriteriaSpecification<OfficeHourBooking, string>(b =>
                    b.OfficeHourSessionId == sessionId && b.Status != OfficeHourBookingStatus.Cancelled));

        private async Task<List<OfficeHourBooking>> ActiveBookings(string sessionId) =>
            (await UnitOfWork.Repository<OfficeHourBooking, string>().GetAllWithSpecAsync(
                new CriteriaSpecification<OfficeHourBooking, string>(b =>
                    b.OfficeHourSessionId == sessionId && b.Status != OfficeHourBookingStatus.Cancelled))).ToList();

        private async Task<List<OfficeHourDto>> MapWithCounts(List<OfficeHourSession> sessions)
        {
            var ids = sessions.Select(s => s.Id).ToList();
            var bookings = ids.Count == 0
                ? new List<OfficeHourBooking>()
                : (await UnitOfWork.Repository<OfficeHourBooking, string>().GetAllWithSpecAsync(
                    new CriteriaSpecification<OfficeHourBooking, string>(b =>
                        ids.Contains(b.OfficeHourSessionId) && b.Status != OfficeHourBookingStatus.Cancelled))).ToList();
            return sessions.Select(s => Map(s, bookings.Count(b => b.OfficeHourSessionId == s.Id))).ToList();
        }

        private static OfficeHourDto Map(OfficeHourSession s, int booked) => new()
        {
            Id = s.Id, TeacherId = s.TeacherId, Title = s.Title, StartsAt = s.StartsAt, EndsAt = s.EndsAt,
            Capacity = s.Capacity, BookedCount = booked, Status = s.Status
        };

        private static BookingDto MapBooking(OfficeHourBooking b) => new()
        {
            Id = b.Id, OfficeHourSessionId = b.OfficeHourSessionId, StudentId = b.StudentId, Status = b.Status, BookedAt = b.BookedAt
        };
    }
}
