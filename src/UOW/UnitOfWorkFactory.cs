using System.Data.SqlClient;

namespace UOW
{
    public interface IUnitOfWorkFactory
    {
        public IUnitOfWork CreateUOW();
    }
    public class UnitOfWorkFactory : IUnitOfWorkFactory
    {
        private readonly string _connectionString;
        private readonly ICurrentUserService _currentUserService;

        public UnitOfWorkFactory(string connectionString, ICurrentUserService currentUserService)
        {
            _connectionString = connectionString;
            _currentUserService = currentUserService;
        }

        public IUnitOfWork CreateUOW()
        {
            var connection = new SqlConnection(_connectionString);
            return new UnitOfWork(connection, _currentUserService);
        }
    }
}
