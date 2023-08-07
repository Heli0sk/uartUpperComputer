using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace 串口通信上位机
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        /* 新增自定义函数：更新可用串口 */
        private void Updata_Serialport_Name(ComboBox MycomboBox)
        {
            string[] ArryPort;
            ArryPort = SerialPort.GetPortNames();            // SerialPort.GetPortNames()函数功能为获取计算机所有可用串口，以字符串数组形式输出
            MycomboBox.Items.Clear();                        // 清除当前组合框下拉菜单内容                  
            for (int i = 0; i < ArryPort.Length; i++){
                MycomboBox.Items.Add(ArryPort[i]);           // 将所有的可用串口号添加到端口对应的组合框中
            }
        }

        /* 启动窗口加载函数：启动窗口后检测可用串口*/
        private void Form1_Load(object sender, EventArgs e)
        {
            Updata_Serialport_Name(comboBox1); // 调用更新可用串口函数，comboBox1为端口号组合框的名称
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false; // 禁用线程之间对控件的跨线程访问检查

        }

        /*"打开串口"按键回调函数*/
        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "打开串口")                                  // 如果当前是串口设备是关闭状态
            {
                try                                                          // try 是尝试部分，如果尝试过程中出现问题，进入catch部分，执行错误处理代码  
                {
                    serialPort1.PortName = comboBox1.Text;                   // 将串口设备的串口号属性设置为comboBox1复选框中选择的串口号
                    serialPort1.BaudRate = Convert.ToInt32(comboBox2.Text);  // 将串口设备的波特率属性设置为comboBox2复选框中选择的波特率
                    serialPort1.Open();                                      // 打开串口，如果打开了继续向下执行，如果失败了，跳转至catch部分
                    comboBox1.Enabled = false;                               // 串口已打开，将comboBox1设置为不可操作
                    comboBox2.Enabled = false;                               // 串口已打开，将comboBox2设置为不可操作
                    button1.BackColor = Color.Red;                           // 将串口开关按键的颜色，改为红色
                    button1.Text = "关闭串口";                               // 将串口开关按键的文字改为“关闭串口”
                }
                catch
                {
                    MessageBox.Show("打开串口失败，请检查串口", "错误");     // 弹出错误对话框
                }
            }
            else                                             // 如果当前串口设备是打开状态
            {
                try
                {
                    serialPort1.Close();                     // 关闭串口
                    comboBox1.Enabled = true;                // 串口已关闭，将comboBox1设置为可操作
                    comboBox2.Enabled = true;                // 串口已关闭，将comboBox2设置为可操作
                    button1.BackColor = Color.Lime;          // 将串口开关按键的颜色，改为青绿色
                    button1.Text = "打开串口";               // 将串口开关按键的文字改为“打开串口”
                }
                catch
                {
                    MessageBox.Show("关闭串口失败，请检查串口", "错误");   // 弹出错误对话框
                }
            }

        }

        /*"清除接收"按键回调函数*/
        private void button2_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
        }

        /*"发送"按键回调函数*/
        private void button3_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen)            // 如果串口设备已经打开了
            {
                if (!checkBox1.Checked)        // 如果是以字符的形式发送数据
                {
                    char[] str = new char[1];  // 定义一个字符数组，只有一位

                    try
                    {
                        for (int i = 0; i < textBox2.Text.Length; i++)
                        {
                            str[0] = Convert.ToChar(textBox2.Text.Substring(i, 1));  // 取待发送文本框中的第i个字符
                            serialPort1.Write(str, 0, 1);                            // 写入串口设备进行发送
                        }
                    }
                    catch
                    {
                        MessageBox.Show("串口字符写入错误!", "错误");   // 弹出发送错误对话框
                        serialPort1.Close();                          // 关闭串口
                        button1.BackColor = Color.Lime;               // 将串口开关按键的颜色，改为青绿色
                        button1.Text = "打开串口";                    // 将串口开关按键的文字改为“打开串口”
                    }
                }
                else                                                  // 如果以数值的形式发送
                {
                    byte[] Data = new byte[1];                        // 定义一个byte类型数据，相当于C语言的unsigned char类型
                    int flag = 0;                                     // 定义一个标志，标志这是第几位
                    try
                    {
                        for (int i = 0; i < textBox2.Text.Length; i++)
                        {
                            if (textBox2.Text.Substring(i, 1) == " " && flag == 0)                // 如果是第一位，并且为空字符
                            {
                                continue;
                            }

                            if (textBox2.Text.Substring(i, 1) != " " && flag == 0)                // 如果是第一位，但不为空字符
                            {
                                flag = 1;                                                         // 标志转到第二位数据去
                                if (i == textBox2.Text.Length - 1)                                // 如果这是文本框字符串的最后一个字符
                                {
                                    Data[0] = Convert.ToByte(textBox2.Text.Substring(i, 1), 16);  // 转化为byte类型数据，以16进制显示
                                    serialPort1.Write(Data, 0, 1);                                // 通过串口发送
                                    flag = 0;                                                     // 标志回到第一位数据去
                                }
                                continue;
                            }
                            else if (textBox2.Text.Substring(i, 1) == " " && flag == 1)           // 如果是第二位，且第二位字符为空
                            {
                                Data[0] = Convert.ToByte(textBox2.Text.Substring(i - 1, 1), 16);  // 只将第一位字符转化为byte类型数据，以十六进制显示
                                serialPort1.Write(Data, 0, 1);                                    // 通过串口发送
                                flag = 0;                                                         // 标志回到第一位数据去
                                continue;
                            }
                            else if (textBox2.Text.Substring(i, 1) != " " && flag == 1)           // 如果是第二位字符，且第一位字符不为空
                            {
                                Data[0] = Convert.ToByte(textBox2.Text.Substring(i - 1, 2), 16);  // 将第一，二位字符转化为byte类型数据，以十六进制显示
                                serialPort1.Write(Data, 0, 1);                                    // 通过串口发送
                                flag = 0;                                                         // 标志回到第一位数据去
                                continue;
                            }
                        }
                    }
                    catch
                    {
                        MessageBox.Show("串口数值写入错误!", "错误");
                        serialPort1.Close();
                        button1.BackColor = Color.Lime;   // 将串口开关按键的颜色，改为青绿色
                        button1.Text = "打开串口";        // 将串口开关按键的文字改为  “打开串口”
                    }
                }
            }

        }

        /*串口接收函数*/
        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (!checkBox2.Checked)                        // 如果以字符串形式读取
            {
                string str = serialPort1.ReadExisting();   // 读取串口接收缓冲区字符串

                textBox1.AppendText(str + "");             // 在接收文本框中进行显示
            }
            else                                           // 以数值形式读取
            {
                int length = serialPort1.BytesToRead;      // 读取串口接收缓冲区字节数

                byte[] data = new byte[length];            // 定义相同字节的数组

                serialPort1.Read(data, 0, length);         // 串口读取缓冲区数据到数组中

                for (int i = 0; i < length; i++)
                {
                    string str = Convert.ToString(data[i], 16).ToUpper();                          // 将数据转换为字符串格式
                    textBox1.AppendText("0X" + (str.Length == 1 ? "0" + str + " " : str + " "));   // 添加到串口接收文本框中
                }
            }

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Updata_Serialport_Name(comboBox1);   // 定时刷新可用串口，可以保证在程序启动之后连接的设备也能被检测到
        }
    }



}
