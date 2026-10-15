using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace Shoes2027
{
    public partial class Form2 : Form
    {
        string connection = "Server=KRN-20-202-MMP1\\MSSQLSERVER2;Database=Shoes_2027;Integrated Security=True;TrustServerCertificate=True";
        public Form2()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string login = textBox1.Text;


            try
            {

                
                SqlConnection conn = new SqlConnection(connection);
                conn.Open();

                string query = "SELECT * FROM [users] WHERE login=@login";

                SqlCommand sqlCommand = new SqlCommand(query, conn);
                sqlCommand.Parameters.AddWithValue("@login", login);
                SqlDataReader reader = sqlCommand.ExecuteReader();


                if (reader.Read())
                {
                    MessageBox.Show("Добро пожаловать", "Авторизация прошла успешно", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);

                    int userId = Convert.ToInt32(reader["user_id"]);
                    int roleId = Convert.ToInt32(reader["role_id"]);
                    Form1 form1 = new Form1(userId, roleId);
                    form1.Show();
                    this.Hide();
                }
                else if (login == "")
                {
                    MessageBox.Show("Поле должно быть заполнено!", "ОШИБКА", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);

                }
                else
                {
                    MessageBox.Show("Неправильный логин", "ОШИБКА", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

    }
 }



