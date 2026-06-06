using EmployeeSystemWPF.BusinessLayer;
using EmployeeSystemWPF.DataLayer;
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
        private readonly EmployeeService _employeeService = new EmployeeService();
        private readonly DepartmentService _departmentService = new DepartmentService();
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
                var departments = _departmentService.GetAllDepartments();
                DepartmentComboBox.ItemsSource = departments;
                FilterDepartmentComboBox.ItemsSource = departments;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void LoadDashboardStats()
        {
            try
            {
                var stats = _employeeService.GetDashboardStats();
                TotalEmployeesText.Text = stats.TotalEmployees.ToString();
                TotalDepartmentsText.Text = stats.TotalDepartments.ToString();
                AverageSalaryText.Text = "$" + stats.AverageSalary.ToString("0.00");
                MaxSalaryText.Text = "$" + stats.MaxSalary.ToString("0.00");
                MinSalaryText.Text = "$" + stats.MinSalary.ToString("0.00");
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void LoadEmployees_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var employees = _employeeService.GetAllEmployees();
                EmployeesGrid.ItemsSource = employees;
                UpdateStatusBar("All", employees.Count);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
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

        private bool ValidateFields()
        {
            bool isValid = true;

            FirstNameError.Visibility = Visibility.Collapsed;
            LastNameError.Visibility = Visibility.Collapsed;
            SalaryError.Visibility = Visibility.Collapsed;
            DepartmentError.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text) || FirstNameTextBox.Text.Length < 2)
            {
                FirstNameError.Text = "⚠ First name must have at least 2 characters.";
                FirstNameError.Visibility = Visibility.Visible;
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(LastNameTextBox.Text) || LastNameTextBox.Text.Length < 2)
            {
                LastNameError.Text = "⚠ Last name must have at least 2 characters.";
                LastNameError.Visibility = Visibility.Visible;
                isValid = false;
            }

            if (!decimal.TryParse(SalaryTextBox.Text, out decimal salary) || salary <= 0)
            {
                SalaryError.Text = "⚠ Enter a valid salary > 0.";
                SalaryError.Visibility = Visibility.Visible;
                isValid = false;
            }

            if (DepartmentComboBox.SelectedValue == null)
            {
                DepartmentError.Text = "⚠ Please select a department.";
                DepartmentError.Visibility = Visibility.Visible;
                isValid = false;
            }

            return isValid;
        }

        private void AddEmployee_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateFields()) return;

                int deptId = Convert.ToInt32(DepartmentComboBox.SelectedValue);

                var emp = new Employee
                {
                    FirstName = FirstNameTextBox.Text.Trim(),
                    LastName = LastNameTextBox.Text.Trim(),
                    Salary = decimal.Parse(SalaryTextBox.Text),
                    Email = EmailTextBox.Text.Trim(),
                    Phone = PhoneTextBox.Text.Trim(),
                    HireDate = HireDatePicker.SelectedDate
                };

                _employeeService.AddEmployee(emp, deptId, selectedPhotoPath);
                MessageBox.Show("Employee added successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                selectedPhotoPath = "";
                ClearFields();
                LoadEmployees_Click(null, null);
                LoadDashboardStats();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void DeleteEmployee_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selected = EmployeesGrid.SelectedItem as Employee;

                if (selected == null)
                {
                    MessageBox.Show("Please select an employee.", "Warning",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"Are you sure you want to delete {selected.FirstName} {selected.LastName}?",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes) return;

                _employeeService.DeleteEmployee(selected.EmployeeId);
                MessageBox.Show("Employee deleted successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                ClearFields();
                LoadEmployees_Click(null, null);
                LoadDashboardStats();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
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

                if (!ValidateFields()) return;

                int deptId = Convert.ToInt32(DepartmentComboBox.SelectedValue);

                var emp = new Employee
                {
                    EmployeeId = selectedEmployeeId,
                    FirstName = FirstNameTextBox.Text.Trim(),
                    LastName = LastNameTextBox.Text.Trim(),
                    Salary = decimal.Parse(SalaryTextBox.Text),
                    Email = EmailTextBox.Text.Trim(),
                    Phone = PhoneTextBox.Text.Trim(),
                    HireDate = HireDatePicker.SelectedDate
                };

                _employeeService.UpdateEmployee(emp, deptId, selectedPhotoPath);
                MessageBox.Show("Employee updated successfully.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                selectedPhotoPath = "";
                ClearFields();
                LoadEmployees_Click(null, null);
                LoadDashboardStats();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void SearchEmployee_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string search = SearchTextBox.Text.Trim();
                var employees = _employeeService.SearchEmployees(search);
                EmployeesGrid.ItemsSource = employees;
                UpdateStatusBar("Search: " + search, employees.Count);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void FilterByDepartment_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (FilterDepartmentComboBox.SelectedValue == null) return;
            try
            {
                int deptId = Convert.ToInt32(FilterDepartmentComboBox.SelectedValue);
                var employees = _employeeService.GetByDepartment(deptId);
                EmployeesGrid.ItemsSource = employees;
                var dept = FilterDepartmentComboBox.SelectedItem as Department;
                UpdateStatusBar("Department: " + dept.DepartmentName, employees.Count);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void ClearFilter_Click(object sender, RoutedEventArgs e)
        {
            FilterDepartmentComboBox.SelectedIndex = -1;
            LoadEmployees_Click(null, null);
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow about = new AboutWindow();
            about.ShowDialog();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to logout?",
                "Logout",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            _clock.Stop();

            LoginWindow login = new LoginWindow();
            Application.Current.MainWindow = login;
            login.Show();
            this.Close();
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
            catch (Exception ex) { MessageBox.Show(ex.Message); }
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
            catch (Exception ex) { MessageBox.Show(ex.Message); }
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

            FirstNameError.Visibility = Visibility.Collapsed;
            LastNameError.Visibility = Visibility.Collapsed;
            SalaryError.Visibility = Visibility.Collapsed;
            DepartmentError.Visibility = Visibility.Collapsed;
        }
    }
}