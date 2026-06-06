using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace EmployeeSystemWPF.DataLayer
{
    public class EmployeeRepository
    {
        private readonly Db _db = new Db();

        public List<Employee> GetAll()
        {
            var list = new List<Employee>();
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                string query = @"
                    SELECT e.employee_id, e.first_name, e.last_name,
                           e.salary, e.email, e.phone, e.hire_date, e.photo,
                           d.department_name
                    FROM employees e
                    LEFT JOIN departments d ON e.department_id = d.department_id";

                var cmd = new MySqlCommand(query, conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                    list.Add(MapEmployee(reader));
            }
            return list;
        }

        public List<Employee> Search(string search)
        {
            var list = new List<Employee>();
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                string query = @"
                    SELECT e.employee_id, e.first_name, e.last_name,
                           e.salary, e.email, e.phone, e.hire_date, e.photo,
                           d.department_name
                    FROM employees e
                    LEFT JOIN departments d ON e.department_id = d.department_id
                    WHERE e.first_name LIKE @s OR e.last_name LIKE @s
                       OR e.email LIKE @s OR e.phone LIKE @s
                       OR d.department_name LIKE @s";

                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@s", "%" + search + "%");
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                    list.Add(MapEmployee(reader));
            }
            return list;
        }

        public List<Employee> GetByDepartment(int departmentId)
        {
            var list = new List<Employee>();
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                string query = @"
                    SELECT e.employee_id, e.first_name, e.last_name,
                           e.salary, e.email, e.phone, e.hire_date, e.photo,
                           d.department_name
                    FROM employees e
                    LEFT JOIN departments d ON e.department_id = d.department_id
                    WHERE e.department_id = @id";

                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", departmentId);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                    list.Add(MapEmployee(reader));
            }
            return list;
        }

        public void Add(Employee emp, int departmentId, string photoFileName)
        {
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                string query = @"
                    INSERT INTO employees
                    (first_name, last_name, salary, department_id, email, phone, hire_date, photo)
                    VALUES
                    (@first_name, @last_name, @salary, @department_id, @email, @phone, @hire_date, @photo)";

                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@first_name", emp.FirstName);
                cmd.Parameters.AddWithValue("@last_name", emp.LastName);
                cmd.Parameters.AddWithValue("@salary", emp.Salary);
                cmd.Parameters.AddWithValue("@department_id", departmentId);
                cmd.Parameters.AddWithValue("@email", emp.Email ?? "");
                cmd.Parameters.AddWithValue("@phone", emp.Phone ?? "");
                cmd.Parameters.AddWithValue("@hire_date", emp.HireDate.HasValue ? (object)emp.HireDate.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@photo", (object)photoFileName ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        public void Update(Employee emp, int departmentId, string photoFileName)
        {
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                string query = @"
                    UPDATE employees
                    SET first_name = @first_name,
                        last_name = @last_name,
                        salary = @salary,
                        department_id = @department_id,
                        email = @email,
                        phone = @phone,
                        hire_date = @hire_date,
                        photo = IFNULL(@photo, photo)
                    WHERE employee_id = @id";

                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@first_name", emp.FirstName);
                cmd.Parameters.AddWithValue("@last_name", emp.LastName);
                cmd.Parameters.AddWithValue("@salary", emp.Salary);
                cmd.Parameters.AddWithValue("@department_id", departmentId);
                cmd.Parameters.AddWithValue("@email", emp.Email ?? "");
                cmd.Parameters.AddWithValue("@phone", emp.Phone ?? "");
                cmd.Parameters.AddWithValue("@hire_date", emp.HireDate.HasValue ? (object)emp.HireDate.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@photo", (object)photoFileName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", emp.EmployeeId);
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete(int employeeId)
        {
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                var cmd = new MySqlCommand("DELETE FROM employees WHERE employee_id = @id", conn);
                cmd.Parameters.AddWithValue("@id", employeeId);
                cmd.ExecuteNonQuery();
            }
        }

        public DashboardStats GetStats()
        {
            using (var conn = _db.GetConnection())
            {
                conn.Open();
                return new DashboardStats
                {
                    TotalEmployees = Convert.ToInt32(new MySqlCommand("SELECT COUNT(*) FROM employees", conn).ExecuteScalar()),
                    TotalDepartments = Convert.ToInt32(new MySqlCommand("SELECT COUNT(*) FROM departments", conn).ExecuteScalar()),
                    AverageSalary = Convert.ToDecimal(new MySqlCommand("SELECT AVG(salary) FROM employees", conn).ExecuteScalar()),
                    MaxSalary = Convert.ToDecimal(new MySqlCommand("SELECT MAX(salary) FROM employees", conn).ExecuteScalar()),
                    MinSalary = Convert.ToDecimal(new MySqlCommand("SELECT MIN(salary) FROM employees", conn).ExecuteScalar()),
                };
            }
        }

        private Employee MapEmployee(MySqlDataReader reader)
        {
            string photoFile = reader["photo"].ToString();
            return new Employee
            {
                EmployeeId = Convert.ToInt32(reader["employee_id"]),
                FirstName = reader["first_name"].ToString(),
                LastName = reader["last_name"].ToString(),
                Salary = Convert.ToDecimal(reader["salary"]),
                Email = reader["email"].ToString(),
                Phone = reader["phone"].ToString(),
                HireDate = reader["hire_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["hire_date"]),
                Department = reader["department_name"].ToString(),
                Photo = string.IsNullOrEmpty(photoFile) ? null : @"C:\xampp\htdocs\hr_system\uploads\" + photoFile
            };
        }
    }
}