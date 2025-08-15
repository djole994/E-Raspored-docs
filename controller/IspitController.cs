
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ERaspored.Models;
using ERaspored.Services;
using ERaspored.ViewModel;
using ERaspored.ViewModel.Ispit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERaspored.Controllers
{
    [Authorize(Roles = "Admin,Organizator,Organizer")]
    public class IspitController : Controller
    {
        // ─────────────────────────────────────────────────────────────────────────────
        // Konstante / "configuration" (po želji prebaciti u appsettings)
        // ─────────────────────────────────────────────────────────────────────────────
        private const string EventTypeIspit = "Ispit";
        private static readonly TimeSpan WorkdayStart = TimeSpan.FromHours(8);
        private static readonly TimeSpan WorkdayEnd   = TimeSpan.FromHours(21);

        // ─────────────────────────────────────────────────────────────────────────────
        // DI
        // ─────────────────────────────────────────────────────────────────────────────
        private readonly ERasporedContext _ctx;
        private readonly GoogleCalendarService _gcal;
        private readonly ILogger<IspitController> _logger;
        private readonly IAuthorizationService _auth;

        public IspitController(
            ERasporedContext ctx,
            GoogleCalendarService gcal,
            ILogger<IspitController> logger,
            IAuthorizationService auth)
        {
            _ctx = ctx;
            _gcal = gcal;
            _logger = logger;
            _auth = auth;
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // Pomoćne metode
        // ─────────────────────────────────────────────────────────────────────────────

        private async Task RepopulateSelectsAsync(CreateExamVM vm)
        {
            vm.Predmeti = await _ctx.Predmets
                .AsNoTracking()
                .Where(p => p.StudijskiProgramId == vm.StudijskiProgramId &&
                            p.GodinaStudijaId == vm.GodinaStudijaId)
                .OrderBy(p => p.Naziv)
                .Select(p => new SelectListItem(p.Naziv, p.Id.ToString()))
                .ToListAsync();

            vm.Profesori = await _ctx.Profesors
                .AsNoTracking()
                .OrderBy(p => p.ImePrezime)
                .Select(p => new SelectListItem(p.ImePrezime, p.Id.ToString()))
                .ToListAsync();

            vm.Prostorije = await _ctx.Prostorijas
                .AsNoTracking()
                .OrderBy(r => r.Oznaka)
                .Select(r => new SelectListItem(r.Oznaka, r.Id.ToString()))
                .ToListAsync();

            vm.IspitniRoks = await _ctx.IspitniRoks
                .AsNoTracking()
                .OrderBy(r => r.Naziv)
                .Select(r => new SelectListItem(r.Naziv, r.Id.ToString()))
                .ToListAsync();
        }

        private async Task PopulateCreateViewDataAsync(CreateExamVM vm)
        {
            // nazivi
            var prog = await _ctx.StudijskiPrograms.AsNoTracking().FirstOrDefaultAsync(p => p.Id == vm.StudijskiProgramId);
            var god = await _ctx.GodinaStudijas
                .AsNoTracking()
                .Include(g => g.Smjer)
                .FirstOrDefaultAsync(g => g.Id == vm.GodinaStudijaId);

            vm.StudijskiProgramNaziv = prog?.Naziv ?? string.Empty;
            vm.GodinaNaziv = god == null ? string.Empty
                : $"{god.Broj}. godina" + (god.Smjer != null ? $" ({god.Smjer.Naziv})" : "");

            // bookings (nastava + ispiti)
            var nastava = await _ctx.Nastavas
                .AsNoTracking()
                .Select(n => new { n.ProstorijaId, n.Datum, n.VrijemeOd, n.VrijemeDo })
                .ToListAsync();

            var ispiti = await _ctx.Ispits
                .AsNoTracking()
                .Select(i => new { i.ProstorijaId, i.Datum, i.VrijemeOd, i.VrijemeDo })
                .ToListAsync();

            ViewBag.ExistingBookings = nastava
                .Concat(ispiti)
                .Select(x => new
                {
                    x.ProstorijaId,
                    Datum = x.Datum.ToString("yyyy-MM-dd"),
                    Od    = x.VrijemeOd.ToString("HH:mm"),
                    Do    = x.VrijemeDo.ToString("HH:mm")
                })
                .ToList();

            // fullDates: svi datumi s ≥2 ispita za dati program/godinu
            ViewBag.FullDates = await _ctx.Ispits
                .AsNoTracking()
                .Where(i => i.StudijskiProgramId == vm.StudijskiProgramId &&
                            i.GodinaStudijaId == vm.GodinaStudijaId)
                .GroupBy(i => i.Datum)
                .Where(g => g.Count() >= 2)
                .Select(g => g.Key.ToString("yyyy-MM-dd"))
                .ToListAsync();
        }

        private async Task PopulateEditViewBagAsync(CreateExamVM vm)
        {
            // nazivi
            var prog = await _ctx.StudijskiPrograms.AsNoTracking().FirstOrDefaultAsync(p => p.Id == vm.StudijskiProgramId);
            vm.StudijskiProgramNaziv = prog?.Naziv ?? string.Empty;

            var god = await _ctx.GodinaStudijas
                .AsNoTracking()
                .Include(g => g.Smjer)
                .FirstOrDefaultAsync(g => g.Id == vm.GodinaStudijaId);

            vm.GodinaNaziv = god == null ? string.Empty
                : $"{god.Broj}. godina" + (god.Smjer != null ? $" ({god.Smjer.Naziv})" : "");

            // slots (nastava + ostali ispiti)
            var nastava = await _ctx.Nastavas
                .AsNoTracking()
                .Select(n => new { n.ProstorijaId, n.Datum, n.VrijemeOd, n.VrijemeDo })
                .ToListAsync();

            var ispiti = await _ctx.Ispits
                .AsNoTracking()
                .Where(i => i.Id != vm.IspitId)
                .Select(i => new { i.ProstorijaId, i.Datum, i.VrijemeOd, i.VrijemeDo })
                .ToListAsync();

            ViewBag.ExistingBookings = nastava
                .Concat(ispiti)
                .Select(x => new
                {
                    x.ProstorijaId,
                    Datum = x.Datum.ToString("yyyy-MM-dd"),
                    Od    = x.VrijemeOd.ToString("HH:mm"),
                    Do    = x.VrijemeDo.ToString("HH:mm")
                })
                .ToList();

            // datumi s ≥2 ispita za dati program/godinu (bez tekućeg ispita)
            ViewBag.FullDates = await _ctx.Ispits
                .AsNoTracking()
                .Where(i => i.StudijskiProgramId == vm.StudijskiProgramId &&
                            i.GodinaStudijaId == vm.GodinaStudijaId &&
                            i.Id != vm.IspitId)
                .GroupBy(i => i.Datum)
                .Where(g => g.Count() >= 2)
                .Select(g => g.Key.ToString("yyyy-MM-dd"))
                .ToListAsync();
        }

        private async Task PopulateDropdownsAsync(CreateExamVM vm)
        {
            vm.Predmeti = await _ctx.Predmets
                .AsNoTracking()
                .Where(p => p.StudijskiProgramId == vm.StudijskiProgramId &&
                            p.GodinaStudijaId == vm.GodinaStudijaId)
                .OrderBy(p => p.Naziv)
                .Select(p => new SelectListItem(p.Naziv, p.Id.ToString()))
                .ToListAsync();

            vm.Profesori = await _ctx.PredmetProfesors
                .AsNoTracking()
                .Where(pp => pp.Predmet.StudijskiProgramId == vm.StudijskiProgramId &&
                             pp.Predmet.GodinaStudijaId == vm.GodinaStudijaId)
                .OrderBy(pp => pp.Profesor.ImePrezime)
                .Select(pp => new SelectListItem(pp.Profesor.ImePrezime, pp.ProfesorId.ToString()))
                .ToListAsync();

            vm.Prostorije = await _ctx.Prostorijas
                .AsNoTracking()
                .OrderBy(r => r.Oznaka)
                .Select(r => new SelectListItem(r.Oznaka, r.Id.ToString()))
                .ToListAsync();

            vm.IspitniRoks = await _ctx.IspitniRoks
                .AsNoTracking()
                .OrderBy(r => r.Naziv)
                .Select(r => new SelectListItem(r.Naziv, r.Id.ToString()))
                .ToListAsync();
        }

        private static bool ValidateTimeWindow(TimeSpan from, TimeSpan to) =>
            from < to && from >= WorkdayStart && to <= WorkdayEnd;

        // ─────────────────────────────────────────────────────────────────────────────
        // API: zauzeća (nastava + ispiti) za dan / opcionalnu prostoriju
        // ─────────────────────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> ApiBookings(string date, int roomId = 0, int? excludeId = null)
        {
            if (!DateOnly.TryParse(date, out var day))
                return BadRequest("Invalid date.");

            var qN = _ctx.Nastavas
                .AsNoTracking()
                .Where(n => n.Datum == day)
                .Select(n => new { n.ProstorijaId, n.Datum, n.VrijemeOd, n.VrijemeDo });

            var qI = _ctx.Ispits
                .AsNoTracking()
                .Where(i => i.Datum == day && !i.IsDeleted);

            if (excludeId.HasValue)
                qI = qI.Where(i => i.Id != excludeId.Value);

            var qI2 = qI.Select(i => new { i.ProstorijaId, i.Datum, i.VrijemeOd, i.VrijemeDo });

            if (roomId > 0)
            {
                qN = qN.Where(n => n.ProstorijaId == roomId);
                qI2 = qI2.Where(i => i.ProstorijaId == roomId);
            }

            var items = await qN.Concat(qI2).ToListAsync();

            var payload = items.Select(x => new
            {
                x.ProstorijaId,
                Datum = x.Datum.ToString("yyyy-MM-dd"),
                Od    = x.VrijemeOd.ToString("HH:mm"),
                Do    = x.VrijemeDo.ToString("HH:mm")
            });

            return Ok(payload);
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // CREATE
        // ─────────────────────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Create(int programId, int godinaId)
        {
            if (godinaId <= 0)
                return RedirectToAction("Ispiti", "Program", new { programId });

            var prog = await _ctx.StudijskiPrograms.AsNoTracking().FirstOrDefaultAsync(p => p.Id == programId);
            var god  = await _ctx.GodinaStudijas.AsNoTracking()
                            .Include(g => g.Smjer)
                            .FirstOrDefaultAsync(g => g.Id == godinaId);

            if (prog == null || god == null) return NotFound();

            // policy check
            var allowed = (await _auth.AuthorizeAsync(User, prog, "CanEditStudijskiProgram")).Succeeded;
            if (!allowed) return Forbid();

            var predmeti = await _ctx.Predmets
                .AsNoTracking()
                .Where(p => p.StudijskiProgramId == programId && p.GodinaStudijaId == godinaId)
                .OrderBy(p => p.Naziv)
                .Select(p => new SelectListItem(p.Naziv, p.Id.ToString()))
                .ToListAsync();

            var profesori = await _ctx.Profesors
                .AsNoTracking()
                .OrderBy(p => p.ImePrezime)
                .Select(p => new SelectListItem(p.ImePrezime, p.Id.ToString()))
                .ToListAsync();

            if (!predmeti.Any())
            {
                TempData["Error"] = "Za odabrani program/godinu nema definisanih predmeta.";
                return RedirectToAction("Ispiti", "Program", new { programId, godinaId });
            }

            var vm = new CreateExamVM
            {
                StudijskiProgramId   = programId,
                StudijskiProgramNaziv= prog.Naziv,
                GodinaStudijaId      = godinaId,
                GodinaNaziv          = $"{god.Broj}. godina" + (god.Smjer != null ? $" ({god.Smjer.Naziv})" : ""),
                Datum                = DateTime.Today,
                VrijemeOd            = TimeSpan.FromHours(8),
                VrijemeDo            = TimeSpan.FromHours(10),
                Predmeti             = predmeti,
                Profesori            = profesori,
                Prostorije           = await _ctx.Prostorijas.AsNoTracking().OrderBy(r => r.Oznaka).Select(r => new SelectListItem(r.Oznaka, r.Id.ToString())).ToListAsync(),
                IspitniRoks          = await _ctx.IspitniRoks.AsNoTracking().OrderBy(r => r.Naziv).Select(r => new SelectListItem(r.Naziv, r.Id.ToString())).ToListAsync()
            };

            ViewBag.FullDates = await _ctx.Ispits
                .AsNoTracking()
                .Where(i => i.StudijskiProgramId == programId && i.GodinaStudijaId == godinaId)
                .GroupBy(i => i.Datum)
                .Where(g => g.Count() >= 2)
                .Select(g => g.Key.ToString("yyyy-MM-dd"))
                .ToListAsync();

            // map predmet-profesor (za JS)
            ViewBag.ProfMap = await _ctx.PredmetProfesors
                .AsNoTracking()
                .Where(pp => pp.Predmet.StudijskiProgramId == programId && pp.Predmet.GodinaStudijaId == godinaId)
                .Select(pp => new { pp.PredmetId, pp.ProfesorId, Ime = pp.Profesor.ImePrezime })
                .ToListAsync();

            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateExamVM vm)
        {
            // repopulate na fail
            async Task<ViewResult> Fail()
            {
                await RepopulateSelectsAsync(vm);
                await PopulateCreateViewDataAsync(vm);
                return View(vm);
            }

            if (!ModelState.IsValid) return await Fail();

            // policy check (POST)
            var prog = await _ctx.StudijskiPrograms.FindAsync(vm.StudijskiProgramId);
            if (prog == null) return NotFound("Studijski program ne postoji.");
            if (!(await _auth.AuthorizeAsync(User, prog, "CanEditStudijskiProgram")).Succeeded)
                return Forbid();

            // invarijante domena
            var predmetOk = await _ctx.Predmets.AnyAsync(p =>
                p.Id == vm.PredmetId &&
                p.StudijskiProgramId == vm.StudijskiProgramId &&
                p.GodinaStudijaId == vm.GodinaStudijaId);

            if (!predmetOk)
                ModelState.AddModelError(nameof(vm.PredmetId), "Nevažeći predmet za dati program/godinu.");

            var profOk = await _ctx.PredmetProfesors.AnyAsync(pp =>
                pp.PredmetId == vm.PredmetId &&
                pp.ProfesorId == vm.ProfesorId);

            if (!profOk)
                ModelState.AddModelError(nameof(vm.ProfesorId), "Profesor nije nosilac ovog predmeta.");

            // provjera vremena (redoslijed i opseg)
            if (!ValidateTimeWindow(vm.VrijemeOd, vm.VrijemeDo))
                ModelState.AddModelError(string.Empty, $"Ispiti mogu biti samo između {WorkdayStart:hh\\:mm} i {WorkdayEnd:hh\\:mm}, a početak mora biti prije kraja.");

            // max 2 ispita
            var dateOnly = DateOnly.FromDateTime(vm.Datum);
            var existingCount = await _ctx.Ispits.CountAsync(i =>
                i.GodinaStudijaId == vm.GodinaStudijaId &&
                i.StudijskiProgramId == vm.StudijskiProgramId &&
                i.Datum == dateOnly);

            if (existingCount >= 2)
                ModelState.AddModelError(nameof(vm.Datum), "Na taj datum već postoje dva ispita.");

            // preklapanje prostorije (nastava + ispiti)
            var od = TimeOnly.FromTimeSpan(vm.VrijemeOd);
            var dn = TimeOnly.FromTimeSpan(vm.VrijemeDo);

            var roomOverlapInClasses = await _ctx.Nastavas.AnyAsync(n =>
                n.ProstorijaId == vm.ProstorijaId &&
                n.Datum == dateOnly &&
                od < n.VrijemeDo &&
                dn > n.VrijemeOd);

            var roomOverlapInExams = await _ctx.Ispits.AnyAsync(i =>
                i.ProstorijaId == vm.ProstorijaId &&
                i.Datum == dateOnly &&
                od < i.VrijemeDo &&
                dn > i.VrijemeOd);

            if (roomOverlapInClasses || roomOverlapInExams)
                ModelState.AddModelError(nameof(vm.ProstorijaId), "Prostorija je zauzeta u odabranom terminu.");

            if (!ModelState.IsValid) return await Fail();

            // upis u DB
            var ent = new Ispit
            {
                StudijskiProgramId = vm.StudijskiProgramId,
                GodinaStudijaId    = vm.GodinaStudijaId,
                PredmetId          = vm.PredmetId,
                ProfesorId         = vm.ProfesorId,
                ProstorijaId       = vm.ProstorijaId,
                IspitniRokId       = vm.IspitniRokId,
                Datum              = dateOnly,
                VrijemeOd          = od,
                VrijemeDo          = dn,
                Tip                = vm.Tip,
                GoogleEventId      = null,
                LastModified       = DateTime.UtcNow,
                IsDeleted          = false
            };

            _ctx.Ispits.Add(ent);
            await _ctx.SaveChangesAsync();

            // CalendarId (EventType = "Ispit")
            var calendarId = await _ctx.CalendarConfigs
                .AsNoTracking()
                .Where(c => c.StudijskiProgramId == ent.StudijskiProgramId &&
                            c.GodinaStudijaId == ent.GodinaStudijaId &&
                            c.EventType == EventTypeIspit)
                .Select(c => c.CalendarId)
                .FirstOrDefaultAsync()
                ?? throw new InvalidOperationException($"Nema kalendara za ispite za program {ent.StudijskiProgramId}, godinu {ent.GodinaStudijaId}");

            _logger.LogInformation("CREATE Ispit #{Id} (SP:{SP}, GOD:{GOD}) → kalendar {CalId}", ent.Id, ent.StudijskiProgramId, ent.GodinaStudijaId, calendarId);

            // Google push (best-effort)
            try
            {
                var pred = await _ctx.Predmets.FindAsync(ent.PredmetId);
                var prof = await _ctx.Profesors.FindAsync(ent.ProfesorId);
                var prst = await _ctx.Prostorijas.FindAsync(ent.ProstorijaId);
                var rok  = await _ctx.IspitniRoks.FindAsync(ent.IspitniRokId);

                if (pred == null || prof == null || prst == null || rok == null)
                    throw new InvalidOperationException("Nedostaju referentni entiteti za Google event.");

                var start  = vm.Datum.Date + vm.VrijemeOd;
                var end    = vm.Datum.Date + vm.VrijemeDo;
                var naslov = $"{pred.Naziv} – {ent.Tip}";
                var opis   = $"Profesor: {prof.ImePrezime}\nProstorija: {prst.Oznaka}\nRok: {rok.Naziv}";

                var evId = await _gcal.AddEventAsync(ent.StudijskiProgramId, ent.GodinaStudijaId, EventTypeIspit, naslov, start, end, opis);

                ent.GoogleEventId = evId;
                ent.LastModified  = DateTime.UtcNow;
                _ctx.Ispits.Update(ent);
                await _ctx.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Greška pri kreiranju Google eventa za ispit #{Id}: {Msg}", ent.Id, ex.Message);

                var payload = JsonSerializer.Serialize(new
                {
                    ent.Id,
                    ent.Datum,
                    ent.VrijemeOd,
                    ent.VrijemeDo,
                    ent.Tip,
                    ent.StudijskiProgramId,
                    ent.GodinaStudijaId
                });

                _ctx.OutboxEvents.Add(new OutboxEvent
                {
                    IspitId   = ent.Id,
                    Payload   = payload,
                    EventType = "Create",
                    Processed = false,
                    CreatedAt = DateTime.UtcNow
                });

                await _ctx.SaveChangesAsync();
            }

            return RedirectToAction("Ispiti", "Program", new { programId = ent.StudijskiProgramId, godinaId = ent.GodinaStudijaId });
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // EDIT
        // ─────────────────────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var isp = await _ctx.Ispits
                .AsNoTracking()
                .Include(i => i.Predmet)
                .Include(i => i.Profesor)
                .Include(i => i.Prostorija)
                .Include(i => i.IspitniRok)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (isp == null) return NotFound();

            ViewData["CurrentProgramId"] = isp.StudijskiProgramId.ToString();

            var god = await _ctx.GodinaStudijas
                .AsNoTracking()
                .Include(g => g.Smjer)
                .FirstOrDefaultAsync(g => g.Id == isp.GodinaStudijaId);

            var godinaNaziv = god == null ? string.Empty
                : $"{god.Broj}. godina" + (god.Smjer is null ? "" : $" ({god.Smjer.Naziv})");

            var vm = new CreateExamVM
            {
                IspitId               = isp.Id,
                StudijskiProgramId    = isp.StudijskiProgramId,
                StudijskiProgramNaziv = (await _ctx.StudijskiPrograms.AsNoTracking().FirstOrDefaultAsync(p => p.Id == isp.StudijskiProgramId))?.Naziv ?? "",
                GodinaStudijaId       = isp.GodinaStudijaId,
                GodinaNaziv           = godinaNaziv,
                PredmetId             = isp.PredmetId,
                ProfesorId            = isp.ProfesorId,
                ProstorijaId          = isp.ProstorijaId,
                IspitniRokId          = isp.IspitniRokId,
                Datum                 = isp.Datum.ToDateTime(TimeOnly.MinValue),
                VrijemeOd             = isp.VrijemeOd.ToTimeSpan(),
                VrijemeDo             = isp.VrijemeDo.ToTimeSpan(),
                Tip                   = isp.Tip,
                GoogleEventId         = isp.GoogleEventId,
                Predmeti              = await _ctx.Predmets.AsNoTracking().Where(p => p.StudijskiProgramId == isp.StudijskiProgramId && p.GodinaStudijaId == isp.GodinaStudijaId).OrderBy(p => p.Naziv).Select(p => new SelectListItem(p.Naziv, p.Id.ToString())).ToListAsync(),
                Profesori             = await _ctx.Profesors.AsNoTracking().OrderBy(p => p.ImePrezime).Select(p => new SelectListItem { Text = p.ImePrezime, Value = p.Id.ToString(), Selected = (p.Id == isp.ProfesorId) }).ToListAsync(),
                Prostorije            = await _ctx.Prostorijas.AsNoTracking().OrderBy(r => r.Oznaka).Select(r => new SelectListItem(r.Oznaka, r.Id.ToString())).ToListAsync(),
                IspitniRoks           = await _ctx.IspitniRoks.AsNoTracking().OrderBy(r => r.Naziv).Select(r => new SelectListItem(r.Naziv, r.Id.ToString())).ToListAsync(),
            };

            // FullDates (bez tekućeg ispita)
            ViewBag.FullDates = await _ctx.Ispits
                .AsNoTracking()
                .Where(i => i.StudijskiProgramId == isp.StudijskiProgramId &&
                            i.GodinaStudijaId == isp.GodinaStudijaId &&
                            i.Id != id)
                .GroupBy(i => i.Datum)
                .Where(g => g.Count() >= 2)
                .Select(g => g.Key.ToString("yyyy-MM-dd"))
                .ToListAsync();

            return View("Edit", vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CreateExamVM vm)
        {
            async Task<ViewResult> Fail()
            {
                await PopulateEditViewBagAsync(vm);
                await PopulateDropdownsAsync(vm);
                return View(vm);
            }

            if (!ModelState.IsValid) return await Fail();

            var ent = await _ctx.Ispits.FindAsync(vm.IspitId);
            if (ent == null) return NotFound();

            // policy check (POST) prema programu ispita
            var prog = await _ctx.StudijskiPrograms.FindAsync(ent.StudijskiProgramId);
            if (prog == null) return NotFound("Studijski program ne postoji.");
            if (!(await _auth.AuthorizeAsync(User, prog, "CanEditStudijskiProgram")).Succeeded)
                return Forbid();

            // invarijante domena
            var predmetOk = await _ctx.Predmets.AnyAsync(p =>
                p.Id == vm.PredmetId &&
                p.StudijskiProgramId == vm.StudijskiProgramId &&
                p.GodinaStudijaId == vm.GodinaStudijaId);

            if (!predmetOk)
                ModelState.AddModelError(nameof(vm.PredmetId), "Nevažeći predmet za dati program/godinu.");

            var profOk = await _ctx.PredmetProfesors.AnyAsync(pp =>
                pp.PredmetId == vm.PredmetId &&
                pp.ProfesorId == vm.ProfesorId);

            if (!profOk)
                ModelState.AddModelError(nameof(vm.ProfesorId), "Profesor nije nosilac ovog predmeta.");

            // vremena
            if (!ValidateTimeWindow(vm.VrijemeOd, vm.VrijemeDo))
                ModelState.AddModelError(string.Empty, $"Ispiti mogu biti samo između {WorkdayStart:hh\\:mm} i {WorkdayEnd:hh\\:mm}, a početak mora biti prije kraja.");

            // max 2 ispita (bez tekućeg)
            var datumOnly = DateOnly.FromDateTime(vm.Datum);
            var existingCount = await _ctx.Ispits
                .Where(i => i.GodinaStudijaId == vm.GodinaStudijaId &&
                            i.StudijskiProgramId == vm.StudijskiProgramId &&
                            i.Datum == datumOnly &&
                            i.Id != vm.IspitId)
                .CountAsync();

            if (existingCount >= 2)
                ModelState.AddModelError(nameof(vm.Datum), "Na taj datum već postoje dva ispita.");

            // preklapanje
            var nastave = await _ctx.Nastavas
                .AsNoTracking()
                .Where(n => n.GodinaStudijaId == vm.GodinaStudijaId && n.Datum == datumOnly)
                .Select(n => new { n.ProstorijaId, n.VrijemeOd, n.VrijemeDo })
                .ToListAsync();

            var ostaliIspiti = await _ctx.Ispits
                .AsNoTracking()
                .Where(i => i.GodinaStudijaId == vm.GodinaStudijaId &&
                            i.StudijskiProgramId == vm.StudijskiProgramId &&
                            i.Datum == datumOnly &&
                            i.Id != vm.IspitId)
                .Select(i => new { i.ProstorijaId, i.VrijemeOd, i.VrijemeDo })
                .ToListAsync();

            var allBookings = nastave.Concat(ostaliIspiti).ToList();

            if (allBookings.Any(b =>
                b.ProstorijaId == vm.ProstorijaId &&
                vm.VrijemeOd < b.VrijemeDo.ToTimeSpan() &&
                vm.VrijemeDo > b.VrijemeOd.ToTimeSpan()))
            {
                ModelState.AddModelError(nameof(vm.ProstorijaId), "Prostorija zauzeta u odabranom terminu!");
            }

            if (!ModelState.IsValid) return await Fail();

            // mapiranje i Google sync
            ent.PredmetId     = vm.PredmetId;
            ent.ProfesorId    = vm.ProfesorId;
            ent.ProstorijaId  = vm.ProstorijaId;
            ent.IspitniRokId  = vm.IspitniRokId;
            ent.Datum         = datumOnly;
            ent.VrijemeOd     = TimeOnly.FromTimeSpan(vm.VrijemeOd);
            ent.VrijemeDo     = TimeOnly.FromTimeSpan(vm.VrijemeDo);
            ent.Tip           = vm.Tip;
            ent.LastModified  = DateTime.UtcNow;

            try
            {
                var pred = await _ctx.Predmets.FindAsync(ent.PredmetId);
                var prof = await _ctx.Profesors.FindAsync(ent.ProfesorId);
                var prst = await _ctx.Prostorijas.FindAsync(ent.ProstorijaId);
                var rok  = await _ctx.IspitniRoks.FindAsync(ent.IspitniRokId);

                if (pred == null || prof == null || prst == null || rok == null)
                    throw new InvalidOperationException("Nedostaju referentni entiteti za Google event.");

                var start  = vm.Datum.Date + vm.VrijemeOd;
                var end    = vm.Datum.Date + vm.VrijemeDo;
                var naslov = $"{pred.Naziv} – {ent.Tip}";
                var opis   = $"Profesor: {prof.ImePrezime}\nProstorija: {prst.Oznaka}\nRok: {rok.Naziv}";

                if (string.IsNullOrEmpty(ent.GoogleEventId))
                {
                    ent.GoogleEventId = await _gcal.AddEventAsync(ent.StudijskiProgramId, ent.GodinaStudijaId, EventTypeIspit, naslov, start, end, opis);
                }
                else
                {
                    await _gcal.UpdateEventAsync(ent.StudijskiProgramId, ent.GodinaStudijaId, EventTypeIspit, ent.GoogleEventId, naslov, start, end, opis);
                }

                _ctx.Ispits.Update(ent);
                await _ctx.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Greška pri ažuriranju Google eventa za ispit #{Id}: {Msg}", ent.Id, ex.Message);

                var payload = JsonSerializer.Serialize(new
                {
                    ent.Id,
                    ent.Datum,
                    ent.VrijemeOd,
                    ent.VrijemeDo,
                    ent.Tip,
                    ent.StudijskiProgramId,
                    ent.GodinaStudijaId
                });

                _ctx.OutboxEvents.Add(new OutboxEvent
                {
                    IspitId   = ent.Id,
                    Payload   = payload,
                    EventType = "Update",
                    Processed = false,
                    CreatedAt = DateTime.UtcNow
                });

                await _ctx.SaveChangesAsync();
            }

            return RedirectToAction("Ispiti", "Program", new { programId = ent.StudijskiProgramId, godinaId = ent.GodinaStudijaId });
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // DELETE
        // ─────────────────────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var isp = await _ctx.Ispits
                .AsNoTracking()
                .Include(i => i.Predmet)
                .Include(i => i.Profesor)
                .Include(i => i.Prostorija)
                .Include(i => i.IspitniRok)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (isp == null) return NotFound();

            ViewData["CurrentProgramId"] = isp.StudijskiProgramId.ToString();

            var vm = new DeleteExamVM
            {
                IspitId        = isp.Id,
                PredmetNaziv   = isp.Predmet.Naziv,
                ProfesorIme    = isp.Profesor.ImePrezime,
                Datum          = isp.Datum.ToString("yyyy-MM-dd"),
                VrijemeOd      = isp.VrijemeOd.ToString(@"hh\:mm"),
                VrijemeDo      = isp.VrijemeDo.ToString(@"hh\:mm"),
                ProstorijaOzn  = isp.Prostorija.Oznaka,
                RokNaziv       = isp.IspitniRok.Naziv,
                StudijskiProgramId = isp.StudijskiProgramId,
                GodinaStudijaId    = isp.GodinaStudijaId
            };

            return View(vm);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var isp = await _ctx.Ispits.FindAsync(id);
            if (isp == null) return NotFound();

            // policy check (POST)
            var prog = await _ctx.StudijskiPrograms.FindAsync(isp.StudijskiProgramId);
            if (prog == null) return NotFound("Studijski program ne postoji.");
            if (!(await _auth.AuthorizeAsync(User, prog, "CanEditStudijskiProgram")).Succeeded)
                return Forbid();

            if (!string.IsNullOrEmpty(isp.GoogleEventId))
            {
                var calendarId = await _ctx.CalendarConfigs
                    .AsNoTracking()
                    .Where(c => c.StudijskiProgramId == isp.StudijskiProgramId &&
                                c.GodinaStudijaId == isp.GodinaStudijaId &&
                                c.EventType == EventTypeIspit)
                    .Select(c => c.CalendarId)
                    .FirstOrDefaultAsync();

                var deletePayload = JsonSerializer.Serialize(new
                {
                    CalendarId   = calendarId,
                    GoogleEventId = isp.GoogleEventId
                });

                _ctx.OutboxEvents.Add(new OutboxEvent
                {
                    IspitId   = isp.Id,
                    Payload   = deletePayload,
                    EventType = "Delete",
                    Processed = false,
                    CreatedAt = DateTime.UtcNow
                });

                // snimi outbox prije brisanja reda
                await _ctx.SaveChangesAsync();
            }

            _ctx.Ispits.Remove(isp);
            await _ctx.SaveChangesAsync();

            _logger.LogInformation("DELETE Ispit #{Id} (SP:{SP}, GOD:{GOD}) izvršeno.", isp.Id, isp.StudijskiProgramId, isp.GodinaStudijaId);

            return RedirectToAction("Ispiti", "Program", new { programId = isp.StudijskiProgramId, godinaId = isp.GodinaStudijaId });
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // DUPLICATE → otvara Create view unaprijed popunjen
        // ─────────────────────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Duplicate(int id, int programId, int godinaId)
        {
            var isp = await _ctx.Ispits
                .AsNoTracking()
                .Include(i => i.Predmet).ThenInclude(p => p.StudijskiProgram)
                .Include(i => i.Predmet).ThenInclude(p => p.GodinaStudija)
                .Include(i => i.Profesor)
                .Include(i => i.Prostorija)
                .Include(i => i.IspitniRok)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (isp == null) return NotFound();

            ViewData["CurrentProgramId"] = isp.StudijskiProgramId.ToString();

            var vm = new CreateExamVM
            {
                StudijskiProgramId    = isp.StudijskiProgramId,
                StudijskiProgramNaziv = isp.Predmet.StudijskiProgram.Naziv,
                GodinaStudijaId       = isp.GodinaStudijaId,
                GodinaNaziv           = isp.Predmet.GodinaStudija.Broj + ". godina",
                PredmetId             = isp.PredmetId,
                ProfesorId            = isp.ProfesorId,
                ProstorijaId          = isp.ProstorijaId,
                IspitniRokId          = isp.IspitniRokId,
                Datum                 = isp.Datum.ToDateTime(TimeOnly.MinValue),
                VrijemeOd             = isp.VrijemeOd.ToTimeSpan(),
                VrijemeDo             = isp.VrijemeDo.ToTimeSpan(),
                Tip                   = isp.Tip,
                Predmeti              = await _ctx.Predmets.AsNoTracking().Where(p => p.StudijskiProgramId == programId && p.GodinaStudijaId == godinaId).OrderBy(p => p.Naziv).Select(p => new SelectListItem(p.Naziv, p.Id.ToString())).ToListAsync(),
                Profesori             = await _ctx.Profesors.AsNoTracking().OrderBy(p => p.ImePrezime).Select(p => new SelectListItem(p.ImePrezime, p.Id.ToString())).ToListAsync(),
                Prostorije            = await _ctx.Prostorijas.AsNoTracking().OrderBy(r => r.Oznaka).Select(r => new SelectListItem(r.Oznaka, r.Id.ToString())).ToListAsync(),
                IspitniRoks           = await _ctx.IspitniRoks.AsNoTracking().OrderBy(r => r.Naziv).Select(r => new SelectListItem(r.Naziv, r.Id.ToString())).ToListAsync()
            };

            ViewBag.FullDates = await _ctx.Ispits
                .AsNoTracking()
                .Where(i => i.Id != id &&
                            i.StudijskiProgramId == programId &&
                            i.GodinaStudijaId == godinaId)
                .GroupBy(i => i.Datum)
                .Where(g => g.Count() >= 2)
                .Select(g => g.Key.ToString("yyyy-MM-dd"))
                .ToListAsync();

            return View("Create", vm);
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // PRINT
        // ─────────────────────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> PrintIspiti(int programId, int godinaId, int month, int? year = null)
        {
            var y = year ?? DateTime.Today.Year;

            var lista = await _ctx.Ispits
                .AsNoTracking()
                .Where(i => i.StudijskiProgramId == programId &&
                            i.GodinaStudijaId == godinaId &&
                            i.Datum.Year == y &&
                            i.Datum.Month == month)
                .OrderBy(i => i.Datum)
                .ThenBy(i => i.VrijemeOd)
                .Select(i => new IspitPrintItem
                {
                    Datum        = i.Datum,
                    VrijemeOd    = i.VrijemeOd,
                    VrijemeDo    = i.VrijemeDo,
                    PredmetNaziv = i.Predmet.Naziv,
                    ProfesorIme  = i.Profesor.ImePrezime,
                    Titula       = i.Profesor.Titula,
                    ProstorijaOpis = i.Prostorija.Oznaka
                })
                .ToListAsync();

            var programEntity = await _ctx.StudijskiPrograms.AsNoTracking().FirstOrDefaultAsync(p => p.Id == programId);
            if (programEntity == null) return NotFound($"Studijski program ({programId}) nije pronađen.");

            var godinaEntity = await _ctx.GodinaStudijas.AsNoTracking().FirstOrDefaultAsync(g => g.Id == godinaId);
            if (godinaEntity == null) return NotFound($"Godina studija ({godinaId}) nije pronađena.");

            var vm = new IspitiPrintVM
            {
                ProgramNaziv     = programEntity.Naziv,
                GodinaBroj       = godinaEntity.Broj,
                IzabraniMjesec   = month,
                Ispiti           = lista,
                StudijskiProgramId = programId,
                GodinaStudijaId    = godinaId
            };

            return View(vm);
        }
    }
}
