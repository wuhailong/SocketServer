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
using ToolFunction;

namespace SocketServer
{
    public partial class Form1 : Form
    {
        #region 所有用户拼接字符串，以';'分割
        string users = "@";
        #endregion       
        Socket newsock = null;
        //Socket client = null;
        IntPtr ip = IntPtr.Zero;
        public static List<Socket> sl = new List<Socket>();
        public static List<String> nl = new List<String>();
        public static Dictionary<string, Socket> userSocketDict = new Dictionary<string, Socket>();
        public static Dictionary<IntPtr, Socket> handleSocketDict = new Dictionary<IntPtr, Socket>();
        public static Dictionary<string, IntPtr> userHandleDict = new Dictionary<string, IntPtr>();
        Thread acceptClientThread = null;
        List<Thread> receiveThreadList = new List<Thread>();
        List<Thread> receiveMessageThreadList = new List<Thread>();
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
                newsock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); //创建一个Socket对象，如果用UDP协议，则要用SocketTyype.Dgram类型的套接字
                newsock.Bind(ipe);    //绑定EndPoint对象
                newsock.Listen(0);    //开始监听
                //为新建立的连接创建新的Socket
                acceptClientThread = new Thread(new ThreadStart(AcceptClient));
                acceptClientThread.Start();
                SetText("开始监听");
            }
            catch (Exception exp)
            {
                CommonFunction.WriteLog(exp, exp.Message);
            }
        }

        /// <summary>
        /// 刷新客户端列表
        /// </summary>
        public void RefreshClient()
        {
            UnionDict();
            DataTable _dtSocket = new DataTable();
            _dtSocket.Columns.Add("Client");
            users = "";
            foreach (string item in userSocketDict.Keys)
            {
                _dtSocket.Rows.Add(item);
                users += item + ";";
            }
            users = "@" + users.Trim(';');
            SendAllMessage(users);
            SetDataSource(_dtSocket);
        }

        /// <summary>
        /// handleSocketDict 与 userHandleDict 合并为 userSocketDict
        /// </summary>
        public void UnionDict()
        {
            foreach (string user in userHandleDict.Keys)
            {
                userSocketDict.Add(user, handleSocketDict[userHandleDict[user]]);
            }
        }

        /// <summary>
        /// 接受客户端，可接受多个客户端同时连入，并对连入的客户端注册到客户端列表
        /// </summary>
        public void AcceptClient()
        {
            try
            {
                while (true)
                {
                    Socket client = newsock.Accept();
                    ip = client.Handle;
                    RegeistUser(client.Handle, client);
                    Thread clientThread = new Thread(new ParameterizedThreadStart(ReceiveData));
                    object o = client;
                    clientThread.Start(o);
                }
            }
            catch (Exception exp)
            {
                CommonFunction.WriteLog(exp, exp.Message);
            }
        }

        /// <summary>
        /// 对连入的客户端注册到客户端列表,名称为guid
        /// </summary>
        /// <param name="ss"></param>
        public void RegeistUser(Socket ss)
        {
           RegeistUser(Guid.NewGuid().ToString(), ss);
        }

        /// <summary>
        /// 将socket注册为指定用户名
        /// </summary>
        /// <param name="user"></param>
        /// <param name="ss"></param>
        public void RegeistUser(string user, Socket ss)
        {
            user = user.Remove(0, 1);
            userSocketDict.Add(user, ss);
            SendOneMessage(ss, "欢迎" + user + "连入！");
            RefreshClient();
        }

        /// <summary>
        /// 指定用户名与handle对应
        /// </summary>
        /// <param name="user"></param>
        /// <param name="hindle"></param>
        public void RegeistUser(string user, IntPtr hindle)
        {
            user = user.Remove(0, 1);
            userHandleDict.Add(user,hindle);
            RefreshClient();
        }


        /// <summary>
        /// 将socket注册为指定用户名
        /// </summary>
        /// <param name="h">socket 句柄</param>
        /// <param name="ss">socket</param>
        public void RegeistUser(IntPtr h, Socket ss)
        {
            handleSocketDict.Add(h,ss);
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
        /// <summary>
        /// 给文本框赋值
        /// </summary>
        /// <param name="text"></param>
        private void SetText(string text)
        {
            if (rich_back.InvokeRequired == true)
            {
                SetTextHandler set = new SetTextHandler(SetText);//委托的方法参数应和SetText一致 
                rich_back.Invoke(set, new object[] { text }); //此方法第二参数用于传入方法,代替形参text 
            }
            else
            {
                rich_back.Text += "\n" + text;
            }

        }

        /// <summary>
        /// 接收客户端数据并，转发到目标客户端。
        /// </summary>
        public void ReceiveData(object o)
        {
            try
            {
                while (true)
                {
                    Socket client = (Socket)o;
                    string recvStr = "";
                    byte[] recvBytes = new byte[1024];
                    int bytes;
                    bytes = client.Receive(recvBytes, recvBytes.Length, 0); //从客户端接受消息
                    recvStr = Encoding.UTF8.GetString(recvBytes, 0, bytes);
                    SendMessage(client, recvStr);
                    SetText(recvStr);
                    CommonFunction.WriteErrorLog(recvStr);
                }
            }
            catch (Exception exp)
            {
                CommonFunction.WriteLog(exp, exp.Message);
            }
        }

        public void ConnectAllClient()
        {
            //while (true)
            //{
            //    string recvStr = "";
            //    byte[] recvBytes = new byte[1024];
            //    int bytes;
            //    bytes = client.Receive(recvBytes, recvBytes.Length, 0); //从客户端接受消息
            //    recvStr = Encoding.UTF8.GetString(recvBytes, 0, bytes);
            //    if ("" != recvStr)
            //    {
            //        SendMessage(client, recvStr);
            //        SetText(recvStr);
            //    }
            //}
        }

        /// <summary>
        /// user 向 target 发送message
        /// </summary>
        /// <param name="user">发送消息人</param>
        /// <param name="target">接收消息内容</param>
        /// <param name="message">消息</param>
        public void SendMessageToTarget(string user, string target, string message)
        {
            SendOneMessage(userSocketDict[target], user + ":" + message);
        }

        /// <summary>
        /// 【转发】客户端信息
        /// </summary>
        /// <param name="userandmess">用户名:消息内容</param>
        public void SendMessageToTarget(string userandmess)
        {
            string[] u = userandmess.Split('@');
            string[] s = u[1].Split(':');
            string sender = u[0];
            string veceiver = s[0];
            string message = s[1];
            SendMessageToTarget(sender, veceiver, message);
        }

        /// <summary>
        /// 判断是用户注册还是发送消息
        /// </summary>
        /// <param name="p_strMessage"></param>
        public void SendMessage(Socket client,string p_strMessage)
        {
            if (p_strMessage.StartsWith("@"))
            {
                RegeistUser(p_strMessage, client);
            }
            else if (p_strMessage.StartsWith(">"))
            {
               
                DeleteClident(p_strMessage);
            }
            else
            {
                //SendMessageToTarget(p_strMessage);
                SendAllMessage(p_strMessage);
            }
        }

        /// <summary>
        /// 从客户端字典中移除客户端
        /// </summary>
        /// <param name="p_strMessage"></param>
        public void DeleteClident(string p_strMessage)
        {
            p_strMessage = p_strMessage.Remove(0, 1);
            userSocketDict.Remove(p_strMessage);
            RefreshClient();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (newsock != null)
            {
                newsock.Shutdown(SocketShutdown.Both);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //给Client端返回信息
            string sendStr = txt_message.Text;
            SendAllMessage(sendStr);
            rich_back.Text += "\n 我：" + sendStr;
            txt_message.Text = "";
        }

        /// <summary>
        /// 群发消息
        /// </summary>
        /// <param name="p_strsend"></param>
        public void SendAllMessage(string p_strsend)
        {
            //MessageBox.Show(p_strsend);
            foreach (string item in userSocketDict.Keys)
            {
                byte[] bs = Encoding.UTF8.GetBytes(p_strsend);
                userSocketDict[item].Send(bs, bs.Length, 0);  
            }
        }

        /// <summary>
        /// 将消息内容发给客户端
        /// </summary>
        /// <param name="p_strsend"></param>
        public void SendOneMessage(Socket so,string p_strsend)
        {
            byte[] bs = Encoding.UTF8.GetBytes(p_strsend);
            so.Send(bs, bs.Length, 0);  //返回信息给客户端
        }


        private void dgv_client_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            //ss = sl[e.RowIndex];
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (newsock != null)
            {
                newsock.Close();
            }
            if (acceptClientThread!=null)
            {
                acceptClientThread.Abort();
            }
        }

    }
}
