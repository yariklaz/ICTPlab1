using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace QuestBooking.Services
{
    public interface IImportService<TEntity> where TEntity : class
    {
        Task<List<string>> ImportFromStreamAsync(Stream stream, CancellationToken cancellationToken);
    }

    public interface IExportService<TEntity> where TEntity : class
    {
        Task WriteToAsync(Stream stream, CancellationToken cancellationToken);
    }

    public interface IDataPortServiceFactory<TEntity> where TEntity : class
    {
        IImportService<TEntity> GetImportService(string contentType);
        IExportService<TEntity> GetExportService(string contentType);
    }
}