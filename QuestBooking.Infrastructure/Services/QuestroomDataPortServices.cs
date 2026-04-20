using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QuestBooking.Domain.Model;
using QuestBooking.Infrastructure;

namespace QuestBooking.Services
{
    // === СЕРВІС ЕКСПОРТУ ===
    public class QuestroomExportService : IExportService<Questroom>
    {
        private readonly QuestBookingIcptContext _context;
        private static readonly string[] HeaderNames = { "Назва", "Ціна", "Час (хв)", "Макс. гравців", "Опис" };

        public QuestroomExportService(QuestBookingIcptContext context)
        {
            _context = context;
        }

        public async Task WriteToAsync(Stream stream, CancellationToken cancellationToken)
        {
            var rooms = await _context.Questrooms.ToListAsync(cancellationToken);

            // Повертаємо класичний підхід: зберігаємо прямо в пам'ять
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Квест-кімнати");

            // Заголовки
            for (int i = 0; i < HeaderNames.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = HeaderNames[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            }

            // Дані
            int rowIndex = 2;
            foreach (var room in rooms)
            {
                worksheet.Cell(rowIndex, 1).Value = room.Title;
                worksheet.Cell(rowIndex, 2).Value = room.BasePrice;
                worksheet.Cell(rowIndex, 3).Value = room.DurationMinutes;
                worksheet.Cell(rowIndex, 4).Value = room.MaxPlayers;
                worksheet.Cell(rowIndex, 5).Value = string.IsNullOrWhiteSpace(room.Description) ? " " : room.Description;
                rowIndex++;
            }

            // Бібліотека оновлена, тому тепер цей рядок спрацює ідеально!
            workbook.SaveAs(stream);
        }
    }

    // === СЕРВІС ІМПОРТУ ===
    public class QuestroomImportService : IImportService<Questroom>
    {
        private readonly QuestBookingIcptContext _context;
        public QuestroomImportService(QuestBookingIcptContext context)
        {
            _context = context;
        }

        public async Task ImportFromStreamAsync(Stream stream, CancellationToken cancellationToken)
        {
            try
            {
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                    throw new Exception("Excel-файл порожній або не містить сторінок.");

                // Беремо всі рядки, крім першого (заголовків)
                var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

                // Завантажуємо назви ВСІХ існуючих кімнат з бази (у нижньому регістрі для точного порівняння)
                var existingRoomTitles = await _context.Questrooms
                    .Select(r => r.Title.ToLower())
                    .ToListAsync(cancellationToken);

                int addedCount = 0;

                foreach (var row in rows)
                {
                    var title = row.Cell(1).Value.ToString().Trim();

                    // Пропускаємо порожні рядки
                    if (string.IsNullOrWhiteSpace(title)) continue;

                    // ПЕРЕВІРКА НА ДУБЛІКАТ: якщо кімната з такою назвою вже є — пропускаємо її
                    if (existingRoomTitles.Contains(title.ToLower()))
                    {
                        continue;
                    }

                    // Якщо це нова кімната — створюємо її
                    var room = new Questroom
                    {
                        Title = title,
                        BasePrice = row.Cell(2).GetValue<decimal>(),
                        DurationMinutes = row.Cell(3).GetValue<int>(),
                        MaxPlayers = row.Cell(4).GetValue<int>(),
                        Description = row.Cell(5).Value.ToString()
                    };

                    _context.Questrooms.Add(room);

                    // Додаємо назву в наш список, щоб не додати два однакових рядки з самого Excel-файлу
                    existingRoomTitles.Add(title.ToLower());
                    addedCount++;
                }

                if (addedCount == 0)
                {
                    throw new Exception("Не знайдено нових кімнат для імпорту. Всі кімнати з файлу вже існують у базі.");
                }

                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                // Якщо сталася помилка формату, відсутності колонок тощо — кидаємо зрозуміле повідомлення
                throw new Exception($"Помилка імпорту: {ex.Message}");
            }
        }
    }

    // === ФАБРИКА ===
    public class QuestroomDataPortFactory : IDataPortServiceFactory<Questroom>
    {
        private readonly QuestBookingIcptContext _context;
        public QuestroomDataPortFactory(QuestBookingIcptContext context)
        {
            _context = context;
        }

        public IExportService<Questroom> GetExportService(string contentType) => new QuestroomExportService(_context);
        public IImportService<Questroom> GetImportService(string contentType) => new QuestroomImportService(_context);
    }
}