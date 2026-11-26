using System.Data;
using MySql.Data.MySqlClient;

namespace CoffeeCare
{
    // Вся работа с MySQL через MySql.Data тут
    public static class Db
    {
        // Если у вас другой пароль / порт - меняйте здесь
        public const string ConnStr =
            "Server=127.0.0.1;Port=3306;Database=coffeecare;Uid=root;Pwd=;CharSet=utf8mb4;";

        static bool _inited = false;

        public static MySqlConnection Open()
        {
            var c = new MySqlConnection(ConnStr);
            c.Open();
            return c;
        }

        public static MySqlParameter P(string name, object value)
        {
            return new MySqlParameter(name, value ?? DBNull.Value);
        }

        public static DataTable Select(string sql, params MySqlParameter[] p)
        {
            using (var c = Open())
            using (var cmd = new MySqlCommand(sql, c))
            {
                cmd.Parameters.AddRange(p);
                var dt = new DataTable();
                using (var a = new MySqlDataAdapter(cmd))
                {
                    a.Fill(dt);
                }
                return dt;
            }
        }

        public static int Exec(string sql, params MySqlParameter[] p)
        {
            using (var c = Open())
            using (var cmd = new MySqlCommand(sql, c))
            {
                cmd.Parameters.AddRange(p);
                return cmd.ExecuteNonQuery();
            }
        }

        public static object Scalar(string sql, params MySqlParameter[] p)
        {
            using (var c = Open())
            using (var cmd = new MySqlCommand(sql, c))
            {
                cmd.Parameters.AddRange(p);
                return cmd.ExecuteScalar();
            }
        }

        // INSERT и вернуть новый id
        public static long Insert(string sql, params MySqlParameter[] p)
        {
            using (var c = Open())
            using (var cmd = new MySqlCommand(sql, c))
            {
                cmd.Parameters.AddRange(p);
                cmd.ExecuteNonQuery();
                return cmd.LastInsertedId;
            }
        }

        // Для комбобоксов: первый столбец - id, второй - текст
        public static List<KeyValuePair<int, string>> Items(string sql, params MySqlParameter[] p)
        {
            var list = new List<KeyValuePair<int, string>>();
            foreach (DataRow r in Select(sql, p).Rows)
            {
                list.Add(new KeyValuePair<int, string>(Convert.ToInt32(r[0]), Convert.ToString(r[1])));
            }
            return list;
        }

        // Создаем таблицу журнала входов и демо-данные (один раз за запуск)
        public static void Init()
        {
            if (_inited) return;

            Exec(@"CREATE TABLE IF NOT EXISTS login_log (
                     id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                     user_id INT NULL,
                     email VARCHAR(150) NOT NULL,
                     event_type VARCHAR(20) NOT NULL,
                     success TINYINT(1) NOT NULL,
                     message VARCHAR(255) NULL,
                     token_id VARCHAR(64) NULL,
                     machine_name VARCHAR(100) NULL,
                     created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                     CONSTRAINT login_log_fk_user FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE SET NULL
                   ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");

            Seeder.Run();
            _inited = true;
        }
    }
}
