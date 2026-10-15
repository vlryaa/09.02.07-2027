using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace Shoes2027
{
    public partial class Form4 : Form
    {

        string connection = "Server=KRN-20-202-MMP1\\MSSQLSERVER2;Database=Shoes_2027;Integrated Security=True;TrustServerCertificate=True";

        int userId;
        int roleId;

        public Form4(int userId, int roleId)
        {
            InitializeComponent();


            this.userId = userId;
            this.roleId = roleId;

            // Кнопки  доступны только пользователю с ролью 2
            button1.Visible = (roleId == 2);
            button2.Visible = (roleId == 2);
            button3.Visible = (roleId == 2);
        }

        // При открытии формы загружаем заказы из базы данных
        private void Form4_Load(object sender, EventArgs e)
        {
            LoadOrders();
        }

        // Загружает список заказов и товары внутри каждого заказа
        private void LoadOrders()
        {
            try
            {
                // удаляем старые строки, чтобы не показывать заказы повторно
                dataGridView1.Rows.Clear();

                using (SqlConnection conn = new SqlConnection(connection))
                {
                    conn.Open();

                    // Сначала получаем основные сведения о каждом заказе:
                    // его ID, дату и ФИО клиента.
                    string query = @"
                        SELECT o.order_id, o.order_date, u.last_name + ' ' + u.first_name + ' ' + u.patronymic AS fio
                        FROM orders o
                        JOIN users u ON o.user_id = u.user_id
                        ORDER BY o.order_date, o.order_id;";

                    DataTable orders = new DataTable();

                    using (SqlDataAdapter adapter = new SqlDataAdapter(query, conn))
                    {
                        adapter.Fill(orders);
                    }

                    // По очереди обрабатываем каждый заказ.
                    foreach (DataRow orderRow in orders.Rows)
                    {
                        int orderId = Convert.ToInt32(orderRow["order_id"]);

                        // В contents собираем описание всех товаров этого заказа.
                        // Сначала строка пустая, потому что товары ещё не добавлены.
                        string contents = "";

                        // В total считаем общую стоимость заказа.
                        decimal total = 0;

                        // Получаем товары, которые относятся только к текущему заказу.
                        string itemsQuery = @"
                            SELECT
                                p.product_name,
                                m.manufacturer_name,
                                s.size_value,
                                oi.quantity,
                                oi.unit_price
                            FROM order_items oi
                            JOIN products p ON oi.product_id = p.product_id
                            JOIN manufacturers m ON p.manufacturer_id = m.manufacturer_id
                            JOIN sizes s ON oi.size_id = s.size_id
                            WHERE oi.order_id = @orderId
                            ORDER BY oi.order_item_id;";

                        using (SqlCommand command = new SqlCommand(itemsQuery, conn))
                        {
                            // Передаём ID заказа отдельным параметром запроса.
                            command.Parameters.Add("@orderId", SqlDbType.Int).Value = orderId;

                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                // Reader возвращает товары по одному.
                                while (reader.Read())
                                {
                                    string model = reader["product_name"].ToString();
                                    string manufacturer = reader["manufacturer_name"].ToString();
                                    string size = reader["size_value"].ToString();

                                    int quantity = Convert.ToInt32(reader["quantity"]);
                                    decimal price = Convert.ToDecimal(reader["unit_price"]);

                                    // Стоимость этой позиции = количество * цена за одну пару.
                                    decimal sum = quantity * price;

                                    // Прибавляем стоимость позиции к общей сумме заказа.
                                    total = total + sum;

                                    // += добавляет описание очередного товара к уже собранному тексту.
                                    // $@"..." позволяет вставлять переменные и писать текст в несколько строк.
                                    contents += $@"

                                                Наименование товара: {model}
                                                Производство: {manufacturer}
                                                Размер: {size}
                                                Количество: {quantity}
                                                Цена за единицу товара: {price:0.00} руб.
                                                Сумма за товар: {sum:0.00} руб.";
                                }
                            }
                        }

                        // Если в заказе нет товаров, сообщаем об этом.
                        if (contents == "")
                        {
                            contents = "Нет товаров в заказе.";
                        }

                        // Добавляем итоговую сумму в конец описания заказа.
                        // Здесь += нужен, чтобы не стереть список товаров.
                        contents += $"ИТОГОВАЯ СУММА ЗАКАЗА: {total:0.00} руб.";

                        // Добавляем одну строку в таблицу:
                        // дата заказа, ФИО клиента и список товаров с общей суммой.
                        int rowIndex = dataGridView1.Rows.Add(
                            Convert.ToDateTime(orderRow["order_date"]).ToString("dd.MM.yyyy"),
                            orderRow["fio"].ToString(),
                            contents);

                        // Сохраняем ID заказа в Tag строки.
                        // Это помогает понять, какой заказ выбрал пользователь.
                        dataGridView1.Rows[rowIndex].Tag = orderId;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки заказов: " + ex.Message);
            }
        }

        // Показывает небольшое окно со списком значений и возвращает ID выбранной записи
        // Например, так пользователь выбирает товар или размер
        private int SelectFromList(string title, string query, string displayField, string valueField)
        {
            DataTable table = new DataTable();

            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();

                using (SqlDataAdapter adapter = new SqlDataAdapter(query, conn))
                {
                    adapter.Fill(table);
                }
            }

            // Если список пустой, выбирать нечего.
            if (table.Rows.Count == 0)
            {
                MessageBox.Show("Подходящие записи не найдены.");
                return -1;
            }

            using (Form form = new Form())
            {
                form.Text = title;
                form.Width = 400;
                form.Height = 150;
                form.StartPosition = FormStartPosition.CenterParent;

                ComboBox combo = new ComboBox();
                combo.DropDownStyle = ComboBoxStyle.DropDownList;
                combo.SetBounds(10, 10, 360, 25);
                combo.DataSource = table;
                combo.DisplayMember = displayField;
                combo.ValueMember = valueField;

                Button ok = new Button();
                ok.Text = "Выбрать";
                ok.SetBounds(270, 50, 100, 30);
                ok.DialogResult = DialogResult.OK;

                form.Controls.Add(combo);
                form.Controls.Add(ok);
                form.AcceptButton = ok;

                // Если пользователь нажал «Выбрать», возвращаем ID выбранной записи.
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    return Convert.ToInt32(combo.SelectedValue);
                }
            }

            // -1 означает, что пользователь закрыл окно без выбора.
            return -1;
        }

        // Показывает окно для ввода текста
        private string Ask(string message)
        {
            using (Form form = new Form())
            {
                form.Text = "Ввод данных";
                form.Width = 350;
                form.Height = 150;
                form.StartPosition = FormStartPosition.CenterParent;

                Label label = new Label();
                label.Text = message;
                label.SetBounds(10, 10, 310, 25);

                TextBox text = new TextBox();
                text.SetBounds(10, 40, 310, 25);

                Button ok = new Button();
                ok.Text = "ОК";
                ok.SetBounds(230, 75, 90, 25);
                ok.DialogResult = DialogResult.OK;

                form.Controls.Add(label);
                form.Controls.Add(text);
                form.Controls.Add(ok);
                form.AcceptButton = ok;

                // Возвращаем введённый текст. Если окно закрыли, возвращаем пустую строку
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    return text.Text.Trim();
                }

                return "";
            }
        }

        // изменить дату выбранного заказа
        private void button1_Click(object sender, EventArgs e)
        {
            // Проверяем, что пользователь действительно выбрал заказ.
            if (dataGridView1.CurrentRow == null || dataGridView1.CurrentRow.Tag == null)
            {
                MessageBox.Show("Выберите заказ.");
                return;
            }

            int orderId = Convert.ToInt32(dataGridView1.CurrentRow.Tag);

            // преобразоуем введённый текст в дату
            DateTime date;
            if (!DateTime.TryParse(Ask("Введите новую дату (дд.мм.гггг):"), out date))
            {
                MessageBox.Show("Введите правильную дату.");
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connection))
                {
                    conn.Open();

                    string query = @"
                        UPDATE orders
                        SET order_date = @date
                        WHERE order_id = @orderId;";

                    using (SqlCommand command = new SqlCommand(query, conn))
                    {
                        command.Parameters.Add("@date", SqlDbType.Date).Value = date.Date;
                        command.Parameters.Add("@orderId", SqlDbType.Int).Value = orderId;

                        // ExecuteNonQuery возвращает количество изменённых строк
                        if (command.ExecuteNonQuery() == 0)
                        {
                            MessageBox.Show("Заказ не найден.");
                            return;
                        }
                    }
                }

                // Перезагружаем таблицу, чтобы показать новую дату
                LoadOrders();
                MessageBox.Show("Дата заказа изменена.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка редактирования: " + ex.Message);
            }
        }

        // добавить товар в выбранный заказ.
        private void button2_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null || dataGridView1.CurrentRow.Tag == null)
            {
                MessageBox.Show("Выберите заказ.");
                return;
            }

            int orderId = Convert.ToInt32(dataGridView1.CurrentRow.Tag);

            // Сначала пользователь выбирает товар
            int productId = SelectFromList(
                "Выбор товара",
                "SELECT product_id, product_name FROM products ORDER BY product_name",
                "product_name",
                "product_id");

            if (productId == -1)
            {
                return;
            }

            // Затем показываем размеры выбранного товара, которые есть на складе
            // ID товара передаём в запрос как параметр, а не склеиваем с текстом SQL
            int sizeId;

            string sizesQuery = @"
                SELECT s.size_id, s.size_value
                FROM sizes s
                JOIN stock_items st ON s.size_id = st.size_id
                WHERE st.product_id = @productId
                  AND st.available_quantity > 0
                ORDER BY s.size_value;";

            DataTable sizesTable = new DataTable();

            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();

                using (SqlCommand command = new SqlCommand(sizesQuery, conn))
                {
                    command.Parameters.Add("@productId", SqlDbType.Int).Value = productId;

                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(sizesTable);
                    }
                }
            }

            // Для списка размеров используем то же окно выбора
            // Запрос выше уже проверил, что размеры есть в наличии
            if (sizesTable.Rows.Count == 0)
            {
                MessageBox.Show("Нет доступных размеров этого товара.");
                return;
            }

            sizeId = SelectFromList(
                "Выбор доступного размера",
                @"SELECT s.size_id, s.size_value
                  FROM sizes s
                  JOIN stock_items st ON s.size_id = st.size_id
                  WHERE st.product_id = " + productId + @"
                    AND st.available_quantity > 0
                  ORDER BY s.size_value;",
                "size_value",
                "size_id");

            if (sizeId == -1)
            {
                return;
            }

            // Проверяем, что количество — целое положительное число.
            int quantity;
            if (!int.TryParse(Ask("Введите количество пар:"), out quantity) || quantity <= 0)
            {
                MessageBox.Show("Количество должно быть больше нуля.");
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connection))
                {
                    conn.Open();

                    // Транзакция объединяет списание со склада и добавление в заказ.
                    // Если одно действие не получится, отменяем оба действия.
                    using (SqlTransaction transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // Получаем текущую цену выбранного товара.
                            decimal price;
                            string query = "SELECT price FROM products WHERE product_id = @productId";

                            using (SqlCommand command = new SqlCommand(query, conn, transaction))
                            {
                                command.Parameters.Add("@productId", SqlDbType.Int).Value = productId;
                                price = Convert.ToDecimal(command.ExecuteScalar());
                            }

                            // Уменьшаем остаток, только если товара достаточно.
                            query = @"
                                UPDATE stock_items
                                SET available_quantity = available_quantity - @quantity
                                WHERE product_id = @productId
                                  AND size_id = @sizeId
                                  AND available_quantity >= @quantity;";

                            using (SqlCommand command = new SqlCommand(query, conn, transaction))
                            {
                                command.Parameters.Add("@quantity", SqlDbType.Int).Value = quantity;
                                command.Parameters.Add("@productId", SqlDbType.Int).Value = productId;
                                command.Parameters.Add("@sizeId", SqlDbType.Int).Value = sizeId;

                                if (command.ExecuteNonQuery() == 0)
                                {
                                    throw new Exception("Недостаточно пар выбранного размера на складе.");
                                }
                            }

                            // Записываем товар в состав заказа.
                            query = @"
                                INSERT INTO order_items
                                    (order_id, product_id, size_id, quantity, unit_price)
                                VALUES
                                    (@orderId, @productId, @sizeId, @quantity, @price);";

                            using (SqlCommand command = new SqlCommand(query, conn, transaction))
                            {
                                command.Parameters.Add("@orderId", SqlDbType.Int).Value = orderId;
                                command.Parameters.Add("@productId", SqlDbType.Int).Value = productId;
                                command.Parameters.Add("@sizeId", SqlDbType.Int).Value = sizeId;
                                command.Parameters.Add("@quantity", SqlDbType.Int).Value = quantity;
                                command.Parameters.Add("@price", SqlDbType.Decimal).Value = price;

                                command.ExecuteNonQuery();
                            }

                            // Сохраняем изменения в базе данных.
                            transaction.Commit();
                        }
                        catch
                        {
                            // Если возникла ошибка, отменяем изменения транзакции.
                            transaction.Rollback();
                            throw;
                        }
                    }
                }

                LoadOrders();
                MessageBox.Show("Товар добавлен в заказ.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка добавления: " + ex.Message);
            }
        }

        // удалить товарную позицию из выбранного заказа
        private void button3_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null || dataGridView1.CurrentRow.Tag == null)
            {
                MessageBox.Show("Выберите заказ.");
                return;
            }

            int orderId = Convert.ToInt32(dataGridView1.CurrentRow.Tag);

            // Выбираем товар, который уже есть в заказе
            int productId = SelectFromList(
                "Выбор товара для удаления",
                @"SELECT DISTINCT p.product_id, p.product_name
                  FROM order_items oi
                  JOIN products p ON oi.product_id = p.product_id
                  WHERE oi.order_id = " + orderId + @"
                  ORDER BY p.product_name;",
                "product_name",
                "product_id");

            if (productId == -1)
            {
                return;
            }

            // Выбираем размер этого товара в заказе
            int sizeId = SelectFromList("Выбор размера",
                @"SELECT DISTINCT s.size_id, s.size_value
                  FROM order_items oi
                  JOIN sizes s ON oi.size_id = s.size_id
                  WHERE oi.order_id = " + orderId +
                  " AND oi.product_id = " + productId + @"
                  ORDER BY s.size_value;",
                "size_value",
                "size_id");

            if (sizeId == -1)
            {
                return;
            }

            // Перед удалением просим подтверждение
            DialogResult answer = MessageBox.Show("Удалить выбранную позицию из заказа?", "Подтверждение", MessageBoxButtons.YesNo);

            if (answer != DialogResult.Yes)
            {
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connection))
                {
                    conn.Open();

                    // Удаление из заказа и возврат товара на склад должны выполниться вместе
                    using (SqlTransaction transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            int itemId;
                            int quantity;

                            // Находим конкретную строку заказа и количество товара в ней.
                            string query = @"
                                SELECT TOP 1 order_item_id, quantity
                                FROM order_items
                                WHERE order_id = @orderId
                                  AND product_id = @productId
                                  AND size_id = @sizeId
                                ORDER BY order_item_id;";

                            using (SqlCommand command = new SqlCommand(query, conn, transaction))
                            {
                                command.Parameters.Add("@orderId", SqlDbType.Int).Value = orderId;
                                command.Parameters.Add("@productId", SqlDbType.Int).Value = productId;
                                command.Parameters.Add("@sizeId", SqlDbType.Int).Value = sizeId;

                                using (SqlDataReader reader = command.ExecuteReader())
                                {
                                    if (!reader.Read())
                                    {
                                        throw new Exception("Товарная позиция не найдена.");
                                    }

                                    itemId = Convert.ToInt32(reader["order_item_id"]);
                                    quantity = Convert.ToInt32(reader["quantity"]);
                                }
                            }

                            // Возвращаем удаляемое количество пар на склад.
                            query = @"
                                UPDATE stock_items
                                SET available_quantity = available_quantity + @quantity
                                WHERE product_id = @productId
                                  AND size_id = @sizeId;";

                            using (SqlCommand command = new SqlCommand(query, conn, transaction))
                            {
                                command.Parameters.Add("@quantity", SqlDbType.Int).Value = quantity;
                                command.Parameters.Add("@productId", SqlDbType.Int).Value = productId;
                                command.Parameters.Add("@sizeId", SqlDbType.Int).Value = sizeId;

                                if (command.ExecuteNonQuery() == 0)
                                {
                                    throw new Exception("Размер не найден на складе.");
                                }
                            }

                            // Удаляем строку заказа по её уникальному ID.
                            query = "DELETE FROM order_items WHERE order_item_id = @itemId;";

                            using (SqlCommand command = new SqlCommand(query, conn, transaction))
                            {
                                command.Parameters.Add("@itemId", SqlDbType.Int).Value = itemId;
                                command.ExecuteNonQuery();
                            }

                            transaction.Commit();
                        }
                        catch
                        {
                            // При ошибке возвращаем базу к состоянию до начала операции.
                            transaction.Rollback();
                            throw;
                        }
                    }
                }

                LoadOrders();
                MessageBox.Show("Товар удалён из заказа.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления: " + ex.Message);
            }
        }
    }
}
