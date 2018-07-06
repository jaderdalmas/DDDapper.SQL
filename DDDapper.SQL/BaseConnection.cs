using System;
using System.Data.SqlClient;

namespace Back.Connection
{
    public class BaseConnection : IDisposable
    {
        /// <summary>
        /// Connection String "data base connection"
        /// </summary>
        private string connectionStrings;

        /// <summary>
        /// Create a Connection Repository
        /// </summary>
        /// <param name="connectionStrings">connection string</param>
        public BaseConnection(String connectionStrings)
        {
            this.connectionStrings = connectionStrings;
        }

        /// <summary>
        /// Connection to DataBase
        /// </summary>
        private SqlConnection dbConnection = null;
        public SqlConnection DbConnection
        {
            get
            {
                if (dbConnection == null)
                    dbConnection = new SqlConnection(this.connectionStrings);

                return dbConnection;
            }
        }

        /// <summary>
        /// Clean and Close Connections
        /// </summary>
        public void Dispose()
        {
            dbConnection.Close();
        }
    }
}
