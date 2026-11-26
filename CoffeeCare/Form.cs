using System.Windows;
using System.Windows.Controls;

namespace CoffeeCare
{
    public class Field
    {
        public string Key;
        public string Label;
        public string Text = "";
        public bool Password = false;
        public List<KeyValuePair<int, string>> Items;   // выпадающий список из БД (id + текст)
        public List<string> Options;                    // выпадающий список из строк
    }

    // Универсальное окошко с полями ввода, чтобы не рисовать 15 окон
    public static class Form
    {
        public static Dictionary<string, string> Show(Window owner, string title, List<Field> fields)
        {
            var win = new Window
            {
                Title = title,
                Width = 400,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = owner,
                ResizeMode = ResizeMode.NoResize
            };

            var panel = new StackPanel { Margin = new Thickness(15) };
            var controls = new Dictionary<string, Control>();

            foreach (var f in fields)
            {
                panel.Children.Add(new TextBlock { Text = f.Label, Margin = new Thickness(0, 8, 0, 2) });
                Control c;
                if (f.Items != null)
                {
                    var cb = new ComboBox { DisplayMemberPath = "Value", SelectedValuePath = "Key", ItemsSource = f.Items };
                    if (f.Items.Count > 0) cb.SelectedIndex = 0;
                    c = cb;
                }
                else if (f.Options != null)
                {
                    var cb = new ComboBox { ItemsSource = f.Options };
                    if (f.Options.Count > 0) cb.SelectedIndex = 0;
                    c = cb;
                }
                else if (f.Password)
                {
                    c = new PasswordBox();
                }
                else
                {
                    c = new TextBox { Text = f.Text };
                }
                panel.Children.Add(c);
                controls[f.Key] = c;
            }

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 15, 0, 0)
            };
            var ok = new Button { Content = "OK", Width = 80, IsDefault = true, Margin = new Thickness(0, 0, 8, 0) };
            var cancel = new Button { Content = "Отмена", Width = 80, IsCancel = true };
            ok.Click += (s, e) => { win.DialogResult = true; };
            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);
            panel.Children.Add(buttons);
            win.Content = panel;

            if (win.ShowDialog() != true) return null;

            var result = new Dictionary<string, string>();
            foreach (var f in fields)
            {
                var c = controls[f.Key];
                if (c is ComboBox)
                {
                    var cb = (ComboBox)c;
                    if (cb.SelectedItem == null) result[f.Key] = "";
                    else if (f.Items != null) result[f.Key] = Convert.ToString(cb.SelectedValue);
                    else result[f.Key] = Convert.ToString(cb.SelectedItem);
                }
                else if (c is PasswordBox) result[f.Key] = ((PasswordBox)c).Password;
                else result[f.Key] = ((TextBox)c).Text.Trim();
            }
            return result;
        }

        // "12,5" и "12.5" оба превращаем в число
        public static bool TryDecimal(string s, out decimal value)
        {
            s = (s ?? "").Replace(',', '.');
            return decimal.TryParse(s, System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out value);
        }
    }
}
