﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace Server
{
    public partial class Server : Form
    {
        public Server()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;

            Connect();

        }
        //md5
        MD5 md5 = new MD5();
        //AES-256     
        Aes_EcbEncrypt aes = new Aes_EcbEncrypt();
        //IP 
        IPEndPoint IP;
        Socket client;
        //Socket(lỗ kết nối )
        Socket server;
        byte[] khoapublic;
        byte[] khoabimat;      
        //Danh sách lưu trữ tất cả client kết nối
        List<Socket> clientList;
        int sec = 20;
        // public key, secret key
        string keypublic;
        string keysecret;
        string keyclient;
        DiffieHellman diff;
        byte[] nhankey;
        byte[] data;
        byte[] tinnhan;
        byte[] nhankeydadoi;
        //Datime Iv
        string dateTimeIV;
        byte[] dateTimeIv;



        //chuoi so sanh
        string compare1, compare2;
        /// <summary>
        /// Ket noi toi server
        /// </summary>
        void Connect()
        {
            clientList = new List<Socket>();
            //IP của server (127.0.0.1)
            IP = new IPEndPoint(IPAddress.Any, 9999);
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //đợi ip
            server.Bind(IP);

            Thread listen = new Thread(() => {
                try
                {
                    while (true)
                    {
                        server.Listen(100);
                        client = server.Accept();                       
                        clientList.Add(client);
                        // lấy độ dài khóa public của server
                        byte[] laydodai = BitConverter.GetBytes(khoapublic.Length);
                        // gửi độ dài khóa public cho client
                        client.Send(laydodai);                       
                        // lấy khóa public
                        byte[] Keypublic = khoapublic;
                        // gửi khóa public cho client
                        client.Send(Keypublic);                     
                        // Nhận độ dài của khóa public từ client
                        byte[] nhandodai = new byte[1024];
                        client.Receive(nhandodai);
                        // Nhận khóa public của client
                        nhankey = new byte[BitConverter.ToInt32(nhandodai, 0)];
                        client.Receive(nhankey);
                        // add vào textbox
                        keyclient = Convert.ToBase64String(nhankey);
                        txtkeyclient.Text = keyclient;
                        //Lấy khóa public từ client để tạo khóa chung
                        diff.LayKhoaBiMat(nhankey);
                        khoabimat = diff.aes.Key;
                        keysecret = Convert.ToBase64String(khoabimat);
                        txbSecretkey.Text = keysecret;
                        Thread receive = new Thread(Receive);                        
                        receive.IsBackground = true;
                        receive.Start(client);                       
                    }
                }
                catch
                {
                  
                }
            });
            TaoKey();
            listen.IsBackground = true;
            listen.Start();
        }

        void TaoKey()
        {
            diff = new DiffieHellman();
            khoapublic = diff.PublicKey;
            keypublic = Convert.ToBase64String(khoapublic);
            txtKey.Text = keypublic;           
        }

        void guilaipublickey()
        {
            byte[] batdauguikey = Encoding.UTF8.GetBytes("guikey");
            client.Send(batdauguikey);                   
            byte[] Keypublic = khoapublic;
            client.Send(Keypublic);            
        }

        void guiLaiKeyChoClientVuaGui()
        {
            byte[] batdauguikey = Encoding.UTF8.GetBytes("guikeytoclient");
            client.Send(batdauguikey);
            byte[] Keypublic = khoapublic;
            client.Send(Keypublic);
        }   
        /// <summary>
        /// Close ket noi hien tai
        /// </summary>
        

        /// <summary>
        /// Send Message
        /// </summary>
        void Send(Socket client)
        {
            dateTimeIV = md5.maHoaMd5(DateTime.Now.ToString());
            string time = dateTimeIV.Substring(0, 16);
            dateTimeIv = Encoding.UTF8.GetBytes(time);
            string a = txbSecretkey.Text.Substring(0, 32);
            byte[] key = Encoding.ASCII.GetBytes(a);
            string s = aes.EncryptString(txtMessage.Text,key, dateTimeIv);

            byte[] mahoa = diff.MaHoaDiffie(nhankey, s);
            byte[] dodai = BitConverter.GetBytes(mahoa.Length);
            byte[] initvector = diff.IV;            
            if (client != null && txtMessage.Text != string.Empty)
            {               
                client.Send(dodai);
                client.Send(mahoa);
                client.Send(initvector);              
            }
           
        }

        /// <summary>
        /// Send By using AES-256
        /// </summary>
        /// <param name="client"></param>
        void AddMessage(string s)
        {
            txtViewMessage.Items.Add(new ListViewItem() { Text = "Người lạ: " + s });
        }

        void Messagefromself(string s)
        {
            txtViewMessage.Items.Add(new ListViewItem() { Text = "Tôi: " + s });
        }
        void MessageHeTHong(string s)
        {
            txtViewMessage.Items.Add(new ListViewItem() { Text = "Hệ Thống : " + s });
        }
        /// <summary>
        /// Receive Message
        /// </summary>
        void Receive(object obj)
        {
            Socket client = obj as Socket;
            try
            {
                c: while (true)//luôn luôn nhận
                {
                    data = new byte[1024];
                    client.Receive(data);
                    if (string.Equals(Encoding.UTF8.GetString(data), "guikeyserver", StringComparison.InvariantCultureIgnoreCase))
                    {                       
                        nhankeydadoi = new byte[140];
                        client.Receive(nhankeydadoi);
                        keypublic = Convert.ToBase64String(nhankeydadoi);
                        txtkeyclient.Text = keypublic;                        
                        diff.LayKhoaBiMat(nhankeydadoi);
                        khoabimat = diff.aes.Key;
                        keysecret = Convert.ToBase64String(khoabimat);
                        txbSecretkey.Text = keysecret;
                        nhankey = nhankeydadoi;                        
                    }
                    else if (string.Equals(Encoding.UTF8.GetString(data), "guikeytoserver", StringComparison.InvariantCultureIgnoreCase))
                    {                       
                        nhankeydadoi = new byte[140];
                        client.Receive(nhankeydadoi);                 
                        keypublic = Convert.ToBase64String(nhankeydadoi);
                        txtkeyclient.Text = keypublic;
                        TaoKey();
                        diff.LayKhoaBiMat(nhankeydadoi);
                        khoabimat = diff.aes.Key;
                        keysecret = Convert.ToBase64String(khoabimat);
                        txbSecretkey.Text = keysecret;
                        nhankey = nhankeydadoi;
                        guiLaiKeyChoClientVuaGui();
                    }
                    else
                    {
                        tinnhan = new byte[BitConverter.ToInt32(data, 0)];
                        client.Receive(tinnhan);
                        byte[] nhanvector = new byte[16];
                        client.Receive(nhanvector);
                        string message = diff.GiaiMaDiffie(nhankey, tinnhan, nhanvector);

                        dateTimeIV = md5.maHoaMd5(DateTime.Now.ToString());
                        string time = dateTimeIV.Substring(0, 16);
                        dateTimeIv = Encoding.UTF8.GetBytes(time);
                        string a = txbSecretkey.Text.Substring(0, 32);
                        byte[] key = Encoding.ASCII.GetBytes(a);
                        string s = aes.DecryptString(message, key, dateTimeIv);
                        foreach (char c in s)
                        {
                            if (c.ToString() == ";")
                            {
                                string[] tokens =s.Trim().Split(';');
                                compare1 = tokens[1];
                                compare2 = md5.maHoaMd5(tokens[0]);
                                if (compare1 != compare2)
                                {
                                    AddMessage(tokens[0]);
                                    MessageHeTHong("chuỗi này đã bị thay đổi vì 2 chuỗi mã hóa khác nhau:");
                                    MessageHeTHong("Chuỗi mã hóa trước khi gửi :" + compare1);
                                    MessageHeTHong("chuỗi mã hóa sau khi mã hóa tin nhận :" + compare2);
                                    goto c;
                                }

                            }

                        }
                        AddMessage(s);
                    }                   
                }
            }

            catch
            {
                clientList.Remove(client);
                client.Close();
            }

        }

        /// <summary>
        /// phân mảnh
        /// </summary>
        byte[] Serialize(object obj)
        {
            MemoryStream stream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();

            formatter.Serialize(stream, obj);
            return stream.ToArray();
        }

        /// <summary>
        /// gom mảnh
        /// </summary>
        /// <returns></returns>
        object Deserialize(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            BinaryFormatter formatter = new BinaryFormatter();
            return formatter.Deserialize(stream);
        }

        private void txtViewMessage_KeyUp(object sender, KeyEventArgs e)
        {
            if (sender != txtViewMessage) return;

            if (e.Control && e.KeyCode == Keys.C)
                CopySelectedValuesToClipboard();
        }
        private void CopySelectedValuesToClipboard()
        {
            var builder = new StringBuilder();
            foreach (ListViewItem item in txtViewMessage.SelectedItems)
                builder.AppendLine(item.SubItems[0].Text);
            Clipboard.SetText(builder.ToString());
        }

        private void Server_FormClosed(object sender, FormClosedEventArgs e)
        {
            server.Close();
        }
        
        private void btnSend_Click(object sender, EventArgs e)
        {
            timer1.Stop();
            foreach (Socket item in clientList)
            {
                Send(item);
            }
            Messagefromself(txtMessage.Text);
            txtMessage.Clear();
            sec = 21;
            timer1.Start();
            
        }
                
        private void timer1_Tick(object sender, EventArgs e)
        {            
            lbltime.Visible = true;
            lbltime.Text = sec.ToString();
            if (sec < 10)
            {
                lbltime.Text = "0" + sec.ToString();
            }
            if (sec <= 0)
            {
                timer1.Stop();
                txtkeyclient.Clear();
                txbSecretkey.Clear();
                TaoKey();
                guilaipublickey();
                laptime();
            }
            sec--;                        
        }

        private void laptime()
        {
            sec = 21;
            timer1 = new System.Windows.Forms.Timer();
            timer1.Tick += new EventHandler(timer1_Tick);
            timer1.Interval = 1000;
            timer1.Start();       
        }
        // Chèn chữ vô vị trí trong string
        private static string RandomString(string baseString, string character, int position)
        {
            var sb = new StringBuilder(baseString);

            sb.Insert(position, character);

            return sb.ToString();
        }

        private void Server_Load(object sender, EventArgs e)
        {
            TimerSession.Start();
              

        }

        private void TimerSession_Tick(object sender, EventArgs e)
        {
            dateTimeIV = md5.maHoaMd5(DateTime.Now.ToString());
            string time = dateTimeIV.Substring(0, 16);
            dateTimeIv = Encoding.UTF8.GetBytes(time);
            txtInitVector.Text = Encoding.UTF8.GetString(dateTimeIv);
        }

        private void btnSendNoise_Click(object sender, EventArgs e)
        {
            dateTimeIV = md5.maHoaMd5(DateTime.Now.ToString());
            Messagefromself(txtMessage.Text);
            //tạo chuỗi mã hóa tin nhắn ban đầu + hash time
            string s = md5.maHoaMd5(txtMessage.Text);
            dateTimeIV = md5.maHoaMd5(DateTime.Now.ToString());
            string Mahoa_time = s + ";" + dateTimeIV;

            //tạo các chữ random để chèn vào
            char[] chars = "abcdefghijklmnopqrstuvwxyz1234567890".ToCharArray();
            Random r = new Random();
            int i = r.Next(chars.Length);
            int vitri = r.Next(0, txtMessage.TextLength);

            //Chuỗi Đã thay đổi
            txtMessage.Text = RandomString(txtMessage.Text, chars[i].ToString(), vitri) + ";" + Mahoa_time;
            foreach (Socket item in clientList)
            {
                Send(item);
            }
        }
    }
}
