using Microsoft.Data.Sqlite;

namespace TanoDevClip.Infrastructure.Database
{
    public sealed class DatabaseConnectionFactory
    {
        private readonly string _databasePath;

        public DatabaseConnectionFactory(string databasePath)
        {
            _databasePath = databasePath;
        }

        public SqliteConnection CreateConnection()
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = _databasePath
            }.ToString();

            return new SqliteConnection(connectionString);
        }
    }
}