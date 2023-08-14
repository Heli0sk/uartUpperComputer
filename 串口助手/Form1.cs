using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;
using System.Windows.Forms;

namespace 串口助手
{
    public partial class Form1 : Form
    {

        string receiveMode = "HEX模式";
        string receiveCoding = "GBK";
        string sendMode = "HEX模式";
        string sendCoding = "GBK";
        private System.Timers.Timer timer;

        List<byte> byteBuffer = new List<byte>();       //接收字节缓存区

        private string BytesToText(byte[] bytes, string encoding)       //字节流转文本
        {
            List<byte> byteDecode = new List<byte>();   //需要转码的缓存区
            byteBuffer.AddRange(bytes);                 //接收字节流到接收字节缓存区
            if (encoding == "GBK")
            {
                int count = byteBuffer.Count;
                for (int i = 0; i < count; i ++)
                {
                    if (byteBuffer.Count == 0)
                    {
                        break;
                    }
                    if (byteBuffer[0] < 0x80)       //1字节字符
                    {
                        byteDecode.Add(byteBuffer[0]);
                        byteBuffer.RemoveAt(0);
                    }
                    else       //2字节字符
                    {
                        if (byteBuffer.Count >= 2)
                        {
                            byteDecode.Add(byteBuffer[0]);
                            byteBuffer.RemoveAt(0);
                            byteDecode.Add(byteBuffer[0]);
                            byteBuffer.RemoveAt(0);
                        }
                    }
                }
            }
            else if (encoding == "UTF-8")
            {
                int count = byteBuffer.Count;
                for (int i = 0; i < count; i++)
                {
                    if (byteBuffer.Count == 0)
                    {
                        break;
                    }
                    if ((byteBuffer[0] & 0x80) == 0x00)     //1字节字符
                    {
                        byteDecode.Add(byteBuffer[0]);
                        byteBuffer.RemoveAt(0);
                    }
                    else if ((byteBuffer[0] & 0xE0) == 0xC0)     //2字节字符
                    {
                        if (byteBuffer.Count >= 2)
                        {
                            byteDecode.Add(byteBuffer[0]);
                            byteBuffer.RemoveAt(0);
                            byteDecode.Add(byteBuffer[0]);
                            byteBuffer.RemoveAt(0);
                        }
                    }
                    else if ((byteBuffer[0] & 0xF0) == 0xE0)     //3字节字符
                    {
                        if (byteBuffer.Count >= 3)
                        {
                            byteDecode.Add(byteBuffer[0]);
                            byteBuffer.RemoveAt(0);
                            byteDecode.Add(byteBuffer[0]);
                            byteBuffer.RemoveAt(0);
                            byteDecode.Add(byteBuffer[0]);
                            byteBuffer.RemoveAt(0);
                        }
                    }
                    else if ((byteBuffer[0] & 0xF8) == 0xF0)     //4字节字符
                    {
                        if (byteBuffer.Count >= 4)
                        {
                            byteDecode.Add(byteBuffer[0]);
                            byteBuffer.RemoveAt(0);
                            byteDecode.Add(byteBuffer[0]);
                            byteBuffer.RemoveAt(0);
                            byteDecode.Add(byteBuffer[0]);
                            byteBuffer.RemoveAt(0);
                            byteDecode.Add(byteBuffer[0]);
                            byteBuffer.RemoveAt(0);
                        }
                    }
                    else        //其他
                    {
                        byteDecode.Add(byteBuffer[0]);
                        byteBuffer.RemoveAt(0);
                    }
                }
            }
            return Encoding.GetEncoding(encoding).GetString(byteDecode.ToArray());
        }

        private string BytesToHex(byte[] bytes)     //字节流转HEX
        {
            string hex = "";
            foreach (byte b in bytes)
            {
                hex += b.ToString("X2") + " ";
            }
            return hex;
        }

        private byte[] TextToBytes(string str, string encoding)     // 文本转字节流
        {
            return Encoding.GetEncoding(encoding).GetBytes(str);
        }

        private byte[] HexToBytes(string str)       // HEX转字节流
        {
            string str1 = Regex.Replace(str, "[^A-F^a-f^0-9]", "");     // 清除非法字符

            double i = str1.Length;     // 将字符两两拆分
            int len = 2;                // 表示每两个字符一组进行拆分
            string[] strList = new string[int.Parse(Math.Ceiling(i / len).ToString())];
            for (int j = 0; j < strList.Length; j++)
            {
                len = len <= str1.Length ? len : str1.Length; // 确定每次拆分的字符长度
                strList[j] = str1.Substring(0, len);
                str1 = str1.Substring(len, str1.Length - len);
            }

            int count = strList.Length;     //将拆分后的字符依次转换为字节
            byte[] bytes = new byte[count];
            for (int j = 0; j < count; j ++)
            {
                bytes[j] = byte.Parse(strList[j], NumberStyles.HexNumber);
            }

            return bytes;
        }

        private void OpenSerialPort()       //打开串口
        {
            try
            {
                serialPort.PortName = cbPortName.Text;                  // 将串口号设置为选择的串口号
                serialPort.BaudRate = Convert.ToInt32(cbBaudRate.Text); // 设置波特率
                serialPort.DataBits = Convert.ToInt32(cbDataBits.Text); // 设置数据位
                StopBits[] sb = { StopBits.One, StopBits.OnePointFive, StopBits.Two }; // 设置停止位
                serialPort.StopBits = sb[cbStopBits.SelectedIndex];     // 根据选择的停止位索引从数组中取值
                Parity[] pt = { Parity.None, Parity.Odd, Parity.Even };
                serialPort.Parity = pt[cbParity.SelectedIndex];         // 设置奇偶校验
                serialPort.Open();

                btnOpen.BackColor = Color.Pink;  // 将打开串口按钮的背景色设置为粉色
                btnOpen.Text = "关闭串口";
                btnSend.Enabled = true;
                cbPortName.Enabled = false;
                cbBaudRate.Enabled = false;
                cbDataBits.Enabled = false;
                cbStopBits.Enabled = false;
                cbParity.Enabled = false;

            }
            catch
            {
                MessageBox.Show("串口打开失败", "提示");
            }
        }

        private void CloseSerialPort()      //关闭串口
        {
            serialPort.Close();

            btnOpen.BackColor = SystemColors.ControlLight;
            btnOpen.Text = "打开串口";
            btnSend.Enabled = false;
            cbPortName.Enabled = true;
            cbBaudRate.Enabled = true;
            cbDataBits.Enabled = true;
            cbStopBits.Enabled = true;
            cbParity.Enabled = true;
        }

        public Form1()
        {
            InitializeComponent();
        }
        
        private void Form1_Load(object sender, EventArgs e)        //窗口加载事件
        {
            cbBaudRate.SelectedIndex = 1;       //控件状态初始化
            cbDataBits.SelectedIndex = 3;
            cbStopBits.SelectedIndex = 0;
            cbParity.SelectedIndex = 0;
            cbReceiveMode.SelectedIndex = 0;
            cbReceiveCoding.SelectedIndex = 0;
            cbSendMode.SelectedIndex = 0;
            cbSendCoding.SelectedIndex = 0;
            btnSend.Enabled = false;
            cbPortName.Enabled = true;
            cbBaudRate.Enabled = true;
            cbDataBits.Enabled = true;
            cbStopBits.Enabled = true;
            cbParity.Enabled = true;
            tbTime.AppendText("1000");
        }

        private void cbPortName_DropDown(object sender, EventArgs e)        //串口号下拉事件
        {
            string currentName = cbPortName.Text;       // 将当前下拉列表的选中项保存到 currentName 变量中
            string[] names = SerialPort.GetPortNames(); // 搜索可用串口号并添加到下拉列表
            cbPortName.Items.Clear();                   // 清空下拉列表中的所有项
            cbPortName.Items.AddRange(names);           // 将 names 数组中的串口号添加到下拉列表中
            cbPortName.Text = currentName;              // 将之前保存的选中项 currentName 设置为当前下拉列表的文本
            // Console.WriteLine(currentName);
        }

        private void btnOpen_Click(object sender, EventArgs e)      //打开串口点击事件
        {
            if (btnOpen.Text == "打开串口"){
                OpenSerialPort();
            }
            else if (btnOpen.Text == "关闭串口"){
                CloseSerialPort();
            }

        }

        protected override void DefWndProc(ref Message m)       //USB拔出事件
        {
            
            if (m.Msg == 0x0219)        //WM_DEVICECHANGE 表示设备状态发生了改变
            {
                if (m.WParam.ToInt32() == 0x8004)  // 表示设备被移除
                {
                    if (btnOpen.Text == "关闭串口" && serialPort.IsOpen == false)
                    {
                        CloseSerialPort();      //USB异常拔出，关闭串口
                    }
                }
            }
            base.DefWndProc(ref m); // 将消息传递给基类的 DefWndProc 方法进行默认的处理
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e) //定时器触发的事件
        {
             // 发送数据
        }

        private void btnSend_Click(object sender, EventArgs e)      //发送点击事件
        {
            if (serialPort.IsOpen){
                if (cbTimingSend.Checked)
                {
                    // TODO: 定时发送
                    Console.WriteLine(int.Parse(tbTime.Text));
                    timer = new System.Timers.Timer();      // 初始化定时器
                    timer.Interval = int.Parse(tbTime.Text);// 设置定时器间隔为1000毫秒
                    timer.Elapsed += Timer_Elapsed;         // 绑定定时器的Elapsed事件
                    timer.Start();                          // 启动定时器
                }
                if (sendMode == "HEX模式"){
                    byte[] dataSend = HexToBytes(tbSend.Text);      //HEX转字节流
                    int count = dataSend.Length;
                    serialPort.Write(dataSend, 0, count);       //串口发送
                }
                else if (sendMode == "文本模式"){
                    byte[] dataSend = TextToBytes(tbSend.Text, sendCoding);      //文本转字节流
                    int count = dataSend.Length;
                    serialPort.Write(dataSend, 0, count);       //串口发送
                }
            }
        }

        private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)      //串口接收数据事件
        {
            if (serialPort.IsOpen){
                int count = serialPort.BytesToRead;     // 返回当前串口接收缓冲区中的字节数
                byte[] dataReceive = new byte[count];   // 创建一个长度为 count 的字节数组 用于存储接收到的数据
                serialPort.Read(dataReceive, 0, count); //串口接收，参数：接收数据的目标字节数组、目标字节数组中的起始位置、要读取的字节数

                this.BeginInvoke((EventHandler)(delegate // 确保在 UI 线程上更新用户界面
                {
                    if (receiveMode == "HEX模式"){
                        tbReceive.AppendText(BytesToHex(dataReceive));  //字节流转HEX
                    }
                    else if (receiveMode == "文本模式"){
                        tbReceive.AppendText(BytesToText(dataReceive, receiveCoding));       //字节流转文本
                    }
                    if (cbBreakLine.Checked)                            // 换行显示
                        tbReceive.AppendText("\r\n");
                }));
            }
        }
        
        private void btnClearReceive_Click(object sender, EventArgs e)      //清空接收区点击事件
        {
            tbReceive.Clear();
        }

        private void btnClearSend_Click(object sender, EventArgs e)      //清空发送区点击事件
        {
            tbSend.Clear();
        }

        private void cbReceiveMode_SelectedIndexChanged(object sender, EventArgs e)     //接收模式选择事件
        {
            if (cbReceiveMode.Text == "HEX模式"){
                cbReceiveCoding.Enabled = false;
                receiveMode = "HEX模式";
            }
            else if (cbReceiveMode.Text == "文本模式"){ //仅接收模式为文本模式时 文本编码才可用
                cbReceiveCoding.Enabled = true;
                receiveMode = "文本模式";
            }
            byteBuffer.Clear();
        }

        private void cbReceiveCoding_SelectedIndexChanged(object sender, EventArgs e)     //接收编码选择事件
        {
            if (cbReceiveCoding.Text == "GBK"){
                receiveCoding = "GBK";
            }
            else if (cbReceiveCoding.Text == "UTF-8"){
                receiveCoding = "UTF-8";
            }
            byteBuffer.Clear();
        }

        private void cbSendMode_SelectedIndexChanged(object sender, EventArgs e)     //发送模式选择事件
        {
            if (cbSendMode.Text == "HEX模式"){
                cbSendCoding.Enabled = false;
                sendMode = "HEX模式";
            }
            else if (cbSendMode.Text == "文本模式"){  //仅发送模式为文本模式时 文本编码才可用
                cbSendCoding.Enabled = true;
                sendMode = "文本模式";
            }
        }

        private void cbSendCoding_SelectedIndexChanged(object sender, EventArgs e)     //发送编码选择事件
        {
            if (cbSendCoding.Text == "GBK"){
                sendCoding = "GBK";
            }
            else if (cbSendCoding.Text == "UTF-8"){
                sendCoding = "UTF-8";
            }
        }

        private void tbSend_TextChanged(object sender, EventArgs e)
        {

        }
    }
}

