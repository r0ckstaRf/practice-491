using System.Data;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace CoffeeCare
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            txtUser.Text = Auth.FullName + "  (" + Auth.RoleName + ")";
            ApplyRoles();
            LoadAll();
        }

        // ================= служебное =================

        // кто что видит (по диаграмме вариантов использования)
        void ApplyRoles()
        {
            string r = Auth.Role();
            tabOrders.Visibility = V(r == "dispatcher" || r == "technician");
            tabMachines.Visibility = V(r == "admin" || r == "dispatcher");
            tabDrinks.Visibility = V(r == "manager");
            tabSales.Visibility = V(r == "manager");
            tabUsers.Visibility = V(r == "admin");
            tabLog.Visibility = V(r == "admin");

            // диспетчер создает заявки и назначает техников, техник оформляет протокол
            btnOrderNew.Visibility = V(r == "dispatcher");
            btnOrderAssign.Visibility = V(r == "dispatcher");
            btnOrderProtocol.Visibility = V(r == "technician");

            // кофемашинами и точками управляет только админ, диспетчер их только смотрит
            btnMachAdd.Visibility = V(r == "admin");
            btnMachStatus.Visibility = V(r == "admin");
            btnMachDelete.Visibility = V(r == "admin");
            btnLocAdd.Visibility = V(r == "admin");

            foreach (TabItem t in tabs.Items)
            {
                if (t.Visibility == Visibility.Visible)
                {
                    tabs.SelectedItem = t;
                    break;
                }
            }
        }

        static Visibility V(bool show) { return show ? Visibility.Visible : Visibility.Collapsed; }

        void LoadAll()
        {
            Safe(() =>
            {
                LoadOrders(); LoadMachines(); LoadDrinks();
                LoadSales(); LoadUsers(); LoadLog();
            });
        }

        // проверка JWT и роли перед каждым действием
        bool Check(params string[] roles)
        {
            if (!Auth.IsValid())
            {
                MessageBox.Show("Сессия закончилась (токен недействителен). Войдите заново.", "JWT");
                GoLogin();
                return false;
            }
            if (!roles.Contains(Auth.Role()))
            {
                Auth.LogDenied("доступ запрещен для роли " + Auth.Role());
                MessageBox.Show("У вас нет прав на это действие", "Доступ запрещен");
                return false;
            }
            return true;
        }

        void Safe(Action a)
        {
            try { a(); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        void GoLogin()
        {
            Auth.Token = "";
            new LoginWindow().Show();
            Close();
        }

        void Logout_Click(object sender, RoutedEventArgs e)
        {
            Auth.Logout();
            new LoginWindow().Show();
            Close();
        }

        int? SelectedId(DataGrid g)
        {
            var row = g.SelectedItem as DataRowView;
            if (row == null)
            {
                MessageBox.Show("Сначала выберите строку в таблице");
                return null;
            }
            return Convert.ToInt32(row["ID"]);
        }

        // ================= ЗАЯВКИ =================

        void LoadOrders()
        {
            string sql =
                "SELECT w.id AS `ID`, w.number AS `Номер`, m.inventory_number AS `Автомат`, l.name AS `Точка`, " +
                "w.maintenance_type AS `Тип`, w.status AS `Статус`, w.priority AS `Приоритет`, " +
                "IFNULL(t.full_name,'не назначен') AS `Техник`, w.due_at AS `Срок`, w.description AS `Описание` " +
                "FROM work_orders w " +
                "JOIN coffee_machines m ON m.id = w.machine_id " +
                "JOIN locations l ON l.id = m.location_id " +
                "LEFT JOIN users t ON t.id = w.technician_id " +
                "{W} ORDER BY w.due_at";

            DataTable dt;
            if (Auth.Role() == "technician")
                dt = Db.Select(sql.Replace("{W}", "WHERE w.technician_id=@u"), Db.P("@u", Auth.UserId()));
            else
                dt = Db.Select(sql.Replace("{W}", ""));
            gridOrders.ItemsSource = dt.DefaultView;
        }

        void OrdersRefresh_Click(object sender, RoutedEventArgs e) { Safe(LoadOrders); }

        void OrderNew_Click(object sender, RoutedEventArgs e)
        {
            if (!Check("dispatcher")) return;
            Safe(() =>
            {
                var r = Form.Show(this, "Новая заявка", new List<Field>
                {
                    new Field{ Key="machine", Label="Кофемашина", Items=Db.Items("SELECT id, inventory_number FROM coffee_machines ORDER BY inventory_number") },
                    new Field{ Key="type", Label="Тип работ", Options=new List<string>{ "repair", "refill", "planned" } },
                    new Field{ Key="desc", Label="Описание" },
                    new Field{ Key="prio", Label="Приоритет (1-3)", Text="2" },
                    new Field{ Key="days", Label="Срок, дней", Text="3" }
                });
                if (r == null) return;

                int prio, days;
                if (r["machine"] == "" || r["desc"] == "") { MessageBox.Show("Заполните кофемашину и описание"); return; }
                if (!int.TryParse(r["prio"], out prio) || prio < 1 || prio > 3) { MessageBox.Show("Приоритет должен быть 1, 2 или 3"); return; }
                if (!int.TryParse(r["days"], out days) || days < 1) { MessageBox.Show("Срок должен быть числом больше 0"); return; }

                long id = Db.Insert(
                    "INSERT INTO work_orders (number, machine_id, created_by, maintenance_type, status, priority, description, due_at) " +
                    "VALUES (@n,@m,@c,@t,'new',@p,@d,@due)",
                    Db.P("@n", "WO-" + DateTime.Now.ToString("yyyyMMdd-HHmmss")),
                    Db.P("@m", int.Parse(r["machine"])), Db.P("@c", Auth.UserId()),
                    Db.P("@t", r["type"]), Db.P("@p", prio), Db.P("@d", r["desc"]),
                    Db.P("@due", DateTime.Now.AddDays(days)));
                Db.Exec("INSERT INTO work_order_history (work_order_id, changed_by, old_status, new_status, reason) " +
                        "VALUES (@w,@u,NULL,'new','Заявка создана')", Db.P("@w", id), Db.P("@u", Auth.UserId()));
                LoadOrders();
            });
        }

        // общий метод смены статуса: пишет историю и освобождает кофемашину
        void ChangeStatus(int orderId, string newStatus, string reason)
        {
            object old = Db.Scalar("SELECT status FROM work_orders WHERE id=@i", Db.P("@i", orderId));
            if (newStatus == "cancelled")
                Db.Exec("UPDATE work_orders SET status=@s, cancellation_reason=@r WHERE id=@i",
                        Db.P("@s", newStatus), Db.P("@r", reason), Db.P("@i", orderId));
            else
                Db.Exec("UPDATE work_orders SET status=@s WHERE id=@i", Db.P("@s", newStatus), Db.P("@i", orderId));

            Db.Exec("INSERT INTO work_order_history (work_order_id, changed_by, old_status, new_status, reason) " +
                    "VALUES (@w,@u,@o,@n,@r)",
                    Db.P("@w", orderId), Db.P("@u", Auth.UserId()), Db.P("@o", old), Db.P("@n", newStatus),
                    Db.P("@r", reason == "" ? null : reason));

            if (newStatus == "done")
                Db.Exec("UPDATE coffee_machines SET status='working' WHERE id=(SELECT machine_id FROM work_orders WHERE id=@i)",
                        Db.P("@i", orderId));
            if (newStatus == "in_progress")
                Db.Exec("UPDATE coffee_machines SET status='maintenance' WHERE id=(SELECT machine_id FROM work_orders WHERE id=@i)",
                        Db.P("@i", orderId));
        }

        void OrderAssign_Click(object sender, RoutedEventArgs e)
        {
            if (!Check("dispatcher")) return;
            Safe(() =>
            {
                int? id = SelectedId(gridOrders);
                if (id == null) return;

                var techs = Db.Items("SELECT u.id, u.full_name FROM users u JOIN roles r ON r.id=u.role_id " +
                                     "WHERE r.code='technician' AND u.is_active=1");
                if (techs.Count == 0) { MessageBox.Show("В системе нет активных техников"); return; }

                var r = Form.Show(this, "Назначение техника", new List<Field>
                {
                    new Field{ Key="tech", Label="Техник", Items=techs }
                });
                if (r == null) return;
                int techId = int.Parse(r["tech"]);

                // предупреждение о перегрузе
                int open = Convert.ToInt32(Db.Scalar(
                    "SELECT COUNT(*) FROM work_orders WHERE technician_id=@t AND status NOT IN ('done','cancelled')",
                    Db.P("@t", techId)));
                if (open >= 5)
                {
                    MessageBox.Show("У техника уже " + open + " открытых заявок. Назначить нельзя (перегруз).");
                    return;
                }

                Db.Exec("UPDATE work_orders SET technician_id=@t WHERE id=@i", Db.P("@t", techId), Db.P("@i", id.Value));
                ChangeStatus(id.Value, "assigned", "Назначен техник");
                Db.Exec("INSERT INTO notifications (user_id, work_order_id, type, title, message) VALUES (@u,@w,'assigned',@t,@m)",
                        Db.P("@u", techId), Db.P("@w", id.Value), Db.P("@t", "Новая заявка"),
                        Db.P("@m", "Вам назначена заявка №" + id.Value));
                LoadOrders();
            });
        }

        void OrderStatus_Click(object sender, RoutedEventArgs e)
        {
            if (!Check("dispatcher", "technician")) return;
            Safe(() =>
            {
                int? id = SelectedId(gridOrders);
                if (id == null) return;

                bool isTech = Auth.Role() == "technician";
                if (isTech)
                {
                    // технику - только свои заявки
                    object owner = Db.Scalar("SELECT technician_id FROM work_orders WHERE id=@i", Db.P("@i", id.Value));
                    if (owner == null || owner == DBNull.Value || Convert.ToInt32(owner) != Auth.UserId())
                    {
                        Auth.LogDenied("попытка изменить чужую заявку " + id.Value);
                        MessageBox.Show("Это не ваша заявка");
                        return;
                    }
                }

                var options = isTech
                    ? new List<string> { "in_progress", "done" }
                    : new List<string> { "new", "assigned", "in_progress", "done", "cancelled" };

                var r = Form.Show(this, "Смена статуса", new List<Field>
                {
                    new Field{ Key="status", Label="Новый статус", Options=options },
                    new Field{ Key="reason", Label="Причина (обязательна при отмене)" }
                });
                if (r == null) return;

                if (r["status"] == "cancelled" && r["reason"] == "")
                {
                    MessageBox.Show("При отмене нужно указать причину");
                    return;
                }
                ChangeStatus(id.Value, r["status"], r["reason"]);
                LoadOrders();
            });
        }

        void OrderProtocol_Click(object sender, RoutedEventArgs e)
        {
            if (!Check("technician")) return;
            Safe(() =>
            {
                int? id = SelectedId(gridOrders);
                if (id == null) return;

                if (Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM protocols WHERE work_order_id=@i", Db.P("@i", id.Value))) > 0)
                {
                    MessageBox.Show("Протокол по этой заявке уже есть");
                    return;
                }

                var r = Form.Show(this, "Протокол обслуживания", new List<Field>
                {
                    new Field{ Key="counter", Label="Показание счетчика (чашек)", Text="0" },
                    new Field{ Key="works", Label="Что сделано" },
                    new Field{ Key="result", Label="Результат", Options=new List<string>{ "ok", "needs_repair" } },
                    new Field{ Key="cost", Label="Стоимость работ", Text="0" }
                });
                if (r == null) return;

                int counter; decimal cost;
                if (r["works"] == "") { MessageBox.Show("Опишите выполненные работы"); return; }
                if (!int.TryParse(r["counter"], out counter) || counter < 0) { MessageBox.Show("Счетчик - целое число"); return; }
                if (!Form.TryDecimal(r["cost"], out cost) || cost < 0) { MessageBox.Show("Стоимость - число"); return; }

                Db.Exec("INSERT INTO protocols (work_order_id, performed_by, counter_value, works_description, result, labor_cost) " +
                        "VALUES (@w,@u,@c,@d,@r,@l)",
                        Db.P("@w", id.Value), Db.P("@u", Auth.UserId()), Db.P("@c", counter),
                        Db.P("@d", r["works"]), Db.P("@r", r["result"]), Db.P("@l", cost));
                ChangeStatus(id.Value, "done", "Протокол оформлен");
                LoadOrders();
                LoadMachines();
                MessageBox.Show("Протокол сохранен, заявка закрыта, кофемашина снова в работе");
            });
        }

        void OrderHistory_Click(object sender, RoutedEventArgs e)
        {
            if (!Check("dispatcher", "technician")) return;
            Safe(() =>
            {
                int? id = SelectedId(gridOrders);
                if (id == null) return;
                var dt = Db.Select(
                    "SELECT h.changed_at, IFNULL(h.old_status,'-') AS old_s, h.new_status, u.full_name, IFNULL(h.reason,'') AS reason " +
                    "FROM work_order_history h JOIN users u ON u.id=h.changed_by WHERE h.work_order_id=@i ORDER BY h.changed_at, h.id",
                    Db.P("@i", id.Value));
                var sb = new StringBuilder();
                foreach (DataRow row in dt.Rows)
                {
                    sb.AppendLine(Convert.ToDateTime(row["changed_at"]).ToString("dd.MM.yyyy HH:mm") + "  " +
                                  row["old_s"] + " -> " + row["new_status"] + "  (" + row["full_name"] + ")  " + row["reason"]);
                }
                MessageBox.Show(sb.Length == 0 ? "Истории нет" : sb.ToString(), "История заявки");
            });
        }

        // ================= КОФЕМАШИНЫ =================

        void LoadMachines()
        {
            var dt = Db.Select(
                "SELECT m.id AS `ID`, m.inventory_number AS `Инвентарный номер`, m.serial_number AS `Серийный номер`, " +
                "CONCAT(mm.manufacturer,' ',mm.name) AS `Модель`, l.name AS `Точка`, m.status AS `Статус`, m.installed_at AS `Установлен` " +
                "FROM coffee_machines m JOIN machine_models mm ON mm.id=m.model_id JOIN locations l ON l.id=m.location_id ORDER BY m.id");
            gridMachines.ItemsSource = dt.DefaultView;
        }

        void MachRefresh_Click(object sender, RoutedEventArgs e) { Safe(LoadMachines); }

        void MachAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!Check("admin")) return;
            Safe(() =>
            {
                var r = Form.Show(this, "Новая кофемашина", new List<Field>
                {
                    new Field{ Key="model", Label="Модель", Items=Db.Items("SELECT id, CONCAT(manufacturer,' ',name) FROM machine_models") },
                    new Field{ Key="loc", Label="Точка", Items=Db.Items("SELECT id, name FROM locations WHERE is_active=1") },
                    new Field{ Key="serial", Label="Серийный номер" },
                    new Field{ Key="inv", Label="Инвентарный номер" }
                });
                if (r == null) return;
                if (r["serial"] == "" || r["inv"] == "" || r["model"] == "" || r["loc"] == "")
                {
                    MessageBox.Show("Заполните все поля");
                    return;
                }
                Db.Exec("INSERT INTO coffee_machines (model_id, location_id, serial_number, inventory_number, status, installed_at) " +
                        "VALUES (@m,@l,@s,@i,'working',CURDATE())",
                        Db.P("@m", int.Parse(r["model"])), Db.P("@l", int.Parse(r["loc"])),
                        Db.P("@s", r["serial"]), Db.P("@i", r["inv"]));
                LoadMachines();
            });
        }

        void MachStatus_Click(object sender, RoutedEventArgs e)
        {
            if (!Check("admin")) return;
            Safe(() =>
            {
                int? id = SelectedId(gridMachines);
                if (id == null) return;
                var r = Form.Show(this, "Статус кофемашины", new List<Field>
                {
                    new Field{ Key="s", Label="Статус", Options=new List<string>{ "working", "broken", "maintenance", "disabled" } }
                });
                if (r == null) return;
                Db.Exec("UPDATE coffee_machines SET status=@s WHERE id=@i", Db.P("@s", r["s"]), Db.P("@i", id.Value));
                LoadMachines();
            });
        }

        void MachDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!Check("admin")) return;
            Safe(() =>
            {
                int? id = SelectedId(gridMachines);
                if (id == null) return;
                if (MessageBox.Show("Удалить кофемашину?", "Подтверждение", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
                try
                {
                    Db.Exec("DELETE FROM coffee_machines WHERE id=@i", Db.P("@i", id.Value));
                }
                catch (Exception)
                {
                    MessageBox.Show("Нельзя удалить: по этой кофемашине есть заявки или продажи. Лучше поставьте статус disabled.");
                }
                LoadMachines();
            });
        }

        void LocAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!Check("admin")) return;
            Safe(() =>
            {
                var r = Form.Show(this, "Новая точка", new List<Field>
                {
                    new Field{ Key="name", Label="Название" },
                    new Field{ Key="addr", Label="Адрес" },
                    new Field{ Key="hours", Label="Часы работы", Text="08:00-22:00" }
                });
                if (r == null) return;
                if (r["name"] == "" || r["addr"] == "") { MessageBox.Show("Название и адрес обязательны"); return; }
                Db.Exec("INSERT INTO locations (name, address, opening_hours) VALUES (@n,@a,@h)",
                        Db.P("@n", r["name"]), Db.P("@a", r["addr"]), Db.P("@h", r["hours"]));
                MessageBox.Show("Точка добавлена");
            });
        }

        // ================= НАПИТКИ =================

        void LoadDrinks()
        {
            var dt = Db.Select(
                "SELECT id AS `ID`, name AS `Название`, volume_ml AS `Объем мл`, price AS `Цена`, " +
                "IF(is_active=1,'да','нет') AS `В продаже` FROM drinks ORDER BY id");
            gridDrinks.ItemsSource = dt.DefaultView;
        }

        void DrinkRefresh_Click(object sender, RoutedEventArgs e) { Safe(LoadDrinks); }

        void DrinkAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!Check("manager")) return;
            Safe(() =>
            {
                var r = Form.Show(this, "Новый напиток", new List<Field>
                {
                    new Field{ Key="name", Label="Название" },
                    new Field{ Key="vol", Label="Объем, мл", Text="200" },
                    new Field{ Key="price", Label="Цена", Text="100" }
                });
                if (r == null) return;
                int vol; decimal price;
                if (r["name"] == "") { MessageBox.Show("Введите название"); return; }
                if (!int.TryParse(r["vol"], out vol) || vol <= 0) { MessageBox.Show("Объем - целое число больше 0"); return; }
                if (!Form.TryDecimal(r["price"], out price) || price < 0) { MessageBox.Show("Цена - число"); return; }
                Db.Exec("INSERT INTO drinks (name, volume_ml, price) VALUES (@n,@v,@p)",
                        Db.P("@n", r["name"]), Db.P("@v", vol), Db.P("@p", price));
                LoadDrinks();
            });
        }

        void DrinkPrice_Click(object sender, RoutedEventArgs e)
        {
            if (!Check("manager")) return;
            Safe(() =>
            {
                int? id = SelectedId(gridDrinks);
                if (id == null) return;
                var r = Form.Show(this, "Новая цена", new List<Field> { new Field{ Key="price", Label="Цена" } });
                if (r == null) return;
                decimal price;
                if (!Form.TryDecimal(r["price"], out price) || price < 0) { MessageBox.Show("Цена - число"); return; }
                Db.Exec("UPDATE drinks SET price=@p WHERE id=@i", Db.P("@p", price), Db.P("@i", id.Value));
                LoadDrinks();
            });
        }

        void DrinkToggle_Click(object sender, RoutedEventArgs e)
        {
            if (!Check("manager")) return;
            Safe(() =>
            {
                int? id = SelectedId(gridDrinks);
                if (id == null) return;
                Db.Exec("UPDATE drinks SET is_active = 1 - is_active WHERE id=@i", Db.P("@i", id.Value));
                LoadDrinks();
            });
        }

        // ================= ПРОДАЖИ =================

        void LoadSales()
        {
            var dt = Db.Select(
                "SELECT s.id AS `ID`, s.number AS `Номер`, m.inventory_number AS `Автомат`, l.name AS `Точка`, " +
                "s.sold_at AS `Дата`, IFNULL(SUM(si.quantity*si.unit_price),0) AS `Сумма`, s.status AS `Статус` " +
                "FROM sales s JOIN coffee_machines m ON m.id=s.machine_id JOIN locations l ON l.id=s.location_id " +
                "LEFT JOIN sale_items si ON si.sale_id=s.id " +
                "GROUP BY s.id, s.number, m.inventory_number, l.name, s.sold_at, s.status ORDER BY s.sold_at DESC");
            gridSales.ItemsSource = dt.DefaultView;
        }

        void SaleRefresh_Click(object sender, RoutedEventArgs e) { Safe(LoadSales); }

        void SaleAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!Check("manager")) return;
            Safe(() =>
            {
                var r = Form.Show(this, "Новая продажа", new List<Field>
                {
                    new Field{ Key="machine", Label="Кофемашина", Items=Db.Items("SELECT id, inventory_number FROM coffee_machines WHERE status='working'") },
                    new Field{ Key="drink", Label="Напиток", Items=Db.Items("SELECT id, name FROM drinks WHERE is_active=1") },
                    new Field{ Key="qty", Label="Количество", Text="1" }
                });
                if (r == null) return;
                int qty;
                if (r["machine"] == "" || r["drink"] == "") { MessageBox.Show("Выберите кофемашину и напиток"); return; }
                if (!int.TryParse(r["qty"], out qty) || qty < 1) { MessageBox.Show("Количество - целое число больше 0"); return; }

                int machineId = int.Parse(r["machine"]);
                int drinkId = int.Parse(r["drink"]);
                object loc = Db.Scalar("SELECT location_id FROM coffee_machines WHERE id=@i", Db.P("@i", machineId));
                object price = Db.Scalar("SELECT price FROM drinks WHERE id=@i", Db.P("@i", drinkId));

                long saleId = Db.Insert(
                    "INSERT INTO sales (number, machine_id, location_id, status) VALUES (@n,@m,@l,'completed')",
                    Db.P("@n", "S-" + DateTime.Now.ToString("yyyyMMdd-HHmmss")), Db.P("@m", machineId), Db.P("@l", loc));
                Db.Exec("INSERT INTO sale_items (sale_id, drink_id, quantity, unit_price) VALUES (@s,@d,@q,@p)",
                        Db.P("@s", saleId), Db.P("@d", drinkId), Db.P("@q", qty), Db.P("@p", price));
                LoadSales();
            });
        }

        void SaleRevenue_Click(object sender, RoutedEventArgs e)
        {
            if (!Check("manager")) return;
            Safe(() =>
            {
                var dt = Db.Select(
                    "SELECT l.name, IFNULL(SUM(si.quantity*si.unit_price),0) AS total " +
                    "FROM locations l LEFT JOIN sales s ON s.location_id=l.id LEFT JOIN sale_items si ON si.sale_id=s.id " +
                    "GROUP BY l.id, l.name ORDER BY total DESC");
                var sb = new StringBuilder();
                decimal all = 0;
                foreach (DataRow row in dt.Rows)
                {
                    decimal t = Convert.ToDecimal(row["total"]);
                    all += t;
                    sb.AppendLine(row["name"] + ":  " + t.ToString("N2") + " руб.");
                }
                sb.AppendLine();
                sb.AppendLine("Итого: " + all.ToString("N2") + " руб.");
                MessageBox.Show(sb.ToString(), "Выручка по точкам");
            });
        }

        // ================= CSV-ОТЧЕТ (менеджер) =================

        static string CsvCell(string v)
        {
            if (v == null) v = "";
            if (v.Contains(";") || v.Contains("\"") || v.Contains("\n"))
                v = "\"" + v.Replace("\"", "\"\"") + "\"";
            return v;
        }

        static string Money(object v)
        {
            return Convert.ToDecimal(v).ToString("0.00", new System.Globalization.CultureInfo("ru-RU"));
        }

        void SaleCsv_Click(object sender, RoutedEventArgs e)
        {
            if (!Check("manager")) return;
            Safe(() =>
            {
                var r = Form.Show(this, "Отчет о продажах в CSV", new List<Field>
                {
                    new Field{ Key="type", Label="Вид отчета", Options=new List<string>{ "Продажи подробно", "Выручка по точкам" } },
                    new Field{ Key="from", Label="С даты (дд.мм.гггг)", Text=DateTime.Now.AddDays(-30).ToString("dd.MM.yyyy") },
                    new Field{ Key="to", Label="По дату (дд.мм.гггг)", Text=DateTime.Now.ToString("dd.MM.yyyy") }
                });
                if (r == null) return;

                DateTime d1, d2;
                var inv = System.Globalization.CultureInfo.InvariantCulture;
                if (!DateTime.TryParseExact(r["from"], "dd.MM.yyyy", inv, System.Globalization.DateTimeStyles.None, out d1) ||
                    !DateTime.TryParseExact(r["to"], "dd.MM.yyyy", inv, System.Globalization.DateTimeStyles.None, out d2))
                {
                    MessageBox.Show("Даты нужно вводить так: 31.12.2026");
                    return;
                }
                if (d2 < d1) { MessageBox.Show("Конечная дата раньше начальной"); return; }
                d2 = d2.AddDays(1); // включая последний день

                var lines = new List<string>();
                int count = 0;

                if (r["type"] == "Продажи подробно")
                {
                    lines.Add("Номер;Дата;Точка;Автомат;Напиток;Количество;Цена;Сумма");
                    var dt = Db.Select(
                        "SELECT s.number, s.sold_at, l.name AS loc, m.inventory_number AS inv, d.name AS drink, " +
                        "si.quantity, si.unit_price, si.quantity*si.unit_price AS total " +
                        "FROM sales s JOIN sale_items si ON si.sale_id=s.id JOIN drinks d ON d.id=si.drink_id " +
                        "JOIN coffee_machines m ON m.id=s.machine_id JOIN locations l ON l.id=s.location_id " +
                        "WHERE s.sold_at >= @a AND s.sold_at < @b ORDER BY s.sold_at",
                        Db.P("@a", d1), Db.P("@b", d2));
                    decimal sum = 0;
                    foreach (DataRow row in dt.Rows)
                    {
                        sum += Convert.ToDecimal(row["total"]);
                        lines.Add(string.Join(";", new[]
                        {
                            CsvCell(Convert.ToString(row["number"])),
                            Convert.ToDateTime(row["sold_at"]).ToString("dd.MM.yyyy HH:mm"),
                            CsvCell(Convert.ToString(row["loc"])),
                            CsvCell(Convert.ToString(row["inv"])),
                            CsvCell(Convert.ToString(row["drink"])),
                            Convert.ToString(row["quantity"]),
                            Money(row["unit_price"]),
                            Money(row["total"])
                        }));
                        count++;
                    }
                    lines.Add(";;;;;;ИТОГО;" + Money(sum));
                }
                else
                {
                    lines.Add("Точка;Количество продаж;Количество напитков;Выручка");
                    var dt = Db.Select(
                        "SELECT l.name, COUNT(DISTINCT s.id) AS cnt, IFNULL(SUM(si.quantity),0) AS cups, " +
                        "IFNULL(SUM(si.quantity*si.unit_price),0) AS total " +
                        "FROM locations l LEFT JOIN sales s ON s.location_id=l.id AND s.sold_at >= @a AND s.sold_at < @b " +
                        "LEFT JOIN sale_items si ON si.sale_id=s.id GROUP BY l.id, l.name ORDER BY total DESC",
                        Db.P("@a", d1), Db.P("@b", d2));
                    decimal sum = 0;
                    foreach (DataRow row in dt.Rows)
                    {
                        sum += Convert.ToDecimal(row["total"]);
                        lines.Add(CsvCell(Convert.ToString(row["name"])) + ";" + row["cnt"] + ";" + row["cups"] + ";" + Money(row["total"]));
                        count++;
                    }
                    lines.Add("ИТОГО;;;" + Money(sum));
                }

                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV файл (*.csv)|*.csv",
                    FileName = "report_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".csv"
                };
                if (dlg.ShowDialog() != true) return;

                // UTF-8 с BOM и разделитель ; - так Excel открывает русский текст нормально
                System.IO.File.WriteAllLines(dlg.FileName, lines, new UTF8Encoding(true));
                MessageBox.Show("Отчет сохранен: " + dlg.FileName + "\nСтрок данных: " + count, "Готово");
            });
        }

        // ================= ПОЛЬЗОВАТЕЛИ =================

        void LoadUsers()
        {
            var dt = Db.Select(
                "SELECT u.id AS `ID`, u.full_name AS `ФИО`, u.email AS `Email`, r.name AS `Роль`, " +
                "IF(u.is_active=1,'да','заблокирован') AS `Активен`, u.created_at AS `Создан` " +
                "FROM users u JOIN roles r ON r.id=u.role_id ORDER BY u.id");
            gridUsers.ItemsSource = dt.DefaultView;
        }

        void UserRefresh_Click(object sender, RoutedEventArgs e) { Safe(LoadUsers); }

        void UserAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!Check("admin")) return;
            Safe(() =>
            {
                var r = Form.Show(this, "Новый пользователь", new List<Field>
                {
                    new Field{ Key="name", Label="ФИО" },
                    new Field{ Key="email", Label="Email" },
                    new Field{ Key="pass", Label="Пароль (минимум 6 символов)", Password=true },
                    new Field{ Key="role", Label="Роль", Items=Db.Items("SELECT id, name FROM roles ORDER BY id") }
                });
                if (r == null) return;
                if (r["name"] == "" || !r["email"].Contains("@")) { MessageBox.Show("Введите ФИО и корректный email"); return; }
                if (r["pass"].Length < 6) { MessageBox.Show("Пароль слишком короткий"); return; }

                if (Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM users WHERE email=@e", Db.P("@e", r["email"]))) > 0)
                {
                    MessageBox.Show("Пользователь с таким email уже есть");
                    return;
                }
                Db.Exec("INSERT INTO users (role_id, full_name, email, password_hash, is_active) VALUES (@r,@n,@e,@h,1)",
                        Db.P("@r", int.Parse(r["role"])), Db.P("@n", r["name"]), Db.P("@e", r["email"]),
                        Db.P("@h", BCrypt.Net.BCrypt.HashPassword(r["pass"])));
                LoadUsers();
            });
        }

        void UserRole_Click(object sender, RoutedEventArgs e)
        {
            if (!Check("admin")) return;
            Safe(() =>
            {
                int? id = SelectedId(gridUsers);
                if (id == null) return;
                if (id.Value == Auth.UserId()) { MessageBox.Show("Свою роль менять нельзя"); return; }
                var r = Form.Show(this, "Назначение роли", new List<Field>
                {
                    new Field{ Key="role", Label="Роль", Items=Db.Items("SELECT id, name FROM roles ORDER BY id") }
                });
                if (r == null) return;
                Db.Exec("UPDATE users SET role_id=@r WHERE id=@i", Db.P("@r", int.Parse(r["role"])), Db.P("@i", id.Value));
                LoadUsers();
            });
        }

        void UserToggle_Click(object sender, RoutedEventArgs e)
        {
            if (!Check("admin")) return;
            Safe(() =>
            {
                int? id = SelectedId(gridUsers);
                if (id == null) return;
                if (id.Value == Auth.UserId()) { MessageBox.Show("Себя блокировать нельзя"); return; }
                Db.Exec("UPDATE users SET is_active = 1 - is_active WHERE id=@i", Db.P("@i", id.Value));
                LoadUsers();
            });
        }

        // ================= ЖУРНАЛ ВХОДОВ =================

        void LoadLog()
        {
            var dt = Db.Select(
                "SELECT l.id AS `ID`, l.created_at AS `Время`, l.email AS `Email`, IFNULL(u.full_name,'-') AS `Пользователь`, " +
                "l.event_type AS `Событие`, IF(l.success=1,'да','нет') AS `Успешно`, l.message AS `Сообщение`, " +
                "l.machine_name AS `Компьютер` " +
                "FROM login_log l LEFT JOIN users u ON u.id=l.user_id ORDER BY l.id DESC LIMIT 200");
            gridLog.ItemsSource = dt.DefaultView;
        }

        void LogRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (!Check("admin")) return;
            Safe(LoadLog);
        }
    }
}
