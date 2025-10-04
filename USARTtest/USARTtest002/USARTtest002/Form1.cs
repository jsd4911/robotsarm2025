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

namespace USARTtest002
{
    public partial class Form1 : Form
    {
        private readonly SerialPort _sp = new SerialPort();  // 建立一個串列埠物件

        ComboBox cboPort = new ComboBox { Left = 10, Top = 10, Width = 120 };                 // COM 埠下拉
        ComboBox cboBaud = new ComboBox { Left = 140, Top = 10, Width = 100 };                // 波特率下拉
        Button btnRefresh = new Button { Left = 250, Top = 10, Width = 80, Text = "Refresh" }; // 重新掃描 COM
        Button btnConnect = new Button { Left = 340, Top = 10, Width = 80, Text = "Connect" }; // 連線按鈕
        Button btnClose = new Button { Left = 430, Top = 10, Width = 80, Text = "Close", Enabled = false }; // 關閉按鈕，預設不能按

        CheckBox chkCRLF = new CheckBox { Left = 10, Top = 50, Width = 120, Text = "附加 CRLF (\\r\\n)", Checked = false }; // 是否附加 CRLF 勾選
        Button btnSend0001 = new Button { Left = 150, Top = 46, Width = 120, Height = 28, Text = "Send 0001", Enabled = false }; // 送出 "0001" 的按鈕，預設不能按
        Button btnSend0010 = new Button { Left = 300, Top = 46, Width = 120, Height = 28, Text = "Send 0010", Enabled = false }; // 送出 "0010" 的按鈕，預設不能按
        Label lblStatus = new Label { Left = 10, Top = 85, Width = 500, Text = "未連線" };     // 狀態文字

        public Form1()                // 建構子：視窗建立時執行
        {
            InitializeComponent();    // WinForms 初始化（設計器基礎設定）
            this.Text = "USART Sender - 0001"; // 視窗標題
            this.Width = 540; this.Height = 150; // 視窗尺寸

            Controls.Add(cboPort);    // 把控制項加入視窗
            Controls.Add(cboBaud);
            Controls.Add(btnRefresh);
            Controls.Add(btnConnect);
            Controls.Add(btnClose);
            Controls.Add(chkCRLF);
            Controls.Add(btnSend0001);
            Controls.Add(btnSend0010);
            Controls.Add(lblStatus);

            cboBaud.Items.AddRange(new object[] { "9600", "19200", "38400", "57600", "115200" }); // 波特率選項
            cboBaud.SelectedItem = "115200";  // 預設選 115200

            btnRefresh.Click += (_, __) => RefreshPorts(); // Refresh 按下觸發掃描 COM
            btnConnect.Click += (_, __) => Connect();      // Connect 按下觸發連線
            btnClose.Click += (_, __) => Disconnect();   // Close 按下觸發關閉
            btnSend0001.Click += (_, __) => Send0001();    // Send 0001 按下觸發送資料
            btnSend0010.Click += (_, __) => Send0010();   // Send 0001 按下觸發送資料

            // SerialPort 基本參數：115200, 8N1, 無流控（此處先設固定屬性，PortName/ BaudRate 稍後由 UI 設定）
            _sp.DataBits = 8;                 // 8 個資料位
            _sp.Parity = Parity.None;         // 無同位檢查
            _sp.StopBits = StopBits.One;      // 1 個停止位
            _sp.Handshake = Handshake.None;   // 無硬體/軟體流控

            RefreshPorts();                   // 啟動時先掃描一次 COM 列表並填入下拉
        }

        private void RefreshPorts()   // 重新掃描電腦可用的 COM 埠
        {
            var ports = SerialPort.GetPortNames().OrderBy(p => p).ToArray(); // 取得全部 COM 名稱並排序為陣列
            cboPort.Items.Clear();                // 清空下拉
            cboPort.Items.AddRange(ports);        // 加入掃描到的 COM
            if (ports.Length > 0) cboPort.SelectedIndex = 0; // 如果有找到，預設選第一個
            lblStatus.Text = ports.Length > 0 ? "請選擇 COM 並連線" : "找不到任何 COM 埠"; // 更新狀態文字
        }

        private void Connect()        // 嘗試開啟串列埠
        {
            if (cboPort.SelectedItem == null || cboBaud.SelectedItem == null) return; // 未選取 COM 或波特率就不處理
            if (_sp.IsOpen) return;   // 已開啟就不重複開

            try
            {
                _sp.PortName = cboPort.SelectedItem.ToString();     // 設定 COM 名稱（如 COM5）
                _sp.BaudRate = int.Parse(cboBaud.SelectedItem.ToString()); // 設定波特率（字串轉 int）
                _sp.Open();                                         // 開啟串列埠

                btnSend0001.Enabled = true;// 開啟後允許送資料0001
                btnSend0010.Enabled = true;// 開啟後允許送資料0010
                btnConnect.Enabled = false;                         // 禁用 Connect（避免重複連）
                btnClose.Enabled = true;                            // 允許 Close
                cboPort.Enabled = cboBaud.Enabled = btnRefresh.Enabled = false; // 連線後鎖定下拉與 Refresh
                lblStatus.Text = $"已連線：{_sp.PortName} @ {_sp.BaudRate}"; // 顯示連線資訊
            }
            catch (Exception ex)
            {
                MessageBox.Show("開啟失敗： " + ex.Message);        // 開啟失敗，跳出錯誤訊息框
                lblStatus.Text = "連線失敗";                        // 更新狀態
            }
        }

        private void Disconnect()     // 關閉串列埠
        {
            try { if (_sp.IsOpen) _sp.Close(); } catch { }          // 若開著就關掉（忽略關閉例外）
            btnSend0001.Enabled = false;                            // 關閉後不能送
            btnSend0010.Enabled = false;
            btnConnect.Enabled = true;                              // 允許重新 Connect
            btnClose.Enabled = false;                               // 關閉按鈕失效
            cboPort.Enabled = cboBaud.Enabled = btnRefresh.Enabled = true; // 重新開放下拉與 Refresh
            lblStatus.Text = "已關閉連線";                          // 狀態更新
        }

        private void Send0001()       // 實際送出字串 "0001"
        {
            if (!_sp.IsOpen) return;  // 沒連線就不送
            try
            {
                if (chkCRLF.Checked)                  // 若勾選附加 CRLF
                    _sp.Write("1\r\n");           // 傳送 "0001" 並加上 \r\n（多數韌體把 CR/LF 視為換行）
                else
                    _sp.Write("1");               // 只傳 ASCII 字元 '0','0','0','1'

                lblStatus.Text = "已送出：0001" + (chkCRLF.Checked ? @" (CRLF)" : ""); // 更新狀態：顯示是否帶 CRLF
            }
            catch (Exception ex)
            {
                MessageBox.Show("送出失敗： " + ex.Message);        // 傳輸失敗顯示錯誤
            }
        }

        private void Send0010()       // 實際送出字串 "0001"
        {
            if (!_sp.IsOpen) return;  // 沒連線就不送
            try
            {
                if (chkCRLF.Checked)                  // 若勾選附加 CRLF
                    _sp.Write("2\r\n");           // 傳送 "0001" 並加上 \r\n（多數韌體把 CR/LF 視為換行）
                else
                    _sp.Write("2");               // 只傳 ASCII 字元 '0','0','0','1'

                lblStatus.Text = "已送出：0010" + (chkCRLF.Checked ? @" (CRLF)" : ""); // 更新狀態：顯示是否帶 CRLF
            }
            catch (Exception ex)
            {
                MessageBox.Show("送出失敗： " + ex.Message);        // 傳輸失敗顯示錯誤
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e) // 視窗關閉事件
        {
            base.OnFormClosing(e);               // 呼叫父類別處理（必要）
            try { if (_sp.IsOpen) _sp.Close(); } catch { } // 關閉時若串列埠開著就關掉，避免佔用殘留
        }

    }
}
