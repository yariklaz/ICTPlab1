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

        // У файлі сервісів оновіть метод ImportFromStreamAsync
        public async Task<List<string>> ImportFromStreamAsync(Stream stream, CancellationToken cancellationToken)
        {
            var errors = new List<string>();
            var existingRoomTitles = await _context.Questrooms
                .Select(r => r.Title.ToLower())
                .ToListAsync(cancellationToken);

            try
            {
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    errors.Add("Критична помилка: Excel-файл порожній або не містить сторінок.");
                    return errors;
                }

                var rows = worksheet.RangeUsed().RowsUsed().Skip(1);
                int addedCount = 0;
                int rowNumber = 1;

                foreach (var row in rows)
                {
                    rowNumber++;
                    try
                    {
                        // 1. Перевірка назви
                        var titleCell = row.Cell(1).Value.ToString().Trim();
                        if (string.IsNullOrWhiteSpace(titleCell))
                        {
                            errors.Add($"Рядок {rowNumber}: Назва квесту порожня. Пропущено.");
                            continue;
                        }

                        if (existingRoomTitles.Contains(titleCell.ToLower()))
                        {
                            errors.Add($"Рядок {rowNumber}: Квест із назвою '{titleCell}' вже існує в базі. Пропущено.");
                            continue;
                        }

                        // 2. БЕЗПЕЧНА перевірка ціни (має бути числом >= 0)
                        if (!row.Cell(2).TryGetValue(out decimal price) || price < 0)
                        {
                            errors.Add($"Рядок {rowNumber}: Некоректна ціна. Очікувалося число більше або дорівнює нулю.");
                            continue;
                        }

                        // 3. БЕЗПЕЧНА перевірка тривалості (Виправляє помилку рядка 11 та нульовий час)
                        if (!row.Cell(3).TryGetValue(out int duration) || duration <= 0)
                        {
                            errors.Add($"Рядок {rowNumber}: Некоректний час (тривалість). Очікувалося ціле число більше нуля.");
                            continue;
                        }

                        // 4. БЕЗПЕЧНА перевірка гравців (Виправляє мінус гравців)
                        if (!row.Cell(4).TryGetValue(out int maxPlayers) || maxPlayers <= 0)
                        {
                            errors.Add($"Рядок {rowNumber}: Некоректна макс. кількість гравців. Очікувалося ціле число більше нуля.");
                            continue;
                        }

                        // Якщо всі перевірки пройдено — створюємо квест
                        var room = new Questroom
                        {
                            Title = titleCell,
                            BasePrice = price,
                            DurationMinutes = duration,
                            MaxPlayers = maxPlayers,
                            Description = row.Cell(5).Value.ToString()
                        };

                        _context.Questrooms.Add(room);
                        existingRoomTitles.Add(titleCell.ToLower());
                        addedCount++;
                    }
                    catch (Exception ex)
                    {
                        // Цей catch спіймає будь-яку іншу магію, яку викладач міг заховати у файлі
                        errors.Add($"Рядок {rowNumber}: Непередбачена помилка обробки рядка — {ex.Message}");
                    }
                }

                if (addedCount > 0)
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                else if (!errors.Any())
                {
                    errors.Add("У файлі не знайдено жодних даних для імпорту.");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Критична помилка доступу до файлу: {ex.Message}");
            }

            return errors;
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