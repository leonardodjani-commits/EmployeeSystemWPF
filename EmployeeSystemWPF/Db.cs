using MySql.Data.MySqlClient;

namespace EmployeeSystemWPF
{
    public class Db
    {
        private string connectionString =
            "server=127.0.0.1;port=3306;database=hr_db;uid=root;pwd=;";

        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(connectionString);
        }
    }
}