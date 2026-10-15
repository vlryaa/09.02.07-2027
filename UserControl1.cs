using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Shoes2027
{
    public partial class UserControl1 : UserControl
    {


        //название выбранного товара передаётся в Form3
        public static string SelectedProductName = "";

        public UserControl1()
        {
            InitializeComponent();

            // обработчик добавляем ко всем элементам карточки, чтобы выбор срабатывал и по картинке, и по тексту
            Click += SelectProduct;
            foreach (Control control in Controls)
                control.Click += SelectProduct;

        }

        private void SelectProduct(object sender, EventArgs e)
        {
            SelectedProductName = label3.Text;
        }

        public void Card(string manufacturer_id, string product_name, string subcategory_id, string composition, int price, string image, int available_quantity, int quantity)
        {
            label1.Text = manufacturer_id;
            label3.Text = product_name;
            label8.Text = subcategory_id;
            label10.Text = composition;
            label9.Text = available_quantity.ToString();
            label4.Text = $"{price.ToString()} руб.";
            pictureBox1.Image = Image.FromFile(Path.Combine(Application.StartupPath, image));

            //подсвечивание товаров которых меньше или = 3 
            if (available_quantity <= 3)
            {
                this.BackColor = ColorTranslator.FromHtml("#FF8080");
            }
            else
            {
                this.BackColor = ColorTranslator.FromHtml("#D2F6E7");
            }

            //расчет скидки
            if(quantity == 0)
            {
                label4.Text = $"{price * 0.75} руб.";
                label4.Text += " (скидка 25%)";
            }
            else
            {
                label4.Text = price + "руб.";
            }

        }

        private void UserControl1_Load(object sender, EventArgs e)
        {

        }
    }
}
