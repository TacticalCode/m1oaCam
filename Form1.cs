using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Threading;
using System.Security.Cryptography;

namespace m1oaCam
{
    public partial class Form1 : Form
    {
        public System.Windows.Forms.Timer t = new System.Windows.Forms.Timer();
        public Uri download = new Uri(@"http://webcams.kevag-telekom.de/webcam4.jpg");
        public string savepath = String.Empty;
        private bool start = false;
        private string lastfile = String.Empty;
        private bool checkdup = false;
        private bool savefiles = false;
        private bool otherurl = false;

        public Form1()
        {
            InitializeComponent();
            t.Tick += new EventHandler(t_Tick);
            savefiles = checkBox2.Checked;
            checkdup = checkBox1.Checked;
            textBox1.Enabled = checkBox2.Checked;
            button1.Enabled = checkBox2.Checked;
            checkBox1.Enabled = checkBox2.Checked;
            textBox2.Enabled = checkBox3.Checked;
            numericUpDown1.Value = (decimal)12;
        }

        //Speicherpfad suchen
        private void button1_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.ShowDialog(this);
            textBox1.Text = folderBrowserDialog1.SelectedPath;
        }

        //Start
        private void button2_Click(object sender, EventArgs e)
        {
            start = !start;
            if (start)
            {
                if (savefiles && !Directory.Exists(textBox1.Text))
                {
                    MessageBox.Show("Bitte einen Ort zum Speichern der Bilder angeben.");
                    return;
                }

                if (savefiles && !textBox1.Text.EndsWith("\\"))  //Auf abschließenden Backslash prüfen
                    textBox1.Text += "\\";          //und ggf. anhängen

                if (otherurl && !String.IsNullOrEmpty(textBox2.Text))
                    download = new Uri(textBox2.Text);

                button2.Text = "STOP";
                textBox1.Enabled = false;                                   //Speicherpfadauswahl deaktivieren
                button1.Enabled = false;                                    //Speicherpfad-Dialog deaktivieren
                numericUpDown1.Enabled = false;                             //Intervallauswahl deaktivieren
                checkBox1.Enabled = false;                                  //DoppelteLöschen check deaktivieren
                t.Interval = Convert.ToInt32(numericUpDown1.Value) * 1000;  //Interval festlegen
                checkdup = checkBox1.Checked;                               //DoppelteLöschen festlegen

                if (savefiles)
                    savepath = textBox1.Text;                               //Speicherpfad festlegen
                else
                {
                    savepath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\m1oaCAM\\";
                    if (!Directory.Exists(savepath))
                        Directory.CreateDirectory(savepath);
                }

                t.Start();                                                  //Timer Starten
                download_picture();                                         //Erstes Bild sofort laden
            }
            else
            {
                button2.Text = "START";
                t.Stop();                               //Timer Stoppen
                textBox1.Enabled = checkBox2.Checked;   //Speicherpfadauswahl aktivieren
                button1.Enabled = checkBox2.Checked;    //Speicherpfad-Dialog aktivieren
                numericUpDown1.Enabled = true;          //Intervallauswahl aktivieren
                checkBox1.Enabled = checkBox2.Checked;  //DoppelteLöschen check aktivieren
            }
        }

        //Download Trigger
        void t_Tick(object sender, EventArgs e)
        {
            if (start)
                download_picture();
        }

        //Download
        void download_picture()
        {
            if (start)
            {
                try
                {
                    WebClient wc = new WebClient();
                    string now = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
                    string filename = savepath + now + ".jpg";
                    wc.DownloadFileCompleted += new AsyncCompletedEventHandler(wc_DownloadFileCompleted);
                    DownloadState dlst = new DownloadState() { FILENAME = filename, CHECKDUPLICATE = checkdup, SAVE = savefiles };
                    if (File.Exists(lastfile))
                        dlst.LASTFILE = lastfile;

                    wc.DownloadFileAsync(download, filename, dlst);
                }
                catch (Exception ex)
                {
                    label2.Text = ex.Message;
                }
            }
        }

        //File-Check etc.
        void wc_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (start)
            {
                DownloadState dl = e.UserState as DownloadState;
                if (!dl.SAVE)
                {
                    try
                    {
                        PicBoxUpdate(dl.FILENAME);
                        if (File.Exists(dl.LASTFILE))
                            File.Delete(dl.LASTFILE);
                        lastfile = dl.FILENAME;
                    }
                    catch (Exception ex)
                    {
                        LabelUpdate(ex.Message);
                    }

                    return;
                }

                bool same = false;
                try
                {
                    if (dl.CHECKDUPLICATE && File.Exists(dl.FILENAME) && File.Exists(dl.LASTFILE))
                    {
                        FileStream fsnew = File.OpenRead(dl.FILENAME);
                        FileStream fsold = File.OpenRead(dl.LASTFILE);
                        if (fsnew.Length == fsold.Length)
                        {
                            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
                            byte[] newhash = md5.ComputeHash(fsnew);
                            byte[] oldhash = md5.ComputeHash(fsold);
                            md5.Clear();
                            if (Encoding.Default.GetString(newhash) == Encoding.Default.GetString(oldhash)) 
                                same = true;
                        }
                        fsnew.Close();
                        fsold.Close();
                    }

                    if (same)
                    {
                        File.Delete(dl.FILENAME);
                        LabelUpdate(dl.FILENAME + " wurde gelöscht");
                    }
                    else
                    {
                        LabelUpdate(dl.FILENAME + " wurde gespeichert.");
                        PicBoxUpdate(dl.FILENAME);
                        lastfile = dl.FILENAME;
                    }
                }
                catch (Exception ex)
                {
                    LabelUpdate(ex.Message);
                }   
            }
        }

        //Status-Label Updaten
        private void LabelUpdate(string text)
        {
            if (start)
                label2.Text = text;
        }

        //PictureBox Updaten
        private void PicBoxUpdate(string path)
        {
            if (start)
            {
                try
                {
                    pictureBox1.ImageLocation = path;
                    pictureBox1.Update();
                }
                catch (Exception ex)
                {
                    label2.Text = ex.Message;
                }
            }
        }

        //Bilderspeicherung toggle
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            textBox1.Enabled = checkBox2.Checked;
            button1.Enabled = checkBox2.Checked;
            savefiles = checkBox2.Checked;
            checkBox1.Enabled = checkBox2.Checked;
        }

        //Fenster verstecken
        private void button3_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            notifyIcon1.Visible = true;
            notifyIcon1.ShowBalloonTip(5000);
        }
        //Fenster wieder anzeigen
        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            open();
        }
        private void notifyIcon1_Click(object sender, EventArgs e)
        {
            open();
        }
        void open()
        {
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
        }

        //Andere URL togglen
        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox3.Checked)
            {
                textBox2.Enabled = true;
                textBox2.Text = "";
                otherurl = true;
            }
            else
            {
                textBox2.Enabled = false;
                textBox2.Text = "ACHTUNG! Nur ändern, wenn du weißt, was du da tust!";
                otherurl = false;
                download = new Uri(@"http://webcams.kevag-telekom.de/webcam4.jpg");
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            About abt = new About();
            abt.ShowDialog(this);
        }
    }

    class DownloadState
    {
        public string FILENAME;
        public string LASTFILE;
        public bool SAVE;
        public bool CHECKDUPLICATE;
    }
}
