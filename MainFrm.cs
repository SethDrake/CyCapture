using System;
using System.Drawing;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Windows.Forms;
using CaptureDevice;

namespace CyCapture
{

    public class MainFrm : System.Windows.Forms.Form
    {
        private Device captureDevice;

        private TextBox txtDeviceName;
        private Label lblVer;
        private TextBox txtVersion;
        private TextBox txtData;
        private Label lblFreq;
        private TextBox txtPortValue;
        private Button btnSetOutputPort;
        private Button btnGetStatus;
        private PictureBox canvas;

        public MainFrm()
        {
            // Required for Windows Form Designer support
            InitializeComponent();

            StartBtn.Enabled = false;

            captureDevice = new Device();
            captureDevice.CaptureCompleted += new EventHandler<CaptureEventArgs>(captureDevice_CaptureCompleted);
            captureDevice.DeviceReady += new EventHandler<DeviceReadyEventArgs>(captureDevice_DeviceReady);
            captureDevice.DeviceNotReady += new EventHandler(captureDevice_DeviceNotReady);
            captureDevice.OpenDevice();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }


        #region Windows Form Designer generated code

        private System.Windows.Forms.MainMenu mainMenu;
        private System.Windows.Forms.MenuItem menuItem1;
        private System.Windows.Forms.MenuItem menuItem2;
        private System.Windows.Forms.MenuItem ExitItem;
        private System.Windows.Forms.MenuItem menuItem3;
        private System.Windows.Forms.MenuItem AboutItem;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ProgressBar ProgressBar;
        private System.Windows.Forms.Button StartBtn;
        private System.Windows.Forms.Label ThroughputLabel;
        private Label lblDevices;
        private System.Windows.Forms.Timer PerfTimer;

        private IContainer components;

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.mainMenu = new System.Windows.Forms.MainMenu(this.components);
            this.menuItem1 = new System.Windows.Forms.MenuItem();
            this.menuItem2 = new System.Windows.Forms.MenuItem();
            this.ExitItem = new System.Windows.Forms.MenuItem();
            this.menuItem3 = new System.Windows.Forms.MenuItem();
            this.AboutItem = new System.Windows.Forms.MenuItem();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.ThroughputLabel = new System.Windows.Forms.Label();
            this.ProgressBar = new System.Windows.Forms.ProgressBar();
            this.StartBtn = new System.Windows.Forms.Button();
            this.lblDevices = new System.Windows.Forms.Label();
            this.PerfTimer = new System.Windows.Forms.Timer(this.components);
            this.txtDeviceName = new System.Windows.Forms.TextBox();
            this.lblVer = new System.Windows.Forms.Label();
            this.txtVersion = new System.Windows.Forms.TextBox();
            this.txtData = new System.Windows.Forms.TextBox();
            this.lblFreq = new System.Windows.Forms.Label();
            this.txtPortValue = new System.Windows.Forms.TextBox();
            this.btnSetOutputPort = new System.Windows.Forms.Button();
            this.canvas = new System.Windows.Forms.PictureBox();
            this.btnGetStatus = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.canvas)).BeginInit();
            this.SuspendLayout();
            // 
            // mainMenu
            // 
            this.mainMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItem1,
            this.menuItem3});
            // 
            // menuItem1
            // 
            this.menuItem1.Index = 0;
            this.menuItem1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItem2,
            this.ExitItem});
            this.menuItem1.Text = "File";
            // 
            // menuItem2
            // 
            this.menuItem2.Index = 0;
            this.menuItem2.Text = "-";
            // 
            // ExitItem
            // 
            this.ExitItem.Index = 1;
            this.ExitItem.Text = "Exit";
            this.ExitItem.Click += new System.EventHandler(this.ExitItem_Click);
            // 
            // menuItem3
            // 
            this.menuItem3.Index = 1;
            this.menuItem3.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.AboutItem});
            this.menuItem3.Text = "Help";
            // 
            // AboutItem
            // 
            this.AboutItem.Index = 0;
            this.AboutItem.Text = "About";
            this.AboutItem.Click += new System.EventHandler(this.AboutItem_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.ThroughputLabel);
            this.groupBox1.Controls.Add(this.ProgressBar);
            this.groupBox1.Location = new System.Drawing.Point(21, 122);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(330, 60);
            this.groupBox1.TabIndex = 8;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = " Throughput (KB/s) ";
            // 
            // ThroughputLabel
            // 
            this.ThroughputLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ThroughputLabel.Location = new System.Drawing.Point(16, 38);
            this.ThroughputLabel.Name = "ThroughputLabel";
            this.ThroughputLabel.Size = new System.Drawing.Size(294, 16);
            this.ThroughputLabel.TabIndex = 1;
            this.ThroughputLabel.Text = "0";
            this.ThroughputLabel.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // ProgressBar
            // 
            this.ProgressBar.ForeColor = System.Drawing.SystemColors.HotTrack;
            this.ProgressBar.Location = new System.Drawing.Point(16, 25);
            this.ProgressBar.Maximum = 40000;
            this.ProgressBar.Name = "ProgressBar";
            this.ProgressBar.Size = new System.Drawing.Size(294, 10);
            this.ProgressBar.TabIndex = 0;
            // 
            // StartBtn
            // 
            this.StartBtn.BackColor = System.Drawing.Color.Aquamarine;
            this.StartBtn.Location = new System.Drawing.Point(223, 79);
            this.StartBtn.Name = "StartBtn";
            this.StartBtn.Size = new System.Drawing.Size(128, 28);
            this.StartBtn.TabIndex = 3;
            this.StartBtn.Text = "Start";
            this.StartBtn.UseVisualStyleBackColor = false;
            this.StartBtn.Click += new System.EventHandler(this.StartBtn_Click);
            // 
            // lblDevices
            // 
            this.lblDevices.AutoSize = true;
            this.lblDevices.Location = new System.Drawing.Point(16, 18);
            this.lblDevices.Name = "lblDevices";
            this.lblDevices.Size = new System.Drawing.Size(44, 13);
            this.lblDevices.TabIndex = 11;
            this.lblDevices.Text = "Device:";
            // 
            // txtDeviceName
            // 
            this.txtDeviceName.Location = new System.Drawing.Point(111, 15);
            this.txtDeviceName.Name = "txtDeviceName";
            this.txtDeviceName.ReadOnly = true;
            this.txtDeviceName.Size = new System.Drawing.Size(240, 20);
            this.txtDeviceName.TabIndex = 17;
            // 
            // lblVer
            // 
            this.lblVer.AutoSize = true;
            this.lblVer.Location = new System.Drawing.Point(16, 46);
            this.lblVer.Name = "lblVer";
            this.lblVer.Size = new System.Drawing.Size(45, 13);
            this.lblVer.TabIndex = 18;
            this.lblVer.Text = "Version:";
            // 
            // txtVersion
            // 
            this.txtVersion.Location = new System.Drawing.Point(111, 43);
            this.txtVersion.Name = "txtVersion";
            this.txtVersion.ReadOnly = true;
            this.txtVersion.Size = new System.Drawing.Size(240, 20);
            this.txtVersion.TabIndex = 19;
            // 
            // txtData
            // 
            this.txtData.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtData.Location = new System.Drawing.Point(21, 197);
            this.txtData.Multiline = true;
            this.txtData.Name = "txtData";
            this.txtData.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtData.Size = new System.Drawing.Size(330, 152);
            this.txtData.TabIndex = 20;
            // 
            // lblFreq
            // 
            this.lblFreq.AutoSize = true;
            this.lblFreq.Location = new System.Drawing.Point(18, 87);
            this.lblFreq.Name = "lblFreq";
            this.lblFreq.Size = new System.Drawing.Size(39, 13);
            this.lblFreq.TabIndex = 21;
            this.lblFreq.Text = "FREQ:";
            // 
            // txtPortValue
            // 
            this.txtPortValue.Location = new System.Drawing.Point(70, 83);
            this.txtPortValue.MaxLength = 4;
            this.txtPortValue.Name = "txtPortValue";
            this.txtPortValue.Size = new System.Drawing.Size(35, 20);
            this.txtPortValue.TabIndex = 22;
            this.txtPortValue.Text = "0x00";
            // 
            // btnSetOutputPort
            // 
            this.btnSetOutputPort.Location = new System.Drawing.Point(111, 83);
            this.btnSetOutputPort.Name = "btnSetOutputPort";
            this.btnSetOutputPort.Size = new System.Drawing.Size(42, 20);
            this.btnSetOutputPort.TabIndex = 23;
            this.btnSetOutputPort.Text = "Set";
            this.btnSetOutputPort.UseVisualStyleBackColor = false;
            this.btnSetOutputPort.Click += new System.EventHandler(this.btnSetPortA_Click);
            // 
            // canvas
            // 
            this.canvas.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.canvas.BackColor = System.Drawing.SystemColors.Window;
            this.canvas.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.canvas.Location = new System.Drawing.Point(21, 366);
            this.canvas.Name = "canvas";
            this.canvas.Size = new System.Drawing.Size(330, 183);
            this.canvas.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.canvas.TabIndex = 24;
            this.canvas.TabStop = false;
            // 
            // btnGetStatus
            // 
            this.btnGetStatus.Location = new System.Drawing.Point(159, 83);
            this.btnGetStatus.Name = "btnGetStatus";
            this.btnGetStatus.Size = new System.Drawing.Size(46, 20);
            this.btnGetStatus.TabIndex = 25;
            this.btnGetStatus.Text = "Status";
            this.btnGetStatus.UseVisualStyleBackColor = false;
            this.btnGetStatus.Click += new System.EventHandler(this.btnGetStatus_Click);
            // 
            // MainFrm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(374, 561);
            this.Controls.Add(this.btnGetStatus);
            this.Controls.Add(this.canvas);
            this.Controls.Add(this.btnSetOutputPort);
            this.Controls.Add(this.txtPortValue);
            this.Controls.Add(this.lblFreq);
            this.Controls.Add(this.txtData);
            this.Controls.Add(this.txtVersion);
            this.Controls.Add(this.lblVer);
            this.Controls.Add(this.txtDeviceName);
            this.Controls.Add(this.lblDevices);
            this.Controls.Add(this.StartBtn);
            this.Controls.Add(this.groupBox1);
            this.Menu = this.mainMenu;
            this.Name = "MainFrm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "CyCapture";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainFrm_FormClosing);
            this.Load += new System.EventHandler(this.MainFrm_Load);
            this.groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.canvas)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion



        /*Summary
           The main entry point for the application.
        */
        [STAThread]
        static void Main()
        {
            try
            {
                Application.Run(new MainFrm());
            }
            catch (Exception e)
            {
                MessageBox.Show(e.StackTrace, "Exception '" + e.Message + "' thrown by " + e.Source);
            }
        }


        private void AboutItem_Click(object sender, System.EventArgs e)
        {

        }



        /*Summary
           Executes on clicking File->Exit
        */
        private void ExitItem_Click(object sender, System.EventArgs e)
        {
            Close();
        }

        private void MainFrm_Load(object sender, System.EventArgs e)
        {

        }



        /*Summary
           Executes on clicking close button
        */
        private void MainFrm_FormClosing(object sender, FormClosingEventArgs e)
        {
            captureDevice.CloseDevice();
        }

        

        private void StopProcessing()
        {
            if (captureDevice.IsRunning)
            {
                captureDevice.StopReceiving();
            }
            StartBtn.Text = "Start";
            StartBtn.BackColor = Color.Aquamarine;
        }

        /*Summary
          Executes on Start Button click 
        */
        private void StartBtn_Click(object sender, System.EventArgs e)
        {
            if (!captureDevice.IsReady)
                return;

            if (StartBtn.Text.Equals("Start"))
            {
                txtData.Clear();

                StartBtn.Text = "Stop";
                StartBtn.BackColor = Color.Pink;

                StatusUpdate(false);

                captureDevice.PPX = 1;
                captureDevice.QueueSz = 32;

                if (!captureDevice.StartReceiveData())
                {
                    StopProcessing();
                    MessageBox.Show("Unable to start capture", "Error!");
                }
            }
            else
            {
                StopProcessing();
            }
        }


        void captureDevice_CaptureCompleted(object sender, CaptureEventArgs e)
        {
            StatusUpdate(!captureDevice.ContinousMode);
            if (e.IsError)
            {
                MessageBox.Show("Error!");
            }
        }

        void captureDevice_DeviceReady(object sender, DeviceReadyEventArgs e)
        {
            txtDeviceName.Text = e.DeviceName;
            txtVersion.Text = String.Format("Version: {0}; Revision: {1}", e.Version, e.Revision);
            StartBtn.Enabled = true;
        }

        void captureDevice_DeviceNotReady(object sender, EventArgs e)
        {
            txtDeviceName.Text = null;
            txtVersion.Text = null;
            StartBtn.Enabled = false;
        }

        /*Summary
          The callback routine delegated to updateUI.
        */
        private void StatusUpdate(bool isCompleted)
        {
            if (captureDevice.XferRate > ProgressBar.Maximum)
                ProgressBar.Maximum = (int)(captureDevice.XferRate * 1.25);

            ProgressBar.Value = (int)captureDevice.XferRate;
            ThroughputLabel.Text = String.Format("{0} KB/s;   {1} bytes received.", ProgressBar.Value, captureDevice.ResultLength);

            int drawLength = 1024;
            if (captureDevice.ResultLength < drawLength)
            {
                drawLength = (int)captureDevice.ResultLength;
            }

            if (captureDevice.ResultLength > 0)
            {
                txtData.Text = BitConverter.ToString(captureDevice.ResultBuffer, 0, drawLength);
                //txtData.Text = string.Join(" ", new List<byte>(buf).Select(x => Convert.ToString(x, 2).PadLeft(8, '0')));
            }

            canvas.Image = null;

            //draw function
            if (captureDevice.ResultLength > 0)
            {
                Bitmap bitmap = new Bitmap(drawLength, 256, PixelFormat.Format16bppRgb565);

                //clear bitmap
                Graphics g = Graphics.FromImage(bitmap);
                g.Clear(Color.White);
                g.SmoothingMode = SmoothingMode.HighQuality;

                for (int i = 0; i < drawLength; i++)
                {
                    int h = 255 - captureDevice.ResultBuffer[i];
                    //bitmap.SetPixel(i, h, Color.Black);
                    if (i > 0)
                    {
                        int prevh = 255 - captureDevice.ResultBuffer[i-1];
                        g.DrawLine(new Pen(Color.Black), new Point(i-1, prevh), new Point(i, h));
                    }
                }
                IntPtr hBitmap = bitmap.GetHbitmap();
                canvas.Image = Image.FromHbitmap(hBitmap);
                canvas.SizeMode = PictureBoxSizeMode.StretchImage;
                canvas.Height = bitmap.Height;
            }

            if (isCompleted)
            {
                StartBtn.Text = "Start";
                StartBtn.BackColor = Color.Aquamarine;
            }
        }

        private void btnSetPortA_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(txtPortValue.Text))
            {
                byte val = 0;
                if (byte.TryParse(txtPortValue.Text.Replace("0x", ""), NumberStyles.HexNumber, new NumberFormatInfo(), out val))
                {
                    captureDevice.SetOutputPort(val);
                }
                else
                {
                    txtPortValue.Text = "0x00";
                }
            }
        }

        private void btnGetStatus_Click(object sender, EventArgs e)
        {
            if (!captureDevice.IsReady)
            {
                return;
            }
            Device.status_info_struct? infoStruct = captureDevice.GetStatusInfo();
            if (infoStruct == null)
            {
                MessageBox.Show("Unable to read status!");
            }
            else
            {
                MessageBox.Show(String.Format("state:0x{0:x2} gpif_status:0x{1:x2} ifconfig_value:0x{2:x2} ",
                    infoStruct.Value.state, infoStruct.Value.gpif_status, infoStruct.Value.ifconfig_value));
            }
        }
    }
}
