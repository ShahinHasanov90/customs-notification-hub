using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace GMAIL_LIKE_TEST_APP
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        SqlConnection bgl = new SqlConnection(@"Data Source=DESKTOP-4CCALSF\SQLEXPRESS01;Initial Catalog=""gmail test"";Integrated Security=True");
        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void maskedTextBox1_MaskInputRejected(object sender, MaskInputRejectedEventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            bgl.Open();
            SqlCommand kmt1 = new SqlCommand("Select * From TBL_PERSON Where NOMRE=@p1 AND SIFRE=@p2", bgl);
            kmt1.Parameters.AddWithValue("@p1", maskedTextBox1.Text);
            kmt1.Parameters.AddWithValue("@p2", textBox1.Text);
            SqlDataReader dr = kmt1.ExecuteReader();
            if (dr.Read())
            {
                Form2 fr = new Form2();
                fr.nomre = maskedTextBox1.Text;
                fr.Show();
                

            }
            else
            {
                MessageBox.Show("XETALI GIRIS");
            }
            bgl.Close();
            
        }
    }
}
