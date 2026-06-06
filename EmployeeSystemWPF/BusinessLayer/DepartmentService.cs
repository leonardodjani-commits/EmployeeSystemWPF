using EmployeeSystemWPF.DataLayer;
using System.Collections.Generic;

namespace EmployeeSystemWPF.BusinessLayer
{
    public class DepartmentService
    {
        private readonly DepartmentRepository _repo = new DepartmentRepository();

        public List<Department> GetAllDepartments() => _repo.GetAll();
    }
}
