using System;
using System.Drawing;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;
using CyUSB;

namespace CyCapture
{

    public class MainFrm : System.Windows.Forms.Form
    {
        private const byte CMD_GET_FW_VERSION = 0xB0;
        private const byte CMD_START = 0xB1;
        private const byte CMD_GET_REVID_VERSION = 0xB2;

        private const byte CMD_START_FLAGS_WIDE_POS = 5;
        private const byte CMD_START_FLAGS_CLK_SRC_POS = 6;

        private const byte CMD_START_FLAGS_SAMPLE_8BIT = (0 << CMD_START_FLAGS_WIDE_POS);
        private const byte CMD_START_FLAGS_SAMPLE_16BIT = (1 << CMD_START_FLAGS_WIDE_POS);

        private const byte CMD_START_FLAGS_CLK_30MHZ = (0 << CMD_START_FLAGS_CLK_SRC_POS);
        private const byte CMD_START_FLAGS_CLK_48MHZ = (1 << CMD_START_FLAGS_CLK_SRC_POS);

        private const byte CMD_START_FLAGS_INV_CLK_POS = 0;
        private const byte CMD_START_FLAGS_INV_CLK = (1 << CMD_START_FLAGS_INV_CLK_POS);

        [StructLayout(LayoutKind.Sequential)]
        public struct cmd_start_acquisition_struct
        {
            public byte flags;
            public byte sample_delay_h;
            public byte sample_delay_l;
        }

        private System.Diagnostics.PerformanceCounter CpuCounter;

        USBDeviceList usbDevices;
        CyUSBDevice MyDevice;
        CyUSBEndPoint ControlEndPoint;
        CyUSBEndPoint BulkInEndPoint;

        DateTime t1, t2;
        TimeSpan elapsed;
        double XferBytes;
        long xferRate;

        int BufSz;
        int QueueSz;
        int PPX;
        int IsoPktBlockSize = 0;

        Thread tListen;
        bool bRunning;

        // These are  needed for Thread to update the UI
        delegate void UpdateUICallback(byte[] buf, int len);
        UpdateUICallback updateUI;
        private TextBox txtDeviceName;
        private Label lblVer;
        private TextBox txtVersion;
        private TextBox txtData;

        // These are needed to close the app from the Thread exception(exception handling)
        delegate void ExceptionCallback();
        ExceptionCallback handleException;

        public MainFrm()
        {
            // Required for Windows Form Designer support
            InitializeComponent();

            // Setup the callback routine for updating the UI
            updateUI = new UpdateUICallback(StatusUpdate);

            // Setup the callback routine for NullReference exception handling
            handleException = new ExceptionCallback(ThreadException);

            // Create the list of USB devices attached to the CyUSB.sys driver.
            usbDevices = new USBDeviceList(CyConst.DEVICES_CYUSB);

            LoadFirmware();

            //Assign event handlers for device attachment and device removal.
            usbDevices.DeviceAttached += new EventHandler(usbDevices_DeviceAttached);
            usbDevices.DeviceRemoved += new EventHandler(usbDevices_DeviceRemoved);

            //Set and search the device with VID-PID 04b4-1003 and if found, selects the end point
            SetDevice();
        }


        void usbDevices_DeviceRemoved(object sender, EventArgs e)
        {
            StopProcessing();

            MyDevice = null;
            ControlEndPoint = null;
            BulkInEndPoint = null;
            txtDeviceName.Text = String.Empty;
            txtVersion.Text = String.Empty;
            SetDevice();
        }


        void usbDevices_DeviceAttached(object sender, EventArgs e)
        {
            LoadFirmware();
            SetDevice();
        }

        private void LoadFirmware()
        {
            USBDevice dev = usbDevices[0x04B4, 0x8613];
            if (dev != null)
            {
                var fx2lp = (CyFX2Device) dev;
                bool isOk = fx2lp.LoadRAM("fx2lafw-cypress-fx2.hex");
                if (!isOk)
                {
                    fx2lp.Reset();
                    return;
                }
                Thread.Sleep(6000);
                usbDevices = new USBDeviceList(CyConst.DEVICES_CYUSB);
            }
            else
            {
                usbDevices = new USBDeviceList(CyConst.DEVICES_CYUSB);
                SetDevice();
            }
        }


        private void SetDevice()
        {
            USBDevice dev = usbDevices[46084, 4099];
            if (dev != null)
            {
                MyDevice = (CyUSBDevice)dev;
                ControlEndPoint = null;
                BulkInEndPoint = null;
                txtDeviceName.Text = dev.FriendlyName;

                GetEndpointsOfNode(MyDevice.Tree);
                if (ControlEndPoint != null)
                {
                    StartBtn.Enabled = true;

                    String ver = GetFirmwareVer();
                    String rev = GetRevision();
                    txtVersion.Text = String.Format("Version: {0}  Revision: {1}", ver, rev);
                }
            }
            else
            {
                StartBtn.Enabled = false;
                txtData.Clear();
            }
        }


        private void GetEndpointsOfNode(TreeNode devTree)
        {
            foreach (TreeNode node in devTree.Nodes)
            {
                if (node.Nodes.Count > 0)
                    GetEndpointsOfNode(node);
                else
                {
                    CyUSBEndPoint ept = node.Tag as CyUSBEndPoint;
                    if (ept != null)
                    {
                        if (node.Text.Contains("Control"))
                        {
                            ControlEndPoint = ept;
                        }
                        else
                        {
                            BulkInEndPoint = ept;
                            CyUSBInterface ifc = node.Parent.Tag as CyUSBInterface;
                            if (ifc != null)
                            {
                                MyDevice.AltIntfc = ifc.bAlternateSetting;
                            }
                        }
                    }
                }
            }
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
            this.groupBox1.SuspendLayout();
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
            this.ThroughputLabel.Location = new System.Drawing.Point(114, 38);
            this.ThroughputLabel.Name = "ThroughputLabel";
            this.ThroughputLabel.Size = new System.Drawing.Size(100, 16);
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
            this.txtData.Location = new System.Drawing.Point(21, 197);
            this.txtData.Multiline = true;
            this.txtData.Name = "txtData";
            this.txtData.Size = new System.Drawing.Size(330, 189);
            this.txtData.TabIndex = 20;
            // 
            // MainFrm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(374, 398);
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



        /*Summary
           Executes on clicking Help->about
        */
        private void AboutItem_Click(object sender, System.EventArgs e)
        {
            string assemblyList = Util.Assemblies;
            MessageBox.Show(assemblyList, Text);
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
            if (usbDevices != null)
                usbDevices.Dispose();
        }

        private String GetFirmwareVer()
        {
            if (ControlEndPoint == null)
            {
                return String.Empty;
            }
            var ctrlEp = (CyControlEndPoint)ControlEndPoint;
            ctrlEp.Target = CyConst.TGT_DEVICE;
            ctrlEp.Direction = CyConst.DIR_FROM_DEVICE;
            ctrlEp.ReqType = CyConst.REQ_VENDOR;
            ctrlEp.ReqCode = CMD_GET_FW_VERSION;
            ctrlEp.Value = 0x0000;
            ctrlEp.Index = 0x0000;
            ctrlEp.TimeOut = 1000;

            byte[] buf = new byte[2];
            int len = 2;

            bool isOk = ctrlEp.XferData(ref buf, ref len);
            return isOk ? String.Format("{0}.{1}", buf[0], buf[1]) : String.Empty;
        }

        private String GetRevision()
        {
            if (ControlEndPoint == null)
            {
                return String.Empty;
            }
            var ctrlEp = (CyControlEndPoint)ControlEndPoint;
            ctrlEp.Target = CyConst.TGT_DEVICE;
            ctrlEp.Direction = CyConst.DIR_FROM_DEVICE;
            ctrlEp.ReqType = CyConst.REQ_VENDOR;
            ctrlEp.ReqCode = CMD_GET_REVID_VERSION;
            ctrlEp.Value = 0x0000;
            ctrlEp.Index = 0x0000;
            ctrlEp.TimeOut = 1000;

            byte[] buf = new byte[1];
            int len = 1;

            bool isOk = ctrlEp.XferData(ref buf, ref len);
            return isOk ? String.Format("{0}", buf[0]) : String.Empty;
        }

        private bool StartCapture()
        {
            if (ControlEndPoint == null || BulkInEndPoint == null)
            {
                return false;
            }

            var ctrlEp = (CyControlEndPoint)ControlEndPoint;
            ctrlEp.Target = CyConst.TGT_DEVICE;
            ctrlEp.Direction = CyConst.DIR_TO_DEVICE;
            ctrlEp.ReqType = CyConst.REQ_VENDOR;
            ctrlEp.ReqCode = CMD_START;
            ctrlEp.Value = 0x0000;
            ctrlEp.Index = 0x0000;
            ctrlEp.TimeOut = 1000;

            var initStruct = new cmd_start_acquisition_struct();
            initStruct.flags = CMD_START_FLAGS_SAMPLE_8BIT;// | CMD_START_FLAGS_INV_CLK;
            initStruct.sample_delay_h = 0;
            initStruct.sample_delay_l = 0;

            byte[] buf = new byte[3];
            int len = 3;

            GCHandle h = GCHandle.Alloc(initStruct, GCHandleType.Pinned);
            Marshal.Copy(h.AddrOfPinnedObject(), buf, 0, len);
            h.Free();

            bool isOk = ctrlEp.XferData(ref buf, ref len);
            return isOk;
        }

        private void ConfigureInEp()
        {
            int maxLen = 0x10000; // 64K
            int ppx = maxLen / BulkInEndPoint.MaxPktSize / 8 * 8;
            if (MyDevice.bHighSpeed && (BulkInEndPoint.Attributes == 1) && (ppx < 8))
            {
                ppx = 8;
            }
            BufSz = BulkInEndPoint.MaxPktSize * ppx;
            QueueSz = 1;
            PPX = ppx;

            BulkInEndPoint.XferSize = BufSz;
        }

        private void StartReceiveData()
        {
            tListen = new Thread(new ThreadStart(XferThread));
            tListen.IsBackground = true;
            tListen.Priority = ThreadPriority.Highest;
            tListen.Start();
        }

        private void StopProcessing()
        {
            if (bRunning)
            {
                if (tListen == null || tListen.IsAlive)
                {
                    StartBtn.Text = "Start";
                    bRunning = false;

                    if (tListen != null)
                    {
                        tListen.Abort();
                        tListen.Join();
                        tListen = null;
                    }

                    StartBtn.BackColor = Color.Aquamarine;
                }
            }
        }

        /*Summary
          Executes on Start Button click 
        */
        private void StartBtn_Click(object sender, System.EventArgs e)
        {
            if (MyDevice == null)
                return;

            if (StartBtn.Text.Equals("Start"))
            {
                txtData.Clear();

                StartBtn.Text = "Stop";
                StartBtn.BackColor = Color.Pink;

                ConfigureInEp();
                if (StartCapture())
                {
                    bRunning = true;
                    StartReceiveData();
                }
                else
                {
                    StopProcessing();
                }
            }
            else
            {
                StopProcessing();
            }

        }


        /*Summary
          Data Xfer Thread entry point. Starts the thread on Start Button click 
        */
        public unsafe void XferThread()
        {
            byte[] buf = new byte[BufSz];
            int len = 0;

            var inEp = BulkInEndPoint;

            inEp.XferMode = XMODE.BUFFERED;
            inEp.TimeOut = 200;

            t1 = DateTime.Now;

            while (bRunning)
            {
                try
                {
                    bool isOk = inEp.XferData(ref buf, ref len);
                    if (!isOk)
                    {
                        Array.Clear(buf, 0, BufSz);
                    }
                }
                catch (Exception)
                {
                    //ex.GetBaseException();
                    this.Invoke(handleException);
                }

                t2 = DateTime.Now;
                elapsed = t2 - t1;
                xferRate = (long)(len / elapsed.TotalMilliseconds);
                xferRate = xferRate / (int)100 * (int)100;
                this.Invoke(updateUI, buf, len);
                Thread.Sleep(1);
            }


            // Setup the queue buffers
            /*byte[][] cmdBufs = new byte[QueueSz][];
            byte[][] xferBufs = new byte[QueueSz][];
            byte[][] ovLaps = new byte[QueueSz][];
            ISO_PKT_INFO[][] pktsInfo = new ISO_PKT_INFO[QueueSz][];

            int xStart = 0;

            try
            {
                LockNLoad(ref xStart, cmdBufs, xferBufs, ovLaps, pktsInfo);
            }
            catch (NullReferenceException e)
            {
                // This exception gets thrown if the device is unplugged 
                // while we're streaming data
                e.GetBaseException();
                this.Invoke(handleException);
            }*/
        }




        /*Summary
          This is a recursive routine for pinning all the buffers used in the transfer in memory.
        It will get recursively called QueueSz times.  On the QueueSz_th call, it will call
        XferData, which will loop, transferring data, until the stop button is clicked.
        Then, the recursion will unwind.
        */
        public unsafe void LockNLoad(ref int j, byte[][] cBufs, byte[][] xBufs, byte[][] oLaps, ISO_PKT_INFO[][] pktsInfo)
        {
            
            // Allocate one set of buffers for the queue. Buffered IO method require user to allocate a buffer as a part of command buffer,
            // the BeginDataXfer does not allocated it. BeginDataXfer will copy the data from the main buffer to the allocated while initializing the commands.
            cBufs[j] = new byte[CyConst.SINGLE_XFER_LEN + IsoPktBlockSize+ ((BulkInEndPoint.XferMode == XMODE.BUFFERED) ? BufSz : 0)];
            xBufs[j] = new byte[BufSz];
            oLaps[j] = new byte[CyConst.OverlapSignalAllocSize];
            pktsInfo[j] = new ISO_PKT_INFO[PPX];

            fixed (byte* tL0 = oLaps[j], tc0 = cBufs[j], tb0 = xBufs[j])  // Pin the buffers in memory
            {
                OVERLAPPED* ovLapStatus = (OVERLAPPED*)tL0;
                ovLapStatus->hEvent = (IntPtr)PInvoke.CreateEvent(0, 0, 0, 0);

                // Pre-load the queue with a request
                int len = BufSz;
                BulkInEndPoint.BeginDataXfer(ref cBufs[j], ref xBufs[j], ref len, ref oLaps[j]);

                j++;

                if (j < QueueSz)
                    LockNLoad(ref j, cBufs, xBufs, oLaps, pktsInfo);  // Recursive call to pin next buffers in memory
                else
                    XferData(cBufs, xBufs, oLaps, pktsInfo);          // All loaded. Let's go!

            }

        }



        /*Summary
          Called at the end of recursive method, LockNLoad().
          XferData() implements the infinite transfer loop
        */
        public unsafe void XferData(byte[][] cBufs, byte[][] xBufs, byte[][] oLaps, ISO_PKT_INFO[][] pktsInfo)
        {
            int k = 0;
            int len = 0;

            XferBytes = 0;
            t1 = DateTime.Now;

            for (; bRunning; )
            {
                // WaitForXfer
                fixed (byte* tmpOvlap = oLaps[k])
                {
                    OVERLAPPED* ovLapStatus = (OVERLAPPED*)tmpOvlap;
                    if (!BulkInEndPoint.WaitForXfer(ovLapStatus->hEvent, 500))
                    {
                        BulkInEndPoint.Abort();
                        PInvoke.WaitForSingleObject(ovLapStatus->hEvent, CyConst.INFINITE);
                    }
                }

                // FinishDataXfer
                if (BulkInEndPoint.FinishDataXfer(ref cBufs[k], ref xBufs[k], ref len, ref oLaps[k]))
                {
                    XferBytes += len;
                }
                
                // Re-submit this buffer into the queue
                len = BufSz;
                BulkInEndPoint.BeginDataXfer(ref cBufs[k], ref xBufs[k], ref len, ref oLaps[k]);

                k++;
                if (k == QueueSz)  // Only update displayed stats once each time through the queue
                {
                    k = 0;

                    t2 = DateTime.Now;
                    elapsed = t2 - t1;

                    xferRate = (long)(XferBytes / elapsed.TotalMilliseconds);
                    xferRate = xferRate / (int)100 * (int)100;

                    // Call StatusUpdate() in the main thread
                    this.Invoke(updateUI);

                    // For small QueueSz or PPX, the loop is too tight for UI thread to ever get service.   
                    // Without this, app hangs in those scenarios.
                    Thread.Sleep(1);
                }               

            } // End infinite loop

        }


        /*Summary
          The callback routine delegated to updateUI.
        */
        public void StatusUpdate(byte[] buf, int len)
        {
            if (xferRate > ProgressBar.Maximum)
                ProgressBar.Maximum = (int)(xferRate * 1.25);

            ProgressBar.Value = (int)xferRate;
            ThroughputLabel.Text = ProgressBar.Value.ToString();

            txtData.Text = BitConverter.ToString(buf, 0, len);
        }


        /*Summary
          The callback routine delegated to handleException.
        */
        public void ThreadException()
        {
            StopProcessing();
        }



    }
}
