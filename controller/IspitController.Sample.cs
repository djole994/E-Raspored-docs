using System;
using System.Linq;
using System.Threading.Tasks;
using ERaspored.Models;                 // ERasporedContext, entities (Nastava, Ispit, ...)
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERaspored.Controllers.Samples
{
    /// <summary>
    /// Public sample excerpt of the real Exams controller.
    /// Shows: secured endpoint, read-only bookings aggregation, clean payload.
    /// Full create/edit/delete rules and external sync are private and documented in /docs.
    /// </summary>
    [Authorize(Roles = "Admin,Organizer,Organizator")]
    public sealed class IspitController : Controller
    {
        private readonly ERasporedContext _ctx;

        // Business-hours window is documented in /docs; shown here as constants for clarity.
        private static readonly TimeSpan ExamsStart = TimeSpan.FromHours(8);  // 08:00
        private static readonly TimeSpan ExamsEnd   = TimeSpan.FromHours(21); // 21:00

        public IspitController(ERasporedContext ctx) => _ctx = ctx;

        /// <summary>
        /// Returns room bookings (classes + exams) for a given date, optionally filtered by room.
        /// Used by the client to visualize occupied slots and prevent overlaps.
        /// </summary>
        /// <param name="date">Target date, format YYYY-MM-DD (e.g., 2025-01-20).</param>
        /// <param name="roomId">Optional room filter (0 = all rooms).</param>
        /// <param name="excludeId">Optional exam ID to exclude (useful when editing an existing exam).</param>
        /// <returns>
        /// 200 OK: Array of { ProstorijaId, Datum, Od, Do }.
        /// 400 Bad Request: invalid date format.
        /// </returns>
        [HttpGet("/api/bookings")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> ApiBookings([FromQuery] string date, [FromQuery] int roomId = 0, [FromQuery] int? excludeId = null)
        {
            // Safer parse: expect ISO date (YYYY-MM-DD)
            if (!DateOnly.TryParse(date, out var day))
                return BadRequest("Invalid date. Expected format: YYYY-MM-DD.");

            // Classes (Nastava) for the day
            var classesQ = _ctx.Nastavas
                .AsNoTracking()
                .Where(n => n.Datum == day)
                .Select(n => new { n.ProstorijaId, n.Datum, n.VrijemeOd, n.VrijemeDo });

            // Exams (Ispit) for the day (ignore soft-deleted)
            var examsQ = _ctx.Ispits
                .AsNoTracking()
                .Where(i => i.Datum == day && !i.IsDeleted);

            if (excludeId.HasValue)
                examsQ = examsQ.Where(i => i.Id != excludeId.Value);

            // Project to a common shape
            var examSlotsQ = examsQ.Select(i => new { i.ProstorijaId, i.Datum, i.VrijemeOd, i.VrijemeDo });

            // Optional room filter
            if (roomId > 0)
            {
                classesQ   = classesQ.Where(n => n.ProstorijaId == roomId);
                examSlotsQ = examSlotsQ.Where(i => i.ProstorijaId == roomId);
            }

            // Merge classes + exams → simple flat list
            var items = await classesQ.Concat(examSlotsQ).ToListAsync();

            // Final lightweight payload for the UI calendar/picker
            var payload = items.Select(x => new
            {
                ProstorijaId = x.ProstorijaId,
                Datum = x.Datum.ToString("yyyy-MM-dd"),
                Od    = x.VrijemeOd.ToString("HH:mm"),
                Do    = x.VrijemeDo.ToString("HH:mm")
            });

            return Ok(payload);
        }

        // NOTE:
        // - Create/Edit/Delete endpoints with validation rules
        //   (08:00–21:00, "max 2 exams per day per year", no room overlaps)
        //   and Google Calendar best-effort + Outbox retry are intentionally not public here.
        // - They are described in /docs (features/security) and covered by tests in the private codebase.
    }
}
