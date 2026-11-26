using System.Windows;

namespace CoffeeCare
{
    public partial class LoginWindow : Window
    {
        bool _dbOk = false;

        public LoginWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => { InitDb(); txtEmail.Focus(); };
        }

        void InitDb()
        {
            try
            {
                Db.Init();
                _dbOk = true;
                txtError.Text = "";
            }
            catch (Exception ex)
            {
                _dbOk = false;
                txtError.Text = "Нет связи с MySQL: " + ex.Message;
            }
        }

        void Login_Click(object sender, RoutedEventArgs e)
        {
            if (!_dbOk) InitDb();
            if (!_dbOk) return;

            try
            {
                string error;
                if (Auth.Login(txtEmail.Text, txtPass.Password, out error))
                {
                    new MainWindow().Show();
                    Close();
                }
                else
                {
                    txtError.Text = error;
                    txtPass.Clear();
                }
            }
            catch (Exception ex)
            {
                txtError.Text = "Ошибка: " + ex.Message;
            }
        }
    }
}
