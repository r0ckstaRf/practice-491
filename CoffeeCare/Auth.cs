using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CoffeeCare
{
    // Вход, JWT-токен и журнал входов
    public static class Auth
    {
        // Секрет для подписи токена. Минимум 32 символа. В реальном проекте так не хранят!
        const string Secret = "CoffeeCare-very-secret-key-for-practice-2026-change-me";
        const string Issuer = "CoffeeCare";
        const string Audience = "CoffeeCareDesktop";

        public static string Token = "";
        public static string FullName = "";
        public static string RoleName = "";
        static string _email = "";

        static SymmetricSecurityKey Key()
        {
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
        }

        // ---------- вход ----------
        public static bool Login(string email, string password, out string error)
        {
            error = "";
            email = (email ?? "").Trim();

            if (email == "" || password == "")
            {
                error = "Введите email и пароль";
                return false;
            }

            // защита от перебора: 5 неудач за 10 минут
            int fails = Convert.ToInt32(Db.Scalar(
                "SELECT COUNT(*) FROM login_log WHERE email=@e AND event_type='login' AND success=0 " +
                "AND created_at > (NOW() - INTERVAL 10 MINUTE)", Db.P("@e", email)));
            if (fails >= 5)
            {
                Log(null, email, "login", false, "Слишком много неудачных попыток", null);
                error = "Слишком много попыток. Подождите 10 минут";
                return false;
            }

            var dt = Db.Select(
                "SELECT u.id, u.full_name, u.password_hash, u.is_active, r.code, r.name AS role_name " +
                "FROM users u JOIN roles r ON r.id = u.role_id WHERE u.email=@e",
                Db.P("@e", email));

            if (dt.Rows.Count == 0)
            {
                Log(null, email, "login", false, "Пользователь не найден", null);
                error = "Неверный email или пароль";
                return false;
            }

            var row = dt.Rows[0];
            int id = Convert.ToInt32(row["id"]);

            if (!Convert.ToBoolean(row["is_active"]))
            {
                Log(id, email, "login", false, "Учетная запись заблокирована", null);
                error = "Учетная запись заблокирована";
                return false;
            }

            if (!BCrypt.Net.BCrypt.Verify(password, Convert.ToString(row["password_hash"])))
            {
                Log(id, email, "login", false, "Неверный пароль", null);
                error = "Неверный email или пароль";
                return false;
            }

            // всё ок - делаем JWT на 12 часов
            string jti = Guid.NewGuid().ToString();
            var claims = new[]
            {
                new Claim("uid", id.ToString()),
                new Claim("role", Convert.ToString(row["code"])),
                new Claim("email", email),
                new Claim(JwtRegisteredClaimNames.Jti, jti)
            };
            var token = new JwtSecurityToken(
                issuer: Issuer,
                audience: Audience,
                claims: claims,
                notBefore: DateTime.UtcNow.AddSeconds(-5),
                expires: DateTime.UtcNow.AddHours(12),
                signingCredentials: new SigningCredentials(Key(), SecurityAlgorithms.HmacSha256));

            Token = new JwtSecurityTokenHandler().WriteToken(token);
            FullName = Convert.ToString(row["full_name"]);
            RoleName = Convert.ToString(row["role_name"]);
            _email = email;

            Log(id, email, "login", true, "Успешный вход", jti);
            return true;
        }

        // ---------- проверка токена ----------
        static JwtSecurityToken Read()
        {
            if (string.IsNullOrEmpty(Token)) return null;
            try
            {
                var p = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = Key(),
                    ValidateIssuer = true,
                    ValidIssuer = Issuer,
                    ValidateAudience = true,
                    ValidAudience = Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(10)
                };
                SecurityToken t;
                new JwtSecurityTokenHandler().ValidateToken(Token, p, out t);
                return (JwtSecurityToken)t;
            }
            catch
            {
                return null;
            }
        }

        static string GetClaim(string type)
        {
            var t = Read();
            if (t == null) return "";
            foreach (var c in t.Claims)
            {
                if (c.Type == type) return c.Value;
            }
            return "";
        }

        public static bool IsValid() { return Read() != null; }

        public static int UserId()
        {
            int id;
            int.TryParse(GetClaim("uid"), out id);
            return id;
        }

        public static string Role() { return GetClaim("role"); }

        // ---------- журнал ----------
        public static void Log(int? userId, string email, string type, bool ok, string message, string tokenId)
        {
            try
            {
                Db.Exec("INSERT INTO login_log (user_id, email, event_type, success, message, token_id, machine_name) " +
                        "VALUES (@u,@e,@t,@s,@m,@j,@pc)",
                        Db.P("@u", userId), Db.P("@e", email), Db.P("@t", type),
                        Db.P("@s", ok ? 1 : 0), Db.P("@m", message), Db.P("@j", tokenId),
                        Db.P("@pc", Environment.MachineName));
            }
            catch
            {
                // журнал не должен ронять программу
            }
        }

        public static void LogDenied(string what)
        {
            int uid = UserId();
            Log(uid == 0 ? (int?)null : uid, _email, "denied", false, "Нет прав: " + what, null);
        }

        public static void Logout()
        {
            int uid = UserId();
            if (uid != 0) Log(uid, _email, "logout", true, "Выход из системы", GetClaim("jti"));
            Token = "";
            FullName = "";
            RoleName = "";
            _email = "";
        }
    }
}
