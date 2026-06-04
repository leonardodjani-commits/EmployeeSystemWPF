namespace EmployeeSystemWPF
{
    public class Employee
    {
        public int EmployeeId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public decimal Salary { get; set; }
        public string? Department { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Photo { get; set; }
        public DateTime? HireDate { get; set; }

        public string YearsOfService
        {
            get
            {
                if (!HireDate.HasValue) return "N/A";

                var diff = DateTime.Today - HireDate.Value;
                int years = (int)(diff.TotalDays / 365);
                int months = (int)((diff.TotalDays % 365) / 30);

                if (years == 0)
                    return months + " month(s)";
                else if (months == 0)
                    return years + " year(s)";
                else
                    return years + " year(s) " + months + " month(s)";
            }
        }
        public string Initials
        {
            get
            {
                string first = string.IsNullOrEmpty(FirstName) ? "?" : FirstName[0].ToString();
                string last = string.IsNullOrEmpty(LastName) ? "" : LastName[0].ToString();
                return (first + last).ToUpper();
            }
        }
    }
}