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

    public partial class Form3 : Form
    {
        int roleId;
        int userId;
        string connection = "Server=KRN-20-202-MMP1\\MSSQLSERVER2;Database=Shoes_2027;Integrated Security=True;TrustServerCertificate=True";
        int productId;
        int availableQuantity;
        decimal price;
        int unit_price;
        public Form3(int userId, int roleId)
        {
            this.userId = userId;
            this.roleId = roleId;

            InitializeComponent();
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            string selectedName = UserControl1.SelectedProductName;

            if (string.IsNullOrWhiteSpace(selectedName))
            {
                MessageBox.Show("Сначала выберите товар в каталоге, нажав на его карточку", "Оформление заказа", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connection))
                {
                    conn.Open();

                    string query = @"SELECT TOP 1 products.product_id, products.product_name,
                                            products.composition, products.description, products.price,
                                            products.image, manufacturers.manufacturer_name,
                                            subcategories.subcategory_name
                                     FROM products
                                     LEFT JOIN manufacturers ON products.manufacturer_id = manufacturers.manufacturer_id
                                     LEFT JOIN subcategories ON products.subcategory_id = subcategories.subcategory_id
                                     WHERE products.product_name = @name";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", selectedName);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                MessageBox.Show("Не удалось найти выбранный товар.");
                                Close();
                                return;
                            }

                            productId = Convert.ToInt32(reader["product_id"]);
                            price = Convert.ToDecimal(reader["price"]);
                            string manufacturer = reader["manufacturer_name"].ToString();
                            string name = reader["product_name"].ToString();
                            string subcategory = reader["subcategory_name"].ToString();
                            string composition = reader["composition"].ToString();
                            string description = reader["description"].ToString();
                            string image = reader["image"].ToString();

                            UserControl1 card = new UserControl1();
                            card.Card(manufacturer, name, subcategory, composition,
                                Convert.ToInt32(price), image, 0, 1);
                            card.Width = flowLayoutPanel1.ClientSize.Width - 10;
                            flowLayoutPanel1.Controls.Clear();
                            flowLayoutPanel1.Controls.Add(card);

                            label15.Text = composition;
                            label16.Text = description;
                            label17.Text = "";
                        }
                    }

                    LoadSizes(conn);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить данные заказа.\n" + ex.Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

            private void LoadSizes(SqlConnection conn)
        {
            comboBox1.Items.Clear();
            comboBox2.Items.Clear();

            // Показываем только размеры, по которым на складе есть товар.
            //DISTINCT, исключение повторений в SQL для одной и нескольких колонок.
            //INNER JOIN — это оператор в SQL, который объединяет строки из двух или более таблиц, но в результат попадают только те строки, для которых есть совпадение по заданному условию в обеих таблицах
            string query = @"SELECT DISTINCT sizes.size_id, sizes.size_value
                             FROM sizes
                             INNER JOIN stock_items ON sizes.size_id = stock_items.size_id
                             WHERE stock_items.product_id = @id AND stock_items.available_quantity > 0
                             ORDER BY sizes.size_value";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@id", productId);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        comboBox1.Items.Add(new SizeItem(Convert.ToInt32(reader["size_id"]), reader["size_value"].ToString()));
                }
            }

            if (comboBox1.Items.Count > 0)
                comboBox1.SelectedIndex = 0;
            else
            {
                label17.Text = "Нет доступных размеров";
                button2.Enabled = false;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBox2.Items.Clear();
            if (!(comboBox1.SelectedItem is SizeItem)) return;

            SizeItem selectedSize = (SizeItem)comboBox1.SelectedItem;
            try
            {
                using (SqlConnection conn = new SqlConnection(connection))
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT available_quantity FROM stock_items WHERE product_id=@product AND size_id=@size", conn))
                {
                    cmd.Parameters.AddWithValue("@product", productId);
                    cmd.Parameters.AddWithValue("@size", selectedSize.Id);
                    conn.Open();
                    object result = cmd.ExecuteScalar();
                    availableQuantity = result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
                }

                label17.Text = "Доступно: " + availableQuantity;
                for (int i = 1; i <= availableQuantity; i++)
                    comboBox2.Items.Add(i);
                if (comboBox2.Items.Count > 0) comboBox2.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить остаток товара.\n" + ex.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Кнопка отказа закрывает форму, не записывая заказ в базу.
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (!(comboBox1.SelectedItem is SizeItem) || comboBox2.SelectedItem == null)
            {
                MessageBox.Show("Выберите размер и количество.", "Заказ",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int quantity = Convert.ToInt32(comboBox2.SelectedItem);
            SizeItem size = (SizeItem)comboBox1.SelectedItem;

            if (quantity > availableQuantity)
            {
                MessageBox.Show("На складе недостаточно товара выбранного размера.");
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connection))
                {
                    conn.Open();
                    using (SqlTransaction transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // Сначала создаём заказ, затем добавляем в него выбранный товар.
                            int orderId;
                            using (SqlCommand cmd = new SqlCommand(
                                "INSERT INTO orders (user_id, order_date) OUTPUT INSERTED.order_id VALUES (@user, GETDATE())",
                                conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@user", userId);
                                orderId = Convert.ToInt32(cmd.ExecuteScalar());
                            }

                            using (SqlCommand cmd = new SqlCommand(
                                "INSERT INTO order_items (order_id, product_id, size_id, quantity, unit_price) VALUES (@order, @product, @size, @quantity, @unit_price)",
                                conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@order", orderId);
                                cmd.Parameters.AddWithValue("@product", productId);
                                cmd.Parameters.AddWithValue("@size", size.Id);
                                cmd.Parameters.AddWithValue("@quantity", quantity);
                                cmd.Parameters.AddWithValue("@unit_price", unit_price);
                                cmd.ExecuteNonQuery();
                            }

                            // Уменьшаем остаток только если на складе хватает выбранного количества.
                            using (SqlCommand cmd = new SqlCommand(
                                "UPDATE stock_items SET available_quantity = available_quantity - @quantity WHERE product_id=@product AND size_id=@size AND available_quantity>=@quantity",
                                conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@quantity", quantity);
                                cmd.Parameters.AddWithValue("@product", productId);
                                cmd.Parameters.AddWithValue("@size", size.Id);
                                if (cmd.ExecuteNonQuery() == 0)
                                    throw new Exception("Остаток изменился. Обновите каталог и попробуйте снова.");
                            }

                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }

                MessageBox.Show("Заказ успешно оформлен!", "Заказ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось оформить заказ. Проверь названия столбцов в таблицах orders, order_items и sizes.\n" + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private class SizeItem
        {
            public int Id;
            public string Name;
            public SizeItem(int id, string name) { Id = id; Name = name; }
            public override string ToString() { return Name; } //такой подход имеет смысл, если у объекта есть ярко выраженное, понятное человеку свойство (например, имя человека, название продукта или товара)
        }
    }
}
