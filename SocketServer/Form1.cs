using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace SocketServer
{
    public partial class Form1 : Form
    {
        string user = "";
        Socket ss = null;
        Socket s = null;
        public static List<Socket> sl = new List<Socket>();
        public static List<String> nl = new List<String>();
        public static Dictionary<string, Socket> sd = new Dictionary<string, Socket>();

        Thread clientThread = null;
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                int port = int.Parse(txt_port.Text);
                string host = txt_ip.Text;
                //创建终结点
                IPAddress ip = IPAddress.Parse(host);
                IPEndPoint ipe = new IPEndPoint(ip, port);
                //创建Socket并开始监听
                s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); //创建一个Socket对象，如果用UDP协议，则要用SocketTyype.Dgram类型的套接字
                s.Bind(ipe);    //绑定EndPoint对象(2000端口和ip地址)
                s.Listen(0);    //开始监听
                //为新建立的连接创建新的Socket
                Thread c = new Thread(new ThreadStart(AcceptClient));
                c.Start();
                SetText("开始监听");
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message);
            }
        }

        public void RefreshClient()
        {
            DataTable _dtSocket = new DataTable();
            _dtSocket.Columns.Add("Client");
            //foreach (string item in nl)
            //{
            //    _dtSocket.Rows.Add(item);
            //}
            foreach (string item in sd.Keys)
            {
                _dtSocket.Rows.Add(item);
            }
            SetDataSource(_dtSocket);
        }

        public void AcceptClient()
        {
            try
            {
                while (true)
                {
                    ss = s.Accept();
                    RegeistUser(ss);
                    RefreshClient();
                    clientThread = new Thread(new ThreadStart(ReceiveData));
                    clientThread.Start();
                }
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.ToString());
            }
        }

        /// <summary>
        /// 注册socket对象到字典
        /// </summary>
        /// <param name="ss"></param>
        public void RegeistUser(Socket ss)
        {
            sd.Add(Guid.NewGuid().ToString(), ss);
        }

        public delegate void SetDataSourceHandler(DataTable p_dtSource);
        private void SetDataSource(DataTable p_dtSource)
        {
            if (dgv_client.InvokeRequired == true)
            {
                SetDataSourceHandler set = new SetDataSourceHandler(SetDataSource);//委托的方法参数应和SetText一致 
                dgv_client.Invoke(set, new object[] { p_dtSource }); //此方法第二参数用于传入方法,代替形参text 
            }
            else
            {
                dgv_client.DataSource = p_dtSource;
            }

        }

        public delegate void SetTextHandler(string text);
        private void SetText(string text)
        {
            if (rich_back.InvokeRequired == true)
            {
                SetTextHandler set = new SetTextHandler(SetText);//委托的方法参数应和SetText一致 
                rich_back.Invoke(set, new object[] { text }); //此方法第二参数用于传入方法,代替形参text 
            }
            else
            {
                rich_back.Text += text;
            }

        } 


        public void ReceiveData()
        {
            try
            {
                while (true)
                {
                    string recvStr = "";
                    byte[] recvBytes = new byte[1024];
                    int bytes;
                    bytes = ss.Receive(recvBytes, recvBytes.Length, 0); //从客户端接受消息
                    recvStr = Encoding.UTF8.GetString(recvBytes, 0, bytes);
                    SetText(recvStr);
                    SendMessageToTarget(recvStr);
                   
                }
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message);
            }
        }

        /// <summary>
        /// 【转发】客户端信息
        /// </summary>
        /// <param name="user">转发目标</param>
        /// <param name="message">消息内容</param>
        public void SendMessageToTarget(string user, string message)
        {
            ss = sd[user];
            SendOneMessage(message);
        }

        /// <summary>
        /// 【转发】客户端信息
        /// </summary>
        /// <param name="userandmess">用户名:消息内容</param>
        public void SendMessageToTarget(string userandmess)
        {
            string[] s = userandmess.Split(':');
            SendMessageToTarget(s[0], s[1]);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (ss != null)
            {
                ss.Shutdown(SocketShutdown.Both);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //给Client端返回信息
            string sendStr = txt_message.Text;
            if (cb_all.Checked)
            {
                SendAllMessage(sendStr);
            }
            else
            {
                SendOneMessage(sendStr);
            }
            rich_back.Text += "\n 我：" + sendStr;
            txt_message.Text = "";
        }

        public void SendAllMessage(string p_strsend)
        {
            foreach (Socket item in sl)
            {
                byte[] bs = Encoding.UTF8.GetBytes(p_strsend);
                ss.Send(bs, bs.Length, 0);  //返回信息给客户端
            }
        }

        /// <summary>
        /// 将消息内容发给客户端
        /// </summary>
        /// <param name="p_strsend"></param>
        public void SendOneMessage(string p_strsend)
        {
            byte[] bs = Encoding.ASCII.GetBytes(p_strsend);
            ss.Send(bs, bs.Length, 0);  //返回信息给客户端
        }


        private void dgv_client_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            //ss = sl[e.RowIndex];
        }

    }
}
