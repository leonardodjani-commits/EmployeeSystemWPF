using EmployeeSystemWPF.DataLayer;
using System;
using System.Collections.Generic;
using System.IO;

namespace EmployeeSystemWPF.BusinessLayer
{
    public class EmployeeService
    {
        private readonly EmployeeRepository _repo = new EmployeeRepository();

        public List<Employee> GetAllEmployees() => _repo.GetAll();

        public List<Employee> SearchEmployees(string search) => _repo.Search(search);

        public List<Employee> GetByDepartment(int departmentId) => _repo.GetByDepartment(departmentId);

        public DashboardStats GetDashboardStats() => _repo.GetStats();

        public string SavePhoto(string sourcePath)
        {
            if (string.IsNullOrEmpty(sourcePath)) return null;
            string fileName = DateTime.Now.Ticks + "_" + Path.GetFileName(sourcePath);
            string dest = @"C:\xampp\htdocs\hr_system\uploads\" + fileName;
            File.Copy(sourcePath, dest, true);
            return fileName;
        }

        public (bool isValid, string errorMessage) ValidateEmployee(string firstName, string lastName, string salary, int? departmentId)
        {
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(salary) || departmentId == null)
                return (false, "Please fill all required fields.");

            if (firstName.Length < 2 || lastName.Length < 2)
                return (false, "First and Last name must have at least 2 characters.");

            if (!decimal.TryParse(salary, out decimal sal) || sal <= 0)
                return (false, "Please enter a valid salary greater than 0.");

            return (true, null);
        }

        public void AddEmployee(Employee emp, int departmentId, string photoPath)
        {
            string photoFileName = SavePhoto(photoPath);
            _repo.Add(emp, departmentId, photoFileName);
        }

        public void UpdateEmployee(Employee emp, int departmentId, string photoPath)
        {
            string photoFileName = SavePhoto(photoPath);
            _repo.Update(emp, departmentId, photoFileName);
        }

        public void DeleteEmployee(int employeeId) => _repo.Delete(employeeId);
    }
}
