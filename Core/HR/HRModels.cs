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

    public static class HRSampleData
    {
        private static readonly (string Name, string Dept, string Title)[] Staff =
        [
            ("أحمد الحمصي", "الإدارة", "مدير عام"),
            ("محمد العتيبي", "المبيعات", "مندوب مبيعات"),
            ("سارة القحطاني", "المحاسبة", "محاسبة"),
            ("خالد الشمري", "المستودعات", "أمين مخزن"),
            ("نورة الدوسري", "الموارد البشرية", "أخصائية موارد بشرية"),
            ("فهد الغامدي", "المبيعات", "مندوب مبيعات"),
            ("ريم الحربي", "الاستيراد", "منسقة استيراد"),
            ("عبدالله الزهراني", "المستودعات", "عامل تحميل"),
        ];

        public static List<EmployeeModel> Generate(int count = 20)
        {
            var rnd = new Random(99);
            var shifts = new[] { "صباحي", "مسائي", "دوام كامل" };
            var list = new List<EmployeeModel>();

            for (int i = 1; i <= count; i++)
            {
                var s = Staff[rnd.Next(Staff.Length)];
                list.Add(new EmployeeModel
                {
                    EmployeeCode = $"EMP-{i:D3}",
                    FullName = i <= Staff.Length ? Staff[i - 1].Name : $"موظف {i}",
                    Department = s.Dept,
                    JobTitle = s.Title,
                    Phone = $"05{rnd.Next(10000000, 99999999)}",
                    HireDate = DateTime.Today.AddMonths(-rnd.Next(3, 60)),
                    BasicSalary = rnd.Next(4000, 15000),
                    Status = i == 5 ? EmployeeStatus.OnLeave : EmployeeStatus.Active,
                    Shift = shifts[rnd.Next(shifts.Length)]
                });
            }

            return list;
        }
    }
}
