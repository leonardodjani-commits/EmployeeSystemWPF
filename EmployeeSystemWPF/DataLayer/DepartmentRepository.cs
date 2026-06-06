using MySql.Data.MySqlClient;
using System.Collections.Generic;

namespace EmployeeSystemWPF.DataLayer
{
    public class DepartmentRepository
    {
        private readonly Db _db = new Db();

        public List<Department> GetAll()
        {
            var list = new List<Department>();
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                var cmd = new MySqlCommand("SELECT department_id, department_name FROM departments", conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new Department
                    {
                        DepartmentId = Convert.ToInt32(reader["department_id"]),
                        DepartmentName = reader["department_name"].ToString()
                    });
                }
            }
            return list;
        }
    }
}