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
    [Authorize(Roles = RoleGate)]
    public class IspitController : Controller
    {
        // ───────────────────────────────────────────────────────────────
        // KONSTANTE I POLJA
        // ───────────────────────────────────────────────────────────────
        private const string RoleGate = "Admin,Organizer,Organizator";
        private const string EventTypeExam = "Ispit";
        private static readonly TimeSpan ExamsStart = TimeSpan.FromHours(8);
        private static readonly TimeSpan ExamsEnd   = TimeSpan.FromHours(21);

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

        // ───────────────────────────────────────────────────────────────
        // API BOOKINGS
        // ───────────────────────────────────────────────────────────────
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

            if (excludeId.HasValue) qI = qI.Where(i => i.Id != excludeId.Value);

            var qI2 = qI.Select(i => new { i.ProstorijaId, i.Datum, i.VrijemeOd, i.VrijemeDo });

            if (roomId > 0)
            {
                qN  = qN.Where(n => n.ProstorijaId == roomId);
                qI2 = qI2.Where(i => i.ProstorijaId == roomId);
            }

            var items = await qN.Concat(qI2).ToListAsync();

            var payload = items.Select(x => new
            {
                ProstorijaId = x.ProstorijaId,
                Datum = x.Datum.ToString("yyyy-MM-dd"),
                Od = x.VrijemeOd.ToString("HH:mm"),
                Do = x.VrijemeDo.ToString("HH:mm")
            });

            return Ok(payload);
        }

        // ───────────────────────────────────────────────────────────────
        // CREATE GET
        // ───────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Create(int programId, int godinaId)
        {
            if (godinaId <= 0)
                return RedirectToAction("Ispiti", "Program", new { programId });

            var prog = await _ctx.StudijskiPrograms.FindAsync(programId);
            var god  = await _ctx.GodinaStudijas
                .AsNoTracking()
                .Include(g => g.Smjer)
                .FirstOrDefaultAsync(g => g.Id == godinaId);

            if (prog == null || god == null) return NotFound();

            var allowed = (await _auth.AuthorizeAsync(User, prog, "CanEditStudijskiProgram")).Succeeded;
            if (!allowed) return Forbid();

            var vm = new CreateExamVM
            {
                StudijskiProgramId    = programId,
                StudijskiProgramNaziv = prog.Naziv,
                GodinaStudijaId       = godinaId,
                GodinaNaziv = $"{god.Broj}. godina" + (god.Smjer != null ? $" ({god.Smjer.Naziv})" : ""),
                Datum       = DateTime.Today,
                VrijemeOd   = ExamsStart, // 08:00
                VrijemeDo   = TimeSpan.FromHours(10),
                Predmeti    = await GetPredmetiSelect(programId, godinaId),
                Profesori   = await GetProfesoriSelect(),
                Prostorije  = await GetProstorijeSelect(),
                IspitniRoks = await GetIspitniRokoviSelect()
            };

            if (!vm.Predmeti.Any())
            {
                TempData["Error"] = "Za odabrani program/godinu nema definisanih predmeta.";
                return RedirectToAction("Ispiti", "Program", new { programId, godinaId });
            }

            ViewBag.ExistingBookings = await BuildExistingBookingsAsync();
            ViewBag.FullDates        = await BuildFullDatesAsync(programId, godinaId);

            return View(vm);
        }

        // ───────────────────────────────────────────────────────────────
        // CREATE POST
        // ───────────────────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateExamVM vm)
        {
            var prog = await _ctx.StudijskiPrograms.FindAsync(vm.StudijskiProgramId);
            if (prog == null) return NotFound();
            if (!(await _auth.AuthorizeAsync(User, prog, "CanEditStudijskiProgram")).Succeeded)
                return Forbid();

            await ValidateExamAsync(vm, excludeIspitId: null);
            if (!ModelState.IsValid)
            {
                await RepopulateSelectsAsync(vm);
                await PopulateCreateScaffoldingAsync(vm);
                return View(vm);
            }

            var ent = new Ispit
            {
                StudijskiProgramId = vm.StudijskiProgramId,
                GodinaStudijaId    = vm.GodinaStudijaId,
                PredmetId          = vm.PredmetId,
                ProfesorId         = vm.ProfesorId,
                ProstorijaId       = vm.ProstorijaId,
                IspitniRokId       = vm.IspitniRokId,
                Datum              = DateOnly.FromDateTime(vm.Datum),
                VrijemeOd          = TimeOnly.FromTimeSpan(vm.VrijemeOd),
                VrijemeDo          = TimeOnly.FromTimeSpan(vm.VrijemeDo),
                Tip                = vm.Tip,
                GoogleEventId      = null,
                LastModified       = DateTime.UtcNow,
                IsDeleted          = false
            };

            _ctx.Ispits.Add(ent);
            await _ctx.SaveChangesAsync();

            var calendarId = await _ctx.CalendarConfigs
                .AsNoTracking()
                .Where(c => c.StudijskiProgramId == ent.StudijskiProgramId &&
                            c.GodinaStudijaId == ent.GodinaStudijaId &&
                            c.EventType        == EventTypeExam)
                .Select(c => c.CalendarId)
                .FirstOrDefaultAsync()
                ?? throw new InvalidOperationException(
                    $"Nema kalendara za ispite za program {ent.StudijskiProgramId}, godinu {ent.GodinaStudijaId}"
                );

            try
            {
                var pred = await _ctx.Predmets.FindAsync(ent.PredmetId);
                var prof = await _ctx.Profesors.FindAsync(ent.ProfesorId);
                var prst = await _ctx.Prostorijas.FindAsync(ent.ProstorijaId);
                var rok  = await _ctx.IspitniRoks.FindAsync(ent.IspitniRokId);

                string naslov = $"{pred!.Naziv} – {ent.Tip}";
                string opis   = $"Profesor: {prof!.ImePrezime}\nProstorija: {prst!.Oznaka}\nRok: {rok!.Naziv}";

                var start = vm.Datum.Date + vm.VrijemeOd;
                var end   = vm.Datum.Date + vm.VrijemeDo;

                var evId = await _gcal.AddEventAsync(
                    ent.StudijskiProgramId, ent.GodinaStudijaId, EventTypeExam,
                    naslov, start, end, opis);

                ent.GoogleEventId = evId;
                ent.LastModified  = DateTime.UtcNow;
                _ctx.Ispits.Update(ent);
                await _ctx.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Greška pri kreiranju Google eventa za ispit #{Id}: {Msg}",
                    ent.Id, ex.Message);

                var payload = JsonSerializer.Serialize(new
                {
                    ent.Id, ent.Datum, ent.VrijemeOd, ent.VrijemeDo, ent.Tip,
                    ent.StudijskiProgramId, ent.GodinaStudijaId
                });
                _ctx.OutboxEvents.Add(new OutboxEvent
                {
                    IspitId    = ent.Id,
                    Payload    = payload,
                    EventType  = "Create",
                    Processed  = false,
                    CreatedAt  = DateTime.UtcNow
                });
                await _ctx.SaveChangesAsync();
            }

            return RedirectToAction("Ispiti", "Program",
                new { programId = ent.StudijskiProgramId, godinaId = ent.GodinaStudijaId });
        }

        // ───────────────────────────────────────────────────────────────
        // EDIT GET
        // ───────────────────────────────────────────────────────────────
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

            var prog = await _ctx.StudijskiPrograms.FindAsync(isp.StudijskiProgramId);
            if (prog == null) return NotFound();
            if (!(await _auth.AuthorizeAsync(User, prog, "CanEditStudijskiProgram")).Succeeded)
                return Forbid();

            ViewData["CurrentProgramId"] = isp.StudijskiProgramId.ToString();

            var god = await _ctx.GodinaStudijas
                .AsNoTracking()
                .Include(g => g.Smjer)
                .FirstOrDefaultAsync(g => g.Id == isp.GodinaStudijaId);

            var vm = new CreateExamVM
            {
                IspitId = isp.Id,
                StudijskiProgramId    = isp.StudijskiProgramId,
                StudijskiProgramNaziv = (await _ctx.StudijskiPrograms.FindAsync(isp.StudijskiProgramId))!.Naziv,
                GodinaStudijaId       = isp.GodinaStudijaId,
                GodinaNaziv = god == null ? "" : $"{god.Broj}. godina" + (god.Smjer is null ? "" : $" ({god.Smjer.Naziv})"),
                PredmetId    = isp.PredmetId,
                ProfesorId   = isp.ProfesorId,
                ProstorijaId = isp.ProstorijaId,
                IspitniRokId = isp.IspitniRokId,
                Datum        = isp.Datum.ToDateTime(TimeOnly.MinValue),
                VrijemeOd    = isp.VrijemeOd.ToTimeSpan(),
                VrijemeDo    = isp.VrijemeDo.ToTimeSpan(),
                Tip          = isp.Tip,
                GoogleEventId = isp.GoogleEventId,
                Predmeti     = await GetPredmetiSelect(isp.StudijskiProgramId, isp.GodinaStudijaId),
                Profesori    = await GetProfesoriSelect(selectedId: isp.ProfesorId),
                Prostorije   = await GetProstorijeSelect(),
                IspitniRoks  = await GetIspitniRokoviSelect()
            };

            ViewBag.ExistingBookings = await BuildExistingBookingsAsync(excludeIspitId: id);
            ViewBag.FullDates        = await BuildFullDatesAsync(isp.StudijskiProgramId, isp.GodinaStudijaId,
                                                                 excludeIspitId: id);

            return View("Edit", vm);
        }

        // ───────────────────────────────────────────────────────────────
        // EDIT POST
        // ───────────────────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CreateExamVM vm)
        {
            var prog = await _ctx.StudijskiPrograms.FindAsync(vm.StudijskiProgramId);
            if (prog == null) return NotFound();
            if (!(await _auth.AuthorizeAsync(User, prog, "CanEditStudijskiProgram")).Succeeded)
                return Forbid();

            await ValidateExamAsync(vm, excludeIspitId: vm.IspitId);
            if (!ModelState.IsValid)
            {
                await PopulateEditScaffoldingAsync(vm);
                await RepopulateSelectsAsync(vm);
                return View(vm);
            }

            var ent = await _ctx.Ispits.FindAsync(vm.IspitId);
            if (ent == null) return NotFound();

            ent.PredmetId    = vm.PredmetId;
            ent.ProfesorId   = vm.ProfesorId;
            ent.ProstorijaId = vm.ProstorijaId;
            ent.IspitniRokId = vm.IspitniRokId;
            ent.Datum        = DateOnly.FromDateTime(vm.Datum);
            ent.VrijemeOd    = TimeOnly.FromTimeSpan(vm.VrijemeOd);
            ent.VrijemeDo    = TimeOnly.FromTimeSpan(vm.VrijemeDo);
            ent.Tip          = vm.Tip;
            ent.LastModified = DateTime.UtcNow;

            var pred = await _ctx.Predmets.FindAsync(ent.PredmetId);
            var prof = await _ctx.Profesors.FindAsync(ent.ProfesorId);
            var prst = await _ctx.Prostorijas.FindAsync(ent.ProstorijaId);
            var rok  = await _ctx.IspitniRoks.FindAsync(ent.IspitniRokId);

            var start  = vm.Datum.Date + vm.VrijemeOd;
            var end    = vm.Datum.Date + vm.VrijemeDo;
            string nas = $"{pred!.Naziv} – {ent.Tip}";
            string opis= $"Profesor: {prof!.ImePrezime}\nProstorija: {prst!.Oznaka}\nRok: {rok!.Naziv}";

            try
            {
                if (string.IsNullOrEmpty(ent.GoogleEventId))
                {
                    var evId = await _gcal.AddEventAsync(ent.StudijskiProgramId, ent.GodinaStudijaId,
                                                         EventTypeExam, nas, start, end, opis);
                    ent.GoogleEventId = evId;
                }
                else
                {
                    await _gcal.UpdateEventAsync(ent.StudijskiProgramId, ent.GodinaStudijaId,
                                                 EventTypeExam, ent.GoogleEventId, nas, start, end, opis);
                }
                _ctx.Ispits.Update(ent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Greška pri ažuriranju Google eventa za ispit #{Id}: {Msg}",
                    ent.Id, ex.Message);

                var payload = JsonSerializer.Serialize(new
                {
                    ent.Id, ent.Datum, ent.VrijemeOd, ent.VrijemeDo, ent.Tip,
                    ent.StudijskiProgramId, ent.GodinaStudijaId
                });
                _ctx.OutboxEvents.Add(new OutboxEvent
                {
                    IspitId    = ent.Id,
                    Payload    = payload,
                    EventType  = "Update",
                    Processed  = false,
                    CreatedAt  = DateTime.UtcNow
                });
            }

            await _ctx.SaveChangesAsync();

            return RedirectToAction("Ispiti", "Program",
                new { programId = ent.StudijskiProgramId, godinaId = ent.GodinaStudijaId });
        }

        // ───────────────────────────────────────────────────────────────
        // DELETE GET
        // ───────────────────────────────────────────────────────────────
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

            var prog = await _ctx.StudijskiPrograms.FindAsync(isp.StudijskiProgramId);
            if (prog == null) return NotFound();
            if (!(await _auth.AuthorizeAsync(User, prog, "CanEditStudijskiProgram")).Succeeded)
                return Forbid();

            ViewData["CurrentProgramId"] = isp.StudijskiProgramId.ToString();

            var vm = new DeleteExamVM
            {
                IspitId          = isp.Id,
                PredmetNaziv     = isp.Predmet.Naziv,
                ProfesorIme      = isp.Profesor.ImePrezime,
                Datum            = isp.Datum.ToString("yyyy-MM-dd"),
                VrijemeOd        = isp.VrijemeOd.ToString(@"hh\:mm"),
                VrijemeDo        = isp.VrijemeDo.ToString(@"hh\:mm"),
                ProstorijaOzn    = isp.Prostorija.Oznaka,
                RokNaziv         = isp.IspitniRok.Naziv,
                StudijskiProgramId = isp.StudijskiProgramId,
                GodinaStudijaId    = isp.GodinaStudijaId
            };
            return View(vm);
        }

        // ───────────────────────────────────────────────────────────────
        // DELETE POST
        // ───────────────────────────────────────────────────────────────
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var isp = await _ctx.Ispits.FindAsync(id);
            if (isp == null) return NotFound();

            var prog = await _ctx.StudijskiPrograms.FindAsync(isp.StudijskiProgramId);
            if (prog == null) return NotFound();
            if (!(await _auth.AuthorizeAsync(User, prog, "CanEditStudijskiProgram")).Succeeded)
                return Forbid();

            if (!string.IsNullOrEmpty(isp.GoogleEventId))
            {
                var calendarId = await _ctx.CalendarConfigs
                    .AsNoTracking()
                    .Where(c => c.StudijskiProgramId == isp.StudijskiProgramId &&
                                c.GodinaStudijaId    == isp.GodinaStudijaId &&
                                c.EventType          == EventTypeExam)
                    .Select(c => c.CalendarId)
                    .FirstOrDefaultAsync();

                var deletePayload = JsonSerializer.Serialize(new
                {
                    CalendarId    = calendarId,
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

                await _ctx.SaveChangesAsync();
            }

            _ctx.Ispits.Remove(isp);
            await _ctx.SaveChangesAsync();

            return RedirectToAction("Ispiti", "Program",
                new { programId = isp.StudijskiProgramId, godinaId = isp.GodinaStudijaId });
        }

        // ───────────────────────────────────────────────────────────────
        // DUPLICATE GET
        // ───────────────────────────────────────────────────────────────
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

            var prog = await _ctx.StudijskiPrograms.FindAsync(isp.StudijskiProgramId);
            if (prog == null) return NotFound();
            if (!(await _auth.AuthorizeAsync(User, prog, "CanEditStudijskiProgram")).Succeeded)
                return Forbid();

            ViewData["CurrentProgramId"] = isp.StudijskiProgramId.ToString();

            var vm = new CreateExamVM
            {
                StudijskiProgramId    = isp.StudijskiProgramId,
                StudijskiProgramNaziv = isp.Predmet.StudijskiProgram.Naziv,
                GodinaStudijaId       = isp.GodinaStudijaId,
                GodinaNaziv           = $"{isp.Predmet.GodinaStudija.Broj}. godina",
                PredmetId    = isp.PredmetId,
                ProfesorId   = isp.ProfesorId,
                ProstorijaId = isp.ProstorijaId,
                IspitniRokId = isp.IspitniRokId,
                Datum        = isp.Datum.ToDateTime(TimeOnly.MinValue),
                VrijemeOd    = isp.VrijemeOd.ToTimeSpan(),
                VrijemeDo    = isp.VrijemeDo.ToTimeSpan(),
                Tip          = isp.Tip,
                Predmeti     = await GetPredmetiSelect(programId, godinaId),
                Profesori    = await GetProfesoriSelect(),
                Prostorije   = await GetProstorijeSelect(),
                IspitniRoks  = await GetIspitniRokoviSelect()
            };

            ViewBag.FullDates         = await BuildFullDatesAsync(programId, godinaId, excludeIspitId: id);
            ViewBag.ExistingBookings  = await BuildExistingBookingsAsync(excludeIspitId: id);

            return View("Create", vm);
        }

        // ───────────────────────────────────────────────────────────────
        // PRINT ISPITI
        // ───────────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult PrintIspiti(int programId, int godinaId, int month)
        {
            var lista = _ctx.Ispits
                .AsNoTracking()
                .Where(i => i.StudijskiProgramId == programId &&
                            i.GodinaStudijaId    == godinaId &&
                            i.Datum.Month        == month)
                .OrderBy(i => i.Datum)
                .ThenBy(i => i.VrijemeOd)
                .Select(i => new IspitPrintItem
                {
                    Datum          = i.Datum,
                    VrijemeOd      = i.VrijemeOd,
                    VrijemeDo      = i.VrijemeDo,
                    PredmetNaziv   = i.Predmet.Naziv,
                    ProfesorIme    = i.Profesor.ImePrezime,
                    Titula         = i.Profesor.Titula,
                    ProstorijaOpis = i.Prostorija.Oznaka
                })
                .ToList();

            var programEntity = _ctx.StudijskiPrograms.Find(programId);
            if (programEntity == null)
                return NotFound($"Studijski program ({programId}) nije pronađen.");

            var godinaEntity = _ctx.GodinaStudijas.Find(godinaId);
            if (godinaEntity == null)
                return NotFound($"Godina studija ({godinaId}) nije pronađena.");

            var vm = new IspitiPrintVM
            {
                ProgramNaziv      = programEntity.Naziv,
                GodinaBroj        = godinaEntity.Broj,
                IzabraniMjesec    = month,
                Ispiti            = lista,
                StudijskiProgramId= programId,
                GodinaStudijaId   = godinaId
            };

            return View(vm);
        }

        // ───────────────────────────────────────────────────────────────
        // HELPERI (POPULATE, VALIDATE)
        // ───────────────────────────────────────────────────────────────

        private async Task PopulateCreateScaffoldingAsync(CreateExamVM vm)
        {
            ViewBag.ExistingBookings = await BuildExistingBookingsAsync();
            ViewBag.FullDates        = await BuildFullDatesAsync(vm.StudijskiProgramId, vm.GodinaStudijaId);
        }

        private async Task PopulateEditScaffoldingAsync(CreateExamVM vm)
        {
            ViewBag.ExistingBookings = await BuildExistingBookingsAsync(excludeIspitId: vm.IspitId);
            ViewBag.FullDates        = await BuildFullDatesAsync(vm.StudijskiProgramId, vm.GodinaStudijaId,
                                                                 excludeIspitId: vm.IspitId);
        }

        private async Task RepopulateSelectsAsync(CreateExamVM vm)
        {
            vm.Predmeti    = await GetPredmetiSelect(vm.StudijskiProgramId, vm.GodinaStudijaId);
            vm.Profesori   = await GetProfesoriSelect();
            vm.Prostorije  = await GetProstorijeSelect();
            vm.IspitniRoks = await GetIspitniRokoviSelect();
        }

        private async Task ValidateExamAsync(CreateExamVM vm, int? excludeIspitId)
        {
            if (vm.VrijemeOd >= vm.VrijemeDo)
                ModelState.AddModelError(nameof(vm.VrijemeOd),
                    "Vrijeme od mora biti prije vremena do.");

            if (vm.VrijemeOd < ExamsStart || vm.VrijemeDo > ExamsEnd)
                ModelState.AddModelError(nameof(vm.VrijemeOd),
                    $"Ispiti mogu biti samo između {ExamsStart:hh\\:mm} i {ExamsEnd:hh\\:mm}.");

            var datumOnly = DateOnly.FromDateTime(vm.Datum);

            var countIspita = await _ctx.Ispits
                .AsNoTracking()
                .Where(i =>
                    i.StudijskiProgramId == vm.StudijskiProgramId &&
                    i.GodinaStudijaId    == vm.GodinaStudijaId &&
                    i.Datum              == datumOnly &&
                    (!excludeIspitId.HasValue || i.Id != excludeIspitId.Value))
                .CountAsync();

            if (countIspita >= 2)
                ModelState.AddModelError(nameof(vm.Datum),
                    "Na taj datum već postoje dva ispita.");

            var nastave = await _ctx.Nastavas
                .AsNoTracking()
                .Where(n => n.Datum == datumOnly && n.ProstorijaId == vm.ProstorijaId)
                .Select(n => new { n.VrijemeOd, n.VrijemeDo })
                .ToListAsync();

            var ispiti = await _ctx.Ispits
                .AsNoTracking()
                .Where(i => i.Datum == datumOnly &&
                            i.ProstorijaId == vm.ProstorijaId &&
                            (!excludeIspitId.HasValue || i.Id != excludeIspitId.Value))
                .Select(i => new { i.VrijemeOd, i.VrijemeDo })
                .ToListAsync();

            var od  = vm.VrijemeOd;
            var doo = vm.VrijemeDo;
            bool overlaps =
                nastave.Any(b => od < b.VrijemeDo.ToTimeSpan() && doo > b.VrijemeOd.ToTimeSpan()) ||
                ispiti.Any(b => od < b.VrijemeDo.ToTimeSpan() && doo > b.VrijemeOd.ToTimeSpan());

            if (overlaps)
                ModelState.AddModelError(nameof(vm.ProstorijaId),
                    "Prostorija je zauzeta u odabranom terminu.");

            var predmetOk = await _ctx.Predmets
                .AsNoTracking()
                .AnyAsync(p =>
                    p.Id == vm.PredmetId &&
                    p.StudijskiProgramId == vm.StudijskiProgramId &&
                    p.GodinaStudijaId    == vm.GodinaStudijaId);

            if (!predmetOk)
                ModelState.AddModelError(nameof(vm.PredmetId),
                    "Nevažeći predmet za dati program/godinu.");

            var profOk = await _ctx.PredmetProfesors
                .AsNoTracking()
                .AnyAsync(pp =>
                    pp.PredmetId == vm.PredmetId && pp.ProfesorId == vm.ProfesorId);

            if (!profOk)
                ModelState.AddModelError(nameof(vm.ProfesorId),
                    "Profesor nije nosilac ovog predmeta.");

            var prostOk = await _ctx.Prostorijas
                .AsNoTracking()
                .AnyAsync(r => r.Id == vm.ProstorijaId);

            if (!prostOk)
                ModelState.AddModelError(nameof(vm.ProstorijaId),
                    "Nevažeća prostorija.");

            var rokOk = await _ctx.IspitniRoks
                .AsNoTracking()
                .AnyAsync(r => r.Id == vm.IspitniRokId);

            if (!rokOk)
                ModelState.AddModelError(nameof(vm.IspitniRokId),
                    "Nevažeći ispitni rok.");
        }

        private async Task<object> BuildExistingBookingsAsync(int? excludeIspitId = null)
        {
            var nastava = await _ctx.Nastavas
                .AsNoTracking()
                .Select(n => new { n.ProstorijaId, n.Datum, n.VrijemeOd, n.VrijemeDo })
                .ToListAsync();

            var ispiti = await _ctx.Ispits
                .AsNoTracking()
                .Where(i => !excludeIspitId.HasValue || i.Id != excludeIspitId.Value)
                .Select(i => new { i.ProstorijaId, i.Datum, i.VrijemeOd, i.VrijemeDo })
                .ToListAsync();

            return nastava.Concat(ispiti)
                .Select(x => new
                {
                    x.ProstorijaId,
                    Datum = x.Datum.ToString("yyyy-MM-dd"),
                    Od    = x.VrijemeOd.ToString("HH:mm"),
                    Do    = x.VrijemeDo.ToString("HH:mm")
                })
                .ToList();
        }

        private async Task<object> BuildFullDatesAsync(int programId, int godinaId, int? excludeIspitId = null)
        {
            var dates = await _ctx.Ispits
                .AsNoTracking()
                .Where(i =>
                    i.StudijskiProgramId == programId &&
                    i.GodinaStudijaId    == godinaId &&
                    (!excludeIspitId.HasValue || i.Id != excludeIspitId.Value))
                .GroupBy(i => i.Datum)
                .Where(g => g.Count() >= 2)
                .Select(g => g.Key)
                .ToListAsync();

            return dates.Select(d => d.ToString("yyyy-MM-dd")).ToList();
        }

        private async Task<SelectListItem[]> GetPredmetiSelect(int programId, int godinaId)
        {
            return await _ctx.Predmets
                .AsNoTracking()
                .Where(p => p.StudijskiProgramId == programId &&
                            p.GodinaStudijaId    == godinaId)
                .OrderBy(p => p.Naziv)
                .Select(p => new SelectListItem(p.Naziv, p.Id.ToString()))
                .ToArrayAsync();
        }

        private async Task<SelectListItem[]> GetProfesoriSelect(int? selectedId = null)
        {
            var items = await _ctx.Profesors
                .AsNoTracking()
                .OrderBy(p => p.ImePrezime)
                .Select(p => new SelectListItem(p.ImePrezime, p.Id.ToString()))
                .ToArrayAsync();

            if (selectedId.HasValue)
            {
                foreach (var it in items)
                    it.Selected = it.Value == selectedId.Value.ToString();
            }
            return items;
        }

        private async Task<SelectListItem[]> GetProstorijeSelect()
        {
            return await _ctx.Prostorijas
                .AsNoTracking()
                .OrderBy(r => r.Oznaka)
                .Select(r => new SelectListItem(r.Oznaka, r.Id.ToString()))
                .ToArrayAsync();
        }

        private async Task<SelectListItem[]> GetIspitniRokoviSelect()
        {
            return await _ctx.IspitniRoks
                .AsNoTracking()
                .OrderBy(r => r.Naziv)
                .Select(r => new SelectListItem(r.Naziv, r.Id.ToString()))
                .ToArrayAsync();
        }
    }
}
