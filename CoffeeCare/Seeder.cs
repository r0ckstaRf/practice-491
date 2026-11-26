namespace CoffeeCare
{
    // Заполняет пустую БД демо-данными, чтобы было что показывать
    public static class Seeder
    {
        static bool Empty(string table)
        {
            return Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM " + table)) == 0;
        }

        static void AddUser(string name, string email, string pass, string role)
        {
            Db.Exec("INSERT INTO users (role_id, full_name, email, password_hash, is_active) " +
                    "VALUES ((SELECT id FROM roles WHERE code=@r), @n, @e, @h, 1)",
                    Db.P("@r", role), Db.P("@n", name), Db.P("@e", email),
                    Db.P("@h", BCrypt.Net.BCrypt.HashPassword(pass)));
        }

        public static void Run()
        {
            if (Empty("roles"))
            {
                Db.Exec("INSERT INTO roles (id, code, name) VALUES " +
                        "(1,'admin','Администратор'),(2,'dispatcher','Диспетчер')," +
                        "(3,'manager','Менеджер'),(4,'technician','Техник')");
            }

            if (Empty("users"))
            {
                AddUser("Админов Админ", "admin@coffee.local", "admin123", "admin");
                AddUser("Диспетчеров Дима", "dispatcher@coffee.local", "disp123", "dispatcher");
                AddUser("Менеджерова Мария", "manager@coffee.local", "manager123", "manager");
                AddUser("Техников Тимур", "tech@coffee.local", "tech123", "technician");
            }

            if (Empty("locations"))
            {
                Db.Exec("INSERT INTO locations (name, address, opening_hours) VALUES " +
                        "('БЦ Север','ул. Ленина, 1','08:00-22:00')," +
                        "('ТЦ Галерея','пр. Мира, 25','10:00-22:00')," +
                        "('Вокзал','Привокзальная пл., 3','круглосуточно')");
            }

            if (Empty("machine_models"))
            {
                Db.Exec("INSERT INTO machine_models (manufacturer, name, maintenance_interval_days) VALUES " +
                        "('DeLonghi','Magnifica',30),('Jura','E8',45)");
            }

            if (Empty("coffee_machines"))
            {
                var models = Db.Items("SELECT id, name FROM machine_models ORDER BY id");
                var locs = Db.Items("SELECT id, name FROM locations ORDER BY id");
                for (int i = 0; i < 6; i++)
                {
                    Db.Exec("INSERT INTO coffee_machines (model_id, location_id, serial_number, inventory_number, status, installed_at) " +
                            "VALUES (@m, @l, @s, @i, 'working', CURDATE())",
                            Db.P("@m", models[i % models.Count].Key),
                            Db.P("@l", locs[i % locs.Count].Key),
                            Db.P("@s", "SN-100" + (i + 1)),
                            Db.P("@i", "CM-00" + (i + 1)));
                }
            }

            if (Empty("drinks"))
            {
                Db.Exec("INSERT INTO drinks (name, volume_ml, price) VALUES " +
                        "('Эспрессо',60,90.00),('Капучино',250,150.00)," +
                        "('Латте',300,170.00),('Американо',200,110.00)");
            }

            if (Empty("sales"))
            {
                var machines = Db.Select("SELECT id, location_id FROM coffee_machines ORDER BY id");
                var drinks = Db.Select("SELECT id, price FROM drinks ORDER BY id");
                for (int i = 0; i < 12; i++)
                {
                    var m = machines.Rows[i % machines.Rows.Count];
                    var d = drinks.Rows[i % drinks.Rows.Count];
                    long saleId = Db.Insert(
                        "INSERT INTO sales (number, machine_id, location_id, sold_at, status) VALUES (@n,@m,@l,@t,'completed')",
                        Db.P("@n", "S-DEMO-" + (i + 1)),
                        Db.P("@m", m["id"]), Db.P("@l", m["location_id"]),
                        Db.P("@t", DateTime.Now.AddHours(-i * 5)));
                    Db.Exec("INSERT INTO sale_items (sale_id, drink_id, quantity, unit_price) VALUES (@s,@d,@q,@p)",
                        Db.P("@s", saleId), Db.P("@d", d["id"]), Db.P("@q", 1 + i % 3), Db.P("@p", d["price"]));
                }
            }

            if (Empty("work_orders"))
            {
                object disp = Db.Scalar("SELECT id FROM users WHERE email='dispatcher@coffee.local'");
                object tech = Db.Scalar("SELECT id FROM users WHERE email='tech@coffee.local'");
                var machines = Db.Select("SELECT id FROM coffee_machines ORDER BY id");
                if (disp != null && tech != null && machines.Rows.Count >= 3)
                {
                    string[] types = { "repair", "refill", "planned" };
                    string[] descr = { "Не греет воду", "Закончились зерна и молоко", "Плановое ТО, чистка" };
                    for (int i = 0; i < 3; i++)
                    {
                        long id = Db.Insert(
                            "INSERT INTO work_orders (number, machine_id, technician_id, created_by, maintenance_type, status, priority, description, due_at) " +
                            "VALUES (@n,@m,@t,@c,@ty,'assigned',@p,@d,@due)",
                            Db.P("@n", "WO-DEMO-" + (i + 1)), Db.P("@m", machines.Rows[i]["id"]),
                            Db.P("@t", tech), Db.P("@c", disp), Db.P("@ty", types[i]),
                            Db.P("@p", i + 1), Db.P("@d", descr[i]), Db.P("@due", DateTime.Now.AddDays(i + 1)));
                        Db.Exec("INSERT INTO work_order_history (work_order_id, changed_by, old_status, new_status, reason) " +
                                "VALUES (@w,@u,NULL,'assigned','Демо-заявка')", Db.P("@w", id), Db.P("@u", disp));
                    }
                }
            }
        }
    }
}
