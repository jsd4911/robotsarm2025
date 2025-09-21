using System;
using System.IO.Ports;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace USARTtest001
{
    public partial class Form1 : Form
    {
        private SerialPort _sp = new SerialPort();

        ComboBox cboPort = new ComboBox { Left = 10, Top = 10, Width = 120 };
        ComboBox cboBaud = new ComboBox { Left = 140, Top = 10, Width = 100 };
        Button btnRefresh = new Button { Left = 250, Top = 10, Width = 80, Text = "Refresh" };
        Button btnConnect = new Button { Left = 340, Top = 10, Width = 80, Text = "Connect" };
        Button btnDisconnect = new Button { Left = 430, Top = 10, Width = 80, Text = "Close", Enabled = false };

        TextBox txtSend = new TextBox { Left = 10, Top = 50, Width = 420 };
        Button btnSend = new Button { Left = 440, Top = 48, Width = 70, Text = "Send", Enabled = false };
        public Form1()
        {
            InitializeComponent();
            this.Text = "USART Sender";
            this.Width = 540; this.Height = 140;

            Controls.Add(cboPort);
            Controls.Add(cboBaud);
            Controls.Add(btnRefresh);
            Controls.Add(btnConnect);
            Controls.Add(btnDisconnect);
            Controls.Add(txtSend);
            Controls.Add(btnSend);

            cboBaud.Items.AddRange(new object[] { "9600", "19200", "38400", "57600", "115200" });
            cboBaud.SelectedItem = "115200";

            btnRefresh.Click += (_, __) => RefreshPorts();
            btnConnect.Click += (_, __) => Connect();
            btnDisconnect.Click += (_, __) => Disconnect();
            btnSend.Click += (_, __) => SendLine();

            // SerialPort 基本設定
            _sp.DataBits = 8;
            _sp.Parity = Parity.None;
            _sp.StopBits = StopBits.One;
            _sp.Handshake = Handshake.None;
            _sp.NewLine = "\r\n";

            RefreshPorts();
        }
        private void RefreshPorts()
        {
            var ports = SerialPort.GetPortNames().OrderBy(p => p).ToArray();
            cboPort.Items.Clear();
            cboPort.Items.AddRange(ports);
            if (ports.Length > 0) cboPort.SelectedIndex = 0;
        }

        private void Connect()
        {
            if (cboPort.SelectedItem == null || cboBaud.SelectedItem == null) return;
            if (_sp.IsOpen) return;

            _sp.PortName = cboPort.SelectedItem.ToString();
            _sp.BaudRate = int.Parse(cboBaud.SelectedItem.ToString());

            try
            {
                _sp.Open();
                btnSend.Enabled = true;
                btnConnect.Enabled = false;
                btnDisconnect.Enabled = true;
                cboPort.Enabled = cboBaud.Enabled = btnRefresh.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Open failed: " + ex.Message);
            }
        }

        private void Disconnect()
        {
            try
            {
                if (_sp.IsOpen) _sp.Close();
            }
            catch { }
            btnSend.Enabled = false;
            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
            cboPort.Enabled = cboBaud.Enabled = btnRefresh.Enabled = true;
        }

        private void SendLine()
        {
            if (!_sp.IsOpen) return;
            try
            {
                // 送出文字，並附上 \r\n（STM32 範例會識別 \r 換行）
                _sp.WriteLine(txtSend.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Send failed: " + ex.Message);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (_sp.IsOpen) { try { _sp.Close(); } catch { } }
        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
