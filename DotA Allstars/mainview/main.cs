using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using TechLifeForum;
using System.IO.Compression;
using System.Xml;
using System.Reflection;
using DotA_Allstars.mainview;
using System.Threading;
using System.Text;
using System.Security.Cryptography;

namespace DotA_Allstars
{

    public partial class main : Form
    {
        IrcClient client;
        public main()
        {
            InitializeComponent();
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this.SuspendLayout();
            this.ResumeLayout(true);
            this.ResizeBegin += (s, e) => { this.SuspendLayout(); };
            this.ResizeEnd += (s, e) => { this.ResumeLayout(true); };
            ver.Text = "ver: " + Assembly.GetExecutingAssembly().GetName().Version.ToString();
            GetRooms();
            settingG();
        }
        public int port = 6667;
        string ip = "irc.pvpgn.mobavietnam.com";
        string ServerPass = "b3APQdYe6ePwhc8X";
        public static string name;
        Dictionary<string, string> rooms = new Dictionary<string, string>();
        private const int cGrip = 16;
        private const int cCaption = 32;
        public static string serverj;
        public static string idroom;
        public string crew;
        Color color = Color.White;
        OpenFileDialog opf = new OpenFileDialog();
        XmlDocument paket = new XmlDocument();
        public static String responseData = String.Empty;
        public static string[] dataRequest = responseData.Split(' ');
        public static bool lsSvExist = false;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x84)
            {
                Point pos = new Point(m.LParam.ToInt32());
                pos = this.PointToClient(pos);
                if (pos.Y < cCaption)
                {
                    m.Result = (IntPtr)2;
                    return;
                }
                if (pos.X >= this.ClientSize.Width - cGrip && pos.Y >= this.ClientSize.Height - cGrip)
                {
                    m.Result = (IntPtr)17;
                    return;
                }
            }
            base.WndProc(ref m);
        }

        public void GetRooms()
        {
            JToken token = JObject.Parse("{\"rooms\":" + Properties.Resources.list_room + "}");
            JArray items = (JArray)token["rooms"];
            int length = items.Count;

            for (int i = 0; i < length; i++)
            {
                string namer = (string)token.SelectToken("rooms[" + i + "].name");
                string idr = (string)token.SelectToken("rooms[" + i + "].networkID");
                rooms.Add(namer, idr);
            }
            foreach (var pair in rooms)
            {
                listRooms.Items.Add(pair.Key);
            }
            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Blizzard Entertainment\\Warcraft III", true))
                {
                    if (key != null)
                    {
                        Object o = key.GetValue("Battle.net Gateways");
                        if (o != null)
                        {
                            lsSvExist = true;
                            List<string> lsServer = new List<string>();
                            lsServer.AddRange((string[])key.GetValue("Battle.net Gateways"));
                            for (int i = 0; i < lsServer.Count; i++)
                            {
                                if (lsServer[i] == "pvpgn.mobavietnam.com")
                                    goto Endloop;
                            }
                            lsServer.AddRange(new string[] { "pvpgn.mobavietnam.com", "7", "mobavietnam.com" });
                            lsServer[1] = (((lsServer.Count - 2) / 3) - 1).ToString();
                            key.SetValue("Battle.net Gateways", lsServer.ToArray());
                            using (Microsoft.Win32.RegistryKey ubn = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Blizzard Entertainment\\Warcraft III\\String", true))
                            {
                                if (ubn != null)
                                {
                                    Object u = ubn.GetValue("userbnet");
                                    if (u != null)
                                    {
                                        ubn.SetValue("userbnet", name.Trim());
                                    }
                                }
                            }
                        Endloop:
                            {
                                using (Microsoft.Win32.RegistryKey ubn = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Blizzard Entertainment\\Warcraft III\\String", true))
                                {
                                    if (ubn != null)
                                    {
                                        Object u = ubn.GetValue("userbnet");
                                        if (u != null)
                                        {
                                            ubn.SetValue("userbnet", name.Trim());
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {

            }
            try
            {
                using (WebClient wcp = new WebClient())
                {
                    wcp.DownloadProgressChanged += wc_DownloadProgressChangedP;
                    wcp.DownloadFileAsync(
                    new Uri("http://103.137.184.98/TFTVersion126a/TFTVersion1.26a.new.zip"),
                                Path.GetDirectoryName(pathwar3.Text) + "\\TFTVersion1.26a.new.zip");
                }
            }
            catch
            {

            }

        }

        private void ListRooms_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            int index = this.listRooms.IndexFromPoint(e.Location);
            if (index != ListBox.NoMatches)
            {
                listRooms.Enabled = false;
                idroom = rooms[listRooms.Items[index].ToString().Substring(0)];
                crew = "#" + idroom;
                serverj = "join " + rooms[listRooms.Items[index].ToString().Substring(0)];
                Success();
            }
        }

        private void ClBt_Click(object sender, EventArgs e)
        {
            Environment.Exit(1);
        }

        private void MmmBt_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void MxmBt_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                this.WindowState = FormWindowState.Maximized;
            }
            else
            {
                this.WindowState = FormWindowState.Normal;
            }
        }

        private void Main_Load(object sender, EventArgs e)
        {
            this.MaximizedBounds = Screen.FromHandle(this.Handle).WorkingArea;
            this.WindowState = FormWindowState.Maximized;

        }

        public void Success()
        {
            bgrroom.Visible = false;
            DoConnect();
        }

        private void DoConnect()
        {
            client = new IrcClient(ip.Trim(), port, false, ServerPass);
            AddEvents();
            client.Nick = name.Trim();
            rtbOutput.Clear(); // in case they reconnect and have old stuff there
            rtbOutput.Text = Properties.Resources.wellcome;
            client.Connect();
            if (login.noticeO == "0")
            {
                notice nf = new notice();
                nf.ShowDialog();
            }
        }
        private void DoDisconnect()
        {
            lstUsers.Items.Clear();
            txtSend.Enabled = false;
            client.Disconnect();
            client = null;
        }

        private void AddEvents()
        {
            client.ChannelMessage += client_ChannelMessage;
            client.ExceptionThrown += client_ExceptionThrown;
            client.NoticeMessage += client_NoticeMessage;
            client.OnConnect += client_OnConnect;
            client.PrivateMessage += client_PrivateMessage;
            client.ServerMessage += client_ServerMessage;
            client.UpdateUsers += client_UpdateUsers;
            client.UserJoined += client_UserJoined;
            client.UserLeft += client_UserLeft;
            client.UserNickChange += client_UserNickChange;
        }

        private void TxtSend_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                if (client.Connected && !String.IsNullOrEmpty(txtSend.Text.Trim()))
                {
                    if (crew.StartsWith("#"))
                        client.SendMessage(crew.Trim(), txtSend.Text.Trim());
                    else
                        client.SendMessage("#" + crew.Trim(), txtSend.Text.Trim());

                    AddToChatWindow(name + ": " + txtSend.Text.Trim());
                    txtSend.Clear();
                    txtSend.Focus();
                }
            }
        }

        private void AddToChatWindow(string message)
        {
            rtbOutput.AppendText(message + "\n");
            HighlightPhrase(rtbOutput, "MobazDota1", Color.Red);
            HighlightPhrase(rtbOutput, "MobazDota2", Color.Red);
            HighlightPhrase(rtbOutput, "MobazDota3", Color.Red);
            HighlightPhrase(rtbOutput, "MobazDota4", Color.Red);
            HighlightPhrase(rtbOutput, "MobazDota5", Color.Red);
            HighlightPhrase(rtbOutput, "MobazDota6", Color.Red);
            HighlightPhrase(rtbOutput, "MobazDota7", Color.Red);
            HighlightPhrase(rtbOutput, "MobazDota8", Color.Red);
            HighlightPhrase(rtbOutput, "MobazBotX", Color.Red);
            HighlightPhrase(rtbOutput, "MobazImbaLod1", Color.Red);
            HighlightPhrase(rtbOutput, "MobazImbaLod2", Color.Red);
            HighlightPhrase(rtbOutput, "MobazImbaLod3", Color.Red);
            HighlightPhrase(rtbOutput, "MobazImbaLod4", Color.Red);
            HighlightPhrase(rtbOutput, "MobazImbaLod5", Color.Red);
            HighlightPhrase(rtbOutput, "MobazImbaLod6", Color.Red);
            HighlightPhrase(rtbOutput, "MobazImbaLod7", Color.Red);
            HighlightPhrase(rtbOutput, "MobazImbaLod8", Color.Red);
            HighlightPhrase(rtbOutput, "MobazDDay1", Color.Red);
            HighlightPhrase(rtbOutput, "MobazDDay2", Color.Red);
            HighlightPhrase(rtbOutput, "MobazDDay3", Color.Red);
            HighlightPhrase(rtbOutput, "MobazDDay4", Color.Red);
            HighlightPhrase(rtbOutput, "MobazDDay5", Color.Red);
            HighlightPhrase(rtbOutput, "MobazDDay6", Color.Red);
            HighlightPhrase(rtbOutput, "MobazDDay7", Color.Red);
            HighlightPhrase(rtbOutput, "MobazDDay8", Color.Red);
            HighlightPhrase(rtbOutput, "MobazDivine1", Color.Red);
            HighlightPhrase(rtbOutput, "MobazDivine2", Color.Red);
            HighlightPhrase(rtbOutput, "MobazDivine3", Color.Red);
            HighlightPhrase(rtbOutput, "MobazDivine4", Color.Red);
            HighlightPhrase(rtbOutput, "MobazDivine5", Color.Red);
            HighlightPhrase(rtbOutput, "MobazDivine6", Color.Red);
            HighlightPhrase(rtbOutput, "MobazDivine7", Color.Red);
            HighlightPhrase(rtbOutput, "MobazDivine8", Color.Red);
            HighlightPhrase(rtbOutput, "MobazGreenTD1", Color.Red);
            HighlightPhrase(rtbOutput, "MobazGreenTD2", Color.Red);
            HighlightPhrase(rtbOutput, "MobazGreenTD3", Color.Red);
            HighlightPhrase(rtbOutput, "MobazGreenTD4", Color.Red);
            HighlightPhrase(rtbOutput, "MobazGreenTD5", Color.Red);
            HighlightPhrase(rtbOutput, "MobazGreenTD6", Color.Red);
            HighlightPhrase(rtbOutput, "MobazGreenTD7", Color.Red);
            HighlightPhrase(rtbOutput, "MobazGreenTD8", Color.Red);
            HighlightPhrase(rtbOutput, "MobazTongHop1", Color.Red);
            HighlightPhrase(rtbOutput, "MobazTongHop2", Color.Red);
            HighlightPhrase(rtbOutput, "MobazTongHop3", Color.Red);
            HighlightPhrase(rtbOutput, "MobazTongHop4", Color.Red);
            HighlightPhrase(rtbOutput, "MobazTongHop5", Color.Red);
            HighlightPhrase(rtbOutput, "MobazTongHop6", Color.Red);
            HighlightPhrase(rtbOutput, "MobazTongHop7", Color.Red);
            HighlightPhrase(rtbOutput, "MobazTongHop8", Color.Red);
            HighlightPhrase(rtbOutput, "style8xmirana", Color.Red);
            if (rtbOutput.BackColor == Color.Cornsilk)
            {
                HighlightPhrase(rtbOutput, "Dota.v6.83d", Color.Blue);
                HighlightPhrase(rtbOutput, "Dota.v6.90a8", Color.Blue);
                HighlightPhrase(rtbOutput, "LoD.v6.87d6", Color.Blue);
                HighlightPhrase(rtbOutput, "LoD.v6.85n3", Color.Blue);
                HighlightPhrase(rtbOutput, "LoD.v6.74c", Color.Blue);
                HighlightPhrase(rtbOutput, "0/10", Color.Blue);
                HighlightPhrase(rtbOutput, "1/10", Color.Blue);
                HighlightPhrase(rtbOutput, "2/10", Color.Blue);
                HighlightPhrase(rtbOutput, "3/10", Color.Blue);
                HighlightPhrase(rtbOutput, "4/10", Color.Blue);
                HighlightPhrase(rtbOutput, "5/10", Color.Blue);
                HighlightPhrase(rtbOutput, "6/10", Color.Blue);
                HighlightPhrase(rtbOutput, "7/10", Color.Blue);
                HighlightPhrase(rtbOutput, "8/10", Color.Blue);
                HighlightPhrase(rtbOutput, "9/10", Color.Blue);
                HighlightPhrase(rtbOutput, "10/10", Color.Blue);
                HighlightPhrase(rtbOutput, "0/12", Color.Blue);
                HighlightPhrase(rtbOutput, "1/12", Color.Blue);
                HighlightPhrase(rtbOutput, "2/12", Color.Blue);
                HighlightPhrase(rtbOutput, "3/12", Color.Blue);
                HighlightPhrase(rtbOutput, "4/12", Color.Blue);
                HighlightPhrase(rtbOutput, "5/12", Color.Blue);
                HighlightPhrase(rtbOutput, "6/12", Color.Blue);
                HighlightPhrase(rtbOutput, "7/12", Color.Blue);
                HighlightPhrase(rtbOutput, "8/12", Color.Blue);
                HighlightPhrase(rtbOutput, "9/12", Color.Blue);
                HighlightPhrase(rtbOutput, "10/12", Color.Blue);
                HighlightPhrase(rtbOutput, "11/12", Color.Blue);
                HighlightPhrase(rtbOutput, "12/12", Color.Blue);
            }   
            else
            {
                HighlightPhrase(rtbOutput, "Dota.v6.83d", Color.Lime);
                HighlightPhrase(rtbOutput, "Dota.v6.90a8", Color.Lime);
                HighlightPhrase(rtbOutput, "LoD.v6.87d6", Color.Lime);
                HighlightPhrase(rtbOutput, "LoD.v6.85n3", Color.Lime);
                HighlightPhrase(rtbOutput, "LoD.v6.74c", Color.Lime);
                HighlightPhrase(rtbOutput, "0/10", Color.Lime);
                HighlightPhrase(rtbOutput, "1/10", Color.Lime);
                HighlightPhrase(rtbOutput, "2/10", Color.Lime);
                HighlightPhrase(rtbOutput, "3/10", Color.Lime);
                HighlightPhrase(rtbOutput, "4/10", Color.Lime);
                HighlightPhrase(rtbOutput, "5/10", Color.Lime);
                HighlightPhrase(rtbOutput, "6/10", Color.Lime);
                HighlightPhrase(rtbOutput, "7/10", Color.Lime);
                HighlightPhrase(rtbOutput, "8/10", Color.Lime);
                HighlightPhrase(rtbOutput, "9/10", Color.Lime);
                HighlightPhrase(rtbOutput, "10/10", Color.Lime);
                HighlightPhrase(rtbOutput, "0/12", Color.Lime);
                HighlightPhrase(rtbOutput, "1/12", Color.Lime);
                HighlightPhrase(rtbOutput, "2/12", Color.Lime);
                HighlightPhrase(rtbOutput, "3/12", Color.Lime);
                HighlightPhrase(rtbOutput, "4/12", Color.Lime);
                HighlightPhrase(rtbOutput, "5/12", Color.Lime);
                HighlightPhrase(rtbOutput, "6/12", Color.Lime);
                HighlightPhrase(rtbOutput, "7/12", Color.Lime);
                HighlightPhrase(rtbOutput, "8/12", Color.Lime);
                HighlightPhrase(rtbOutput, "9/12", Color.Lime);
                HighlightPhrase(rtbOutput, "10/12", Color.Lime);
                HighlightPhrase(rtbOutput, "11/12", Color.Lime);
                HighlightPhrase(rtbOutput, "12/12", Color.Lime);
            }   
                
            rtbOutput.ScrollToCaret();
        }

        static void HighlightPhrase(RichTextBox box, string phrase, Color color)
        {
            int pos = box.SelectionStart;
            string s = box.Text;
            for (int ix = 0; ;)
            {
                int jx = s.IndexOf(phrase, ix, StringComparison.CurrentCultureIgnoreCase);
                if (jx < 0) break;
                box.SelectionStart = jx;
                box.SelectionLength = phrase.Length;
                box.SelectionColor = color;
                ix = jx + 1;
            }
            box.SelectionStart = pos;
            box.SelectionLength = 0;
        }

        #region Event Listeners

        void client_OnConnect(object sender, EventArgs e)
        {
            rtbOutput.Enabled = true;
            rtbOutput.Visible = true;
            txtSend.Enabled = true;
            txtSend.Visible = true;
            lstUsers.Enabled = true;
            lstUsers.Visible = true;
            searchPing.Visible = true;
            btnSetting.Enabled = true;
            btnSetting.Visible = true;
            btnStart.Enabled = true;
            btnStart.Visible = true;
            CnRoom();
            if (crew.StartsWith("#"))
                client.JoinChannel(crew.Trim());
            else
                client.JoinChannel("#" + crew.Trim());

        }

        void client_UserNickChange(object sender, UserNickChangedEventArgs e)
        {
            lstUsers.Items[lstUsers.Items.IndexOf(e.Old)] = e.New;
        }

        void client_UserLeft(object sender, UserLeftEventArgs e)
        {
            lstUsers.Items.Remove(e.User);
        }

        void client_UserJoined(object sender, UserJoinedEventArgs e)
        {
            lstUsers.Items.Add(e.User);
        }

        void client_UpdateUsers(object sender, UpdateUsersEventArgs e)
        {
            lstUsers.Items.Clear();
            lstUsers.Items.AddRange(e.UserList);
        }

        void client_ServerMessage(object sender, StringEventArgs e)
        {
            Console.WriteLine(e.Result);
        }

        void client_PrivateMessage(object sender, PrivateMessageEventArgs e)
        {
            AddToChatWindow("PM FROM " + e.From + ": " + e.Message);
        }

        void client_NoticeMessage(object sender, NoticeMessageEventArgs e)
        {
            AddToChatWindow("NOTICE FROM " + e.From + ": " + e.Message);
        }

        void client_ExceptionThrown(object sender, ExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.Message);
        }

        void client_ChannelMessage(object sender, ChannelMessageEventArgs e)
        {
            AddToChatWindow(e.From + ": " + e.Message);
        }
        #endregion

        private void PictureBox2_Click(object sender, EventArgs e)
        {
            if (settingP.Visible == true)
            {
                settingP.Visible = false;
                settingP.Enabled = false;
            }
            else
            {
                settingP.Visible = true;
                settingP.Enabled = true;
            }
        }

        private void BtnBrower_Click(object sender, EventArgs e)
        {
            opf.Filter = "War3.exe |war3.exe";
            if (opf.ShowDialog() == DialogResult.OK)
            {
                if (!string.IsNullOrEmpty(opf.FileName))
                {
                    pathwar3.Text = Path.Combine(Path.GetDirectoryName(opf.FileName), opf.FileName);
                    pathwar3.Enabled = false;
                    btnBrower.Enabled = false;
                    btnStart.Enabled = false;
                    btnSave.Enabled = false;
                    using (WebClient wcp = new WebClient())
                    {
                        wcp.DownloadProgressChanged += wc_DownloadProgressChangedP;
                        wcp.DownloadFileAsync(
                        new Uri("http://103.137.184.98/TFTVersion126a/TFTVersion1.26a.new.zip"),
                                Path.GetDirectoryName(pathwar3.Text) + "\\TFTVersion1.26a.new.zip");                      
                    }

                    /*var gameVer = FileVersionInfo.GetVersionInfo(pathwar3.Text);
                    if (gameVer.FileVersion != "1, 26, 0, 6401" || !File.Exists(Path.GetDirectoryName(pathwar3.Text) + "\\w3l.exe") || !File.Exists(Path.GetDirectoryName(pathwar3.Text) + "\\w3lh.dll") || !File.Exists(Path.GetDirectoryName(pathwar3.Text) + "\\wl27.dll"))
                    {
                       
                    }
                    /*try
                    {
                        if (!File.Exists(System.IO.Path.GetDirectoryName(pathwar3.Text) + "\\Maps\\DotA-6.83d-MobaZ-v1.0.w3x"))
                        {
                            mapName.ForeColor = Color.Red;
                        }
                        else
                        {
                            mapName.ForeColor = Color.Green;
                        }
                    }
                    catch (ArgumentException)
                    {
                        mapName.ForeColor = Color.Red;
                    }*/
                }
            }
        }

        void wc_DownloadProgressChangedP(object sender, DownloadProgressChangedEventArgs e)
        {
            sttDl.Text = "1.26a new downloading..." + e.ProgressPercentage + "%";
            if (e.ProgressPercentage == 100)
            {
                sttDl.Text = "Giản nén...";
                doneDownloadP();
            }
        }

        public async void doneDownloadP()
        {
            await Task.Delay(2000);
            {               
                sttDl.Text = "Done!";
                await Task.Delay(2000);
                sttDl.Text = "";
                pathwar3.Enabled = true;
                btnBrower.Enabled = true;
                btnStart.Enabled = true;
                btnSave.Enabled = true;
            }


        }
        public void settingG()
        {
            pathwar3.Text = login.path;
            if (login.tagetW == "1")
                window.Checked = true;
            else
                window.Checked = false;
            mapList.SelectedIndex = 0;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            paket.Load("paket.xml");
            XmlNode save = paket.SelectSingleNode("settings/war3");
            XmlNode taget = paket.SelectSingleNode("settings/taget");
            save.Attributes[0].Value = pathwar3.Text;
            if (window.Checked == true)
                taget.Attributes[0].Value = "1";
            else
                taget.Attributes[0].Value = "0";

            paket.Save("paket.xml");
            sttSV.Visible = true;
            settingP.Visible = false;
            settingP.Enabled = false;
            TheEnclosingMethod();
        }

        public async void TheEnclosingMethod()
        {
            await Task.Delay(300);
            sttSV.Text = "";
            settingP.Visible = false;
        }

        private void MapName_Click(object sender, EventArgs e)
        {
            if (File.Exists(pathwar3.Text))
            {
                using (WebClient wc = new WebClient())
                {
                    wc.DownloadProgressChanged += wc_DownloadProgressChanged;
                    wc.DownloadFileAsync(
                        new System.Uri("http://103.137.184.98/dota.zip"),
                        Path.GetDirectoryName(pathwar3.Text) + "\\Allmap.zip"
                    );
                }
            }
            else
            {
                btnBrower.PerformClick();
            }
        }
        void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            sttDl.Text = "Map downloading..." + e.ProgressPercentage + "%";
            if (e.ProgressPercentage == 100)
            {
                sttDl.Text = "Giải nén...";
                doneDownload();
            }
        }

        public async void doneDownload()
        {
            await Task.Delay(2000);
            try
            {              
                sttDl.Text = "Done!";
                await Task.Delay(2000);
                sttDl.Text = "";
            }
            catch
            {

            }

        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            try
            {
                if (lsSvExist != true)
                {
                    try
                    {
                        using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Blizzard Entertainment\\Warcraft III", true))
                        {
                            if (key != null)
                            {
                                Object o = key.GetValue("Battle.net Gateways");
                                if (o != null)
                                {
                                    lsSvExist = true;
                                    List<string> lsServer = new List<string>();
                                    lsServer.AddRange((string[])key.GetValue("Battle.net Gateways"));
                                    for (int i = 0; i < lsServer.Count; i++)
                                    {
                                        if (lsServer[i] == "pvpgn.mobavietnam.com")
                                            goto Endloop;
                                    }
                                    lsServer.AddRange(new string[] { "pvpgn.mobavietnam.com", "7", "mobavietnam.com" });
                                    lsServer[1] = (((lsServer.Count - 2) / 3) - 1).ToString();
                                    key.SetValue("Battle.net Gateways", lsServer.ToArray());
                                    using (Microsoft.Win32.RegistryKey ubn = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Blizzard Entertainment\\Warcraft III\\String", true))
                                    {
                                        if (ubn != null)
                                        {
                                            Object u = ubn.GetValue("userbnet");
                                            if (u != null)
                                            {
                                                ubn.SetValue("userbnet", name.Trim());
                                            }
                                        }
                                    }
                                Endloop:
                                    {
                                        using (Microsoft.Win32.RegistryKey ubn = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software\\Blizzard Entertainment\\Warcraft III\\String", true))
                                        {
                                            if (ubn != null)
                                            {
                                                Object u = ubn.GetValue("userbnet");
                                                if (u != null)
                                                {
                                                    ubn.SetValue("userbnet", name.Trim());
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {

                    }
                }
                var gameVer = FileVersionInfo.GetVersionInfo(pathwar3.Text);
                if (gameVer.FileVersion != "1, 26, 0, 6401" || !File.Exists(Path.GetDirectoryName(pathwar3.Text) + "\\w3l.exe"))
                {
                    MessageBox.Show("Lỗi phiên bản, mời cập nhập lên 1.26a new", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    Process RunGame = new Process();
                    RunGame.StartInfo.UseShellExecute = false;
                    RunGame.StartInfo.WorkingDirectory = Path.GetDirectoryName(pathwar3.Text);
                    RunGame.StartInfo.FileName = Path.GetDirectoryName(pathwar3.Text) + "\\w3l.exe";
                    if (window.Checked == true)
                        RunGame.StartInfo.Arguments = string.Concat(" -window");
                    RunGame.Start();

                    this.WindowState = FormWindowState.Minimized;
                }
            }
            catch
            {
                MessageBox.Show("Sai đường dẫn đến war3.exe hoặc trống!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void LeaveN()
        {
            Invoke((MethodInvoker)delegate
            {
                bgrroom.Visible = true;
                rtbOutput.Enabled = false;
                rtbOutput.Visible = false;
                txtSend.Enabled = false;
                txtSend.Visible = false;
                lstUsers.Enabled = false;
                lstUsers.Visible = false;
                searchPing.Visible = false;
                btnSetting.Enabled = false;
                btnSetting.Visible = false;
                btnStart.Enabled = false;
                btnStart.Visible = false;
                DcnRoom();
                DoDisconnect();
                listRooms.Enabled = true;
                rooms.Clear();
                listRooms.Items.Clear();
                GetRooms();
            });
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            foreach (var process in Process.GetProcessesByName("war3"))
            {
                if (process.ProcessName == "War3" || process.ProcessName == "war3")
                {
                    MessageBox.Show("Warcraft III đang chạy!", "Cảnh báo!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    goto DontRun;
                }
                else
                {
                    goto EndLoop;
                }
            }
        EndLoop:
            {
                if (btnHost.Enabled == false)
                {
                    MessageBox.Show("Bạn đã tạo một host, vui lòng hủy trước khi thoát room!", "Cảnh báo!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    LeaveN();
                }
            }
        DontRun:;
        }

        private void rtbOutput_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            Process.Start(e.LinkText);
        }

        private void btnClr_Click(object sender, EventArgs e)
        {
            rtbOutput.Clear();
        }

        public void CnRoom()
        {
            Invoke((MethodInvoker)delegate
            {
                button1.Visible = true;
                label1.Visible = true;
                label5.Visible = true;
                mapList.Visible = true;
                btnHost.Visible = true;
                btnCCHost.Visible = true;
                btnClr.Visible = true;
                huongdanlabel.Visible = true;
                huongdan1linklabel.Visible = true;
                huongdan2linklabel.Visible = true;
                huongdan3linklabel.Visible = true;
                huongdan4linklabel.Visible = true;
                huongdan5linklabel.Visible = true;
                huongdan6linklabel.Visible = true;
                listRooms.Visible = false;
                danhsachphonglabel.Visible = false;
                obscheckbox.Visible = true;
                obslabel.Visible = true;
                battlenetkovaodclabel.Visible = true;
                battlesaipasslabel.Visible = true;
                gotiengvietlabel.Visible = true;
                roomP.Visible = true;
                btndoimau.Visible = true;
                addadminbtn.Visible = true;
                hotrotructuyen.Visible = true;
                logocall.Visible = true;
                logomessage.Visible = true;
                modegametxtbox.Visible = true;
                modegamelabel.Visible = true;

            });
        }
        public void DcnRoom()
        {
            button1.Visible = false;
            label1.Visible = false;
            label5.Visible = false;
            mapList.Visible = false;
            btnHost.Visible = false;
            btnCCHost.Visible = false;
            btnClr.Visible = false;
            huongdanlabel.Visible = false;
            huongdan1linklabel.Visible = false;
            huongdan2linklabel.Visible = false;
            huongdan3linklabel.Visible = false;
            huongdan4linklabel.Visible = false;
            huongdan5linklabel.Visible = false;
            huongdan6linklabel.Visible = false;
            listRooms.Visible = true;
            danhsachphonglabel.Visible = true;
            obslabel.Visible = false;
            obscheckbox.Visible = false;
            battlenetkovaodclabel.Visible = false;
            battlesaipasslabel.Visible = false;
            gotiengvietlabel.Visible = false;
            roomP.Visible = false;
            btndoimau.Visible = false;
            addadminbtn.Visible = false;
            hotrotructuyen.Visible = false;
            logocall.Visible = false;
            logomessage.Visible = false;
            modegametxtbox.Visible = false;
            modegamelabel.Visible = false;
        }

        private void btnHost_Click(object sender, EventArgs e)
        {
            timer.Interval = 1000;
            timer.Tick += timer_Tick;
            timer.Start();
            btnHost.Enabled = false;

            timer1.Interval = 1000;
            timer1.Tick += timer1_Tick;
            timer1.Start();
            btnCCHost.Enabled = false;

            string loadmap = string.Empty;
            string typemap = string.Empty;

            switch (mapList.SelectedIndex)
            {
                case 0:
                    loadmap = "dota683dmobaz";
                    typemap = "Dota.v6.83d";
                    break;
                case 1:
                    loadmap = "dotargc";
                    typemap = "Dota.v6.90a8";
                    break;
                case 2:
                    loadmap = "dotalodrgc";
                    typemap = "LoD.v6.87d6";
                    break;
                case 3:
                    loadmap = "lod685";
                    typemap = "LoD.v6.85n3";
                    break;
                case 4:
                    loadmap = "lod674c";
                    typemap = "LoD.v6.74c";
                    break;
                case 5:
                    loadmap = "imba26en";
                    typemap = "Imba.v2.6";
                    break;
                case 6:
                    loadmap = "imba2018v4en";
                    typemap = "Imba.v4";
                    break;
                case 7:
                    loadmap = "legend99";
                    typemap = "Legend.v99.9";
                    break;
                case 8:
                    loadmap = "legiontd41x20";
                    typemap = "Legion.4.1";
                    break;
                case 9:
                    loadmap = "dday199b";
                    typemap = "DDay.19.9b";
                    break;
                case 10:
                    loadmap = "divide120q";
                    typemap = "Divine.1.20";
                    break;
                case 11:
                    loadmap = "tonghop49";
                    typemap = "TongHop.V49";
                    break;
                case 12:
                    loadmap = "kiemthien8";
                    typemap = "ThienKiem.8";
                    break;
                case 13:
                    loadmap = "warlock102";
                    typemap = "Warlock.1.02";
                    break;
                case 14:
                    loadmap = "pokemonfinal";
                    typemap = "PoKeMon";
                    break;
                case 15:
                    loadmap = "xhero345";
                    typemap = "Xhero.3.45";
                    break;
                case 16:
                    loadmap = "greentdpro215";
                    typemap = "Green.21.5";
                    break;
                case 17:
                    loadmap = "greentdhpny60";
                    typemap = "GreenHPNY.v6.0";
                    break;
                case 18:
                    loadmap = "greentdhpny59";
                    typemap = "GreenHPNY.v5.9";
                    break;
                case 19:
                    loadmap = "greentdhpny42";
                    typemap = "GreenHPNY.v4.2";
                    break;
                case 20:
                    loadmap = "greentdhpnyfinal";
                    typemap = "GreenHPNY.Final";
                    break;
                case 21:
                    loadmap = "greentd31";
                    typemap = "GreenTD.3.1";
                    break;
                case 22:
                    loadmap = "greentd21";
                    typemap = "GreenTD.2.1";
                    break;
                case 23:
                    loadmap = "greenvkl";
                    typemap = "GreenVKL";
                    break;
                case 24:
                    loadmap = "greencricle1";
                    typemap = "GreenCricle.EP1";
                    break;
                case 25:
                    loadmap = "greencricle62";
                    typemap = "GreenCricle.6.2";
                    break;
                case 26:
                    loadmap = "greencricle19";
                    typemap = "GreenCricle.19.0";
                    break;
                case 27:
                    loadmap = "greencricle16";
                    typemap = "GreenCricle.16.7";
                    break;
                case 28:
                    loadmap = "greencricle12";
                    typemap = "GreenCricle.12.1";
                    break;
                case 29:
                    loadmap = "greencriclemega16";
                    typemap = "GreenCricleMG.16.7";
                    break;
                case 30:
                    loadmap = "greencriclemega11";
                    typemap = "GreenCricleMG.11";
                    break;
                case 31:
                    loadmap = "greencriclemega109";
                    typemap = "GreenCricleMG.10.9";
                    break;
                case 32:
                    loadmap = "greencriclemega106";
                    typemap = "GreenCricleMG.10.6";
                    break;
                case 33:
                    loadmap = "greencriclemega96";
                    typemap = "GreenCricleMG.9.6";
                    break;
                case 34:
                    loadmap = "greencricleahmad74";
                    typemap = "GreenCricleAh.74";
                    break;
                case 35:
                    loadmap = "greencricle10";
                    typemap = "GreenCricle.10";
                    break;
                case 36:
                    loadmap = "greencricleanime9";
                    typemap = "GreenAnimeEx.9";
                    break;
                case 37:
                    loadmap = "greencricle18";
                    typemap = "GreenCricle.18.5";
                    break;
                case 38:
                    loadmap = "greencricle235";
                    typemap = "GreenCricleTD.2.3.50";
                    break;
                case 39:
                    loadmap = "greencricle627";
                    typemap = "GreenCricleTD.6.2.7";
                    break;
                case 40:
                    loadmap = "greencricle2f18";
                    typemap = "GreenCricleTD.2f1.8";
                    break;
                case 41:
                    loadmap = "greentd99";
                    typemap = "GreenCricle.9.9";
                    break;
            }

            AddToChatWindow("MobaZBotX: [AURA] Creating game [" + name.Trim() + "-" + typemap + modegametxtbox.Text.Trim() + "] is setting");
            Invoke((MethodInvoker)delegate
        {
            if (client.Connected)
            {
                {
                    if (obscheckbox.Checked == true)
                    {
                        if (crew.StartsWith("#"))
                        {
                            client.SendMessage(crew.Trim(), "!load " + loadmap);
                            client.SendMessage(crew.Trim(), "@load " + loadmap);
                            client.SendMessage(crew.Trim(), ">load " + loadmap);
                            client.SendMessage(crew.Trim(), "$load " + loadmap);
                            client.SendMessage(crew.Trim(), "%load " + loadmap);
                            client.SendMessage(crew.Trim(), "^load " + loadmap);
                            client.SendMessage(crew.Trim(), "&load " + loadmap);
                            client.SendMessage(crew.Trim(), "*load " + loadmap);
                        }

                        else
                        {
                            client.SendMessage("#" + crew.Trim(), "!load " + loadmap);
                            client.SendMessage("#" + crew.Trim(), "@load " + loadmap);
                            client.SendMessage("#" + crew.Trim(), ">load " + loadmap);
                            client.SendMessage("#" + crew.Trim(), "$load " + loadmap);
                            client.SendMessage("#" + crew.Trim(), "%load " + loadmap);
                            client.SendMessage("#" + crew.Trim(), "^load " + loadmap);
                            client.SendMessage("#" + crew.Trim(), "&load " + loadmap);
                            client.SendMessage("#" + crew.Trim(), "*load " + loadmap);
                        }
                    }


                    else
                    {
                        if (crew.StartsWith("#"))
                        {
                            client.SendMessage(crew.Trim(), "!map " + loadmap);
                            client.SendMessage(crew.Trim(), "@map " + loadmap);
                            client.SendMessage(crew.Trim(), ">map " + loadmap);
                            client.SendMessage(crew.Trim(), "$map " + loadmap);
                            client.SendMessage(crew.Trim(), "%map " + loadmap);
                            client.SendMessage(crew.Trim(), "^map " + loadmap);
                            client.SendMessage(crew.Trim(), "&map " + loadmap);
                            client.SendMessage(crew.Trim(), "*map " + loadmap);
                        }


                        else
                        {
                            client.SendMessage("#" + crew.Trim(), "!map " + loadmap);
                            client.SendMessage("#" + crew.Trim(), "@map " + loadmap);
                            client.SendMessage("#" + crew.Trim(), ">map " + loadmap);
                            client.SendMessage("#" + crew.Trim(), "$map " + loadmap);
                            client.SendMessage("#" + crew.Trim(), "%map " + loadmap);
                            client.SendMessage("#" + crew.Trim(), "^map " + loadmap);
                            client.SendMessage("#" + crew.Trim(), "&map " + loadmap);
                            client.SendMessage("#" + crew.Trim(), "*map " + loadmap);
                        }
                    }


                }


                //AddToChatWindow(name + ": " + zone + loadmap);
            }
            if (client.Connected)
            {
                if (crew.StartsWith("#"))
                {
                    client.SendMessage(crew.Trim(), "!pub " + name.Trim() + "-" + typemap + modegametxtbox.Text.Trim());
                    client.SendMessage(crew.Trim(), "%pub " + name.Trim() + "-" + typemap + modegametxtbox.Text.Trim());
                    client.SendMessage(crew.Trim(), ">pub " + name.Trim() + "-" + typemap + modegametxtbox.Text.Trim());
                    client.SendMessage(crew.Trim(), "&pub " + name.Trim() + "-" + typemap + modegametxtbox.Text.Trim());
                    client.SendMessage(crew.Trim(), "@pub " + name.Trim() + "-" + typemap + modegametxtbox.Text.Trim());                 
                    client.SendMessage(crew.Trim(), "$pub " + name.Trim() + "-" + typemap + modegametxtbox.Text.Trim());                   
                    client.SendMessage(crew.Trim(), "^pub " + name.Trim() + "-" + typemap + modegametxtbox.Text.Trim());              
                    client.SendMessage(crew.Trim(), "*pub " + name.Trim() + "-" + typemap + modegametxtbox.Text.Trim());
                }

                else
                {
                    client.SendMessage("#" + crew.Trim(), "!pub " + name.Trim() + "-" + typemap + modegametxtbox.Text.Trim());
                    client.SendMessage("#" + crew.Trim(), ">pub " + name.Trim() + "-" + typemap + modegametxtbox.Text.Trim());
                    client.SendMessage("#" + crew.Trim(), "%pub " + name.Trim() + "-" + typemap + modegametxtbox.Text.Trim());
                    client.SendMessage("#" + crew.Trim(), "&pub " + name.Trim() + "-" + typemap + modegametxtbox.Text.Trim());
                    client.SendMessage("#" + crew.Trim(), "@pub " + name.Trim() + "-" + typemap + modegametxtbox.Text.Trim());                  
                    client.SendMessage("#" + crew.Trim(), "$pub " + name.Trim() + "-" + typemap + modegametxtbox.Text.Trim());
                    client.SendMessage("#" + crew.Trim(), "^pub " + name.Trim() + "-" + typemap + modegametxtbox.Text.Trim());
                    client.SendMessage("#" + crew.Trim(), "*pub " + name.Trim() + "-" + typemap + modegametxtbox.Text.Trim());
                }



                //AddToChatWindow(name + ": " + zone + "pub " + name.Trim() + mode);
            }
            //if (client.Connected)
            //{
            //    if (crew.StartsWith("#"))
            //    {
            //        client.SendMessage(crew.Trim(), "!getgames");
            //        client.SendMessage(crew.Trim(), "@getgames");
            //        client.SendMessage(crew.Trim(), ">getgames");
            //        client.SendMessage(crew.Trim(), "$getgames");
            //        client.SendMessage(crew.Trim(), "%getgames");
            //        client.SendMessage(crew.Trim(), "^getgames");
            //        client.SendMessage(crew.Trim(), "&getgames");
            //        client.SendMessage(crew.Trim(), "*getgames");
            //    }

            //    else
            //    {
            //        client.SendMessage("#" + crew.Trim(), "!getgames");
            //        client.SendMessage("#" + crew.Trim(), "@getgames");
            //        client.SendMessage("#" + crew.Trim(), ">getgames");
            //        client.SendMessage("#" + crew.Trim(), "$getgames");
            //        client.SendMessage("#" + crew.Trim(), "%getgames");
            //        client.SendMessage("#" + crew.Trim(), "^getgames");
            //        client.SendMessage("#" + crew.Trim(), "&getgames");
            //        client.SendMessage("#" + crew.Trim(), "*getgames");
            //        client.SendMessage("#" + crew.Trim(), "!getgames");
            //    }

            //        //AddToChatWindow(name + ": " + zone[i] + "getgames");
            //    }

        });

        }

        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        int countdown = 60;
        System.Windows.Forms.Timer timer1 = new System.Windows.Forms.Timer();
        int countdown1 = 15;

        private void btnCCHost_Click(object sender, EventArgs e)
        {
            timer1.Interval = 1000;
            timer1.Tick += timer1_Tick;
            timer1.Start();
            btnCCHost.Enabled = false;

            if (client.Connected)
            {
                if (crew.StartsWith("#"))
                {
                    client.SendMessage(crew.Trim(), "!getgames");
                    client.SendMessage(crew.Trim(), "@getgames");
                    client.SendMessage(crew.Trim(), ">getgames");
                    client.SendMessage(crew.Trim(), "$getgames");
                    client.SendMessage(crew.Trim(), "%getgames");
                    client.SendMessage(crew.Trim(), "^getgames");
                    client.SendMessage(crew.Trim(), "&getgames");
                    client.SendMessage(crew.Trim(), "*getgames");
                }

                else
                {
                    client.SendMessage("#" + crew.Trim(), "!getgames");
                    client.SendMessage("#" + crew.Trim(), "@getgames");
                    client.SendMessage("#" + crew.Trim(), ">getgames");
                    client.SendMessage("#" + crew.Trim(), "$getgames");
                    client.SendMessage("#" + crew.Trim(), "%getgames");
                    client.SendMessage("#" + crew.Trim(), "^getgames");
                    client.SendMessage("#" + crew.Trim(), "&getgames");
                    client.SendMessage("#" + crew.Trim(), "*getgames");
                    client.SendMessage("#" + crew.Trim(), "!getgames");
                }
            }
        }

        void timer1_Tick(object sender, System.EventArgs e)
        {
            if (--countdown1 <= 0)
            {
                btnCCHost.Enabled = true;
                btnCCHost.Text = "Xem host";
                timer1.Stop();
                timer1.Tick -= timer1_Tick;
                countdown1 = 15;
            }
            else
            {
                Invoke((MethodInvoker)delegate
                {
                    btnCCHost.Text = string.Format("Xem host ({0})", countdown1);
                });
                //(string.Format("Remaining: {0}s", countdown1));
            }
        }

        void timer_Tick(object sender, System.EventArgs e)
        {
            if (--countdown <= 0)
            {
                btnHost.Enabled = true;
                btnHost.Text = "Tạo host";
                timer.Stop();
                timer.Tick -= timer_Tick;
                countdown = 60;
            }
            else
            {
                Invoke((MethodInvoker)delegate
                {
                    btnHost.Text = string.Format("Tạo host ({0})", countdown);
                });
                //(string.Format("Remaining: {0}s", countdown));
            }
        }

        private void mapLOD_Click(object sender, EventArgs e)
        {
            if (File.Exists(pathwar3.Text))
            {
                using (WebClient wc = new WebClient())
                {
                    wc.DownloadProgressChanged += wc_DownloadProgressChanged;
                    wc.DownloadFileAsync(
                        new System.Uri("http://103.137.184.98/lodimba.zip"),
                        Path.GetDirectoryName(pathwar3.Text) + "\\Allmap.zip"
                    );
                }
            }
            else
            {
                btnBrower.PerformClick();
            }
        }

        private void mapDDay_Click(object sender, EventArgs e)
        {
            if (File.Exists(pathwar3.Text))
            {
                using (WebClient wc = new WebClient())
                {
                    wc.DownloadProgressChanged += wc_DownloadProgressChanged;
                    wc.DownloadFileAsync(
                        new System.Uri("http://103.137.184.98/ddaydivine.zip"),
                        Path.GetDirectoryName(pathwar3.Text) + "\\Allmap.zip"
                    );
                }
            }
            else
            {
                btnBrower.PerformClick();
            }
        }

        private void mapGreen_Click(object sender, EventArgs e)
        {
            if (File.Exists(pathwar3.Text))
            {
                using (WebClient wc = new WebClient())
                {
                    wc.DownloadProgressChanged += wc_DownloadProgressChanged;
                    wc.DownloadFileAsync(
                        new System.Uri("http://103.137.184.98/greentd.zip"),
                        Path.GetDirectoryName(pathwar3.Text) + "\\Allmap.zip"
                    );
                }
            }
            else
            {
                btnBrower.PerformClick();
            }
        }

        private void mapTonghop_Click(object sender, EventArgs e)
        {
            if (File.Exists(pathwar3.Text))
            {
                using (WebClient wc = new WebClient())
                {
                    wc.DownloadProgressChanged += wc_DownloadProgressChanged;
                    wc.DownloadFileAsync(
                        new System.Uri("http://103.137.184.98/tonghop.zip"),
                        Path.GetDirectoryName(pathwar3.Text) + "\\Allmap.zip"
                    );
                }
            }
            else
            {
                btnBrower.PerformClick();
            }
        }

        private void huongdan1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://mobavietnam.com/index.php/huong-dan-choi-warcraft-3-tren-mobaz/");
        }

        private void huongdan2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://mobavietnam.com/index.php/huong-dan-tao-host-bang-bot-server/");
        }

        private void huongdan3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://mobavietnam.com/index.php/huong-dan-tu-tao-host-tren-may-tinh-ca-nhan/");
        }

        private void huongdan4_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://mobavietnam.com/index.php/cac-lenh-co-ban-cua-bot-trong-game/");
        }

        private void huongdan5linklabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://mobavietnam.com/index.php/huong-dan-cai-warcraft-iii/");
        }

        private void huongdan6linklabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://mobavietnam.com/index.php/huong-dan-dung-warkey-phim-tat-warcraft-iii/");
        }

        private void mainP_Paint(object sender, PaintEventArgs e)
        {

        }

        private void gotiengvietlabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://mobavietnam.com/index.php/go-tieng-viet-trong-mobaz-client/");
        }

        private void battlesaipasslabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://mobavietnam.com/index.php/battlenet-bao-sai-mat-khau/");
        }

        private void battlenetkovaodclabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://mobavietnam.com/index.php/khong-vao-duoc-battlenet/");
        }

        private void btndoimau_Click(object sender, EventArgs e)
        {

            if (rtbOutput.BackColor == Color.Black)
            {
                rtbOutput.BackColor = Color.Cornsilk;
                rtbOutput.ForeColor = Color.Black;
                lstUsers.BackColor = Color.Cornsilk;
                lstUsers.ForeColor = Color.Black;
                
            }
            else
            {
                rtbOutput.BackColor = Color.Black;
                rtbOutput.ForeColor = Color.LightBlue;
                lstUsers.BackColor = Color.Black;
                lstUsers.ForeColor = Color.LightBlue;
                
            }

        }


        private void addadminbtn_Click(object sender, EventArgs e)
        {
            if (name.Trim() == "style8xmirana")
            {
                if (client.Connected && !String.IsNullOrEmpty(searchPing.Text.Trim()))
                {
                    if (crew.StartsWith("#"))
                        client.SendMessage(crew.Trim(), searchPing.Text.Trim());
                    else
                        client.SendMessage("#" + crew.Trim(), searchPing.Text.Trim());

                    AddToChatWindow(name + ": !addadmin " + searchPing.Text.Trim());
                    AddToChatWindow(name + ": @addadmin " + searchPing.Text.Trim());
                    AddToChatWindow(name + ": >addadmin " + searchPing.Text.Trim());
                    AddToChatWindow(name + ": $addadmin " + searchPing.Text.Trim());
                    AddToChatWindow(name + ": %addadmin " + searchPing.Text.Trim());
                    AddToChatWindow(name + ": ^addadmin " + searchPing.Text.Trim());
                    AddToChatWindow(name + ": &addadmin " + searchPing.Text.Trim());
                    AddToChatWindow(name + ": *addadmin " + searchPing.Text.Trim());
                    txtSend.Clear();
                    txtSend.Focus();
                }
            }
            else
                addadminbtn.Enabled = false;
        }

        private void logocall_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Vui lòng alo cho 8x qua SĐT: 079.544.86.79", "Hỗ trợ trực tuyến");
        }

        private void logomessage_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "Ấn Help và gửi tin nhắn cho 8x qua Facebook",
                "Hỗ trợ trực tiếp",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information,
                MessageBoxDefaultButton.Button1,
                0,
                "https://www.facebook.com/huynguyen.style8x",
                "keyword");
        }
    }
}




