using DerasaX.Domain.Entities.Models;
using DerasaX.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DerasaX.Api.SeedData
{
    /// <summary>
    /// Development/local "showcase" seed data. Gives the natural local demo logins a
    /// realistic, richly-connected experience so no main-role dashboard lands empty
    /// after a reset + reseed:
    ///   * Omar Ahmed (ST-001 / login "omar.ahmed") — the primary demo student. Has
    ///     homework in every visible category (assigned, submitted, graded, late,
    ///     missing), available + completed quizzes, subject progress, attendance,
    ///     notifications, badges and a streak.
    ///   * Nada Ashraf (ST-002) — a second classmate with lighter data.
    ///   * Malak Hassan (login "malak") — the demo teacher who owns the showcase class,
    ///     assignments and quizzes (a non-empty teacher dashboard + grading queue).
    ///   * Magdy Ahmed (login "PARENT-DEMO") — the demo parent, linked to both children
    ///     (a non-empty parent dashboard).
    ///   * Nabil Sherif (login "admin") — the tenant-1 SchoolAdmin (already sees the
    ///     whole tenant aggregate).
    ///
    /// These actors are deliberately NOT the Phase 8 E2E reset actors (STU-T1/STU-T2),
    /// and the showcase students sit in their own class (SHOW-CLASS-T1), so this data
    /// never perturbs the deterministic E2E fixtures and is never wiped by the E2E
    /// reset. Every helper is idempotent (Ensure-by-id), so repeated startups are safe.
    /// </summary>
    public partial class DataSeederService
    {
        private async Task SeedShowcaseFixturesAsync()
        {
            var password = _configuration["Seed:DefaultPassword"] ?? "Local@Dev123";
            var now = DateTime.UtcNow;

            var omar = await UserIdByLoginCodeAsync("ST-001");   // Omar Ahmed
            var nada = await UserIdByLoginCodeAsync("ST-002");   // Nada Ashraf
            var malak = await UserIdByLoginCodeAsync("TEACH002"); // Malak Hassan
            if (omar is null || malak is null) return; // base demo accounts not present — skip defensively

            // ---- Demo parent linked to both children ----
            await EnsureUser<Parent>("PARENT-DEMO", "Magdy Ahmed", "tenant-1", "Parent", password);
            var parent = await UserIdByLoginCodeAsync("PARENT-DEMO");

            // ---- Showcase class taught by Malak; both demo students enrolled ----
            await EnsureSchoolClass("SHOW-CLASS-T1", "Grade 7 - Nile", "SHOWC1", "tenant-1", "G7-ID", "PH8-AY-T1");
            await EnsureTeacherClassAssignment("SHOW-TCA-T1", "tenant-1", malak, "SHOW-CLASS-T1", "PH8-SUBJECT-T1");
            await EnsureTeacherSubjectAssignment("SHOW-TSA-T1", "tenant-1", malak, "PH8-SUBJECT-T1");
            await EnsureEnrollment("SHOW-ENR-OMAR", "tenant-1", omar, "SHOW-CLASS-T1", "PH8-AY-T1");
            if (nada is not null)
                await EnsureEnrollment("SHOW-ENR-NADA", "tenant-1", nada, "SHOW-CLASS-T1", "PH8-AY-T1");

            if (parent is not null)
            {
                await EnsureParentStudentLink("SHOW-PSR-OMAR", "tenant-1", parent, omar);
                if (nada is not null)
                    await EnsureParentStudentLink("SHOW-PSR-NADA", "tenant-1", parent, nada);
            }

            // =================================================================
            // Homework — Omar gets >= 3 records in every visible category, all
            // targeted at the showcase class (so STU-T1 in PH8-CLASS-T1 never sees
            // them). Each tuple is (idSuffix, title, dueOffsetDays, kind).
            //   kind: "assigned"  -> published, future due, NO submission   (Pending)
            //         "submitted" -> submitted, NOT graded                  (Submitted)
            //         "graded"    -> submitted + graded with a score        (Graded)
            //         "late"      -> submitted after the due date           (Late)
            //         "missing"   -> past due, NO submission                (Overdue)
            // =================================================================
            var homework = new (string id, string title, int dueDays, string kind, int score)[]
            {
                ("HW-A1", "Fractions and Decimals", 5, "assigned", 0),
                ("HW-A2", "Order of Operations Drill", 6, "assigned", 0),
                ("HW-A3", "Coordinate Plane Basics", 8, "assigned", 0),
                ("HW-S1", "Geometry Basics Worksheet", 2, "submitted", 0),
                ("HW-S2", "Ratios and Proportions", 3, "submitted", 0),
                ("HW-S3", "Integers Practice", 1, "submitted", 0),
                ("HW-G1", "Algebra Worksheet 1", -3, "graded", 18),
                ("HW-G2", "Number Patterns", -5, "graded", 15),
                ("HW-G3", "Linear Equations Set 2", -7, "graded", 20),
                ("HW-L1", "Word Problems Set 2", -2, "late", 0),
                ("HW-L2", "Perimeter and Area", -4, "late", 0),
                ("HW-L3", "Data and Graphs", -6, "late", 0),
                ("HW-M1", "Reading Reflection", -2, "missing", 0),
                ("HW-M2", "Times Tables Review", -8, "missing", 0),
            };

            foreach (var (suffix, title, dueDays, kind, score) in homework)
            {
                var asgId = $"SHOW-{suffix}";
                await EnsureAssignment(asgId, "tenant-1", title, AssignmentType.Homework,
                    AssignmentStatus.Published, malak,
                    availableFrom: now.AddDays(-10), dueDate: now.AddDays(dueDays),
                    maxScore: 20, subjectId: "PH8-SUBJECT-T1",
                    description: $"{title} — complete every problem and show your full working.");
                await EnsureAssignmentTarget($"SHOW-{suffix}-T", "tenant-1", asgId, AssignmentTargetType.Class, schoolClassId: "SHOW-CLASS-T1");

                switch (kind)
                {
                    case "submitted":
                        await EnsureAssignmentSubmission($"SHOW-{suffix}-SUB", "tenant-1", asgId, omar,
                            "My completed answers are attached.", SubmissionStatus.Submitted,
                            submittedAt: now.AddDays(dueDays - 1));
                        break;
                    case "graded":
                        await EnsureAssignmentSubmission($"SHOW-{suffix}-SUB", "tenant-1", asgId, omar,
                            "My completed answers are attached.", SubmissionStatus.Graded,
                            submittedAt: now.AddDays(dueDays - 1), score: score,
                            gradedAt: now.AddDays(dueDays + 1), gradedBy: malak,
                            feedback: "Good work — review the steps you skipped on the last question.");
                        break;
                    case "late":
                        await EnsureAssignmentSubmission($"SHOW-{suffix}-SUB", "tenant-1", asgId, omar,
                            "Sorry this is late — here are my answers.", SubmissionStatus.Late,
                            submittedAt: now.AddDays(dueDays + 1));
                        break;
                    // "assigned" / "missing" -> intentionally no submission row.
                }
            }

            // A couple of submitted/graded items for Nada so the teacher's grading
            // queue and the parent's second child are not empty either.
            if (nada is not null)
            {
                await EnsureAssignment("SHOW-HW-N1", "tenant-1", "Algebra Worksheet 1", AssignmentType.Homework,
                    AssignmentStatus.Published, malak, availableFrom: now.AddDays(-10), dueDate: now.AddDays(-3),
                    maxScore: 20, subjectId: "PH8-SUBJECT-T1");
                await EnsureAssignmentTarget("SHOW-HW-N1-T", "tenant-1", "SHOW-HW-N1", AssignmentTargetType.Class, schoolClassId: "SHOW-CLASS-T1");
                await EnsureAssignmentSubmission("SHOW-HW-N1-SUB", "tenant-1", "SHOW-HW-N1", nada,
                    "Nada's answers.", SubmissionStatus.Graded, submittedAt: now.AddDays(-4), score: 19,
                    gradedAt: now.AddDays(-2), gradedBy: malak, feedback: "Excellent — full marks on most questions.");
            }

            // =================================================================
            // Quizzes — one completed (with a scored attempt + answers) and one still
            // available, both published and assigned to the showcase class.
            // =================================================================
            // 1) Completed quiz: 3 objective questions, Omar scores 2/3.
            await EnsureQuiz("SHOW-QUIZ-1", "tenant-1", "Algebra Quiz 1", "PH8-SUBJECT-T1", "PH8-LESSON-T1");
            await EnsureQuestion("SHOW-Q1", "tenant-1", "SHOW-QUIZ-1", "What is the value of x in x + 5 = 12?", QuestionType.MCQ, 1, 1);
            await EnsureOption("SHOW-Q1-A", "tenant-1", "SHOW-Q1", "5", false);
            await EnsureOption("SHOW-Q1-B", "tenant-1", "SHOW-Q1", "7", true);
            await EnsureOption("SHOW-Q1-C", "tenant-1", "SHOW-Q1", "17", false);
            await EnsureQuestion("SHOW-Q2", "tenant-1", "SHOW-QUIZ-1", "Which of these is a linear equation?", QuestionType.MCQ, 2, 1);
            await EnsureOption("SHOW-Q2-A", "tenant-1", "SHOW-Q2", "y = 2x + 1", true);
            await EnsureOption("SHOW-Q2-B", "tenant-1", "SHOW-Q2", "y = x squared", false);
            await EnsureOption("SHOW-Q2-C", "tenant-1", "SHOW-Q2", "y = 1 / x", false);
            await EnsureQuestion("SHOW-Q3", "tenant-1", "SHOW-QUIZ-1", "A linear equation always graphs as a straight line.", QuestionType.TrueFalse, 3, 1);
            await EnsureOption("SHOW-Q3-T", "tenant-1", "SHOW-Q3", "True", true);
            await EnsureOption("SHOW-Q3-F", "tenant-1", "SHOW-Q3", "False", false);
            await EnsureAssignment("SHOW-QASSIGN-1", "tenant-1", "Algebra Quiz 1", AssignmentType.Quiz,
                AssignmentStatus.Published, malak, availableFrom: now.AddDays(-7), dueDate: now.AddDays(7), quizId: "SHOW-QUIZ-1");
            await EnsureAssignmentTarget("SHOW-QASSIGN-1-T", "tenant-1", "SHOW-QASSIGN-1", AssignmentTargetType.Class, schoolClassId: "SHOW-CLASS-T1");

            // Omar's completed, auto-graded attempt (2 of 3 correct).
            await EnsureQuizSubmission("SHOW-QS-OMAR", "tenant-1", "SHOW-QUIZ-1", omar,
                achievedScore: 2, totalScore: 3, startedAt: now.AddDays(-2).AddMinutes(-20),
                submittedAt: now.AddDays(-2), gradedAt: now.AddDays(-2), assignmentId: "SHOW-QASSIGN-1");
            await EnsureSubmissionAnswer("SHOW-SA-O1", "tenant-1", "SHOW-QS-OMAR", "SHOW-Q1", "SHOW-Q1-B", correct: true, points: 1);
            await EnsureSubmissionAnswer("SHOW-SA-O2", "tenant-1", "SHOW-QS-OMAR", "SHOW-Q2", "SHOW-Q2-A", correct: true, points: 1);
            await EnsureSubmissionAnswer("SHOW-SA-O3", "tenant-1", "SHOW-QS-OMAR", "SHOW-Q3", "SHOW-Q3-F", correct: false, points: 0);

            // 2) Available quiz Omar has not attempted yet.
            await EnsureQuiz("SHOW-QUIZ-2", "tenant-1", "Geometry Quiz", "PH8-SUBJECT-T1", "PH8-LESSON-T1");
            await EnsureQuestion("SHOW-Q4", "tenant-1", "SHOW-QUIZ-2", "How many degrees are in a right angle?", QuestionType.MCQ, 1, 1);
            await EnsureOption("SHOW-Q4-A", "tenant-1", "SHOW-Q4", "45", false);
            await EnsureOption("SHOW-Q4-B", "tenant-1", "SHOW-Q4", "90", true);
            await EnsureOption("SHOW-Q4-C", "tenant-1", "SHOW-Q4", "180", false);
            await EnsureAssignment("SHOW-QASSIGN-2", "tenant-1", "Geometry Quiz", AssignmentType.Quiz,
                AssignmentStatus.Published, malak, availableFrom: now.AddDays(-1), dueDate: now.AddDays(10), quizId: "SHOW-QUIZ-2");
            await EnsureAssignmentTarget("SHOW-QASSIGN-2-T", "tenant-1", "SHOW-QASSIGN-2", AssignmentTargetType.Class, schoolClassId: "SHOW-CLASS-T1");

            // =================================================================
            // Progress / insights / attendance / notifications / badges for Omar.
            // =================================================================
            await Ensure(_context.subjectProgresses, x => x.Id == "SHOW-SP-OMAR", () => new SubjectProgress
            {
                Id = "SHOW-SP-OMAR", TenantId = "tenant-1", StudentId = omar, SubjectId = "PH8-SUBJECT-T1",
                CompletionPercentage = 62m, AverageScore = 84m, LessonsCompleted = 8, TotalLessons = 13, LastActivityAt = now.AddDays(-1)
            });
            await Ensure(_context.studentMetricHistories, x => x.Id == "SHOW-MET-OMAR-1", () => new StudentMetricHistory
            {
                Id = "SHOW-MET-OMAR-1", TenantId = "tenant-1", StudentId = omar, MetricType = ProgressMetricType.QuizScore,
                Value = 67m, MeasuredAt = now.AddDays(-14), Notes = "Algebra Quiz 1"
            });
            await Ensure(_context.studentMetricHistories, x => x.Id == "SHOW-MET-OMAR-2", () => new StudentMetricHistory
            {
                Id = "SHOW-MET-OMAR-2", TenantId = "tenant-1", StudentId = omar, MetricType = ProgressMetricType.QuizScore,
                Value = 84m, MeasuredAt = now.AddDays(-3), Notes = "Homework average"
            });
            await Ensure(_context.studentInsights, x => x.Id == "SHOW-INS-OMAR", () => new StudentInsight
            {
                Id = "SHOW-INS-OMAR", TenantId = "tenant-1", StudentId = omar, Performance = PerformanceLevel.OnTrack,
                ConfidenceScore = 0.82m, Summary = "Strong in algebra; word problems need more practice.",
                Period = InsightPeriod.Weekly, PeriodStart = now.AddDays(-7), PeriodEnd = now
            });
            await Ensure(_context.painPoints, x => x.Id == "SHOW-PAIN-OMAR", () => new PainPoint
            {
                Id = "SHOW-PAIN-OMAR", TenantId = "tenant-1", StudentId = omar, StudentInsightId = "SHOW-INS-OMAR",
                Category = PainPointCategory.Skill, Title = "Translating word problems into equations", ConfidenceScore = 0.71m,
                DetectedAt = now.AddDays(-4), ReviewStatus = HumanReviewStatus.Approved
            });
            await Ensure(_context.studentRecommendations, x => x.Id == "SHOW-REC-OMAR", () => new StudentRecommendation
            {
                Id = "SHOW-REC-OMAR", TenantId = "tenant-1", StudentId = omar, StudentInsightId = "SHOW-INS-OMAR",
                Title = "Practice word problems", Body = "Work through 5 word problems and write the equation for each before solving.",
                Status = RecommendationStatus.Open, GeneratedAt = now.AddDays(-2)
            });

            // Attendance across several days (present / late / absent).
            await EnsureAttendance("SHOW-ATT-OMAR-1", "tenant-1", omar, "SHOW-CLASS-T1", now.AddDays(-1).ToString("yyyy-MM-dd"), AttendanceStatus.Present, AttendanceSource.Manual, "day");
            await EnsureAttendance("SHOW-ATT-OMAR-2", "tenant-1", omar, "SHOW-CLASS-T1", now.AddDays(-2).ToString("yyyy-MM-dd"), AttendanceStatus.Present, AttendanceSource.Manual, "day");
            await EnsureAttendance("SHOW-ATT-OMAR-3", "tenant-1", omar, "SHOW-CLASS-T1", now.AddDays(-3).ToString("yyyy-MM-dd"), AttendanceStatus.Late, AttendanceSource.Manual, "day");
            await EnsureAttendance("SHOW-ATT-OMAR-4", "tenant-1", omar, "SHOW-CLASS-T1", now.AddDays(-6).ToString("yyyy-MM-dd"), AttendanceStatus.Absent, AttendanceSource.Import, "day");
            await EnsureAttendance("SHOW-ATT-OMAR-5", "tenant-1", omar, "SHOW-CLASS-T1", now.AddDays(-7).ToString("yyyy-MM-dd"), AttendanceStatus.Present, AttendanceSource.Manual, "day");
            if (nada is not null)
            {
                await EnsureAttendance("SHOW-ATT-NADA-1", "tenant-1", nada, "SHOW-CLASS-T1", now.AddDays(-1).ToString("yyyy-MM-dd"), AttendanceStatus.Present, AttendanceSource.Manual, "day");
                await EnsureAttendance("SHOW-ATT-NADA-2", "tenant-1", nada, "SHOW-CLASS-T1", now.AddDays(-3).ToString("yyyy-MM-dd"), AttendanceStatus.Present, AttendanceSource.Manual, "day");
            }

            // Unread notifications for Omar.
            await Ensure(_context.notifications, x => x.Id == "SHOW-NOTIF-OMAR-1", () => new Notification
            {
                Id = "SHOW-NOTIF-OMAR-1", TenantId = "tenant-1", UserId = omar, Title = "Homework graded",
                Body = "Your 'Algebra Worksheet 1' was graded: 18/20.", NotificationCategory = NotificationCategory.General,
                NotificationType = NotificationType.System, IsRead = false, CreatedAt = now.AddMinutes(-30)
            });
            await Ensure(_context.notifications, x => x.Id == "SHOW-NOTIF-OMAR-2", () => new Notification
            {
                Id = "SHOW-NOTIF-OMAR-2", TenantId = "tenant-1", UserId = omar, Title = "New quiz available",
                Body = "A new 'Geometry Quiz' is available to attempt.", NotificationCategory = NotificationCategory.General,
                NotificationType = NotificationType.System, IsRead = false, CreatedAt = now.AddMinutes(-90)
            });

            // Earned badge + study streak for Omar (reusing the platform badge catalog).
            await Ensure(_context.studentBadges, x => x.Id == "SHOW-SB-OMAR", () => new StudentBadge
            {
                Id = "SHOW-SB-OMAR", TenantId = "tenant-1", StudentId = omar, BadgeId = "E2E-PH8-BADGE-1",
                AwardedAt = now.AddDays(-3), AwardedReason = "Completed your first lesson"
            });
            await Ensure(_context.studentStreaks, x => x.StudentId == omar, () => new StudentStreak
            {
                Id = "SHOW-STREAK-OMAR", TenantId = "tenant-1", StudentId = omar, CurrentCount = 4, LongestCount = 9,
                LastActivityDate = now.AddDays(-1)
            });
        }

        // ---- Idempotent helpers specific to the showcase fixtures ----

        private async Task EnsureAssignmentSubmission(string id, string tenantId, string assignmentId, string studentId,
            string content, SubmissionStatus status, DateTime submittedAt, int? score = null, DateTime? gradedAt = null,
            string? gradedBy = null, string? feedback = null)
        {
            await Ensure(_context.assignmentSubmissions, x => x.Id == id, () => new AssignmentSubmission
            {
                Id = id, TenantId = tenantId, AssignmentId = assignmentId, StudentId = studentId,
                Content = content, Status = status, SubmittedAt = submittedAt,
                Score = score, GradedAt = gradedAt, GradedByTeacherId = gradedBy, Feedback = feedback,
                CreatedAt = submittedAt
            });
        }

        private async Task EnsureQuizSubmission(string id, string tenantId, string quizId, string studentId,
            int achievedScore, int totalScore, DateTime startedAt, DateTime submittedAt, DateTime gradedAt, string? assignmentId)
        {
            await Ensure(_context.quizSubmissions, x => x.Id == id, () => new QuizSubmission
            {
                Id = id, TenantId = tenantId, QuizId = quizId, StudentId = studentId,
                AchievedScore = achievedScore, TotalScore = totalScore,
                submissionStatus = SubmissionStatus.Graded, GradingMethod = GradingMethod.Automatic,
                AttemptNumber = 1, IsLatestAttempt = true,
                StartedAt = startedAt, SubmittedAt = submittedAt, GradedAt = gradedAt, AssignmentId = assignmentId
            });
        }

        private async Task EnsureSubmissionAnswer(string id, string tenantId, string quizSubmissionId, string questionId,
            string? selectedOptionId, bool correct, int points)
        {
            await Ensure(_context.submissionAnswers, x => x.Id == id, () => new SubmissionAnswer
            {
                Id = id, TenantId = tenantId, QuizSubmissionId = quizSubmissionId, QuestionId = questionId,
                SelectedOptionId = selectedOptionId, IsCorrect = correct, PointsEarned = points,
                GradingMethod = GradingMethod.Automatic, GradedAt = DateTime.UtcNow
            });
        }
    }
}
