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
    public partial class Form1 : Form
    {

        int userId;
        int roleId;

        string connection = "Server=KRN-20-202-MMP1\\MSSQLSERVER2;Database=Shoes_2027;Integrated Security=True;TrustServerCertificate=True";


        public Form1(int userId, int roleId)
        {

            InitializeComponent();


            this.userId = userId;
            this.roleId = roleId;
            button3.Visible = (roleId == 3 || roleId == 2);

           
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            try
            {
                //отображение ФИО пользователя
                SqlConnection conn1 = new SqlConnection(connection);
                conn1.Open();

                string queryfullname = "SELECT last_name + ' ' + first_name + ' ' + patronymic FROM [users] WHERE user_id = @id";

                SqlCommand fullnamesql = new SqlCommand(queryfullname, conn1);
                fullnamesql.Parameters.AddWithValue("id", userId);
                if (userId != 0)
                {
                    label1.Text = fullnamesql.ExecuteScalar().ToString();
                }

                //вывод товаров в карточке и подключение карточки к flowlayotpanel
                SqlConnection conn = new SqlConnection(connection);
                conn.Open();

                string query = @"SELECT *
                        FROM products
                        LEFT JOIN manufacturers ON products.manufacturer_id = manufacturers.manufacturer_id
                        LEFT JOIN subcategories ON products.subcategory_id = subcategories.subcategory_id
                        LEFT JOIN categories ON subcategories.category_id = categories.category_id
                        LEFT JOIN order_items ON products.product_id = order_items.product_id
                        LEFT JOIN stock_items ON products.product_id = stock_items.product_id";

                SqlCommand sql1 = new SqlCommand(query, conn);
                SqlDataReader reader = sql1.ExecuteReader();


                while (reader.Read())
                {
                    UserControl1 userControl = new UserControl1();
                    userControl.Card(
                        reader["manufacturer_name"].ToString(),
                        reader["product_name"].ToString(),
                        reader["subcategory_name"].ToString(),
                        reader["composition"].ToString(),
                        Convert.ToInt32(reader["price"]),
                        reader["image"].ToString(),
                        Convert.ToInt32(reader["available_quantity"]),
                        reader["quantity"] == DBNull.Value ? 0 : Convert.ToInt32(reader["quantity"]));
                    flowLayoutPanel1.Controls.Add(userControl);

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        //  кнопка назад
        private void button1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Для входа Вам сново необходимо будет ввести логин", "Обратите внимание!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Form2 form2 = new Form2();
            form2.Show();
            this.Hide();

        }

        //фильтрация
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

            string query = @"SELECT *
                        FROM products
                        LEFT JOIN manufacturers ON products.manufacturer_id = manufacturers.manufacturer_id
                        LEFT JOIN subcategories ON products.subcategory_id = subcategories.subcategory_id
                        LEFT JOIN categories ON subcategories.category_id = categories.category_id
                        LEFT JOIN order_items ON products.product_id = order_items.product_id
                        LEFT JOIN stock_items ON products.product_id = stock_items.product_id";

            if (comboBox1.SelectedIndex == 0)
                query += " ORDER BY price ASC";
            else if (comboBox1.SelectedIndex == 1)
                query += " ORDER BY price DESC";
            else if (comboBox2.SelectedIndex == 2)
                query += " WHERE price > 0";
            flowLayoutPanel1.Controls.Clear();


            SqlConnection conn = new SqlConnection(connection);
            conn.Open();

            SqlCommand sql = new SqlCommand(query, conn);
            SqlDataReader reader = sql.ExecuteReader();

            while (reader.Read())
            {
                UserControl1 userControl = new UserControl1();
                userControl.Card(
                    reader["manufacturer_name"].ToString(),
                    reader["product_name"].ToString(),
                    reader["subcategory_name"].ToString(),
                    reader["composition"].ToString(),
                    Convert.ToInt32(reader["price"]),
                    reader["image"].ToString(),
                    Convert.ToInt32(reader["available_quantity"]),
                    reader["quantity"] == DBNull.Value ? 0 : Convert.ToInt32(reader["quantity"]));
                flowLayoutPanel1.Controls.Add(userControl);

            }

        }


        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

            string query = @"SELECT *
                        FROM products
                        LEFT JOIN manufacturers ON products.manufacturer_id = manufacturers.manufacturer_id
                        LEFT JOIN subcategories ON products.subcategory_id = subcategories.subcategory_id
                        LEFT JOIN categories ON subcategories.category_id = categories.category_id
                        LEFT JOIN order_items ON products.product_id = order_items.product_id
                        LEFT JOIN stock_items ON products.product_id = stock_items.product_id";

            if (comboBox2.SelectedIndex == 1)
                query += " WHERE categories.category_id = 1";
            else if (comboBox2.SelectedIndex == 2)
                query += " WHERE categories.category_id = 2";
            else if (comboBox2.SelectedIndex == 3)
                query += " WHERE categories.category_id = 3";
            else if (comboBox2.SelectedIndex == 0)
                query += " WHERE price > 0";
            flowLayoutPanel1.Controls.Clear();


            SqlConnection conn = new SqlConnection(connection);
            conn.Open();

            SqlCommand sql = new SqlCommand(query, conn);
            SqlDataReader reader = sql.ExecuteReader();

            while (reader.Read())
            {
                UserControl1 userControl = new UserControl1();
                userControl.Card(
                    reader["manufacturer_name"].ToString(),
                    reader["product_name"].ToString(),
                    reader["subcategory_name"].ToString(),
                    reader["composition"].ToString(),
                    Convert.ToInt32(reader["price"]),
                    reader["image"].ToString(),
                    Convert.ToInt32(reader["available_quantity"]),
                    reader["quantity"] == DBNull.Value ? 0 : Convert.ToInt32(reader["quantity"]));
                flowLayoutPanel1.Controls.Add(userControl);

            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            string search = textBox1.Text.Trim();
            string query = @"SELECT *
                        FROM products
                        LEFT JOIN manufacturers ON products.manufacturer_id = manufacturers.manufacturer_id
                        LEFT JOIN subcategories ON products.subcategory_id = subcategories.subcategory_id
                        LEFT JOIN categories ON subcategories.category_id = categories.category_id
                        LEFT JOIN order_items ON products.product_id = order_items.product_id
                        LEFT JOIN stock_items ON products.product_id = stock_items.product_id";

            if (!string.IsNullOrEmpty(search))
            {
                query += " WHERE product_name LIKE '%" + search + "%' OR description LIKE '%" + search + "%' ";
            }
            flowLayoutPanel1.Controls.Clear();

            SqlConnection conn = new SqlConnection(connection);
            conn.Open();

            SqlCommand sql = new SqlCommand(query, conn);
            SqlDataReader reader = sql.ExecuteReader();

            while (reader.Read())
            {
                UserControl1 userControl = new UserControl1();
                userControl.Card(
                    reader["manufacturer_name"].ToString(),
                    reader["product_name"].ToString(),
                    reader["subcategory_name"].ToString(),
                    reader["composition"].ToString(),
                    Convert.ToInt32(reader["price"]),
                    reader["image"].ToString(),
                    Convert.ToInt32(reader["available_quantity"]),
                    reader["quantity"] == DBNull.Value ? 0 : Convert.ToInt32(reader["quantity"]));
                flowLayoutPanel1.Controls.Add(userControl);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            
            Form3 form3 = new Form3(roleId, userId);
            form3.Show();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Form4 form4 = new Form4(userId, roleId);
            form4.Show();
        }
    }
}
