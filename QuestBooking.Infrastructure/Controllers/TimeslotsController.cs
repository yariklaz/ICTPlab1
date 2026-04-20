using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuestBooking.Domain.Model;
using QuestBooking.Infrastructure;

namespace QuestBooking.Infrastructure.Controllers
{
    public class TimeslotsController : Controller
    {
        private readonly QuestBookingIcptContext _context;

        public TimeslotsController(QuestBookingIcptContext context)
        {
            _context = context;
        }

        // GET: Timeslots
        // GET: Timeslots
        // GET: Timeslots
        public async Task<IActionResult> Index()
        {
            // 1. Передаємо список кімнат для ВИПАДАЮЧОГО СПИСКУ (для кнопки "Очистити")
            ViewBag.RoomId = new SelectList(_context.Questrooms, "Id", "Title");

            // 2. Передаємо самі кімнати для ВКЛАДОК (щоб малювалися картки)
            ViewBag.Rooms = await _context.Questrooms.ToListAsync();

            // 3. Завантажуємо слоти
            var timeslots = _context.Timeslots.Include(t => t.Room).OrderBy(t => t.StartTime);

            return View(await timeslots.ToListAsync());
        }

        // GET: Timeslots/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var timeslot = await _context.Timeslots
                .Include(t => t.Room)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (timeslot == null)
            {
                return NotFound();
            }

            return View(timeslot);
        }

        // GET: Timeslots/Create
        public IActionResult Create()
        {
            ViewData["RoomId"] = new SelectList(_context.Questrooms, "Id", "Title");
            return View();
        }

        // POST: Timeslots/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        // POST: Timeslots/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RoomId,StartTime,IsAvailable,Id")] Timeslot timeslot)
        {
            // Знімаємо перевірку з навігаційної властивості
            ModelState.Remove("Room");

            if (ModelState.IsValid)
            {
                _context.Add(timeslot);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["RoomId"] = new SelectList(_context.Questrooms, "Id", "Title", timeslot.RoomId);
            return View(timeslot);
        }

        // GET: Timeslots/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var timeslot = await _context.Timeslots.FindAsync(id);
            if (timeslot == null)
            {
                return NotFound();
            }
            ViewData["RoomId"] = new SelectList(_context.Questrooms, "Id", "Title", timeslot.RoomId);
            return View(timeslot);
        }

        // POST: Timeslots/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RoomId,StartTime,IsAvailable,Id")] Timeslot timeslot)
        {
            if (id != timeslot.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(timeslot);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TimeslotExists(timeslot.Id))
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
            ViewData["RoomId"] = new SelectList(_context.Questrooms, "Id", "Title", timeslot.RoomId);
            return View(timeslot);
        }

        // GET: Timeslots/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var timeslot = await _context.Timeslots
                .Include(t => t.Room)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (timeslot == null)
            {
                return NotFound();
            }

            return View(timeslot);
        }

        // GET: Timeslots/Generate
        public IActionResult Generate()
        {
            // Передаємо список кімнат для випадаючого списку
            ViewData["RoomId"] = new SelectList(_context.Questrooms, "Id", "Title");
            return View();
        }

        // POST: Timeslots/Generate
        // POST: Timeslots/Generate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(int roomId, DateTime startDate, DateTime endDate, TimeSpan startTime, TimeSpan endTime, int intervalMinutes)
        {
            // Захист від помилок: якщо випадково кінець вибрали раніше початку
            if (startDate > endDate)
            {
                var temp = startDate;
                startDate = endDate;
                endDate = temp;
            }

            // Зовнішній цикл: перебираємо КОЖЕН ДЕНЬ у вказаному діапазоні
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                var currentTime = startTime;

                // Внутрішній цикл: створюємо слоти на поточний день
                while (currentTime < endTime)
                {
                    DateTime slotDateTime = date + currentTime;

                    // Перевіряємо, чи немає вже такого слоту, щоб уникнути дублікатів
                    if (!_context.Timeslots.Any(s => s.RoomId == roomId && s.StartTime == slotDateTime))
                    {
                        var slot = new Timeslot
                        {
                            RoomId = roomId,
                            StartTime = slotDateTime,
                            IsAvailable = true
                        };
                        _context.Timeslots.Add(slot);
                    }

                    currentTime = currentTime.Add(TimeSpan.FromMinutes(intervalMinutes));
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: Timeslots/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var timeslot = await _context.Timeslots.FindAsync(id);
            if (timeslot != null)
            {
                _context.Timeslots.Remove(timeslot);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TimeslotExists(int id)
        {
            return _context.Timeslots.Any(e => e.Id == id);
        }

        // POST: Timeslots/ClearAvailable
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearAvailable(int? roomId)
        {
            // Починаємо з вибору всіх вільних слотів
            var query = _context.Timeslots.Where(s => s.IsAvailable == true);

            // Якщо користувач обрав конкретну кімнату, фільтруємо за її ID
            if (roomId.HasValue && roomId.Value > 0)
            {
                query = query.Where(s => s.RoomId == roomId.Value);
            }

            var slotsToRemove = await query.ToListAsync();

            if (slotsToRemove.Any())
            {
                _context.Timeslots.RemoveRange(slotsToRemove);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
    }
