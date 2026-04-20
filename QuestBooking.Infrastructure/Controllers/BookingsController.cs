using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuestBooking.Domain.Model;
using QuestBooking.Infrastructure;

namespace QuestBooking.Infrastructure.Controllers
{
    public class BookingsController : Controller
    {
        private readonly QuestBookingIcptContext _context;

        public BookingsController(QuestBookingIcptContext context)
        {
            _context = context;
        }

        // GET: Bookings (Для панелі Адміністратора)
        public async Task<IActionResult> Index()
        {
            var bookings = _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.Slot)
                    .ThenInclude(s => s.Room)
                .Include(b => b.Promocode)
                .OrderByDescending(b => b.CreatedAt);

            return View(await bookings.ToListAsync());
        }

        // GET: Bookings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.Promocode)
                .Include(b => b.Slot)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (booking == null) return NotFound();

            return View(booking);
        }

        // GET: Bookings/Create (Форма для клієнта)
        public IActionResult Create(int questroomId, DateTime? bookingDate)
        {
            ViewBag.QuestroomId = questroomId;

            DateTime selectedDate = bookingDate ?? DateTime.Today;
            ViewBag.SelectedDate = selectedDate.ToString("yyyy-MM-dd");

            var availableSlots = _context.Timeslots
                .Where(s => s.RoomId == questroomId && s.IsAvailable == true && s.StartTime.Date == selectedDate.Date)
                .OrderBy(s => s.StartTime)
                .ToList();

            ViewBag.SlotId = new SelectList(
                availableSlots.Select(s => new { Id = s.Id, Time = s.StartTime.ToString("HH:mm") }),
                "Id",
                "Time"
            );

            return View();
        }

        // POST: Bookings/Create (Збереження замовлення)
        // POST: Bookings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Booking booking, string clientName, string clientPhone, string clientEmail, DateTime BookingDate, string? enteredPromoCode, int questroomId)
        {
            // === ВНУТРІШНЯ ФУНКЦІЯ: Щоб не дублювати код при кожній помилці ===
            void ReloadFormState()
            {
                var availableSlots = _context.Timeslots
                    .Where(s => s.RoomId == questroomId && s.StartTime.Date == BookingDate.Date && s.IsAvailable == true)
                    .OrderBy(s => s.StartTime)
                    .ToList();

                ViewBag.SlotId = new SelectList(
                    availableSlots.Select(s => new { Id = s.Id, Time = s.StartTime.ToString("HH:mm") }),
                    "Id", "Time"
                );
                ViewBag.QuestroomId = questroomId;
                ViewBag.SelectedDate = BookingDate.ToString("yyyy-MM-dd");
            }
            // ====================================================================

            // 1. Валідація обов'язкових полів (Ім'я та Телефон)
            if (string.IsNullOrWhiteSpace(clientName) || string.IsNullOrWhiteSpace(clientPhone))
            {
                ModelState.AddModelError("", "Ім'я та телефон є обов'язковими для бронювання!");
                ReloadFormState(); // Викликаємо нашу зручну функцію
                return View(booking);
            }

            // 2. Валідація Телефону
            string cleanedPhone = Regex.Replace(clientPhone, @"[\s\-\(\)]", "");
            if (!Regex.IsMatch(cleanedPhone, @"^(?:\+380|380|0)\d{9}$"))
            {
                ModelState.AddModelError("", "Введіть коректний український номер телефону (наприклад: 0501234567 або +380501234567).");
                ReloadFormState();
                return View(booking);
            }

            // 3. ВАЛІДАЦІЯ EMAIL (Новий блок)
            if (!string.IsNullOrWhiteSpace(clientEmail))
            {
                string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                if (!Regex.IsMatch(clientEmail, emailPattern))
                {
                    ModelState.AddModelError("", "Будь ласка, введіть коректну адресу електронної пошти (наприклад: name@example.com).");
                    ReloadFormState();
                    return View(booking);
                }
            }

            // 4. Зводимо телефон до єдиного ідеального стандарту: +380...
            if (cleanedPhone.StartsWith("0"))
                cleanedPhone = "+38" + cleanedPhone;
            else if (cleanedPhone.StartsWith("380"))
                cleanedPhone = "+" + cleanedPhone;

            clientPhone = cleanedPhone;

            // 5. Робота з базою клієнтів
            var client = await _context.Clients.FirstOrDefaultAsync(c => c.Phone == clientPhone);

            if (client == null)
            {
                client = new Client
                {
                    FullName = clientName,
                    Phone = clientPhone,
                    Email = clientEmail
                };
                _context.Clients.Add(client);
                await _context.SaveChangesAsync();
            }
            else if (string.IsNullOrEmpty(client.Email) && !string.IsNullOrEmpty(clientEmail))
            {
                client.Email = clientEmail;
                _context.Update(client);
                await _context.SaveChangesAsync();
            }

            booking.ClientId = client.Id;

            var slot = await _context.Timeslots.FindAsync(booking.SlotId);
            var room = await _context.Questrooms.FindAsync(slot!.RoomId);
            decimal finalPrice = room!.BasePrice;

            // 6. ЛОГІКА ДЛЯ ТЕКСТОВОГО ПРОМОКОДУ
            if (!string.IsNullOrWhiteSpace(enteredPromoCode))
            {
                var promo = await _context.Promocodes.FirstOrDefaultAsync(p => p.Code == enteredPromoCode);

                if (promo != null && promo.ValidFrom <= DateTime.Now && promo.ValidTo >= DateTime.Now)
                {
                    decimal discountAmount = finalPrice * ((decimal)promo.DiscountPercent / 100);
                    finalPrice = finalPrice - discountAmount;

                    booking.PromocodeId = promo.Id;

                    promo.ValidTo = DateTime.Now;
                    _context.Update(promo);
                }
                else
                {
                    ModelState.AddModelError("", "Цей промокод недійсний або його вже було використано!");
                    ReloadFormState(); // І знову використовуємо нашу функцію!
                    return View(booking);
                }
            }
            else
            {
                booking.PromocodeId = null;
            }

            // 7. Збереження фінального бронювання
            booking.TotalPrice = finalPrice;
            booking.Status = "New";
            booking.CreatedAt = DateTime.Now;
            booking.BookingDate = BookingDate;

            ModelState.Remove("ClientId");
            ModelState.Remove("Client");
            ModelState.Remove("Slot");

            if (ModelState.IsValid)
            {
                _context.Add(booking);

                slot!.IsAvailable = false;
                _context.Update(slot);

                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Questrooms");
            }

            // На випадок непередбачених помилок моделі
            ReloadFormState();
            return View(booking);
        }

        // GET: Bookings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            ViewData["ClientId"] = new SelectList(_context.Clients, "Id", "FullName", booking.ClientId);
            ViewData["PromocodeId"] = new SelectList(_context.Promocodes, "Id", "Code", booking.PromocodeId);
            ViewData["SlotId"] = new SelectList(_context.Timeslots, "Id", "Id", booking.SlotId);
            return View(booking);
        }

        // POST: Bookings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ClientId,SlotId,PromocodeId,TotalPrice,Status,CreatedAt,BookingDate,Id")] Booking booking)
        {
            if (id != booking.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(booking);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookingExists(booking.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ClientId"] = new SelectList(_context.Clients, "Id", "FullName", booking.ClientId);
            ViewData["PromocodeId"] = new SelectList(_context.Promocodes, "Id", "Code", booking.PromocodeId);
            ViewData["SlotId"] = new SelectList(_context.Timeslots, "Id", "Id", booking.SlotId);
            return View(booking);
        }

        // GET: Bookings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.Promocode)
                .Include(b => b.Slot)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (booking == null) return NotFound();

            return View(booking);
        }

        // POST: Bookings/Delete/5 (Видалення та звільнення часу)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                var slot = await _context.Timeslots.FindAsync(booking.SlotId);
                if (slot != null)
                {
                    slot.IsAvailable = true;
                    _context.Timeslots.Update(slot);
                }

                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // --- ТОЙ САМИЙ ЗАГУБЛЕНИЙ МЕТОД ---
        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(e => e.Id == id);
        }
    }
}