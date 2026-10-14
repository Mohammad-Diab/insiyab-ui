using System.Globalization;

namespace PerfLab.Pages;

/// <summary>An order row of the sample data.</summary>
public sealed record Order(int Id, string Customer, string City, DateTime Date, decimal Amount, int Items, string Status, string Channel, string Agent, string Note);

/// <summary>Neutral sample data: orders, tickets and documents, the same on every run.</summary>
public static class Sample
{
    public static readonly string[] Customers =
    [
        "ليلى حداد", "عمر الخطيب", "سارة منصور", "كريم يوسف", "نور سليمان", "هادي عيسى", "ريم ناصر", "طارق عبدالله", "دانة سعيد", "ماجد فارس",
        "لينا جابر", "سامي حمدان", "هالة رشيد", "فادي داود", "جنى بكر", "وليد قاسم",
    ];

    public static readonly string[] Cities = ["الرياض", "عمّان", "القاهرة", "دبي", "الدوحة", "بيروت", "تونس", "مسقط", "الكويت", "الرباط"];

    public static readonly string[] Statuses = ["جديد", "قيد التجهيز", "مشحون", "مكتمل", "ملغى", "معلق"];

    public static readonly string[] Channels = ["المتجر", "الهاتف", "التطبيق", "البريد"];

    public static readonly string[] Words =
    [
        "تسليم الطلب", "مراجعة الفاتورة", "اجتماع الفريق", "مكالمة مورد", "جرد المخزون", "تحديث الأسعار", "متابعة تذكرة", "إرسال العرض", "تدقيق الحسابات", "تجهيز الشحنة",
    ];

    public static List<Order> Orders(int count)
    {
        var random = new Random(42);
        var start = new DateTime(2026, 1, 1);
        var list = new List<Order>(count);
        for (var i = 0; i < count; i++)
            list.Add(new Order(
                10000 + i,
                Customers[random.Next(Customers.Length)],
                Cities[random.Next(Cities.Length)],
                start.AddDays(random.Next(270)).AddMinutes(random.Next(1440)),
                Math.Round((decimal)(random.NextDouble() * 4000 + 20), 2),
                random.Next(1, 40),
                Statuses[random.Next(Statuses.Length)],
                Channels[random.Next(Channels.Length)],
                Customers[random.Next(Customers.Length)].Split(' ')[0],
                Words[random.Next(Words.Length)]));
        return list;
    }

    public static string Money(decimal value) => value.ToString("#,0.00", CultureInfo.InvariantCulture);
}
