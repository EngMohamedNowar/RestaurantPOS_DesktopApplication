// Services/ShiftService.cs
using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using PizzaPOS.Data;
using PizzaPOS.Models;

namespace PizzaPOS.Services
{
    public class ShiftService
    {
        readonly string _cs;

        public ShiftService() : this(DatabaseHelper.CS) { }

        /// <summary>بنفس النمط بتاع InventoryService وUserService — عشان
        /// الاختبارات تقدر تبني القاعدة في مسار مؤقت.</summary>
        public ShiftService(string connectionString) => _cs = connectionString;

        /// <summary>
        /// شرط اللي بيخلّي أوردر يتحسب في تسوية الوردية.
        ///
        /// كان 'completed' وبس — وده كان غلط كبير. البيع في الكاشير
        /// بيتحفظ بـ Status="new" (شاشة المطبخ هي اللي بتنقله بعدين
        /// لـ completed)، فأي أوردر ماوصلش لشاشة المطبخ كان بيتحسب
        /// صفر. النتيجة: expectedCash = فلوس البداية بس، وتقفيل
        /// الوردية بيقول إن الكاشير ضيّع كل مبيعات النقدية اليوم —
        /// والخزنة физически فيها الفلوس.
        ///
        /// 'completed' مش اختياري هنا كمان: لو خلّينا البيع يتحفظ
        /// completed فورًا، الأوردر هيختفي من شاشة المطبخ من أول
        /// لحظة (الشاشة بتعرض غير completed بس).
        ///
        /// أي أوردر مش ملغي ومش معلّق = اتحصّل فلوسه. ده نفس العُرف
        /// اللي 16 استعلام تاني في المشروع بيستخدمه.
        /// </summary>
        const string SaleStatusFilter = "o.Status NOT IN ('cancelled','held')";

        SqliteConnection Open()
        {
            var c = new SqliteConnection(_cs);
            c.Open();
            DatabaseHelper.Configure(c);
            return c;
        }

        /// <summary>
        /// قراءة نص آمنة. الـ schema بيفرض DEFAULT على OpenedAt وStatus،
        /// بس الـ DEFAULT مش حماية: صف قديم اتضاف بإيد وحده، أو عمود
        /// اتضاف لجدول قديم بـ ALTER TABLE من غير DEFAULT، سايبهم NULL.
        ///
        /// GetOpenShift كان بيقرأ r.GetString(4) على طول، فNULL واحد كان
        /// بيطلّع استثناء "data is NULL at ordinal 4" من شاشة الـ login،
        /// والرسالة مبيقولش ولا كلمة عن قاعدة البيانات. GetHistory كان
        /// فيه الحماية واللي كان ناقص، فالحماية بقت في الاتنين.
        /// </summary>
        static string Text(SqliteDataReader r, int i) => r.IsDBNull(i) ? "" : r.GetString(i);

        public Shift? GetOpenShift(int userId)
        {
            using var c = Open(); var cmd = c.CreateCommand();
            cmd.CommandText = $@"SELECT s.Id,s.UserId,u.FullName,s.OpeningCash,s.OpenedAt,s.Status,
                COALESCE(SUM(o.Total),0),COUNT(o.Id)
                FROM Shifts s JOIN Users u ON u.Id=s.UserId
                LEFT JOIN Orders o ON o.ShiftId=s.Id AND {SaleStatusFilter}
                WHERE s.UserId=@uid AND s.Status='open' GROUP BY s.Id ORDER BY s.Id DESC LIMIT 1";
            cmd.Parameters.AddWithValue("@uid", userId);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;
            return new Shift { Id = r.GetInt32(0), UserId = r.GetInt32(1), UserName = Text(r, 2), OpeningCash = r.GetDouble(3), OpenedAt = Text(r, 4), Status = Text(r, 5), TotalSales = r.GetDouble(6), OrderCount = r.GetInt32(7) };
        }

        public Shift OpenShift(int userId, double openingCash)
        {
            // تشيل BeforeOpenShift كان كلها UI: LoginWindow كان بيسأل
            // الأول، بس مفيش حماية في الخدمة نفسها. دبل كليك أو
            // استدعاء تاني = shiftين مفتوحين، والتسوية بتتقسّم عليهم
            // والـ difference في كل وحد فيهم بيبقى غلط.
            if (GetOpenShift(userId) != null)
                throw new InvalidOperationException(
                    "فيه وردية مفتوحة بالفعل لهذا المستخدم. اقفلها الأول.");

            using var c = Open(); var cmd = c.CreateCommand();
            cmd.CommandText = "INSERT INTO Shifts(UserId,OpeningCash) VALUES(@u,@oc); SELECT last_insert_rowid()";
            cmd.Parameters.AddWithValue("@u", userId); cmd.Parameters.AddWithValue("@oc", openingCash);
            long id = (long)cmd.ExecuteScalar()!;
            return new Shift { Id = (int)id, UserId = userId, OpeningCash = openingCash, OpenedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"), Status = "open" };
        }

        public Shift CloseShift(int shiftId, double closingCash)
        {
            using var conn = Open();
            var calc = conn.CreateCommand();
            calc.CommandText = $@"SELECT s.OpeningCash,COALESCE(SUM(CASE WHEN o.PayMethod='كاش' THEN o.Total ELSE 0 END),0),COALESCE(SUM(o.Total),0),COUNT(o.Id)
                FROM Shifts s LEFT JOIN Orders o ON o.ShiftId=s.Id AND {SaleStatusFilter} WHERE s.Id=@id GROUP BY s.Id";
            calc.Parameters.AddWithValue("@id", shiftId);
            using var r = calc.ExecuteReader(); r.Read();
            double openCash = r.GetDouble(0), cashSales = r.GetDouble(1), totalSales = r.GetDouble(2);
            int orders = r.GetInt32(3); r.Close();
            double expected = openCash + cashSales;
            double diff = closingCash - expected;
            var upd = conn.CreateCommand();
            upd.CommandText = "UPDATE Shifts SET ClosingCash=@cc,ExpectedCash=@ec,Difference=@d,ClosedAt=datetime('now','localtime'),Status='closed' WHERE Id=@id";
            upd.Parameters.AddWithValue("@cc", closingCash); upd.Parameters.AddWithValue("@ec", expected);
            upd.Parameters.AddWithValue("@d", diff); upd.Parameters.AddWithValue("@id", shiftId);
            upd.ExecuteNonQuery();
            return new Shift { Id = shiftId, OpeningCash = openCash, ClosingCash = closingCash, ExpectedCash = expected, Difference = diff, TotalSales = totalSales, OrderCount = orders, Status = "closed" };
        }

        public List<Shift> GetHistory()
        {
            using var c = Open(); var cmd = c.CreateCommand();
            cmd.CommandText = $@"SELECT s.Id,s.UserId,u.FullName,s.OpeningCash,s.ClosingCash,s.ExpectedCash,s.Difference,s.OpenedAt,s.ClosedAt,s.Status,COALESCE(SUM(o.Total),0),COUNT(o.Id)
                FROM Shifts s JOIN Users u ON u.Id=s.UserId LEFT JOIN Orders o ON o.ShiftId=s.Id AND {SaleStatusFilter}
                GROUP BY s.Id ORDER BY s.Id DESC LIMIT 30";
            var list = new List<Shift>();
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new Shift { Id = r.GetInt32(0), UserId = r.GetInt32(1), UserName = Text(r, 2), OpeningCash = r.GetDouble(3), ClosingCash = r.IsDBNull(4) ? null : r.GetDouble(4), ExpectedCash = r.GetDouble(5), Difference = r.GetDouble(6), OpenedAt = Text(r, 7), ClosedAt = r.IsDBNull(8) ? null : r.GetString(8), Status = Text(r, 9), TotalSales = r.GetDouble(10), OrderCount = r.GetInt32(11) });
            return list;
        }
    }
}

