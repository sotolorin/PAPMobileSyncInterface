using System;
using System.Windows.Forms;
using ScrapDragon.Custom.Pap.WebService;
using ScrapDragon.Custom.Pap.WebService.RnR;

namespace UI
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var client = new PapWebService();
            var response = client.ReceivePapData(new ReceivePapDataRequest());
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var client = new PapWebService();
            var response = client.SendPapData(new SendPapDataRequest());
        }
    }
}

