using System;// 基本型別與事件等
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;// SerialPort 串列通訊 API
using System.Linq;// LINQ（排序、ToArray）
using System.Text;// 編碼（ASCII/UTF8）
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;// WinForms UI 元件

namespace USARTtest003
{
    public partial class Form1 : Form // 視窗類別，繼承 Form
    {

        private readonly SerialPort _sp = new SerialPort(); // 串列埠物件（全域成員，反覆使用）

        ComboBox cboPort = new ComboBox { Left = 10, Top = 10, Width = 70 }; // COM 埠下拉
        ComboBox cboBaud = new ComboBox { Left = 90, Top = 10, Width = 70};                // 波特率下拉
        Button btnRefresh = new Button { Left = 250, Top = 10, Width = 70, Text = "Refresh" };// 重新掃描埠
        Button btnConnect = new Button { Left = 340, Top = 10, Width = 70, Text = "Connect" }; // 連線
        Button btnClose = new Button { Left = 430, Top = 10, Width = 70, Text = "Close", Enabled = false }; // 關閉

        TextBox txtSend = new TextBox { Left = 10, Top = 45, Width = 300 }; // 輸入要送的文字
        CheckBox chkCRLF = new CheckBox { Left = 320, Top = 47, Width = 100, Text = "CRLF", Checked = true }; // 是否自動附 \r\n
        Button btnSend0001 = new Button { Left = 180, Top = 76, Width = 50, Height = 28, Text = "右30", Enabled = false }; // 送出 "1" 的按鈕，預設不能按
        Button btnSend0010 = new Button { Left = 240, Top = 76, Width = 50, Height = 28, Text = "左30", Enabled = false }; // 送出 "0010" 的按鈕，預設不能按
        Button btnSend = new Button { Left = 420, Top = 46, Width = 80, Text = "Send", Enabled = false };// 送出按鈕
        Label lblStatus = new Label { Left = 180, Top = 105, Width = 300, Text = "未連線" };     // 狀態文字

        // ★ 新增：專門顯示回傳「整數值」的大文字框
        Label lblIntTitle = new Label { Left = 10, Top = 80, Width = 120, Text = "臂1角度 ：" };
        TextBox txtInt = new TextBox{Left = 70,Top = 76,Width = 100,ReadOnly = true,TextAlign = HorizontalAlignment.Right,
            Font = new System.Drawing.Font("Consolas", 16, System.Drawing.FontStyle.Bold)};


        TextBox txtLog = new TextBox{Left = 10,Top = 120,Width = 490,Height = 200, // 日誌視窗（多行唯讀）
            Multiline = true,ScrollBars = ScrollBars.Vertical,ReadOnly = true};

        
        public Form1()
        {
            InitializeComponent(); // 初始化 WinForms（必要呼叫）
            Text = "PC ⇄ STM32 USART (115200,8N1)"; // 視窗標題
            Width = 530; Height = 400; // 視窗大小

            // 把控制項加到視窗
            Controls.AddRange(new Control[]{ cboPort, cboBaud, btnRefresh, btnConnect, btnClose,
                                             txtSend, chkCRLF, btnSend0001, btnSend0010, btnSend, lblIntTitle, txtInt, txtLog, lblStatus});

            cboBaud.Items.AddRange(new object[] { "9600", "19200", "38400", "57600", "115200" }); // 波特率選項
            cboBaud.SelectedItem = "115200";  // 預設選 115200

            // 綁定按鈕事件
            btnRefresh.Click += (_, __) => RefreshPorts(); // 掃 COM
            btnConnect.Click += (_, __) => Connect(); // 開啟 COM
            btnClose.Click += (_, __) => Disconnect(); // 關閉 COM
            btnSend0001.Click += (_, __) => Send0001();    // Send 0001 按下觸發送資料
            btnSend0010.Click += (_, __) => Send0010();   // Send 0001 按下觸發送資料
            btnSend.Click += (_, __) => Send(); // 寫資料

            // 串口參數（與 STM32 一致）
            _sp.DataBits = 8; // 8 資料位
            _sp.Parity = Parity.None; // 無同位
            _sp.StopBits = StopBits.One; // 1 停止位
            _sp.Handshake = Handshake.None; // 無流控

            // 接收設定：逐行顯示；STM32 以 \r\n 結尾
            _sp.Encoding = Encoding.ASCII; // 以 ASCII 解碼（STM32 端多為 ASCII）
            _sp.NewLine = "\r\n"; // ReadLine() 的行結尾定義（配合韌體 CRLF）
            _sp.ReceivedBytesThreshold = 1; // >=1 byte 觸發 DataReceived
            _sp.DataReceived += OnDataReceived; //  非同步接收事件處理

            // 可選事件
            _sp.ErrorReceived += (_, e) => AppendLog($"[ERR] {e.EventType}");
            _sp.PinChanged += (_, e) => AppendLog($"[PIN] {e.EventType}");

            RefreshPorts(); // 啟動時先掃描一次可用 COM
        }

        private void RefreshPorts()
        {
            var ports = SerialPort.GetPortNames().OrderBy(p => p).ToArray(); // 取得目前所有 COMx
            cboPort.Items.Clear(); // 清空下拉
            cboPort.Items.AddRange(ports); // 加入掃描到的 COM
            if (ports.Length > 0) cboPort.SelectedIndex = 0; // 預設選第一個
            AppendLog(ports.Length > 0 ? "請選擇 COM 並 Connect" : "未找到任何 COM"); // 提示
        }

        private void Connect()
        {
            if (cboPort.SelectedItem == null || _sp.IsOpen) return; // 無選擇或已開啟就不處理
            _sp.PortName = cboPort.SelectedItem.ToString(); // 指定要開的 COMx
            try
            {
                _sp.PortName = cboPort.SelectedItem.ToString();     // 設定 COM 名稱（如 COM5）
                _sp.BaudRate = int.Parse(cboBaud.SelectedItem.ToString()); // 設定波特率（字串轉 int）
                _sp.Open(); // 嘗試開啟埠
                btnSend0001.Enabled = true;// 開啟後允許送資料0001
                btnSend0010.Enabled = true;// 開啟後允許送資料0010
                btnSend.Enabled = true; // 允許發送
                btnConnect.Enabled = false; // 禁止再連線
                btnClose.Enabled = true; // 允許關閉
                cboPort.Enabled = btnRefresh.Enabled = false; // 鎖住下拉/刷新，避免誤操作
                AppendLog($"[OPEN] {_sp.PortName} @ {_sp.BaudRate}");// 紀錄
            }
            catch (Exception ex) { AppendLog("[OPEN FAIL] " + ex.Message); }// 失敗訊息
        }

        private void Disconnect()
        {
            try { if (_sp.IsOpen) _sp.Close(); } catch { } // 安全關閉串口
            btnSend0001.Enabled = false;                            // 關閉後不能送
            btnSend0010.Enabled = false;
            btnSend.Enabled = false; // 禁止發送
            btnConnect.Enabled = true; // 允許重新連
            btnClose.Enabled = false; // 關閉鍵關閉
            cboPort.Enabled = btnRefresh.Enabled = true; // 解鎖下拉/刷新
            AppendLog("[CLOSE]"); // 紀錄
        }

        private void Send0001()       // 實際送出字串 "0001"
        {
            if (!_sp.IsOpen) return;  // 沒連線就不送
            try
            {
                if (chkCRLF.Checked)                  // 若勾選附加 CRLF
                    for (int i = 0; i < 1; i++)
                    {
                        _sp.Write("1\r\n");// 傳送 "1" 並加上 \r\n（多數韌體把 CR/LF 視為換行
                    }                      
                else
                    for (int i = 0; i < 1; i++)
                    {
                        _sp.Write("1");// 只傳 ASCII 字元 '1'
                    }                          

                lblStatus.Text = "已送出：1" + (chkCRLF.Checked ? @" (CRLF)" : ""); // 更新狀態：顯示是否帶 CRLF
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
                    for (int i = 0; i < 1; i++)
                    {
                        _sp.Write("2\r\n");// 傳送 "2" 並加上 \r\n（多數韌體把 CR/LF 視為換行
                    }           
                else
                    for (int i = 0; i < 1; i++)
                    {
                        _sp.Write("2");// 只傳 ASCII 字元 '2'
                    }

                lblStatus.Text = "已送出：2" + (chkCRLF.Checked ? @" (CRLF)" : ""); // 更新狀態：顯示是否帶 CRLF
            }
            catch (Exception ex)
            {
                MessageBox.Show("送出失敗： " + ex.Message);        // 傳輸失敗顯示錯誤
            }
        }

        private void Send()
        {
            if (!_sp.IsOpen) return; // 沒連線就不送
            try
            {
                var s = txtSend.Text ?? ""; // 取輸入框內容（null 安全）
                if (chkCRLF.Checked) _sp.WriteLine(s); else _sp.Write(s); // 視勾選決定是否附上 \r\n
                AppendLog($">> {s}"); // 在日誌顯示已送出的內容
            }
            catch (Exception ex) { AppendLog("[SEND FAIL] " + ex.Message); } // 送失敗訊息
        }

        private static readonly Regex AngleRegex = new Regex(@"\bAngle\s*=\s*(-?\d+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        // ★ 串口背景執行緒觸發的接收事件：逐行讀（以 _sp.NewLine = "\r\n" 為行結束）
        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string line;
                while (_sp.IsOpen && (line = _sp.ReadLine()) != null) // 讀到一行就處理
                {
                    var trimmed = line.Trim();

                    var m = AngleRegex.Match(trimmed);
                    // 1) 直接判斷整行是否為整數（含負號）
                    if (m.Success && int.TryParse(m.Groups[1].Value, out var angle))
                    {
                        SetIntBox(angle);                 // 顯示到「回傳整數」文字框
                        AppendLog($"<< {trimmed}");       // 仍記錄原始行到 Log（需要可移除）
                    }
                    else
                    {
                        // 非目標格式—只記錄
                        AppendLog($"<< {trimmed}");
                    }
                }
                    //AppendLog($"<< {line}"); // 顯示在日誌（含時間戳）
            }
            catch { /* 忽略關閉或逾時 */ }
        }
        // 跨執行緒安全更新「整數框」
        private void SetIntBox(int value)
        {
            if (txtInt.InvokeRequired) { BeginInvoke(new Action<int>(SetIntBox), value); return; }
            txtInt.Text = value.ToString();
        }

        // 跨執行緒安全地修改 UI（DataReceived 在背景執行緒）
        private void AppendLog(string text)
        {
            if (txtLog.InvokeRequired)
            {// 若非 UI 執行緒
                BeginInvoke(new Action<string>(AppendLog), text); // 切回 UI 執行緒
                return; }
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {text}\r\n");// 寫到多行 TextBox
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e); // 讓基底處理關閉
            try { if (_sp.IsOpen) _sp.Close(); } catch { } // 關視窗時記得關埠
        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
