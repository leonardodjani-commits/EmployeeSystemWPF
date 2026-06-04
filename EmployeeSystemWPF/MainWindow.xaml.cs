using EmployeeSystemWPF;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace EmployeeSystemWPF
{
    public partial class MainWindow : Window
    {
        Db db = new Db();
        private int selectedEmployeeId = 0;
        private string selectedPhotoPath = "";
        private bool isDarkMode = false;
        private System.Windows.Threading.DispatcherTimer _clock;
        public MainWindow()
        {
            InitializeComponent();
            LoadDepartments();
            LoadEmployees_Click(null, null);
            LoadDashboardStats();
            StartClock();
        }

        private void LoadDepartments()
        {
            try
            {
                List<Department> departments = new List<Department>();

                using (MySqlConnection conn = db.GetConnection())
                {
                    conn.Open();
                    string query = "SELECT department_id, department_name FROM departments";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    MySqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        departments.Add(new Department
                        {
                            DepartmentId = Convert.ToInt32(reader["department_id"]),
                            DepartmentName = reader["department_name"].ToString()
                        });
                    }
                }

                DepartmentComboBox.ItemsSource = departments;
                FilterDepartmentComboBox.ItemsSource = departments;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void LoadDashboardStats()
        {
            try
            {
                using (MySqlConnection conn = db.GetConnection())
                {
                    conn.Open();

                    MySqlCommand totalEmployeesCmd = new MySqlCommand("SELECT COUNT(*) FROM employees", conn);
                    MySqlCommand totalDepartmentsCmd = new MySqlCommand("SELECT COUNT(*) FROM departments", conn);
                    MySqlCommand avgSalaryCmd = new MySqlCommand("SELECT AVG(salary) FROM employees", conn);
                    MySqlCommand maxSalaryCmd = new MySqlCommand("SELECT MAX(salary) FROM employees", conn);
                    MySqlCommand minSalaryCmd = new MySqlCommand("SELECT MIN(salary) FROM employees", conn);

                    TotalEmployeesText.Text = totalEmployeesCmd.ExecuteScalar().ToString();
                    TotalDepartmentsText.Text = totalDepartmentsCmd.ExecuteScalar().ToString();

                    object avgSalary = avgSalaryCmd.ExecuteScalar();
                    object maxSalary = maxSalaryCmd.ExecuteScalar();
                    object minSalary = minSalaryCmd.ExecuteScalar();

                    AverageSalaryText.Text = "$" + Convert.ToDecimal(avgSalary).ToString("0.00");
                    MaxSalaryText.Text = "$" + Convert.ToDecimal(maxSalary).ToString("0.00");
                    MinSalaryText.Text = "$" + Convert.ToDecimal(minSalary).ToString("0.00");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private List<Employee> ReadEmployees(MySqlDataReader reader)
        {
            List<Employee> employees = new List<Employee>();

            while (reader.Read())
            {
                string photoFile = reader["photo"].ToString();

                employees.Add(new Employee
                {
                    EmployeeId = Convert.ToInt32(reader["employee_id"]),
                    FirstName = reader["first_name"].ToString(),
                    LastName = reader["last_name"].ToString(),
                    Salary = Convert.ToDecimal(reader["salary"]),
                    Email = reader["email"].ToString(),
                    Phone = reader["phone"].ToString(),
                    HireDate = reader["hire_date"] == DBNull.Value
                        ? (DateTime?)null
                        : Convert.ToDateTime(reader["hire_date"]),
                    Department = reader["department_name"].ToString(),
                    Photo = string.IsNullOrEmpty(photoFile)
                        ? null
                        : @"C:\xampp\htdocs\hr_system\uploads\" + photoFile
                });
            }

            return employees;
        }

        private void LoadEmployees_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (MySqlConnection conn = db.GetConnection())
                {
                    conn.Open();

                    string query = @"
                        SELECT
                            e.employee_id,
                            e.first_name,
                            e.last_name,
                            e.salary,
                            e.email,
                            e.phone,
                            e.hire_date,
                            d.department_name,
                            e.photo
                        FROM employees e
                        LEFT JOIN departments d
                        ON e.department_id = d.department_id";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    List<Employee> employees = ReadEmployees(reader);

                    EmployeesGrid.ItemsSource = employees;
                    UpdateStatusBar("All", employees.Count);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ChoosePhoto_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif";

            if (dialog.ShowDialog() == true)
            {
                selectedPhotoPath = dialog.FileName;
                MessageBox.Show("Photo selected successfully.");
            }
        }

        private void AddEmployee_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(LastNameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(SalaryTextBox.Text) ||
                    DepartmentComboBox.SelectedValue == null)
                {
                    MessageBox.Show("Please fill all fields.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(SalaryTextBox.Text, out decimal salary) || salary <= 0)
                {
                    MessageBox.Show("Please enter a valid salary greater than 0.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (FirstNameTextBox.Text.Length < 2 || LastNameTextBox.Text.Length < 2)
                {
                    MessageBox.Show("First and Last name must have at least 2 characters.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int departmentId = Convert.ToInt32(DepartmentComboBox.SelectedValue);

                using (MySqlConnection conn = db.GetConnection())
                {
                    conn.Open();

                    string photoFileName = null;

                    if (!string.IsNullOrEmpty(selectedPhotoPath))
                    {
                        photoFileName = DateTime.Now.Ticks + "_" + Path.GetFileName(selectedPhotoPath);
                        string destinationPath = @"C:\xampp\htdocs\hr_system\uploads\" + photoFileName;
                        File.Copy(selectedPhotoPath, destinationPath, true);
                    }

                    string query = @"
                        INSERT INTO employees
                        (first_name, last_name, salary, department_id, email, phone, hire_date, photo)
                        VALUES
                        (@first_name, @last_name, @salary, @department_id, @email, @phone, @hire_date, @photo)";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@first_name", FirstNameTextBox.Text.Trim());
                    cmd.Parameters.AddWithValue("@last_name", LastNameTextBox.Text.Trim());
                    cmd.Parameters.AddWithValue("@salary", salary);
                    cmd.Parameters.AddWithValue("@department_id", departmentId);
                    cmd.Parameters.AddWithValue("@email", EmailTextBox.Text.Trim());
                    cmd.Parameters.AddWithValue("@phone", PhoneTextBox.Text.Trim());
                    cmd.Parameters.AddWithValue("@hire_date",
                        HireDatePicker.SelectedDate.HasValue
                        ? (object)HireDatePicker.SelectedDate.Value
                        : DBNull.Value);
                    cmd.Parameters.AddWithValue("@photo", (object)photoFileName ?? DBNull.Value);

                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("Employee added successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                selectedPhotoPath = "";
                ClearFields();
                LoadEmployees_Click(null, null);
                LoadDashboardStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void DeleteEmployee_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Employee selectedEmployee = EmployeesGrid.SelectedItem as Employee;

                if (selectedEmployee == null)
                {
                    MessageBox.Show("Please select an employee.", "Warning",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBoxResult result = MessageBox.Show(
                    $"Are you sure you want to delete {selectedEmployee.FirstName} {selectedEmployee.LastName}?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result != MessageBoxResult.Yes) return;

                using (MySqlConnection conn = db.GetConnection())
                {
                    conn.Open();
                    string query = "DELETE FROM employees WHERE employee_id = @id";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@id", selectedEmployee.EmployeeId);
                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("Employee deleted successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                ClearFields();
                LoadEmployees_Click(null, null);
                LoadDashboardStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void EditEmployee_Click(object sender, RoutedEventArgs e)
        {
            Employee selectedEmployee = EmployeesGrid.SelectedItem as Employee;

            if (selectedEmployee == null)
            {
                MessageBox.Show("Please select an employee.", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            selectedEmployeeId = selectedEmployee.EmployeeId;
            FirstNameTextBox.Text = selectedEmployee.FirstName;
            LastNameTextBox.Text = selectedEmployee.LastName;
            SalaryTextBox.Text = selectedEmployee.Salary.ToString();
            EmailTextBox.Text = selectedEmployee.Email;
            PhoneTextBox.Text = selectedEmployee.Phone;
            HireDatePicker.SelectedDate = selectedEmployee.HireDate;

            foreach (Department dept in DepartmentComboBox.Items)
            {
                if (dept.DepartmentName == selectedEmployee.Department)
                {
                    DepartmentComboBox.SelectedItem = dept;
                    break;
                }
            }
        }

        private void UpdateEmployee_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (selectedEmployeeId == 0)
                {
                    MessageBox.Show("Please select an employee to edit.", "Warning",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(LastNameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(SalaryTextBox.Text) ||
                    DepartmentComboBox.SelectedValue == null)
                {
                    MessageBox.Show("Please fill all fields.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(SalaryTextBox.Text, out decimal salary) || salary <= 0)
                {
                    MessageBox.Show("Please enter a valid salary greater than 0.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (FirstNameTextBox.Text.Length < 2 || LastNameTextBox.Text.Length < 2)
                {
                    MessageBox.Show("First and Last name must have at least 2 characters.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int departmentId = Convert.ToInt32(DepartmentComboBox.SelectedValue);
                string photoFileName = null;

                if (!string.IsNullOrEmpty(selectedPhotoPath))
                {
                    photoFileName = DateTime.Now.Ticks + "_" + Path.GetFileName(selectedPhotoPath);
                    string destinationPath = @"C:\xampp\htdocs\hr_system\uploads\" + photoFileName;
                    File.Copy(selectedPhotoPath, destinationPath, true);
                }

                using (MySqlConnection conn = db.GetConnection())
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
                        WHERE employee_id = @employee_id";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@first_name", FirstNameTextBox.Text.Trim());
                    cmd.Parameters.AddWithValue("@last_name", LastNameTextBox.Text.Trim());
                    cmd.Parameters.AddWithValue("@salary", salary);
                    cmd.Parameters.AddWithValue("@department_id", departmentId);
                    cmd.Parameters.AddWithValue("@email", EmailTextBox.Text.Trim());
                    cmd.Parameters.AddWithValue("@phone", PhoneTextBox.Text.Trim());
                    cmd.Parameters.AddWithValue("@hire_date",
                        HireDatePicker.SelectedDate.HasValue
                        ? (object)HireDatePicker.SelectedDate.Value
                        : DBNull.Value);
                    cmd.Parameters.AddWithValue("@employee_id", selectedEmployeeId);
                    cmd.Parameters.AddWithValue("@photo", (object)photoFileName ?? DBNull.Value);

                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("Employee updated successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                selectedPhotoPath = "";
                ClearFields();
                LoadEmployees_Click(null, null);
                LoadDashboardStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void SearchEmployee_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string search = SearchTextBox.Text.Trim();

                using (MySqlConnection conn = db.GetConnection())
                {
                    conn.Open();

                    string query = @"
                        SELECT
                            e.employee_id,
                            e.first_name,
                            e.last_name,
                            e.salary,
                            e.email,
                            e.phone,
                            e.hire_date,
                            d.department_name,
                            e.photo
                        FROM employees e
                        LEFT JOIN departments d
                        ON e.department_id = d.department_id
                        WHERE e.first_name LIKE @search
                           OR e.last_name LIKE @search
                           OR e.email LIKE @search
                           OR e.phone LIKE @search
                           OR d.department_name LIKE @search";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@search", "%" + search + "%");

                    MySqlDataReader reader = cmd.ExecuteReader();
                    List<Employee> employees = ReadEmployees(reader);

                    EmployeesGrid.ItemsSource = employees;
                    UpdateStatusBar("Search: " + search, employees.Count);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void FilterByDepartment_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (FilterDepartmentComboBox.SelectedValue == null) return;

            try
            {
                int departmentId = Convert.ToInt32(FilterDepartmentComboBox.SelectedValue);

                using (MySqlConnection conn = db.GetConnection())
                {
                    conn.Open();

                    string query = @"
                        SELECT
                            e.employee_id,
                            e.first_name,
                            e.last_name,
                            e.salary,
                            e.email,
                            e.phone,
                            e.hire_date,
                            d.department_name,
                            e.photo
                        FROM employees e
                        LEFT JOIN departments d
                        ON e.department_id = d.department_id
                        WHERE e.department_id = @department_id";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@department_id", departmentId);

                    MySqlDataReader reader = cmd.ExecuteReader();
                    List<Employee> employees = ReadEmployees(reader);

                    EmployeesGrid.ItemsSource = employees;
                    var dept = FilterDepartmentComboBox.SelectedItem as Department;
                    UpdateStatusBar("Department: " + dept.DepartmentName, employees.Count);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void About_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow about = new AboutWindow();
            about.ShowDialog();
        }
        private void ClearFilter_Click(object sender, RoutedEventArgs e)
        {
            FilterDepartmentComboBox.SelectedIndex = -1;
            LoadEmployees_Click(null, null);
        }

        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            isDarkMode = !isDarkMode;

            if (isDarkMode)
            {
                this.Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1a1a2e"));
                EmployeesGrid.Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#16213e"));
                EmployeesGrid.Foreground = System.Windows.Media.Brushes.White;
                EmployeesGrid.RowBackground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#16213e"));
                EmployeesGrid.AlternatingRowBackground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0f3460"));
                ThemeToggleButton.Content = "☀️ Light Mode";
            }
            else
            {
                this.Background = System.Windows.Media.Brushes.White;
                EmployeesGrid.Background = System.Windows.Media.Brushes.White;
                EmployeesGrid.Foreground = System.Windows.Media.Brushes.Black;
                EmployeesGrid.RowBackground = System.Windows.Media.Brushes.White;
                EmployeesGrid.AlternatingRowBackground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#f2f2f2"));
                ThemeToggleButton.Content = "🌙 Dark Mode";
            }
        }

        private void ExportToPDF_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var employees = EmployeesGrid.ItemsSource as List<Employee>;

                if (employees == null || employees.Count == 0)
                {
                    MessageBox.Show("No employees to export.", "Warning",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SaveFileDialog saveDialog = new SaveFileDialog();
                saveDialog.Filter = "PDF Files|*.pdf";
                saveDialog.FileName = "Employees_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

                if (saveDialog.ShowDialog() != true) return;

                using (var stream = new FileStream(saveDialog.FileName, FileMode.Create))
                {
                    Document doc = new Document(PageSize.A4.Rotate(), 25, 25, 30, 30);
                    PdfWriter.GetInstance(doc, stream);
                    doc.Open();

                    var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, new BaseColor(33, 37, 41));
                    var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10, BaseColor.WHITE);
                    var cellFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, new BaseColor(33, 37, 41));

                    var title = new Paragraph("Employee Management System\n\n", titleFont);
                    title.Alignment = Element.ALIGN_CENTER;
                    doc.Add(title);

                    var dateFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, new BaseColor(100, 100, 100));
                    var date = new Paragraph("Generated: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") + "\n\n", dateFont);
                    date.Alignment = Element.ALIGN_RIGHT;
                    doc.Add(date);

                    PdfPTable table = new PdfPTable(8);
                    table.WidthPercentage = 100;
                    table.SetWidths(new float[] { 0.5f, 1.5f, 1.5f, 1.5f, 2f, 1.5f, 1.5f, 1.5f });

                    BaseColor headerBg = new BaseColor(33, 37, 41);
                    BaseColor altRowBg = new BaseColor(242, 242, 242);

                    string[] headers = { "ID", "First Name", "Last Name", "Salary", "Email", "Phone", "Hire Date", "Department" };
                    foreach (string h in headers)
                    {
                        PdfPCell cell = new PdfPCell(new Phrase(h, headerFont));
                        cell.BackgroundColor = headerBg;
                        cell.HorizontalAlignment = Element.ALIGN_CENTER;
                        cell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        cell.Padding = 8;
                        table.AddCell(cell);
                    }

                    for (int i = 0; i < employees.Count; i++)
                    {
                        BaseColor rowColor = (i % 2 == 0) ? BaseColor.WHITE : altRowBg;

                        PdfPCell idCell = new PdfPCell(new Phrase(employees[i].EmployeeId.ToString(), cellFont));
                        PdfPCell firstCell = new PdfPCell(new Phrase(employees[i].FirstName, cellFont));
                        PdfPCell lastCell = new PdfPCell(new Phrase(employees[i].LastName, cellFont));
                        PdfPCell salaryCell = new PdfPCell(new Phrase("$" + employees[i].Salary.ToString("0.00"), cellFont));
                        PdfPCell emailCell = new PdfPCell(new Phrase(employees[i].Email ?? "", cellFont));
                        PdfPCell phoneCell = new PdfPCell(new Phrase(employees[i].Phone ?? "", cellFont));
                        PdfPCell hireDateCell = new PdfPCell(new Phrase(
                            employees[i].HireDate.HasValue
                            ? employees[i].HireDate.Value.ToString("dd/MM/yyyy")
                            : "", cellFont));
                        PdfPCell deptCell = new PdfPCell(new Phrase(employees[i].Department, cellFont));

                        foreach (PdfPCell cell in new[] { idCell, firstCell, lastCell, salaryCell, emailCell, phoneCell, hireDateCell, deptCell })
                        {
                            cell.BackgroundColor = rowColor;
                            cell.HorizontalAlignment = Element.ALIGN_CENTER;
                            cell.VerticalAlignment = Element.ALIGN_MIDDLE;
                            cell.Padding = 6;
                        }

                        table.AddCell(idCell);
                        table.AddCell(firstCell);
                        table.AddCell(lastCell);
                        table.AddCell(salaryCell);
                        table.AddCell(emailCell);
                        table.AddCell(phoneCell);
                        table.AddCell(hireDateCell);
                        table.AddCell(deptCell);
                    }

                    doc.Add(table);

                    var footerFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, new BaseColor(100, 100, 100));
                    var footer = new Paragraph("\nTotal Employees: " + employees.Count, footerFont);
                    footer.Alignment = Element.ALIGN_LEFT;
                    doc.Add(footer);

                    doc.Close();
                }

                MessageBox.Show("PDF exported successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var employees = EmployeesGrid.ItemsSource as List<Employee>;

                if (employees == null || employees.Count == 0)
                {
                    MessageBox.Show("No employees to export.", "Warning",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (var workbook = new ClosedXML.Excel.XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Employees");

                    var headerRow = worksheet.Row(1);
                    headerRow.Style.Font.Bold = true;
                    headerRow.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#212529");
                    headerRow.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;

                    worksheet.Cell(1, 1).Value = "ID";
                    worksheet.Cell(1, 2).Value = "First Name";
                    worksheet.Cell(1, 3).Value = "Last Name";
                    worksheet.Cell(1, 4).Value = "Salary";
                    worksheet.Cell(1, 5).Value = "Email";
                    worksheet.Cell(1, 6).Value = "Phone";
                    worksheet.Cell(1, 7).Value = "Hire Date";
                    worksheet.Cell(1, 8).Value = "Department";

                    for (int i = 0; i < employees.Count; i++)
                    {
                        int row = i + 2;
                        worksheet.Cell(row, 1).Value = employees[i].EmployeeId;
                        worksheet.Cell(row, 2).Value = employees[i].FirstName;
                        worksheet.Cell(row, 3).Value = employees[i].LastName;
                        worksheet.Cell(row, 4).Value = employees[i].Salary;
                        worksheet.Cell(row, 5).Value = employees[i].Email ?? "";
                        worksheet.Cell(row, 6).Value = employees[i].Phone ?? "";
                        worksheet.Cell(row, 7).Value = employees[i].HireDate.HasValue
                            ? employees[i].HireDate.Value.ToString("dd/MM/yyyy")
                            : "";
                        worksheet.Cell(row, 8).Value = employees[i].Department;

                        if (i % 2 == 0)
                        {
                            worksheet.Row(row).Style.Fill.BackgroundColor =
                                ClosedXML.Excel.XLColor.FromHtml("#f2f2f2");
                        }
                    }

                    worksheet.Column(4).Style.NumberFormat.Format = "$#,##0.00";
                    worksheet.Columns().AdjustToContents();

                    var range = worksheet.Range(1, 1, employees.Count + 1, 8);
                    range.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                    range.Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;

                    SaveFileDialog saveDialog = new SaveFileDialog();
                    saveDialog.Filter = "Excel Files|*.xlsx";
                    saveDialog.FileName = "Employees_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

                    if (saveDialog.ShowDialog() == true)
                    {
                        workbook.SaveAs(saveDialog.FileName);
                        MessageBox.Show("Exported successfully!", "Success",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void EmployeesGrid_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Employee selectedEmployee = EmployeesGrid.SelectedItem as Employee;
            if (selectedEmployee == null) return;

            selectedEmployeeId = selectedEmployee.EmployeeId;
            FirstNameTextBox.Text = selectedEmployee.FirstName;
            LastNameTextBox.Text = selectedEmployee.LastName;
            SalaryTextBox.Text = selectedEmployee.Salary.ToString();
            EmailTextBox.Text = selectedEmployee.Email;
            PhoneTextBox.Text = selectedEmployee.Phone;
            HireDatePicker.SelectedDate = selectedEmployee.HireDate;

            foreach (Department dept in DepartmentComboBox.Items)
            {
                if (dept.DepartmentName == selectedEmployee.Department)
                {
                    DepartmentComboBox.SelectedItem = dept;
                    break;
                }
            }

            FirstNameTextBox.Focus();
            StatusSelectedText.Text = "Editing: " + selectedEmployee.FirstName + " " + selectedEmployee.LastName;
        }

        private void UpdateStatusBar(string filter = "All", int count = 0)
        {
            StatusEmployeesText.Text = "Employees: " + count;
            StatusFilterText.Text = "Showing: " + filter;
            StatusLastUpdatedText.Text = "Last updated: " + DateTime.Now.ToString("HH:mm:ss");
            StatusSelectedText.Text = "Selected: None";
        }

        private void EmployeesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Employee selected = EmployeesGrid.SelectedItem as Employee;
            if (selected != null)
                StatusSelectedText.Text = "Selected: " + selected.FirstName + " " + selected.LastName;
            else
                StatusSelectedText.Text = "Selected: None";
        }

        private void StartClock()
        {
            _clock = new System.Windows.Threading.DispatcherTimer();
            _clock.Interval = TimeSpan.FromSeconds(1);
            _clock.Tick += (s, e) =>
            {
                StatusClockText.Text = "🕐 " + DateTime.Now.ToString("HH:mm:ss");
            };
            _clock.Start();
        }

        private void ClearFields()
        {
            FirstNameTextBox.Clear();
            LastNameTextBox.Clear();
            SalaryTextBox.Clear();
            EmailTextBox.Clear();
            PhoneTextBox.Clear();
            HireDatePicker.SelectedDate = null;
            DepartmentComboBox.SelectedIndex = -1;
            selectedEmployeeId = 0;
        }
    }
}