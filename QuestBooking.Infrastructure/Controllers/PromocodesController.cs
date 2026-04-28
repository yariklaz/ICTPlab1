using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuestBooking.Domain.Model;
using QuestBooking.Infrastructure;
using Microsoft.AspNetCore.Authorization;

namespace QuestBooking.Infrastructure.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PromocodesController : Controller
    {
        private readonly QuestBookingIcptContext _context;

        public PromocodesController(QuestBookingIcptContext context)
        {
            _context = context;
        }

        // GET: Promocodes
        public async Task<IActionResult> Index()
        {
            return View(await _context.Promocodes.ToListAsync());
        }

        // GET: Promocodes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var promocode = await _context.Promocodes
                .FirstOrDefaultAsync(m => m.Id == id);
            if (promocode == null)
            {
                return NotFound();
            }

            return View(promocode);
        }

        // GET: Promocodes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Promocodes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        // POST: Timeslots/Create
        // POST: Promocodes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Code,DiscountPercent,ValidFrom,ValidTo")] Promocode promocode)
        {
            if (ModelState.IsValid)
            {
                _context.Add(promocode);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(promocode);
        }

        // GET: Promocodes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var promocode = await _context.Promocodes.FindAsync(id);
            if (promocode == null)
            {
                return NotFound();
            }
            return View(promocode);
        }

        // POST: Promocodes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Code,DiscountPercent,ValidFrom,ValidTo,Id")] Promocode promocode)
        {
            if (id != promocode.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(promocode);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PromocodeExists(promocode.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(promocode);
        }

        // GET: Promocodes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var promocode = await _context.Promocodes
                .FirstOrDefaultAsync(m => m.Id == id);
            if (promocode == null)
            {
                return NotFound();
            }

            return View(promocode);
        }

        // POST: Promocodes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var promocode = await _context.Promocodes.FindAsync(id);
            if (promocode != null)
            {
                _context.Promocodes.Remove(promocode);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PromocodeExists(int id)
        {
            return _context.Promocodes.Any(e => e.Id == id);
        }
    }
}
