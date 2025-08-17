// src/CreateExam.cs (SAMPLE)
// Notes:
// - Names mapped to English for docs purposes (Exam, Course, Room, Professor, StudyYear, ExamPeriod).
// - Business rules kept: 08–21, max 2 exams per year/day, room conflicts with classes & exams.
// - Best-effort Google push + outbox fallback. Audit snapshot after create.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

public sealed class ExamsController : Controller
{
    private readonly AppDbContext _ctx;
    private readonly IAuthorizationService _auth;
    private readonly IGoogleCalendarService _gcal;
    private readonly IAuditService _audit;
    private readonly ILogger<ExamsController> _logger;

    // Business hours (move to config in real app)
    private static readonly TimeSpan BusinessStart = TimeSpan.FromHours(8);
    private static readonly TimeSpan BusinessEnd   = TimeSpan.FromHours(21);

    public ExamsController(
        AppDbContext ctx,
        IAuthorizationService auth,
        IGoogleCalendarService gcal,
        IAuditService audit,
        ILogger<ExamsController> logger)
    {
        _ctx = ctx;
        _auth = auth;
        _gcal = gcal;
        _audit = audit;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Create(int programId, int yearId, CancellationToken ct)
    {
        if (yearId <= 0)
            return RedirectToAction("Exams", "Program", new { programId });

        var program = await _ctx.StudyPrograms.FindAsync(new object?[] { programId }, ct);
        var year = await _ctx.StudyYears
            .Include(y => y.Track) // optional “Smjer”
            .FirstOrDefaultAsync(y => y.Id == yearId, ct);

        if (program is null || year is null) return NotFound();

        var allowed = (await _auth.AuthorizeAsync(User, program, "CanEditStudyProgram")).Succeeded;
        if (!allowed) return Forbid();

        var courses = await _ctx.Courses
            .Where(c => c.StudyProgramId == programId && c.StudyYearId == yearId)
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem(c.Name, c.Id.ToString()))
            .ToListAsync(ct);

        if (!courses.Any())
        {
            TempData["Error"] = "No courses defined for selected program/year.";
            return RedirectToAction("Exams", "Program", new { programId, yearId });
        }

        var professors = await _ctx.Professors
            .OrderBy(p => p.FullName)
            .Select(p => new SelectListItem(p.FullName, p.Id.ToString()))
            .ToListAsync(ct);

        var vm = new CreateExamVM
        {
            StudyProgramId = programId,
            StudyProgramName = program.Name,
            StudyYearId = yearId,
            StudyYearName = $"{year.Number}. year" + (year.Track != null ? $" ({year.Track.Name})" : ""),
            Date = DateTime.Today,
            StartTime = TimeSpan.FromHours(8),
            EndTime = TimeSpan.FromHours(10),
            Courses = courses,
            Professors = professors,
            Rooms = await _ctx.Rooms.OrderBy(r => r.Code)
                .Select(r => new SelectListItem(r.Code, r.Id.ToString()))
                .ToListAsync(ct),
            ExamPeriods = await _ctx.ExamPeriods.OrderBy(r => r.Name)
                .Select(r => new SelectListItem(r.Name, r.Id.ToString()))
                .ToListAsync(ct)
        };

        // dates with already “max 2 exams”
        ViewBag.FullDates = await _ctx.Exams
            .Where(e => e.StudyYearId == yearId)
            .GroupBy(e => e.Date)
            .Where(g => g.Count() >= 2)
            .Select(g => g.Key.ToString("yyyy-MM-dd"))
            .ToListAsync(ct);

        // optional: Course→Professor pivot for UI dependent selects
        ViewBag.CourseProfessorMap = await _ctx.CourseProfessors
            .Where(cp => cp.Course.StudyProgramId == programId && cp.Course.StudyYearId == yearId)
            .Select(cp => new { cp.CourseId, cp.ProfessorId, Name = cp.Professor.FullName })
            .ToListAsync(ct);

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateExamVM vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            RepopulateSelects(vm, ct);
            PopulateCreateViewData(vm, ct);
            return View(vm);
        }

        // Convert TimeSpan to TimeOnly
        var start = TimeOnly.FromTimeSpan(vm.StartTime);
        var end   = TimeOnly.FromTimeSpan(vm.EndTime);

        // 1) Start < End
        if (start >= end)
            ModelState.AddModelError(nameof(vm.StartTime), "Start time must be before end time.");

        // 2) Max 2 exams per year on the same date
        int countToday = await _ctx.Exams.CountAsync(e =>
            e.StudyYearId == vm.StudyYearId &&
            e.Date == DateOnly.FromDateTime(vm.Date), ct);

        if (countToday >= 2)
            ModelState.AddModelError(nameof(vm.Date), "There are already two exams on that date.");

        // 3) Room occupancy (classes + exams)
        var date = DateOnly.FromDateTime(vm.Date);

        bool roomOverlapInClasses = await _ctx.Classes.AnyAsync(n =>
            n.RoomId == vm.RoomId && n.Date == date && start < n.EndTime && end > n.StartTime, ct);

        bool roomOverlapInExams = await _ctx.Exams.AnyAsync(e =>
            e.RoomId == vm.RoomId && e.Date == date && start < e.EndTime && end > e.StartTime, ct);

        if (roomOverlapInClasses || roomOverlapInExams)
            ModelState.AddModelError(nameof(vm.RoomId), "The room is occupied in the selected time slot.");

        // 4) Business hours 08–21
        if (vm.StartTime < BusinessStart || vm.EndTime > BusinessEnd)
            ModelState.AddModelError(nameof(vm.StartTime), "Exams must be between 08:00 and 21:00.");

        if (!ModelState.IsValid)
        {
            RepopulateSelects(vm, ct);
            PopulateCreateViewData(vm, ct);
            return View(vm);
        }

        // Persist exam
        var entity = new Exam
        {
            StudyProgramId = vm.StudyProgramId,
            StudyYearId    = vm.StudyYearId,
            CourseId       = vm.CourseId,
            ProfessorId    = vm.ProfessorId,
            RoomId         = vm.RoomId,
            ExamPeriodId   = vm.ExamPeriodId,
            Date           = DateOnly.FromDateTime(vm.Date),
            StartTime      = TimeOnly.FromTimeSpan(vm.StartTime),
            EndTime        = TimeOnly.FromTimeSpan(vm.EndTime),
            Type           = vm.Type,
            GoogleEventId  = null,
            LastModified   = DateTime.UtcNow,
            IsDeleted      = false
        };
        _ctx.Exams.Add(entity);
        await _ctx.SaveChangesAsync(ct);

        // Audit snapshot
        var snapshot = await _ctx.Exams.AsNoTracking()
            .Where(e => e.Id == entity.Id)
            .Select(e => new {
                Course    = e.Course.Name,
                Professor = e.Professor.FullName,
                Room      = e.Room.Code,
                Period    = e.ExamPeriod.Name,
                Date      = e.Date.ToString("yyyy-MM-dd"),
                Start     = e.StartTime.ToString("HH:mm"),
                End       = e.EndTime.ToString("HH:mm"),
                e.Type
            })
            .FirstAsync(ct);

        await _audit.LogAsync("Create", "Exam", entity.Id.ToString(), snapshot, ct);

        // Calendar config lookup
        var calendarId = await _ctx.CalendarConfigs
            .Where(c => c.StudyProgramId == entity.StudyProgramId &&
                        c.StudyYearId == entity.StudyYearId &&
                        c.EventType == "Exam")
            .Select(c => c.CalendarId)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException(
                $"No calendar configured for exams (program {entity.StudyProgramId}, year {entity.StudyYearId}).");

        _logger.LogInformation("Using calendar {CalendarId} for exam ({Prog},{Year})",
            calendarId, entity.StudyProgramId, entity.StudyYearId);

        // Best-effort Google push, with outbox fallback
        try
        {
            var course = await _ctx.Courses.FindAsync(new object?[] { entity.CourseId }, ct);
            var prof   = await _ctx.Professors.FindAsync(new object?[] { entity.ProfessorId }, ct);
            var room   = await _ctx.Rooms.FindAsync(new object?[] { entity.RoomId }, ct);
            var period = await _ctx.ExamPeriods.FindAsync(new object?[] { entity.ExamPeriodId }, ct);

            var starts = vm.Date.Date + vm.StartTime;
            var ends   = vm.Date.Date + vm.EndTime;
            var title  = $"{course!.Name} – {entity.Type}";
            var body   = $"Professor: {prof!.FullName}\nRoom: {room!.Code}\nPeriod: {period!.Name}";

            var evId = await _gcal.AddEventAsync(
                entity.StudyProgramId, entity.StudyYearId, "Exam", title, starts, ends, body, ct);

            entity.GoogleEventId = evId;
            entity.LastModified = DateTime.UtcNow;
            _ctx.Exams.Update(entity);
            await _ctx.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Google event for exam #{Id}: {Msg}", entity.Id, ex.Message);

            var payload = JsonSerializer.Serialize(new
            {
                entity.Id,
                entity.Date,
                entity.StartTime,
                entity.EndTime,
                entity.Type,
                entity.StudyProgramId,
                entity.StudyYearId
            });

            _ctx.OutboxEvents.Add(new OutboxEvent
            {
                ExamId    = entity.Id,
                Payload   = payload,
                EventType = "Create",
                Processed = false,
                CreatedAt = DateTime.UtcNow
            });
            await _ctx.SaveChangesAsync(ct);
        }

        return RedirectToAction("Exams", "Program",
            new { programId = entity.StudyProgramId, yearId = entity.StudyYearId });
    }

    // Helpers (kept simple for the sample)
    private void RepopulateSelects(CreateExamVM vm, CancellationToken ct)
    {
        vm.Courses = _ctx.Courses
            .Where(c => c.StudyProgramId == vm.StudyProgramId && c.StudyYearId == vm.StudyYearId)
            .Select(c => new SelectListItem(c.Name, c.Id.ToString()))
            .ToList();

        vm.Professors = _ctx.Professors
            .OrderBy(p => p.FullName)
            .Select(p => new SelectListItem(p.FullName, p.Id.ToString()))
            .ToList();

        vm.Rooms = _ctx.Rooms
            .OrderBy(r => r.Code)
            .Select(r => new SelectListItem(r.Code, r.Id.ToString()))
            .ToList();

        vm.ExamPeriods = _ctx.ExamPeriods
            .OrderBy(r => r.Name)
            .Select(r => new SelectListItem(r.Name, r.Id.ToString()))
            .ToList();
    }

    private void PopulateCreateViewData(CreateExamVM vm, CancellationToken ct)
    {
        // If you need to refill ViewBag data for the View, do it here
    }
}

// ViewModel sample (adjust to your real model)
public sealed class CreateExamVM
{
    public int StudyProgramId { get; set; }
    public string StudyProgramName { get; set; } = "";
    public int StudyYearId { get; set; }
    public string StudyYearName { get; set; } = "";

    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public int CourseId { get; set; }
    public int ProfessorId { get; set; }
    public int RoomId { get; set; }
    public int ExamPeriodId { get; set; }
    public string Type { get; set; } = "Exam";

    public List<SelectListItem> Courses { get; set; } = new();
    public List<SelectListItem> Professors { get; set; } = new();
    public List<SelectListItem> Rooms { get; set; } = new();
    public List<SelectListItem> ExamPeriods { get; set; } = new();
}
