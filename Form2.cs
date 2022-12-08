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
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }
        SqlConnection bgl = new SqlConnection(@"Data Source=DESKTOP-4CCALSF\SQLEXPRESS01;Initial Catalog=""gmail test"";Integrated Security=True");
        public string nomre;

        private void label9_Click(object sender, EventArgs e)
        {

        }
        void gelen()
        {
            SqlDataAdapter da1 = new SqlDataAdapter("Select MESAJID,(AD+'  '+SOYAD) AS GONDEREN ,BASLIQ,MEZMUN From TBL_MESAJLAR inner join TBL_Person ON TBL_MESAJLAR.GONDEREN=TBL_PERSON.NOMRE WHERE ALICI=" + nomre, bgl);
            DataTable dt = new DataTable();
            da1.Fill(dt);
            dataGridView5.DataSource = dt;
        }

        void geden()
        {
            SqlDataAdapter da2 = new SqlDataAdapter("Select MESAJID,(AD+'  '+SOYAD) AS ALICI,BASLIQ,MEZMUN FROM TBL_MESAJLAR inner join TBL_PERSON ON TBL_MESAJLAR.ALICI=TBL_PERSON.NOMRE WHERE GONDEREN=" + nomre, bgl);
            DataTable dt2 = new DataTable();
            da2.Fill(dt2);
            dataGridView6.DataSource = dt2;
        }
        private void label12_Click(object sender, EventArgs e)
        {

        }

        private void groupBox4_Enter(object sender, EventArgs e)
        {

        }

        private void groupBox5_Enter(object sender, EventArgs e)
        {

        }

        private void dataGridView7_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void Form2_Load(object sender, EventArgs e)
        {
            lblnomre.Text = nomre;
            gelen();
            geden();

            bgl.Open();
            SqlCommand kk = new SqlCommand("Select AD,SOYAD from TBL_PERSON Where NOMRE=" + nomre, bgl);
            SqlDataReader dr1 = kk.ExecuteReader();
            while (dr1.Read())
            {
                lbladsyd.Text = dr1[0] + " " + dr1[1];
            }
            bgl.Close();
        }

        private void dataGridView5_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            bgl.Open();
            SqlCommand komut1 = new SqlCommand("Insert into TBL_MESAJLAR (GONDEREN,ALICI,BASLIQ,MEZMUN) values (@p1,@p2,@p3,@p4)", bgl);
            komut1.Parameters.AddWithValue("@p1", nomre);
            komut1.Parameters.AddWithValue("@p2", maskedTextBox2.Text);
            komut1.Parameters.AddWithValue("@p3",textBox2.Text);
            komut1.Parameters.AddWithValue("@p4",richTextBox2.Text);
            komut1.ExecuteNonQuery();
            bgl.Close();
            MessageBox.Show("Mesaj Gonderildi");
            geden();

        }
    }
}
