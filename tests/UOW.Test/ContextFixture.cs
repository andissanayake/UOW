using Dapper;
using MartinCostello.SqlLocalDb;
using System.Data;

namespace UOW.Test
{
    public class ContextFixture
    {
        private ISqlLocalDbInstanceInfo instance;
        public ICurrentUserService CurrentUserService { get; } = new CurrentUserService();
        public ContextFixture()
        {
            using var localDB = new SqlLocalDbApi();

            instance = localDB.GetOrCreateInstance($"Test{Guid.NewGuid()}");
            ISqlLocalDbInstanceManager manager = instance.Manage();

            if (!instance.IsRunning)
            {
                manager.Start();
            }

            using var connection1 = instance.CreateConnection();
            DatabaseInitializer.Migrate(connection1);

            using var connection2 = instance.CreateConnection();
            connection2.Open();
            connection2.Execute(@"
                delete from [dbo].[YourEntity1]
                delete from [dbo].[YourEntity2]
            ");
            connection2.Dispose();
        }
        public IDbConnection GetDbConnection()
        {
            return instance.CreateConnection();
        }
    }
}
