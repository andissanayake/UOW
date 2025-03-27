using System.Data;

namespace UOW
{
    public interface IUnitOfWork : IDisposable
    {
        IDbConnection Connection { get; }

        Task InsertAsync<T>(T entity) where T : class;
        Task UpdateAsync<T>(T entity) where T : class;
        Task DeleteAsync<T>(T entity) where T : class;
        Task<T?> GetAsync<T>(object id) where T : class;
        Task<IEnumerable<T>> GetAllAsync<T>() where T : class;
        Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null);
        Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null);
        Task<T> QuerySingleAsync<T>(string sql, object? param = null);
        Task<int> ExecuteAsync(string sql, object? param = null);
        Task BulkCopyAsync<T>(IEnumerable<T> items, string? tableName = null);
        void Commit();
        void Rollback();
    }
}
