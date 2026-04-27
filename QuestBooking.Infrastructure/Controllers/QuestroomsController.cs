using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuestBooking.Domain.Model;
using QuestBooking.Infrastructure;
using QuestBooking.Services;

namespace QuestBooking.Infrastructure.Controllers
{
    public class QuestroomsController : Controller
    {
        private readonly QuestBookingIcptContext _context;

        private readonly IDataPortServiceFactory<Questroom> _dataPortFactory;

        public QuestroomsController(QuestBookingIcptContext context, IDataPortServiceFactory<Questroom> dataPortFactory)
        {
            _context = context;
            _dataPortFactory = dataPortFactory;
        }

        [HttpGet]
        public IActionResult Import() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile fileExcel, CancellationToken cancellationToken)
        {
            if (fileExcel == null || fileExcel.Length == 0)
            {
                TempData["ErrorMessage"] = "Будь ласка, оберіть Excel-файл.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                using var stream = fileExcel.OpenReadStream();
                var importService = _dataPortFactory.GetImportService("xlsx");

                // Отримуємо список помилок від сервісу
                List<string> importErrors = await importService.ImportFromStreamAsync(stream, cancellationToken);

                if (importErrors.Any())
                {
                    // Формуємо вміст текстового файлу
                    string logContent = $"Звіт про імпорт квестів від {DateTime.Now}\n";
                    logContent += "--------------------------------------------------\n";
                    logContent += string.Join("\n", importErrors);

                    byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(logContent);
                    string fileName = $"Import_Log_{DateTime.Now:yyyyMMdd_HHmm}.txt";

                    // Повідомляємо користувача, що були проблеми
                    TempData["ErrorMessage"] = "Імпорт завершено з попередженнями. Лог помилок завантажено.";

                    // Повертаємо файл логу
                    return File(fileBytes, "text/plain", fileName);
                }

                TempData["SuccessMessage"] = "Імпорт успішно завершено без помилок!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Критична помилка: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Export(CancellationToken cancellationToken)
        {
            var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            var exportService = _dataPortFactory.GetExportService(contentType);
            var memoryStream = new MemoryStream();

            await exportService.WriteToAsync(memoryStream, cancellationToken);
            await memoryStream.FlushAsync(cancellationToken);
            memoryStream.Position = 0;

            return new FileStreamResult(memoryStream, contentType)
            {
                FileDownloadName = $"questrooms_{DateTime.UtcNow.ToShortDateString()}.xlsx"
            };
        }

        // GET: Questrooms
        public async Task<IActionResult> Index()
        {
            return View(await _context.Questrooms.ToListAsync());
        }

        // GET: Questrooms/Details/5
        // GET: Questrooms/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Шукаємо кімнату в базі даних
            var questroom = await _context.Questrooms
                .FirstOrDefaultAsync(m => m.Id == id);

            if (questroom == null)
            {
                return NotFound();
            }

            // Головний рядок: повертаємо нашу красиву сторінку Details.cshtml замість RedirectToAction!
            return View(questroom);
        }

        // GET: Questrooms/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Questrooms/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,MaxPlayers,BasePrice,DurationMinutes,Id")] Questroom questroom)
        {
            if (ModelState.IsValid)
            {
                _context.Add(questroom);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(questroom);
        }

        // GET: Questrooms/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var questroom = await _context.Questrooms.FindAsync(id);
            if (questroom == null)
            {
                return NotFound();
            }
            return View(questroom);
        }

        // POST: Questrooms/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Title,Description,MaxPlayers,BasePrice,DurationMinutes,Id")] Questroom questroom)
        {
            if (id != questroom.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(questroom);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!QuestroomExists(questroom.Id))
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
            return View(questroom);
        }

        // GET: Questrooms/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var questroom = await _context.Questrooms
                .FirstOrDefaultAsync(m => m.Id == id);
            if (questroom == null)
            {
                return NotFound();
            }

            return View(questroom);
        }

        // POST: Questrooms/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var questroom = await _context.Questrooms.FindAsync(id);
            if (questroom != null)
            {
                _context.Questrooms.Remove(questroom);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool QuestroomExists(int id)
        {
            return _context.Questrooms.Any(e => e.Id == id);
        }


    }
}
