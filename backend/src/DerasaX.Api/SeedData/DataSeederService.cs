using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using DerasaX.Infrastructure.DbHelper.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DerasaX.Api.SeedData
{
    public partial class DataSeederService
    {
        private readonly DerasaXDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
        public DataSeederService(DerasaXDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }
        public async Task SeedAllAsync(string baseRootPath)
        {
            await SeedRoles();
            var jsonFolderPath = Path.Combine(baseRootPath, "JsonFile");

            // Seed Tenants
            await SeedFile<Tenant>(Path.Combine(jsonFolderPath, "tenants.json"), _context.tenants, t => t.Id);

            await SeedAdmin();
            await SeedTeachers();

            // Seed Grades
            await SeedFile<Grade>(Path.Combine(jsonFolderPath, "grades.json"), _context.grades, t => t.Id);

            // Seed Students
            await SeedStudents(Path.Combine(jsonFolderPath, "students.json"));

            // Phase 3 security fixtures: two active tenants + one suspended tenant,
            // all five roles, a platform SystemAdmin and a disabled account, so the
            // cross-tenant / authorization / lockout tests have real data to use.
            await SeedPhase3SecurityFixturesAsync();
            await SeedPhase8StudentPortalFixturesAsync();
            await SeedPhase8E2EAcceptanceFixturesAsync();
            await SeedPhase9TeacherPortalFixturesAsync();
            await SeedPhase10ParentPortalFixturesAsync();
            await SeedPhase11SchoolAdminPortalFixturesAsync();
            await SeedPhase12SystemAdminPortalFixturesAsync();
            await SeedPhase13CommunicationFixturesAsync();

            // Realistic, richly-connected "showcase" data for the natural local demo
            // accounts (Omar Ahmed / Nada Ashraf students, Malak Hassan teacher, their
            // parent and the tenant-1 school admin) so every role lands on a non-empty,
            // realistic dashboard after a reset + reseed. These actors are NOT E2E
            // reset actors, so the data is stable and never wiped by the E2E reset.
            await SeedShowcaseFixturesAsync();
        }
        private async Task SeedRoles()
        {
            string[] roles = { "SchoolAdmin", "Teacher", "Student", "Parent", "SystemAdmin" };

            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        //private async Task SeedFile<T>(string path, DbSet<T> dbSet) where T : class
        //{
        //    if (await dbSet.AnyAsync()) return;
        //    var data = await File.ReadAllTextAsync(path);
        //    var items = JsonSerializer.Deserialize<List<T>>(data);
        //    if (items != null)
        //    {
        //        await dbSet.AddRangeAsync(items);
        //        await _context.SaveChangesAsync();
        //    }
        //}
        private async Task SeedFile<T>(string path,DbSet<T> dbSet,Func<T, object> keySelector) where T : class
        {
            var data = await File.ReadAllTextAsync(path);
            var items = JsonSerializer.Deserialize<List<T>>(data);

            if (items == null) return;

            // Seeding runs without an authenticated tenant context; bypass the global
            // tenant query filter for the existence check only (tightly controlled).
            var existingItems = await dbSet.IgnoreQueryFilters().ToListAsync();

            foreach (var item in items)
            {
                var key = keySelector(item);

                bool exists = existingItems.Any(e => keySelector(e).Equals(key));

                if (!exists)
                {
                    await dbSet.AddAsync(item);
                }
            }

            await _context.SaveChangesAsync();
        }

        //private async Task SeedStudents(string path)
        //{
        //    if (await _context.students.AnyAsync()) return;

        //    var data = await File.ReadAllTextAsync(path);

        //    var students = JsonSerializer.Deserialize<List<Student>>(data);

        //    if (students == null) return;

        //    foreach (var s in students)
        //    {


        //        // 1- Create Identity User
        //        var result = await _userManager.CreateAsync(s, "P@ssword123");

        //        if (!result.Succeeded)
        //            continue;

        //        // 2- Assign Role
        //        await _userManager.AddToRoleAsync(s, "Student");

        //        // 3- TPT mapping
        //        _context.students.Add(new Student
        //        {
        //            Id = s.Id
        //        });
        //    }

        //    await _context.SaveChangesAsync();
        //}
        private async Task SeedStudents(string path)
        {
            if (await _context.students.AnyAsync()) return;

            var data = await File.ReadAllTextAsync(path);

            var students = JsonSerializer.Deserialize<List<Student>>(data);

            if (students == null) return;

            var password = _configuration["Seed:DefaultPassword"] ?? "Local@Dev123";

            foreach (var s in students)
            {
                // Create the Student directly as a Table-Per-Hierarchy subtype of
                // ApplicationUser. Creating a base ApplicationUser and then adding a
                // separate Student row with the same key causes an EF identity-map
                // conflict (both map to the same TPH key), so we create the derived
                // type in one step — the same pattern used by SeedTeachers().
                var student = new Student
                {
                    UserName = s.UserName,
                    FullName = s.FullName,
                    LoginCode = s.LoginCode,
                    Gender = s.Gender,
                    TenantId = s.TenantId,
                    GradeId = s.GradeId
                };

                var result = await _userManager.CreateAsync(student, password);

                if (!result.Succeeded)
                    continue;

                await _userManager.AddToRoleAsync(student, "Student");
            }
        }
        private async Task SeedAdmin()
        {
            if (await _context.SchoolAdmin.AnyAsync()) return;

            var password = _configuration["Seed:DefaultPassword"] ?? "Local@Dev123";

            // Create the SchoolAdmin directly as a TPH subtype (see SeedStudents note).
            var admin = new SchoolAdmin
            {
                UserName = "admin",
                FullName = "Nabil Sherif",
                LoginCode = "27102004",
                Gender =(Domain.Enums.Gender?)1,
                TenantId = "tenant-1"

            };

            var result = await _userManager.CreateAsync(admin, password);

            if (!result.Succeeded)
                return;

            await _userManager.AddToRoleAsync(admin, "SchoolAdmin");
        }

        private async Task SeedTeachers()
        {
            var userName = "malak";
            var password = _configuration["Seed:DefaultPassword"] ?? "Local@Dev123";

            var user = await _userManager.FindByNameAsync(userName);

            if (user == null)
            {
                var teacher = new Teacher
                {
                    UserName = userName,
                    FullName = "Malak Hassan",
                    LoginCode = "TEACH002",
                    TenantId = "tenant-1",
                    Gender = (Domain.Enums.Gender?)2,
                };

                var result = await _userManager.CreateAsync(teacher, password);

                if (!result.Succeeded)
                {
                    throw new Exception(string.Join(",", result.Errors.Select(e => e.Description)));
                }

                await _userManager.AddToRoleAsync(teacher, "Teacher");
            }
        }

        // ---------------------------------------------------------------------
        // Phase 3 security fixtures (Development/Test only). Login codes are stable
        // and documented; the password is the configured local seed password
        // (Seed:DefaultPassword, default below) and is NEVER a production secret.
        // ---------------------------------------------------------------------
        private async Task SeedPhase3SecurityFixturesAsync()
        {
            var password = _configuration["Seed:DefaultPassword"] ?? "Local@Dev123";

            await EnsureTenant("tenant-1", "Nile Future International School", TenantStatus.Active);
            await EnsureTenant("tenant-2", "Al-Nahda STEM School", TenantStatus.Active);
            await EnsureTenant("tenant-suspended", "Horizon Language Academy", TenantStatus.Suspended);

            await EnsureGrade("G7-ID", "Grade 7", "tenant-1");
            await EnsureGrade("T2-G7", "Grade 7", "tenant-2");
            await EnsureGrade("SUS-G7", "Grade 7", "tenant-suspended");

            // Tenant 1 — full role set
            await EnsureUser<SchoolAdmin>("ADMIN-T1", "Hala Mansour", "tenant-1", "SchoolAdmin", password);
            await EnsureUser<Teacher>("TEACH-T1", "Karim Adel", "tenant-1", "Teacher", password);
            await EnsureUser<Parent>("PARENT-T1", "Sherif Naguib", "tenant-1", "Parent", password);
            await EnsureUser<Student>("STU-T1", "Youssef Ibrahim", "tenant-1", "Student", password, gradeId: "G7-ID");

            // Tenant 2 — full role set (overlapping roles, distinct records)
            await EnsureUser<SchoolAdmin>("ADMIN-T2", "Mona Saleh", "tenant-2", "SchoolAdmin", password);
            await EnsureUser<Teacher>("TEACH-T2", "Tarek Fahmy", "tenant-2", "Teacher", password);
            await EnsureUser<Parent>("PARENT-T2", "Amani Darwish", "tenant-2", "Parent", password);
            await EnsureUser<Student>("STU-T2", "Laila Hosny", "tenant-2", "Student", password, gradeId: "T2-G7");

            // Platform SystemAdmin (no tenant) + suspended-tenant user + disabled user
            await EnsureUser<SystemAdmin>("SYS-1", "Adham Roushdy", null, "SystemAdmin", password);
            await EnsureUser<Student>("STU-SUS", "Hana Lotfy", "tenant-suspended", "Student", password, gradeId: "SUS-G7");
            await EnsureUser<Student>("STU-DIS", "Fares Zaki", "tenant-1", "Student", password, gradeId: "G7-ID", isDisabled: true);
        }

        private async Task EnsureTenant(string id, string name, TenantStatus status)
        {
            var existing = await _context.tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id);
            if (existing is null)
            {
                _context.tenants.Add(new Tenant { Id = id, Name = name, Status = status });
            }
            else if (existing.Status != status)
            {
                existing.Status = status;
            }
            await _context.SaveChangesAsync();
        }

        private async Task EnsureGrade(string id, string name, string tenantId)
        {
            var exists = await _context.grades.IgnoreQueryFilters().AnyAsync(g => g.Id == id);
            if (!exists)
            {
                _context.grades.Add(new Grade { Id = id, Name = name, TenantId = tenantId });
                await _context.SaveChangesAsync();
            }
        }

        private async Task EnsureUser<TUser>(string loginCode, string fullName, string? tenantId, string role, string password, string? gradeId = null, bool isDisabled = false)
            where TUser : ApplicationUser, new()
        {
            var existing = await _userManager.Users.FirstOrDefaultAsync(u => u.LoginCode == loginCode);
            if (existing is not null)
            {
                existing.UserName = loginCode.ToLowerInvariant();
                existing.FullName = fullName;
                existing.TenantId = tenantId;
                existing.IsDeleted = isDisabled;

                if (existing is Student existingStudent && gradeId is not null)
                    existingStudent.GradeId = gradeId;

                await ThrowIfFailed(_userManager.UpdateAsync(existing), $"Seed user {loginCode} update failed");
                await ThrowIfFailed(_userManager.ResetAccessFailedCountAsync(existing), $"Seed user {loginCode} access reset failed");
                await ThrowIfFailed(_userManager.SetLockoutEndDateAsync(existing, null), $"Seed user {loginCode} unlock failed");

                if (!await _userManager.CheckPasswordAsync(existing, password))
                {
                    if (await _userManager.HasPasswordAsync(existing))
                    {
                        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(existing);
                        await ThrowIfFailed(_userManager.ResetPasswordAsync(existing, resetToken, password), $"Seed user {loginCode} password reset failed");
                    }
                    else
                    {
                        await ThrowIfFailed(_userManager.AddPasswordAsync(existing, password), $"Seed user {loginCode} password set failed");
                    }
                }

                if (!await _userManager.IsInRoleAsync(existing, role))
                    await ThrowIfFailed(_userManager.AddToRoleAsync(existing, role), $"Seed user {loginCode} role assignment failed");

                return;
            }

            var user = new TUser
            {
                UserName = loginCode.ToLowerInvariant(),
                FullName = fullName,
                LoginCode = loginCode,
                TenantId = tenantId,
                IsDeleted = isDisabled
            };

            if (user is Student student && gradeId is not null)
                student.GradeId = gradeId;

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new Exception($"Seed user {loginCode} failed: {string.Join(",", result.Errors.Select(e => e.Description))}");

            await _userManager.AddToRoleAsync(user, role);
        }

        private static async Task ThrowIfFailed(Task<IdentityResult> resultTask, string message)
        {
            var result = await resultTask;
            if (!result.Succeeded)
                throw new Exception($"{message}: {string.Join(",", result.Errors.Select(e => e.Description))}");
        }

        private async Task SeedPhase8StudentPortalFixturesAsync()
        {
            await EnsureAcademicYear("PH8-AY-T1", "Academic Year 2030/2031", "PH8AYT1", "tenant-1");
            await EnsureAcademicYear("PH8-AY-T2", "Academic Year 2030/2031", "PH8AYT2", "tenant-2");
            await EnsureSchoolClass("PH8-CLASS-T1", "Grade 7 - A", "PH8C1", "tenant-1", "G7-ID", "PH8-AY-T1");
            await EnsureSchoolClass("PH8-CLASS-T2", "Grade 7 - B", "PH8C2", "tenant-2", "T2-G7", "PH8-AY-T2");

            await EnsureSubject("PH8-SUBJECT-T1", "Mathematics", "tenant-1", "G7-ID");
            await EnsureSubject("PH8-SUBJECT-T2", "Mathematics", "tenant-2", "T2-G7");
            await EnsureUnit("PH8-UNIT-T1", "Algebra", "tenant-1", "PH8-SUBJECT-T1");
            await EnsureUnit("PH8-UNIT-T2", "Algebra", "tenant-2", "PH8-SUBJECT-T2");
            await EnsureLesson("PH8-LESSON-T1", "Linear Equations", "tenant-1", "PH8-UNIT-T1");
            await EnsureLesson("PH8-LESSON-T2", "Linear Equations", "tenant-2", "PH8-UNIT-T2");

            var stuT1 = await _userManager.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.LoginCode == "STU-T1");
            var stuT2 = await _userManager.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.LoginCode == "STU-T2");
            if (stuT1 is not null)
            {
                await EnsureEnrollment("PH8-ENR-T1", "tenant-1", stuT1.Id, "PH8-CLASS-T1", "PH8-AY-T1");
                await EnsureAttendance("PH8-ATT-T1-1", "tenant-1", stuT1.Id, "PH8-CLASS-T1", "2031-01-05", AttendanceStatus.Present, AttendanceSource.Manual, "day");
                await EnsureAttendance("PH8-ATT-T1-2", "tenant-1", stuT1.Id, "PH8-CLASS-T1", "2031-01-06", AttendanceStatus.Late, AttendanceSource.Manual, "day");
                await EnsureAttendance("PH8-ATT-T1-3", "tenant-1", stuT1.Id, "PH8-CLASS-T1", "2031-01-07", AttendanceStatus.Absent, AttendanceSource.Import, "day");
            }
            if (stuT2 is not null)
            {
                await EnsureEnrollment("PH8-ENR-T2", "tenant-2", stuT2.Id, "PH8-CLASS-T2", "PH8-AY-T2");
                await EnsureAttendance("PH8-ATT-T2-1", "tenant-2", stuT2.Id, "PH8-CLASS-T2", "2031-01-05", AttendanceStatus.Present, AttendanceSource.Manual, "day");
            }
        }

        private async Task EnsureAcademicYear(string id, string name, string code, string tenantId)
        {
            if (await _context.academicYears.IgnoreQueryFilters().AnyAsync(x => x.Id == id)) return;
            _context.academicYears.Add(new AcademicYear { Id = id, TenantId = tenantId, Name = name, Code = code, StartDate = new DateTime(2030, 9, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2031, 6, 30, 0, 0, 0, DateTimeKind.Utc) });
            await _context.SaveChangesAsync();
        }

        private async Task EnsureSchoolClass(string id, string name, string code, string tenantId, string gradeId, string academicYearId)
        {
            if (await _context.schoolClasses.IgnoreQueryFilters().AnyAsync(x => x.Id == id)) return;
            _context.schoolClasses.Add(new SchoolClass { Id = id, TenantId = tenantId, Name = name, Code = code, GradeId = gradeId, AcademicYearId = academicYearId, Capacity = 40 });
            await _context.SaveChangesAsync();
        }

        private async Task EnsureSubject(string id, string name, string tenantId, string gradeId)
        {
            if (await _context.subjects.IgnoreQueryFilters().AnyAsync(x => x.Id == id)) return;
            _context.subjects.Add(new Subject { Id = id, TenantId = tenantId, Name = name, GradeId = gradeId });
            await _context.SaveChangesAsync();
        }

        private async Task EnsureUnit(string id, string title, string tenantId, string subjectId)
        {
            if (await _context.units.IgnoreQueryFilters().AnyAsync(x => x.Id == id)) return;
            _context.units.Add(new Unit { Id = id, TenantId = tenantId, Title = title, SubjectId = subjectId });
            await _context.SaveChangesAsync();
        }

        private async Task EnsureLesson(string id, string title, string tenantId, string unitId)
        {
            if (await _context.lessons.IgnoreQueryFilters().AnyAsync(x => x.Id == id)) return;
            _context.lessons.Add(new Lesson { Id = id, TenantId = tenantId, Title = title, Content = "An introduction to solving linear equations and graphing straight lines, with worked examples and practice problems.", UnitId = unitId });
            await _context.SaveChangesAsync();
        }

        private async Task EnsureEnrollment(string id, string tenantId, string studentId, string classId, string academicYearId)
        {
            if (await _context.enrollments.IgnoreQueryFilters().AnyAsync(x => x.Id == id)) return;
            _context.enrollments.Add(new Enrollment { Id = id, TenantId = tenantId, StudentId = studentId, SchoolClassId = classId, AcademicYearId = academicYearId, Status = EnrollmentStatus.Active, EnrolledAt = DateTime.UtcNow });
            await _context.SaveChangesAsync();
        }

        private async Task EnsureAttendance(string id, string tenantId, string studentId, string classId, string date, AttendanceStatus status, AttendanceSource source, string sessionKey)
        {
            var attendanceDate = DateTime.SpecifyKind(DateTime.Parse(date), DateTimeKind.Utc);
            var exists = await _context.studentAttendanceRecords.IgnoreQueryFilters().AnyAsync(x =>
                x.TenantId == tenantId && x.StudentId == studentId && x.AttendanceDate == attendanceDate && x.SessionKey == sessionKey);
            if (exists) return;
            _context.studentAttendanceRecords.Add(new StudentAttendanceRecord
            {
                Id = id,
                TenantId = tenantId,
                StudentId = studentId,
                SchoolClassId = classId,
                AttendanceDate = attendanceDate,
                RecordedAt = attendanceDate.AddHours(8),
                Status = status,
                Source = source,
                SessionKey = sessionKey
            });
            await _context.SaveChangesAsync();
        }

        // =====================================================================
        // Phase 8 §9 live-acceptance fixtures (Development/Test only). Builds the
        // deterministic data the A1–M91 live Playwright matrix exercises across
        // homework, quizzes, communities, competitions, office hours, badges,
        // notifications, announcements and the progress read models — for two
        // isolated tenants. All identifiers use the E2E-PH8-* namespace. Every
        // helper is idempotent (Ensure-by-id) so repeated startups are safe; the
        // mutable per-run state is cleared by ResetE2EAcceptanceStateAsync.
        // =====================================================================
        private async Task<string?> UserIdByLoginCodeAsync(string loginCode)
        {
            var user = await _userManager.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.LoginCode == loginCode);
            return user?.Id;
        }

        private async Task Ensure<T>(DbSet<T> set, System.Linq.Expressions.Expression<Func<T, bool>> exists, Func<T> create) where T : class
        {
            if (await set.IgnoreQueryFilters().AnyAsync(exists)) return;
            await set.AddAsync(create());
            await _context.SaveChangesAsync();
        }

        private async Task SeedPhase8E2EAcceptanceFixturesAsync()
        {
            var password = _configuration["Seed:DefaultPassword"] ?? "Local@Dev123";

            // Supporting "other" tenant-1 student: the second actor used to make a
            // capacity-full office-hour slot, to seed leaderboard rivals and to own
            // pre-existing community content. It is NEVER reset.
            await EnsureUser<Student>("PH8-OTHER-T1", "Salma Adel", "tenant-1", "Student", password, gradeId: "G7-ID");
            await EnsureEnrollmentSafe("PH8-ENR-OTHER-T1", "tenant-1", "PH8-OTHER-T1", "PH8-CLASS-T1", "PH8-AY-T1");

            var stuT1 = await UserIdByLoginCodeAsync("STU-T1");
            var stuT2 = await UserIdByLoginCodeAsync("STU-T2");
            var otherT1 = await UserIdByLoginCodeAsync("PH8-OTHER-T1");
            var teachT1 = await UserIdByLoginCodeAsync("TEACH-T1");
            var teachT2 = await UserIdByLoginCodeAsync("TEACH-T2");
            if (stuT1 is null || teachT1 is null) return;

            var now = DateTime.UtcNow;

            // ---- B14 lesson material (tenant-1, on the assigned lesson) ----
            await Ensure(_context.lessonMaterials, x => x.Id == "E2E-PH8-MAT-T1", () => new LessonMaterial
            {
                Id = "E2E-PH8-MAT-T1", TenantId = "tenant-1", LessonId = "PH8-LESSON-T1",
                Title = "Algebra Worksheet", Url = "/files/algebra-worksheet.pdf", Type = AttachmentType.Document
            });

            // ---- B19 unassigned lesson (different grade in same tenant → completion 404) ----
            await EnsureGrade("PH8-G8-T1", "Grade 8", "tenant-1");
            await EnsureSubject("E2E-PH8-SUBJECT-G8-T1", "Science", "tenant-1", "PH8-G8-T1");
            await EnsureUnit("E2E-PH8-UNIT-G8-T1", "Forces and Motion", "tenant-1", "E2E-PH8-SUBJECT-G8-T1");
            await EnsureLesson("E2E-PH8-LESSON-UNASSIGNED-T1", "Solving Inequalities", "tenant-1", "E2E-PH8-UNIT-G8-T1");

            // ---- C homework (tenant-1 open, unassigned, and cross-tenant) ----
            await EnsureAssignment("E2E-PH8-HW-OPEN", "tenant-1", "Linear Equations Practice Set", AssignmentType.Homework,
                AssignmentStatus.Published, teachT1, availableFrom: now.AddDays(-2), dueDate: now.AddDays(7), maxScore: 100, subjectId: "PH8-SUBJECT-T1");
            await EnsureAssignmentTarget("E2E-PH8-HWT-OPEN", "tenant-1", "E2E-PH8-HW-OPEN", AssignmentTargetType.Class, schoolClassId: "PH8-CLASS-T1");

            await EnsureAssignment("E2E-PH8-HW-UNASSIGNED", "tenant-1", "Word Problems Worksheet", AssignmentType.Homework,
                AssignmentStatus.Published, teachT1, availableFrom: now.AddDays(-2), dueDate: now.AddDays(7), maxScore: 100);
            await EnsureAssignmentTarget("E2E-PH8-HWT-UNASSIGNED", "tenant-1", "E2E-PH8-HW-UNASSIGNED", AssignmentTargetType.Student, studentId: otherT1);

            await EnsureAssignment("E2E-PH8-HW-T2", "tenant-2", "Mathematics Homework", AssignmentType.Homework,
                AssignmentStatus.Published, teachT2, availableFrom: now.AddDays(-2), dueDate: now.AddDays(7), maxScore: 100);
            await EnsureAssignmentTarget("E2E-PH8-HWT-T2", "tenant-2", "E2E-PH8-HW-T2", AssignmentTargetType.Class, schoolClassId: "PH8-CLASS-T2");

            // ---- D quiz (tenant-1 published+assigned; cross-tenant published) ----
            await EnsureQuiz("E2E-PH8-QUIZ-T1", "tenant-1", "Algebra Quiz", "PH8-SUBJECT-T1", "PH8-LESSON-T1");
            await EnsureQuestion("E2E-PH8-Q1", "tenant-1", "E2E-PH8-QUIZ-T1", "2 + 2 = ?", QuestionType.MCQ, 1, 1);
            await EnsureOption("E2E-PH8-Q1-A", "tenant-1", "E2E-PH8-Q1", "3", false);
            await EnsureOption("E2E-PH8-Q1-B", "tenant-1", "E2E-PH8-Q1", "4", true);
            await EnsureOption("E2E-PH8-Q1-C", "tenant-1", "E2E-PH8-Q1", "5", false);
            await EnsureQuestion("E2E-PH8-Q2", "tenant-1", "E2E-PH8-QUIZ-T1", "A linear equation graphs a straight line.", QuestionType.TrueFalse, 2, 1);
            await EnsureOption("E2E-PH8-Q2-T", "tenant-1", "E2E-PH8-Q2", "True", true);
            await EnsureOption("E2E-PH8-Q2-F", "tenant-1", "E2E-PH8-Q2", "False", false);
            await EnsureQuestion("E2E-PH8-Q3", "tenant-1", "E2E-PH8-QUIZ-T1", "Explain how to solve x + 1 = 3.", QuestionType.Essay, 3, 2);
            await EnsureAssignment("E2E-PH8-QASSIGN-T1", "tenant-1", "Algebra Quiz", AssignmentType.Quiz,
                AssignmentStatus.Published, teachT1, availableFrom: now.AddDays(-2), dueDate: now.AddDays(7), quizId: "E2E-PH8-QUIZ-T1");
            await EnsureAssignmentTarget("E2E-PH8-QASSIGNT-T1", "tenant-1", "E2E-PH8-QASSIGN-T1", AssignmentTargetType.Class, schoolClassId: "PH8-CLASS-T1");

            await EnsureQuiz("E2E-PH8-QUIZ-T2", "tenant-2", "Mathematics Quiz", "PH8-SUBJECT-T2", "PH8-LESSON-T2");
            await EnsureQuestion("E2E-PH8-Q1-T2", "tenant-2", "E2E-PH8-QUIZ-T2", "1 + 1 = ?", QuestionType.MCQ, 1, 1);
            await EnsureOption("E2E-PH8-Q1-T2-A", "tenant-2", "E2E-PH8-Q1-T2", "2", true);
            await EnsureOption("E2E-PH8-Q1-T2-B", "tenant-2", "E2E-PH8-Q1-T2", "3", false);

            // ---- F progress / recommendations / engagement read models (tenant-1) ----
            await Ensure(_context.subjectProgresses, x => x.TenantId == "tenant-1" && x.StudentId == stuT1 && x.SubjectId == "PH8-SUBJECT-T1", () => new SubjectProgress
            {
                Id = "E2E-PH8-SP-T1", TenantId = "tenant-1", StudentId = stuT1, SubjectId = "PH8-SUBJECT-T1",
                CompletionPercentage = 40m, AverageScore = 78m, LessonsCompleted = 2, TotalLessons = 5, LastActivityAt = now.AddDays(-1)
            });
            await Ensure(_context.studentMetricHistories, x => x.Id == "E2E-PH8-METRIC-T1", () => new StudentMetricHistory
            {
                Id = "E2E-PH8-METRIC-T1", TenantId = "tenant-1", StudentId = stuT1, MetricType = ProgressMetricType.QuizScore,
                Value = 78m, MeasuredAt = now.AddDays(-3), Notes = "Algebra quiz score recorded"
            });
            await Ensure(_context.studentInsights, x => x.Id == "E2E-PH8-INSIGHT-T1", () => new StudentInsight
            {
                Id = "E2E-PH8-INSIGHT-T1", TenantId = "tenant-1", StudentId = stuT1, Performance = PerformanceLevel.OnTrack,
                ConfidenceScore = 0.8m, Summary = "Steady progress in algebra; keep practising word problems.",
                Period = InsightPeriod.Weekly, PeriodStart = now.AddDays(-7), PeriodEnd = now
            });
            await Ensure(_context.painPoints, x => x.Id == "E2E-PH8-PAIN-T1", () => new PainPoint
            {
                Id = "E2E-PH8-PAIN-T1", TenantId = "tenant-1", StudentId = stuT1, StudentInsightId = "E2E-PH8-INSIGHT-T1",
                Category = PainPointCategory.Skill, Title = "Solving multi-step equations", ConfidenceScore = 0.7m,
                DetectedAt = now.AddDays(-4), ReviewStatus = HumanReviewStatus.Approved
            });
            await Ensure(_context.studentRecommendations, x => x.Id == "E2E-PH8-REC-T1", () => new StudentRecommendation
            {
                Id = "E2E-PH8-REC-T1", TenantId = "tenant-1", StudentId = stuT1, StudentInsightId = "E2E-PH8-INSIGHT-T1",
                Title = "Practice linear equations", Body = "Complete 5 practice problems on linear equations this week.",
                Status = RecommendationStatus.Open, GeneratedAt = now.AddDays(-2)
            });

            // ---- H communities (tenant-1; seeded with an "other" member + post; STU-T1 not a member) ----
            await Ensure(_context.communities, x => x.Id == "E2E-PH8-COMM-T1", () => new Community
            {
                Id = "E2E-PH8-COMM-T1", TenantId = "tenant-1", Name = "Mathematics Club",
                Description = "A place to discuss math problems and share solutions.", Visibility = CommunityVisibility.TenantOnly
            });
            if (otherT1 is not null)
            {
                await Ensure(_context.communityMemberships, x => x.Id == "E2E-PH8-CM-OTHER-T1", () => new CommunityMembership
                {
                    Id = "E2E-PH8-CM-OTHER-T1", TenantId = "tenant-1", CommunityId = "E2E-PH8-COMM-T1", UserId = otherT1,
                    Role = CommunityMemberRole.Owner, JoinedAt = now.AddDays(-5)
                });
                await Ensure(_context.posts, x => x.Id == "E2E-PH8-POST-SEED-T1", () => new Post
                {
                    Id = "E2E-PH8-POST-SEED-T1", TenantId = "tenant-1", CommunityId = "E2E-PH8-COMM-T1", UserId = otherT1,
                    Content = "Welcome to the Mathematics Club! Share your favourite problems here.", CreatedAt = now.AddDays(-5)
                });
            }
            // Cross-tenant community (tenant-2) for H62 isolation.
            await Ensure(_context.communities, x => x.Id == "E2E-PH8-COMM-T2", () => new Community
            {
                Id = "E2E-PH8-COMM-T2", TenantId = "tenant-2", Name = "Science Club",
                Description = "Science enthusiasts at Al-Nahda STEM School.", Visibility = CommunityVisibility.TenantOnly
            });

            // ---- I competitions + leaderboard (tenant-1 active; cross-tenant) ----
            await Ensure(_context.competitions, x => x.Id == "E2E-PH8-COMP-T1", () => new Competition
            {
                Id = "E2E-PH8-COMP-T1", TenantId = "tenant-1", Title = "Mathematics Olympiad",
                Description = "Solve challenging problems to climb the leaderboard.", Status = CompetitionStatus.Active,
                StartsAt = now.AddDays(-1), EndsAt = now.AddDays(14)
            });
            if (otherT1 is not null)
            {
                // Leaderboard is computed from CompetitionEntry + CompetitionScore, so the
                // rival's standing must be seeded as a scored entry (not a LeaderboardEntry row).
                await Ensure(_context.competitionEntries, x => x.Id == "E2E-PH8-CE-OTHER-T1", () => new CompetitionEntry
                {
                    Id = "E2E-PH8-CE-OTHER-T1", TenantId = "tenant-1", CompetitionId = "E2E-PH8-COMP-T1", StudentId = otherT1,
                    EnteredAt = now.AddDays(-1)
                });
                await Ensure(_context.competitionScores, x => x.Id == "E2E-PH8-CS-OTHER-T1", () => new CompetitionScore
                {
                    Id = "E2E-PH8-CS-OTHER-T1", TenantId = "tenant-1", CompetitionEntryId = "E2E-PH8-CE-OTHER-T1",
                    Score = 95m, Rank = 1, ScoredAt = now.AddHours(-2)
                });
            }
            await Ensure(_context.competitions, x => x.Id == "E2E-PH8-COMP-T2", () => new Competition
            {
                Id = "E2E-PH8-COMP-T2", TenantId = "tenant-2", Title = "Science Challenge",
                Status = CompetitionStatus.Active, StartsAt = now.AddDays(-1), EndsAt = now.AddDays(14)
            });

            // ---- J office hours (tenant-1 open slot + a capacity-full slot) ----
            await Ensure(_context.officeHourSessions, x => x.Id == "E2E-PH8-OH-OPEN-T1", () => new OfficeHourSession
            {
                Id = "E2E-PH8-OH-OPEN-T1", TenantId = "tenant-1", TeacherId = teachT1, Title = "Mathematics Office Hour",
                StartsAt = now.AddDays(2), EndsAt = now.AddDays(2).AddHours(1), Capacity = 5, Status = OfficeHourStatus.Scheduled
            });
            await Ensure(_context.officeHourSessions, x => x.Id == "E2E-PH8-OH-FULL-T1", () => new OfficeHourSession
            {
                Id = "E2E-PH8-OH-FULL-T1", TenantId = "tenant-1", TeacherId = teachT1, Title = "Algebra Help Session",
                StartsAt = now.AddDays(3), EndsAt = now.AddDays(3).AddHours(1), Capacity = 1, Status = OfficeHourStatus.Scheduled
            });
            if (otherT1 is not null)
            {
                await Ensure(_context.officeHourBookings, x => x.Id == "E2E-PH8-OHB-FULL-OTHER", () => new OfficeHourBooking
                {
                    Id = "E2E-PH8-OHB-FULL-OTHER", TenantId = "tenant-1", OfficeHourSessionId = "E2E-PH8-OH-FULL-T1",
                    StudentId = otherT1, Status = OfficeHourBookingStatus.Confirmed, BookedAt = now.AddDays(-1)
                });
            }

            // ---- K announcements (tenant-1) ----
            await Ensure(_context.announcements, x => x.Id == "E2E-PH8-ANN-T1", () => new Announcement
            {
                Id = "E2E-PH8-ANN-T1", TenantId = "tenant-1", Title = "Welcome Back to School",
                Body = "Welcome to the new term! Please check your timetable and review this week's homework.", TargetAudience = TargetAudience.All,
                IsActive = true, CreatedAt = now.AddDays(-1)
            });

            // ---- M badges (platform catalog) + earned badge + streak (tenant-1) ----
            await Ensure(_context.badges, x => x.Id == "E2E-PH8-BADGE-1", () => new Badge
            {
                Id = "E2E-PH8-BADGE-1", Code = "PH8_FIRST_LESSON", Name = "First Lesson", Type = BadgeType.Achievement,
                Description = "Awarded for completing your first lesson."
            });
            await Ensure(_context.badges, x => x.Id == "E2E-PH8-BADGE-2", () => new Badge
            {
                Id = "E2E-PH8-BADGE-2", Code = "PH8_STREAK_3", Name = "3-Day Streak", Type = BadgeType.Streak,
                Description = "Awarded for a 3-day study streak."
            });
            await Ensure(_context.studentBadges, x => x.Id == "E2E-PH8-SB-T1", () => new StudentBadge
            {
                Id = "E2E-PH8-SB-T1", TenantId = "tenant-1", StudentId = stuT1, BadgeId = "E2E-PH8-BADGE-1",
                AwardedAt = now.AddDays(-2), AwardedReason = "Completed your first lesson"
            });
            // The streak fixture is owned by the supporting PH8-OTHER-T1 student, not
            // STU-T1: a pre-existing engagement integration test creates and tears down
            // STU-T1's streak, and StudentStreak is unique per (tenant, student). Keeping
            // the E2E streak on PH8-OTHER-T1 lets both coexist on the shared local DB.
            if (otherT1 is not null)
            {
                await Ensure(_context.studentStreaks, x => x.StudentId == otherT1, () => new StudentStreak
                {
                    Id = "E2E-PH8-STREAK-T1", TenantId = "tenant-1", StudentId = otherT1, CurrentCount = 3, LongestCount = 5,
                    LastActivityDate = now.AddDays(-1)
                });
            }

            // ---- K notifications: ensure fresh unread fixture notifications exist ----
            await EnsureE2ENotificationsAsync(stuT1);
        }

        private async Task EnsureEnrollmentSafe(string id, string tenantId, string? studentLoginCode, string classId, string academicYearId)
        {
            var sid = studentLoginCode is null ? null : await UserIdByLoginCodeAsync(studentLoginCode);
            if (sid is null) return;
            await EnsureEnrollment(id, tenantId, sid, classId, academicYearId);
        }

        private async Task EnsureAssignment(string id, string tenantId, string title, AssignmentType type, AssignmentStatus status,
            string? teacherId, DateTime? availableFrom = null, DateTime? dueDate = null, decimal? maxScore = null, string? subjectId = null, string? quizId = null, string? description = null)
        {
            await Ensure(_context.assignments, x => x.Id == id, () => new Assignment
            {
                Id = id, TenantId = tenantId, Title = title, Description = description, Type = type, Status = status,
                AvailableFrom = availableFrom, DueDate = dueDate, MaxScore = maxScore, SubjectId = subjectId,
                QuizId = quizId, AssignedByTeacherId = teacherId
            });
        }

        private async Task EnsureAssignmentTarget(string id, string tenantId, string assignmentId, AssignmentTargetType type,
            string? schoolClassId = null, string? studentId = null)
        {
            if (type == AssignmentTargetType.Student && studentId is null) return;
            await Ensure(_context.assignmentTargets, x => x.Id == id, () => new AssignmentTarget
            {
                Id = id, TenantId = tenantId, AssignmentId = assignmentId, TargetType = type,
                SchoolClassId = schoolClassId, StudentId = studentId
            });
        }

        private async Task EnsureQuiz(string id, string tenantId, string title, string? subjectId, string? lessonId)
        {
            await Ensure(_context.quizzes, x => x.Id == id, () => new Quiz
            {
                Id = id, TenantId = tenantId, Title = title, Status = QuizStatus.Published, Type = QuizType.Practice,
                TimeLimitMinutes = 30, MaxAttempts = null, SubjectId = subjectId, LessonId = lessonId, DueDate = null
            });
        }

        private async Task EnsureQuestion(string id, string tenantId, string quizId, string text, QuestionType type, int order, int points)
        {
            await Ensure(_context.questions, x => x.Id == id, () => new Question
            {
                Id = id, TenantId = tenantId, QuizId = quizId, Text = text, Type = type, Order = order, Points = points
            });
        }

        private async Task EnsureOption(string id, string tenantId, string questionId, string text, bool isCorrect)
        {
            await Ensure(_context.questionOptions, x => x.Id == id, () => new QuestionOption
            {
                Id = id, TenantId = tenantId, QuestionId = questionId, Text = text, IsCorrect = isCorrect
            });
        }

        private async Task EnsureE2ENotificationsAsync(string studentId)
        {
            // Fresh deterministic unread notifications for the K group. Re-created by
            // the reset; here we only ensure at least the two fixture rows exist.
            var notifs = new[]
            {
                ("E2E-PH8-NOTIF-1", "New homework assigned", "Your teacher assigned 'Linear Equations Practice Set'. It is due in 7 days."),
                ("E2E-PH8-NOTIF-2", "Quiz results published", "Your results for the 'Algebra Quiz' are now available to review."),
            };
            var idx = 1;
            foreach (var (nid, title, body) in notifs)
            {
                var n = nid; var i = idx; var ti = title; var bo = body;
                await Ensure(_context.notifications, x => x.Id == n, () => new Notification
                {
                    Id = n, TenantId = "tenant-1", UserId = studentId, Title = ti,
                    Body = bo, NotificationCategory = NotificationCategory.General,
                    NotificationType = NotificationType.System, IsRead = false, CreatedAt = DateTime.UtcNow.AddMinutes(-i)
                });
                idx++;
            }
        }

        // ---------------------------------------------------------------------
        // Development/Test-only reset of the mutable per-run state owned by the
        // Phase 8 E2E fixture actors (STU-T1, STU-T2). It NEVER drops the database
        // and NEVER touches non-fixture user data or the supporting PH8-OTHER-T1
        // seed (which keeps the capacity-full slot, leaderboard rival and seeded
        // community post in place). Callable only from the Development-guarded
        // dev endpoint.
        // ---------------------------------------------------------------------
        public async Task ResetE2EAcceptanceStateAsync()
        {
            var actorCodes = new[] { "STU-T1", "STU-T2" };
            var actorIds = new List<string>();
            foreach (var code in actorCodes)
            {
                var id = await UserIdByLoginCodeAsync(code);
                if (id is not null) actorIds.Add(id);
            }
            if (actorIds.Count == 0) return;

            // Quiz attempts + their answers.
            var attempts = await _context.quizSubmissions.IgnoreQueryFilters()
                .Where(s => actorIds.Contains(s.StudentId)).ToListAsync();
            var attemptIds = attempts.Select(a => a.Id).ToList();
            if (attemptIds.Count > 0)
            {
                var answers = await _context.submissionAnswers.IgnoreQueryFilters()
                    .Where(a => attemptIds.Contains(a.QuizSubmissionId)).ToListAsync();
                _context.submissionAnswers.RemoveRange(answers);
                _context.quizSubmissions.RemoveRange(attempts);
            }

            // Homework submissions.
            var subs = await _context.assignmentSubmissions.IgnoreQueryFilters()
                .Where(s => actorIds.Contains(s.StudentId)).ToListAsync();
            _context.assignmentSubmissions.RemoveRange(subs);

            // Community memberships / posts / comments authored by the actors.
            var memberships = await _context.communityMemberships.IgnoreQueryFilters()
                .Where(m => actorIds.Contains(m.UserId)).ToListAsync();
            _context.communityMemberships.RemoveRange(memberships);
            var actorPosts = await _context.posts.IgnoreQueryFilters()
                .Where(p => actorIds.Contains(p.UserId)).ToListAsync();
            var actorPostIds = actorPosts.Select(p => p.Id).ToList();
            var commentsOnActorPosts = actorPostIds.Count == 0 ? new List<PostComment>()
                : await _context.postComments.IgnoreQueryFilters().Where(c => actorPostIds.Contains(c.PostId)).ToListAsync();
            var actorComments = await _context.postComments.IgnoreQueryFilters()
                .Where(c => actorIds.Contains(c.UserId)).ToListAsync();
            _context.postComments.RemoveRange(commentsOnActorPosts.Concat(actorComments).Distinct());
            _context.posts.RemoveRange(actorPosts);

            // Competition entries (+ their scores) by the actors.
            var entries = await _context.competitionEntries.IgnoreQueryFilters()
                .Where(e => actorIds.Contains(e.StudentId)).ToListAsync();
            var entryIds = entries.Select(e => e.Id).ToList();
            if (entryIds.Count > 0)
            {
                var scores = await _context.competitionScores.IgnoreQueryFilters()
                    .Where(s => entryIds.Contains(s.CompetitionEntryId)).ToListAsync();
                _context.competitionScores.RemoveRange(scores);
            }
            _context.competitionEntries.RemoveRange(entries);

            // Office-hour bookings made by the actors (NOT the seeded PH8-OTHER-T1 one).
            var bookings = await _context.officeHourBookings.IgnoreQueryFilters()
                .Where(b => actorIds.Contains(b.StudentId)).ToListAsync();
            _context.officeHourBookings.RemoveRange(bookings);

            // Suggestions submitted by the actors.
            var suggestions = await _context.suggestions.IgnoreQueryFilters()
                .Where(s => actorIds.Contains(s.SubmittedByUserId)).ToListAsync();
            _context.suggestions.RemoveRange(suggestions);

            // Phase 15 CV-confirmed attendance for the actors: remove ComputerVision-source records
            // so the Phase 8 attendance fixture returns to its deterministic seeded (manual/import)
            // baseline regardless of test execution order. The seeded manual/import records are kept.
            var cvAttendance = await _context.studentAttendanceRecords.IgnoreQueryFilters()
                .Where(a => actorIds.Contains(a.StudentId) && a.Source == AttendanceSource.ComputerVision).ToListAsync();
            _context.studentAttendanceRecords.RemoveRange(cvAttendance);

            await _context.SaveChangesAsync();

            // Test-created communities & competitions accumulate run-over-run: the Phase 14 closure
            // flow seeds fresh ones each pass via the teacher API and never deletes them, so without a
            // full reset-local-db they pile up and crowd the deterministic seeded fixtures off the
            // Phase 8 H/I "eligible X are listed" surfaces. Remove every community/competition EXCEPT
            // the seeded Phase 8 fixtures (with their dependents) so the listings are stable run-over-run.
            // (reset-local-db remains the full hard reset; the seeded fixtures' own member/post/score
            // rows are kept because the fixture ids are retained.)
            var seededCommunityIds = new[] { "E2E-PH8-COMM-T1", "E2E-PH8-COMM-T2" };
            var staleCommunityIds = await _context.communities.IgnoreQueryFilters()
                .Where(c => !seededCommunityIds.Contains(c.Id)).Select(c => c.Id).ToListAsync();
            if (staleCommunityIds.Count > 0)
            {
                var stalePostIds = await _context.posts.IgnoreQueryFilters()
                    .Where(p => p.CommunityId != null && staleCommunityIds.Contains(p.CommunityId)).Select(p => p.Id).ToListAsync();
                if (stalePostIds.Count > 0)
                {
                    _context.postComments.RemoveRange(await _context.postComments.IgnoreQueryFilters().Where(c => stalePostIds.Contains(c.PostId)).ToListAsync());
                    _context.postReports.RemoveRange(await _context.postReports.IgnoreQueryFilters().Where(r => stalePostIds.Contains(r.PostId)).ToListAsync());
                    _context.posts.RemoveRange(await _context.posts.IgnoreQueryFilters().Where(p => stalePostIds.Contains(p.Id)).ToListAsync());
                }
                _context.communityMemberships.RemoveRange(await _context.communityMemberships.IgnoreQueryFilters().Where(m => staleCommunityIds.Contains(m.CommunityId)).ToListAsync());
                _context.communities.RemoveRange(await _context.communities.IgnoreQueryFilters().Where(c => staleCommunityIds.Contains(c.Id)).ToListAsync());
            }

            var seededCompetitionIds = new[] { "E2E-PH8-COMP-T1", "E2E-PH8-COMP-T2" };
            var staleCompetitionIds = await _context.competitions.IgnoreQueryFilters()
                .Where(c => !seededCompetitionIds.Contains(c.Id)).Select(c => c.Id).ToListAsync();
            if (staleCompetitionIds.Count > 0)
            {
                var staleEntryIds = await _context.competitionEntries.IgnoreQueryFilters()
                    .Where(e => staleCompetitionIds.Contains(e.CompetitionId)).Select(e => e.Id).ToListAsync();
                if (staleEntryIds.Count > 0)
                    _context.competitionScores.RemoveRange(await _context.competitionScores.IgnoreQueryFilters().Where(s => staleEntryIds.Contains(s.CompetitionEntryId)).ToListAsync());
                _context.competitionEntries.RemoveRange(await _context.competitionEntries.IgnoreQueryFilters().Where(e => staleCompetitionIds.Contains(e.CompetitionId)).ToListAsync());
                _context.competitionSubmissions.RemoveRange(await _context.competitionSubmissions.IgnoreQueryFilters().Where(s => staleCompetitionIds.Contains(s.CompetitionId)).ToListAsync());
                _context.leaderboardEntries.RemoveRange(await _context.leaderboardEntries.IgnoreQueryFilters().Where(l => staleCompetitionIds.Contains(l.CompetitionId)).ToListAsync());
                _context.competitions.RemoveRange(await _context.competitions.IgnoreQueryFilters().Where(c => staleCompetitionIds.Contains(c.Id)).ToListAsync());
            }

            await _context.SaveChangesAsync();

            // Reset the actor's lesson-progress on the fixture lesson so B16 (explicit
            // completion) can run fresh each pass.
            var progress = await _context.studentLessonProgresses.IgnoreQueryFilters()
                .Where(p => actorIds.Contains(p.StudentId) && p.LessonId == "PH8-LESSON-T1").ToListAsync();
            _context.studentLessonProgresses.RemoveRange(progress);
            await _context.SaveChangesAsync();

            // Fresh unread notifications for the K group: delete the fixture rows and
            // re-create them unread so unread-count and mark-read are deterministic.
            var stuT1 = await UserIdByLoginCodeAsync("STU-T1");
            if (stuT1 is not null)
            {
                var oldNotifs = await _context.notifications.IgnoreQueryFilters()
                    .Where(n => n.Id == "E2E-PH8-NOTIF-1" || n.Id == "E2E-PH8-NOTIF-2").ToListAsync();
                _context.notifications.RemoveRange(oldNotifs);
                await _context.SaveChangesAsync();
                await EnsureE2ENotificationsAsync(stuT1);
            }

            // ---- Phase 9: restore the AI quiz draft + clear test-created quiz ----
            // The Teacher Portal live matrix publishes/assigns the AI draft quiz, so
            // reset must put it back to AiGenerated and remove the assignment a test
            // created, keeping draft -> review -> publish -> assign repeatable.
            await ResetPhase9TeacherStateAsync();

            // ---- Phase 10: clear parent-created document requests ----
            // The Parent Portal live matrix submits document requests, so reset must
            // remove PARENT-T1's requests (+ their responses) to keep create repeatable.
            await ResetPhase10ParentStateAsync();

            // ---- Phase 11: clear admin-created links/assignments ----
            // The School Admin Portal live matrix creates a parent↔student link and a
            // teacher↔class assignment using DEDICATED Phase 11 actors. Reset removes
            // those rows so the create flows stay repeatable (and so the unique index
            // on the relationship/assignment never blocks a re-create).
            await ResetPhase11SchoolAdminStateAsync();

            // ---- Phase 12: restore platform-admin fixtures ----
            // The System Admin Portal live matrix suspends/reactivates a dedicated tenant
            // and responds to a seeded support ticket. Reset restores both to their initial
            // state so the lifecycle/handle-ticket flows stay repeatable.
            await ResetPhase12SystemAdminStateAsync();

            // ---- Phase 13: clear comms created during the run + restore deterministic notifs ----
            // The live matrix publishes announcements (which fan out notifications), starts a
            // conversation + posts a message, and toggles a notification preference. Reset removes
            // all PH13-tenant conversations/messages/announcements/notifications/preferences and
            // re-seeds STUDENT-A's deterministic unread set, keeping every flow repeatable.
            await ResetPhase13CommunicationStateAsync();
        }

        // =====================================================================
        // Phase 9 §Teacher Portal fixtures (Development/Test only). Deterministic
        // teacher assignment graph + an AI quiz draft for the live matrix:
        //   * TEACH-T1 is ACTIVELY assigned to PH8-CLASS-T1 and PH8-SUBJECT-T1.
        //   * TEACH-T9-UNASSIGNED is a same-tenant teacher with NO assignment
        //     (negative authorization actor — must get 403, not data).
        //   * TEACH-T2 is assigned to the tenant-2 class/subject (cross-tenant
        //     isolation actor — must never see tenant-1 scope).
        //   * E2E-PH9-DRAFT-T1 is an AI-generated DRAFT quiz (never auto-published)
        //     on the assigned subject, ready for review -> edit -> publish -> assign.
        // All helpers are idempotent. Mutable per-run state is restored by
        // ResetPhase9TeacherStateAsync (called from the E2E reset).
        // =====================================================================
        private async Task SeedPhase9TeacherPortalFixturesAsync()
        {
            var password = _configuration["Seed:DefaultPassword"] ?? "Local@Dev123";

            // Unassigned same-tenant teacher (negative actor).
            await EnsureUser<Teacher>("TEACH-T9-UNASSIGNED", "Rana Magdy", "tenant-1", "Teacher", password);

            var teachT1 = await UserIdByLoginCodeAsync("TEACH-T1");
            var teachT2 = await UserIdByLoginCodeAsync("TEACH-T2");
            if (teachT1 is null) return;

            // TEACH-T1 active assignments (class + subject) in tenant-1.
            await EnsureTeacherClassAssignment("PH9-TCA-T1", "tenant-1", teachT1, "PH8-CLASS-T1", "PH8-SUBJECT-T1");
            await EnsureTeacherSubjectAssignment("PH9-TSA-T1", "tenant-1", teachT1, "PH8-SUBJECT-T1");

            // TEACH-T2 active assignments in tenant-2 (cross-tenant isolation actor).
            if (teachT2 is not null)
            {
                await EnsureTeacherClassAssignment("PH9-TCA-T2", "tenant-2", teachT2, "PH8-CLASS-T2", "PH8-SUBJECT-T2");
                await EnsureTeacherSubjectAssignment("PH9-TSA-T2", "tenant-2", teachT2, "PH8-SUBJECT-T2");
            }

            // AI-generated quiz DRAFT on the assigned subject (review-gated; never published on seed).
            await EnsureDraftQuiz("E2E-PH9-DRAFT-T1", "tenant-1", "Linear Equations Review Quiz", "PH8-SUBJECT-T1", "PH8-LESSON-T1");
            await EnsureQuestion("E2E-PH9-DRAFT-Q1", "tenant-1", "E2E-PH9-DRAFT-T1", "What is 5 + 7?", QuestionType.MCQ, 1, 1);
            await EnsureOption("E2E-PH9-DRAFT-Q1-A", "tenant-1", "E2E-PH9-DRAFT-Q1", "12", true);
            await EnsureOption("E2E-PH9-DRAFT-Q1-B", "tenant-1", "E2E-PH9-DRAFT-Q1", "11", false);
            await EnsureOption("E2E-PH9-DRAFT-Q1-C", "tenant-1", "E2E-PH9-DRAFT-Q1", "13", false);
            await EnsureQuizGeneration("E2E-PH9-GEN-T1", "tenant-1", "E2E-PH9-DRAFT-T1");
        }

        private async Task EnsureTeacherClassAssignment(string id, string tenantId, string teacherId, string classId, string? subjectId)
        {
            await Ensure(_context.teacherClassAssignments, x => x.Id == id, () => new TeacherClassAssignment
            {
                Id = id, TenantId = tenantId, TeacherId = teacherId, SchoolClassId = classId, SubjectId = subjectId,
                Role = TeacherClassRole.SubjectTeacher, IsActive = true, ActiveFrom = DateTime.UtcNow.AddMonths(-1)
            });
        }

        private async Task EnsureTeacherSubjectAssignment(string id, string tenantId, string teacherId, string subjectId)
        {
            await Ensure(_context.teacherSubjectAssignments, x => x.Id == id, () => new TeacherSubjectAssignment
            {
                Id = id, TenantId = tenantId, TeacherId = teacherId, SubjectId = subjectId,
                IsActive = true, ActiveFrom = DateTime.UtcNow.AddMonths(-1)
            });
        }

        private async Task EnsureDraftQuiz(string id, string tenantId, string title, string? subjectId, string? lessonId)
        {
            await Ensure(_context.quizzes, x => x.Id == id, () => new Quiz
            {
                Id = id, TenantId = tenantId, Title = title, Status = QuizStatus.AiGenerated, Origin = QuizOrigin.AiGenerated,
                Type = QuizType.Practice, TimeLimitMinutes = 20, MaxAttempts = null, SubjectId = subjectId, LessonId = lessonId, DueDate = null
            });
        }

        private async Task EnsureQuizGeneration(string id, string tenantId, string quizId)
        {
            await Ensure(_context.quizGenerations, x => x.Id == id, () => new QuizGeneration
            {
                Id = id, TenantId = tenantId, QuizId = quizId, PromptUsed = "Generate a short review quiz from the Linear Equations lesson.",
                AiProvider = "fixture", AiModel = "fixture", ModelVersion = "fixture-v1", PromptVersion = "quiz.v1",
                Status = QuizGenerationStatus.Pending, GeneratedAt = DateTime.UtcNow.AddDays(-1)
            });
        }

        // Restore the Phase 9 AI draft quiz to its pristine review-gated state and
        // remove any quiz assignment a live test created for it.
        private async Task ResetPhase9TeacherStateAsync()
        {
            var draft = await _context.quizzes.IgnoreQueryFilters().FirstOrDefaultAsync(q => q.Id == "E2E-PH9-DRAFT-T1");
            if (draft is not null)
            {
                draft.Status = QuizStatus.AiGenerated;
                draft.Origin = QuizOrigin.AiGenerated;
                draft.ApprovedByTeacherId = null;
                draft.ApprovedAt = null;
                draft.ReviewedByTeacherId = null;
                draft.ReviewedAt = null;
            }

            // Remove assignments (+ targets) created for the draft quiz by tests.
            var asgs = await _context.assignments.IgnoreQueryFilters()
                .Where(a => a.QuizId == "E2E-PH9-DRAFT-T1").ToListAsync();
            var asgIds = asgs.Select(a => a.Id).ToList();
            if (asgIds.Count > 0)
            {
                var targets = await _context.assignmentTargets.IgnoreQueryFilters()
                    .Where(t => asgIds.Contains(t.AssignmentId)).ToListAsync();
                _context.assignmentTargets.RemoveRange(targets);
                _context.assignments.RemoveRange(asgs);
            }
            await _context.SaveChangesAsync();
        }

        // =====================================================================
        // Phase 10 §Parent Portal fixtures (Development/Test only). Deterministic
        // parent-student relationship graph for the live matrix + auth tests:
        //   * PH10-PARENT-T1 is LINKED to STU-T1 (CanViewProgress=true). STU-T1
        //     already has Phase 8 academic data (lessons, quiz attempts, attendance,
        //     stored insights), so the child has meaningful data to monitor. A
        //     DEDICATED parent is used (not the generic PARENT-T1) so the Phase 4
        //     AcademicDomainTests, which create+drop a temporary PARENT-T1<->STU-T1
        //     link, are not perturbed by a permanent seeded link on that triple.
        //   * PH10-PARENT-NOCHILD-T1 is a same-tenant parent with NO links
        //     (empty-state actor — must see zero children, not data).
        //   * DELIBERATELY no link for PH10-PARENT-T1 -> PH8-OTHER-T1 (same-tenant,
        //     unlinked -> must be 403) nor -> STU-T2 (cross-tenant -> 404; the
        //     same-tenant integrity DB trigger also forbids seeding such a link).
        // All helpers are idempotent. Parent-created document requests are removed by
        // ResetPhase10ParentStateAsync (called from the E2E reset).
        // =====================================================================
        private async Task SeedPhase10ParentPortalFixturesAsync()
        {
            var password = _configuration["Seed:DefaultPassword"] ?? "Local@Dev123";

            // Dedicated linked parent + a same-tenant parent with NO linked children.
            await EnsureUser<Parent>("PH10-PARENT-T1", "Hassan Fathy", "tenant-1", "Parent", password);
            await EnsureUser<Parent>("PH10-PARENT-NOCHILD-T1", "Dalia Sami", "tenant-1", "Parent", password);

            var parentT1 = await UserIdByLoginCodeAsync("PH10-PARENT-T1");
            var stuT1 = await UserIdByLoginCodeAsync("STU-T1");
            if (parentT1 is null || stuT1 is null) return;

            // PH10-PARENT-T1 actively linked to STU-T1, progress-permitted.
            await EnsureParentStudentLink("PH10-PSR-T1", "tenant-1", parentT1, stuT1);
        }

        private async Task EnsureParentStudentLink(string id, string tenantId, string parentId, string studentId)
        {
            await Ensure(_context.parentStudentRelationships, x => x.Id == id, () => new ParentStudentRelationship
            {
                Id = id,
                TenantId = tenantId,
                ParentId = parentId,
                StudentId = studentId,
                Relationship = GuardianRelationship.Father,
                IsPrimary = true,
                CanViewProgress = true,
                CanRequestDocuments = true,
                CanContactTeachers = true,
                IsActive = true,
                ActiveFrom = DateTime.UtcNow.AddMonths(-1)
            });
        }

        // Remove document requests (and their responses) created by PH10-PARENT-T1
        // during a live run, so the Parent Portal create-request flow stays
        // repeatable. No Phase 10 request fixtures are seeded, so clearing all of
        // that parent's requests is safe and deterministic on the local test database.
        private async Task ResetPhase10ParentStateAsync()
        {
            var parentT1 = await UserIdByLoginCodeAsync("PH10-PARENT-T1");
            if (parentT1 is null) return;

            var requests = await _context.parentRequests.IgnoreQueryFilters()
                .Where(r => r.ParentId == parentT1).ToListAsync();
            if (requests.Count == 0) return;

            var requestIds = requests.Select(r => r.Id).ToList();
            var responses = await _context.parentRequestResponses.IgnoreQueryFilters()
                .Where(x => requestIds.Contains(x.ParentRequestId)).ToListAsync();
            _context.parentRequestResponses.RemoveRange(responses);
            _context.parentRequests.RemoveRange(requests);
            await _context.SaveChangesAsync();
        }

        // =====================================================================
        // Phase 11 §School Admin Portal fixtures (Development/Test only).
        // Deterministic, DEDICATED actors so Phase 11 never perturbs Phase 8/9/10:
        //   * PH11-SCHOOLADMIN-T1 — tenant-1 SchoolAdmin (primary admin actor; the
        //     whole tenant-1 data set from Phases 8/9/10 gives a real dashboard).
        //   * PH11-SCHOOLADMIN-T2 — tenant-2 SchoolAdmin (cross-tenant isolation
        //     actor — must only ever see tenant-2 data).
        //   * PH11-PARENT-T1 + PH11-STUDENT-T1 — an UNLINKED same-tenant parent and
        //     student in tenant-1, used by the live matrix to create a parent↔student
        //     link (repeatable: the row is deleted by reset).
        //   * PH11-TEACHER-T1 + PH11-CLASS-T1 — an UNASSIGNED same-tenant teacher and a
        //     class in tenant-1, used to create a teacher↔class assignment (repeatable).
        // The relationships/assignments LISTS are already non-empty deterministically
        // via the seeded PH10-PSR-T1 link and PH9-TCA-T1 assignment in tenant-1.
        // All helpers are idempotent. Mutable per-run rows are cleared by
        // ResetPhase11SchoolAdminStateAsync (called from the E2E reset).
        // =====================================================================
        private async Task SeedPhase11SchoolAdminPortalFixturesAsync()
        {
            var password = _configuration["Seed:DefaultPassword"] ?? "Local@Dev123";

            await EnsureUser<SchoolAdmin>("PH11-SCHOOLADMIN-T1", "Walid Abdel Rahman", "tenant-1", "SchoolAdmin", password);
            await EnsureUser<SchoolAdmin>("PH11-SCHOOLADMIN-T2", "Nour El-Sayed", "tenant-2", "SchoolAdmin", password);

            // Unlinked parent + student in tenant-1 for the create-relationship flow.
            await EnsureUser<Parent>("PH11-PARENT-T1", "Ayman Sobhy", "tenant-1", "Parent", password);
            await EnsureUser<Student>("PH11-STUDENT-T1", "Omar Khaled", "tenant-1", "Student", password, gradeId: "G7-ID");

            // Unassigned teacher + a class in tenant-1 for the create-assignment flow.
            await EnsureUser<Teacher>("PH11-TEACHER-T1", "Ghada Talaat", "tenant-1", "Teacher", password);
            await EnsureSchoolClass("PH11-CLASS-T1", "Grade 7 - C", "PH11C1", "tenant-1", "G7-ID", "PH8-AY-T1");
        }

        // Remove the parent↔student links and teacher↔class assignments created during a
        // live run by the dedicated Phase 11 actors, so the create flows stay repeatable.
        // Rows are DELETED (not just deactivated) because the unique indexes on these
        // tables would otherwise block re-creating the same pair.
        private async Task ResetPhase11SchoolAdminStateAsync()
        {
            var parentT1 = await UserIdByLoginCodeAsync("PH11-PARENT-T1");
            if (parentT1 is not null)
            {
                var links = await _context.parentStudentRelationships.IgnoreQueryFilters()
                    .Where(r => r.ParentId == parentT1).ToListAsync();
                if (links.Count > 0) _context.parentStudentRelationships.RemoveRange(links);
            }

            var teacherT1 = await UserIdByLoginCodeAsync("PH11-TEACHER-T1");
            if (teacherT1 is not null)
            {
                var assignments = await _context.teacherClassAssignments.IgnoreQueryFilters()
                    .Where(a => a.TeacherId == teacherT1).ToListAsync();
                if (assignments.Count > 0) _context.teacherClassAssignments.RemoveRange(assignments);
            }

            await _context.SaveChangesAsync();
        }

        // =====================================================================
        // Phase 12 §System Admin (platform) Portal fixtures (Development/Test only).
        // Deterministic platform-admin actors + lifecycle/support targets for the live
        // matrix:
        //   * PH12-SYSADMIN — a dedicated platform SystemAdmin (no tenant) used by the
        //     live Playwright matrix (kept separate from SYS-1 to avoid coupling).
        //   * PH12-TENANT-SUSPEND ("Phase 12 Lifecycle Tenant", Active) — the tenant the
        //     suspend → reactivate flow drives (reset restores it to Active).
        //   * PH12-SUPPORT-REQ-T1 — a Pending support ticket in tenant-1 (owner STU-T1)
        //     for the handle-support-ticket flow (reset restores it to Pending).
        // The tenants list and support inbox are therefore non-empty deterministically.
        // The onboarding flow creates its OWN uniquely-id'd tenant per run, so it needs no
        // reset. All helpers are idempotent.
        // =====================================================================
        private async Task SeedPhase12SystemAdminPortalFixturesAsync()
        {
            var password = _configuration["Seed:DefaultPassword"] ?? "Local@Dev123";

            // Dedicated platform SystemAdmin (TenantId stays null).
            await EnsureUser<SystemAdmin>("PH12-SYSADMIN", "Sameh Victor", null, "SystemAdmin", password);

            // Real subscription-plan definitions (platform-owned catalog) so the Plans page and the
            // onboarding assign-plan step operate on genuine data (no fabricated plans in the UI).
            await EnsurePlan("PH12-PLAN-FREE", "FREE", "Free", SubscriptionPlan.Free, 0m, 50, 5, 1024, 100);
            await EnsurePlan("PH12-PLAN-PRO", "PRO", "Pro", SubscriptionPlan.Pro, 49m, 1000, 100, 10240, 5000);

            // A dedicated Active tenant the live matrix can suspend then reactivate.
            await EnsureTenant("PH12-TENANT-SUSPEND", "Rosetta Modern School", TenantStatus.Active);

            // A Pending support ticket in tenant-1 for the handle-support-ticket flow.
            await EnsureSupportRequest("PH12-SUPPORT-REQ-T1", "tenant-1", "STU-T1",
                "I can't open my child's report card. Please assist.");
        }

        // Restore the Phase 12 lifecycle tenant to Active and the seeded support ticket to
        // Pending so the suspend/reactivate and handle-ticket flows stay repeatable.
        private async Task ResetPhase12SystemAdminStateAsync()
        {
            var tenant = await _context.tenants.IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == "PH12-TENANT-SUSPEND");
            if (tenant is not null && tenant.Status != TenantStatus.Active)
            {
                tenant.Status = TenantStatus.Active;
            }

            var ticket = await _context.supportRequests.IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Id == "PH12-SUPPORT-REQ-T1");
            if (ticket is not null &&
                (ticket.Status != RequestStatus.Pending || ticket.ResponseMessage != null || ticket.RespondedAt != null))
            {
                ticket.Status = RequestStatus.Pending;
                ticket.ResponseMessage = null;
                ticket.RespondedAt = null;
            }

            await _context.SaveChangesAsync();
        }

        // Idempotent seed of a platform-owned subscription-plan definition.
        private async Task EnsurePlan(string id, string code, string name, SubscriptionPlan tier, decimal price,
            int maxStudents, int maxTeachers, int maxStorageMb, int maxAi)
        {
            var exists = await _context.subscriptionPlanDefinitions.IgnoreQueryFilters().AnyAsync(p => p.Id == id);
            if (!exists)
            {
                _context.subscriptionPlanDefinitions.Add(new SubscriptionPlanDefinition
                {
                    Id = id, Code = code, Name = name, Tier = tier, BillingPeriod = BillingPeriod.Monthly,
                    Price = price, Currency = "USD", MaxStudents = maxStudents, MaxTeachers = maxTeachers,
                    MaxStorageMb = maxStorageMb, MaxAiGenerationsPerMonth = maxAi, TrialDays = 14, IsActive = true
                });
                await _context.SaveChangesAsync();
            }
        }

        // =====================================================================
        // Phase 13 §Messaging, Notifications & Real-Time fixtures (Dev/Test only).
        // A DEDICATED small tenant (PH13-TENANT) so announcement fan-out is bounded
        // and deterministic (the Phase-3 seed inflates tenant-1 to ~1940 students):
        //   * PH13-ADMIN — publishes announcements + triggers notifications.
        //   * PH13-STUDENT-A — targeted recipient; enrolled in PH13-CLASS so the
        //     teacher may message them; carries a deterministic unread notif set.
        //   * PH13-STUDENT-B — NOT enrolled (non-participant) and used by the
        //     preference-suppression test (it disables the Announcement category).
        //   * PH13-TEACHER — assigned to PH13-CLASS (messaging actor; NOT a target
        //     of a Students-only announcement → proves recipients-only routing).
        //   * PH13-PARENT — non-targeted role.
        // All helpers are idempotent; per-run rows are cleared by
        // ResetPhase13CommunicationStateAsync (called from the E2E reset).
        // =====================================================================
        private async Task SeedPhase13CommunicationFixturesAsync()
        {
            var password = _configuration["Seed:DefaultPassword"] ?? "Local@Dev123";

            await EnsureTenant("PH13-TENANT", "Pyramids International School", TenantStatus.Active);

            await EnsureUser<SchoolAdmin>("PH13-ADMIN", "Heba Rashad", "PH13-TENANT", "SchoolAdmin", password);
            await EnsureUser<Teacher>("PH13-TEACHER", "Ziad Mostafa", "PH13-TENANT", "Teacher", password);
            await EnsureUser<Student>("PH13-STUDENT-A", "Mariam Adel", "PH13-TENANT", "Student", password, gradeId: "G7-ID");
            await EnsureUser<Student>("PH13-STUDENT-B", "Kareem Wael", "PH13-TENANT", "Student", password, gradeId: "G7-ID");
            await EnsureUser<Parent>("PH13-PARENT", "Sara Lamloum", "PH13-TENANT", "Parent", password);

            // Minimal academic structure so teacher↔student messaging is allowed for STUDENT-A only.
            // schoolClasses carries a COMPOSITE FK (TenantId, GradeId) → grades, so the class needs a
            // grade that lives in PH13-TENANT (the shared G7-ID belongs to another tenant).
            await EnsureGrade("PH13-G7", "Grade 7", "PH13-TENANT");
            await EnsureAcademicYear("PH13-AY", "Academic Year 2030/2031", "PH13AY", "PH13-TENANT");
            await EnsureSchoolClass("PH13-CLASS", "Grade 7 - A", "PH13C1", "PH13-TENANT", "PH13-G7", "PH13-AY");

            var teacherId = await UserIdByLoginCodeAsync("PH13-TEACHER");
            var studentAId = await UserIdByLoginCodeAsync("PH13-STUDENT-A");
            if (teacherId is not null)
                await EnsureTeacherClassAssignment("PH13-TCA", "PH13-TENANT", teacherId, "PH13-CLASS", null);
            if (studentAId is not null)
            {
                await EnsureEnrollment("PH13-ENR-A", "PH13-TENANT", studentAId, "PH13-CLASS", "PH13-AY");
                await EnsurePhase13NotificationsAsync(studentAId);
            }
        }

        // Deterministic unread notifications for the Phase 13 notification-center / unread-count /
        // mark-read tests, owned by PH13-STUDENT-A. Re-created by reset.
        private async Task EnsurePhase13NotificationsAsync(string studentId)
        {
            var notifs = new[]
            {
                ("PH13-NOTIF-1", "New message from your teacher", "You have a new message about this week's lesson."),
                ("PH13-NOTIF-2", "School announcement posted", "A new announcement has been posted for your class."),
            };
            var idx = 1;
            foreach (var (nid, title, body) in notifs)
            {
                var n = nid; var i = idx; var ti = title; var bo = body;
                await Ensure(_context.notifications, x => x.Id == n, () => new Notification
                {
                    Id = n, TenantId = "PH13-TENANT", UserId = studentId, Title = ti,
                    Body = bo, NotificationCategory = NotificationCategory.General,
                    NotificationType = NotificationType.System, IsRead = false, CreatedAt = DateTime.UtcNow.AddMinutes(-i)
                });
                idx++;
            }
        }

        // Remove every PH13-tenant conversation/message/announcement/notification/preference created during a
        // live run, then re-seed STUDENT-A's deterministic unread set so all Phase 13 flows stay repeatable.
        private async Task ResetPhase13CommunicationStateAsync()
        {
            var convs = await _context.conversations.IgnoreQueryFilters().Where(c => c.TenantId == "PH13-TENANT").ToListAsync();
            var convIds = convs.Select(c => c.Id).ToList();
            if (convIds.Count > 0)
            {
                var msgs = await _context.messages.IgnoreQueryFilters().Where(m => convIds.Contains(m.ConversationId)).ToListAsync();
                var msgIds = msgs.Select(m => m.Id).ToList();
                if (msgIds.Count > 0)
                {
                    var receipts = await _context.messageReadReceipts.IgnoreQueryFilters().Where(r => msgIds.Contains(r.MessageId)).ToListAsync();
                    var atts = await _context.messageAttachments.IgnoreQueryFilters().Where(a => msgIds.Contains(a.MessageId)).ToListAsync();
                    _context.messageReadReceipts.RemoveRange(receipts);
                    _context.messageAttachments.RemoveRange(atts);
                }
                var parts = await _context.conversationParticipants.IgnoreQueryFilters().Where(p => convIds.Contains(p.ConversationId)).ToListAsync();
                _context.messages.RemoveRange(msgs);
                _context.conversationParticipants.RemoveRange(parts);
                _context.conversations.RemoveRange(convs);
            }

            var anns = await _context.announcements.IgnoreQueryFilters().Where(a => a.TenantId == "PH13-TENANT").ToListAsync();
            _context.announcements.RemoveRange(anns);
            var notifs = await _context.notifications.IgnoreQueryFilters().Where(n => n.TenantId == "PH13-TENANT").ToListAsync();
            _context.notifications.RemoveRange(notifs);
            var prefs = await _context.notificationPreferences.IgnoreQueryFilters().Where(p => p.TenantId == "PH13-TENANT").ToListAsync();
            _context.notificationPreferences.RemoveRange(prefs);
            await _context.SaveChangesAsync();

            var studentAId = await UserIdByLoginCodeAsync("PH13-STUDENT-A");
            if (studentAId is not null)
            {
                await EnsurePhase13NotificationsAsync(studentAId);
                await _context.SaveChangesAsync();
            }
        }

        // Idempotent seed of a support request with a deterministic id, owned by a seeded user.
        private async Task EnsureSupportRequest(string id, string tenantId, string ownerLoginCode, string message)
        {
            var ownerId = await UserIdByLoginCodeAsync(ownerLoginCode);
            if (ownerId is null) return; // owner not seeded yet — skip (defensive)

            var existing = await _context.supportRequests.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == id);
            if (existing is null)
            {
                _context.supportRequests.Add(new SupportRequest
                {
                    Id = id,
                    TenantId = tenantId,
                    UserId = ownerId,
                    Type = RequestType.TechnicalSupport,
                    Status = RequestStatus.Pending,
                    Message = message,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
        }
    }
}
