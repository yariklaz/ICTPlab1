using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestBooking.Infrastructure; // Переконайся, що твій QuestBookingIcptContext знаходиться тут
using System;
using System.Linq;
using System.Threading.Tasks;

namespace QuestBooking.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChartsController : ControllerBase
    {
        private readonly QuestBookingIcptContext _context;

        public ChartsController(QuestBookingIcptContext context)
        {
            _context = context;
        }

        [HttpGet("GetChartData")]
        public async Task<IActionResult> GetChartData(DateTime? startDate, DateTime? endDate)
        {
            // 1. Починаємо формувати запит до БД (Include потрібен, щоб підтягнути зв'язки)
            var query = _context.Bookings
                .Include(b => b.Slot)
                .ThenInclude(s => s.Room)
                .AsQueryable();

            // 2. Фільтруємо по даті СЛОТА (бо дата гри лежить саме в таблиці Timeslots)
            if (startDate.HasValue)
            {
                query = query.Where(b => b.Slot.StartTime >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                // Включаємо весь останній обраний день до 23:59:59
                var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(b => b.Slot.StartTime <= endOfDay);
            }

            // 3. Витягуємо сирі, але вже відфільтровані по даті дані з бази
            var allBookings = await query.ToListAsync();

            // 4. ЗАЛІЗНИЙ ЩИТ: Захист від помилок 
            // (відкидаємо замовлення з випадково видаленими слотами чи кімнатами)
            var validBookings = allBookings
                .Where(b => b.Slot != null && b.Slot.Room != null)
                .ToList();

            // 5. Безпечне групування в оперативній пам'яті (щоб EF Core не падав)
            var popularityData = validBookings
                .GroupBy(b => b.Slot.Room.Title)
                .Select(g => new {
                    room = g.Key,
                    count = g.Count()
                })
                .ToList();

            var profitData = validBookings
                .GroupBy(b => b.Slot.Room.Title)
                .Select(g => new {
                    room = g.Key,
                    profit = g.Sum(b => b.TotalPrice)
                })
                .ToList();

            // 6. Повертаємо ідеальний JSON, який очікує наш JavaScript на фронтенді
            return Ok(new { popularity = popularityData, profit = profitData });
        }
    }
}