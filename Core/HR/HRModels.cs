namespace ERPSystem.Core.HR
{
    public enum EmployeeStatus { Active, OnLeave, Suspended, Terminated }

    public class EmployeeModel
    {
        public string EmployeeCode { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Department { get; set; } = "";
        public string JobTitle { get; set; } = "";
        public string Phone { get; set; } = "";
        public DateTime HireDate { get; set; }
        public decimal BasicSalary { get; set; }
        public EmployeeStatus Status { get; set; }
        public string Shift { get; set; } = "";

        public string StatusDisplay => Status switch
        {
            EmployeeStatus.Active => "نشط",
            EmployeeStatus.OnLeave => "في إجازة",
            EmployeeStatus.Suspended => "موقوف",
            EmployeeStatus.Terminated => "منتهي",
            _ => ""
        };
    }
}
