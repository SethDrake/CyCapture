using CyUSB;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace CaptureDevice
{
    public class CaptureEventArgs : EventArgs
    {
        public long BytesCaptures;
        public bool IsError;
        
        public CaptureEventArgs() { }
    }

    public class DeviceReadyEventArgs : EventArgs
    {
        public String DeviceName;
        public ushort[] VidPid;
        public String Version;
        public String Revision;

        public DeviceReadyEventArgs() { }
    }

    public class Device
    {
        private const byte CMD_GET_FW_VERSION = 0xB0;
        private const byte CMD_START = 0xB1;
        private const byte CMD_STOP = 0xB2;
        private const byte CMD_GET_REVID_VERSION = 0xB3;
        private const byte CMD_SET_OUTPUT = 0xB4;
        private const byte CMD_SET_PORTA = 0xB5;

        private const byte CMD_GET_STATUS = 0xB6;

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


        [StructLayout(LayoutKind.Sequential)]
        public struct status_info_struct
        {
            public byte state;
            public byte gpif_status;
            public byte ifconfig_value;
        }

        // These are needed to close the app from the Thread exception(exception handling)
        delegate void ExceptionCallback();
        ExceptionCallback handleException;

        private readonly SynchronizationContext syncContext;

        public event EventHandler<CaptureEventArgs> CaptureCompleted;
        public event EventHandler<DeviceReadyEventArgs> DeviceReady;
        public event EventHandler DeviceNotReady;

        protected USBDeviceList usbDevices;
        protected CyUSBDevice cyDevice;
        protected CyControlEndPoint controlEndPoint;
        protected CyBulkEndPoint bulkInEndPoint;

        protected String name;
        protected String version;
        protected String revision;

        protected byte[] resultBuffer;
        protected long resultLength;

        public double XferBytes;
        protected long xferRate;

        public int BufSz = 1;
        public int QueueSz = 128;
        public int PPX = 1;

        protected bool bRunning;
        protected bool isReady;
        protected bool continousMode;
        protected Thread tCapture;

        private Stopwatch sw;

        public string Name => name;

        public string Version => version;

        public string Revision => revision;

        public long XferRate => xferRate;

        public bool IsRunning => bRunning;

        public bool IsReady => isReady;

        public bool ContinousMode => continousMode;

        public byte[] ResultBuffer => resultBuffer;

        public long ResultLength => resultLength;

        public Device()
        {
            // Setup the callback routine for NullReference exception handling
            handleException = new ExceptionCallback(ThreadException);
            syncContext = AsyncOperationManager.SynchronizationContext;
        }

        public void OpenDevice()
        {
            if (isReady)
            {
                return;
            }

            // Create the list of USB devices attached to the CyUSB.sys driver.
            usbDevices = new USBDeviceList(CyConst.DEVICES_CYUSB);

            LoadFirmware();

            //Assign event handlers for device attachment and device removal.
            usbDevices.DeviceAttached += new EventHandler(usbDevices_DeviceAttached);
            usbDevices.DeviceRemoved += new EventHandler(usbDevices_DeviceRemoved);

            //Set and search the device with VID-PID 04b4-1003 and if found, selects the end point
            SetDevice();
        }

        public void CloseDevice()
        {
            if (IsRunning)
            {
               StopReceiving();
            }
            usbDevices?.Dispose();
        }

        void usbDevices_DeviceRemoved(object sender, EventArgs e)
        {
            if (IsRunning)
            {
                StopReceiving();
            }
            isReady = false;

            name = null;
            version = null;
            revision = null;

            cyDevice = null;
            controlEndPoint = null;
            bulkInEndPoint = null;
            
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
                var fx2lp = (CyFX2Device)dev;
                bool isOk = fx2lp.LoadRAM("fx2lafw-cypress-fx2.hex");
                if (!isOk)
                {
                    fx2lp.Reset();
                }
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
                cyDevice = (CyUSBDevice)dev;
                controlEndPoint = null;
                bulkInEndPoint = null;

                GetEndpointsOfNode(cyDevice.Tree);
                if (controlEndPoint != null)
                {
                    isReady = true;

                    name = dev.FriendlyName;
                    version = GetFirmwareVer();
                    revision = GetRevision();

                    DeviceReadyEventArgs args = new DeviceReadyEventArgs();
                    args.DeviceName = name;
                    args.VidPid = new[] { dev.VendorID, dev.ProductID };
                    args.Version = version;
                    args.Revision = revision;

                    DeviceReady?.Invoke(this, args);
                }
            }
            else
            {
                isReady = false;
                DeviceNotReady?.Invoke(this, null);
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
                    CyControlEndPoint ctrlEp = node.Tag as CyControlEndPoint;
                    CyBulkEndPoint bulkEp = node.Tag as CyBulkEndPoint;

                    if (ctrlEp != null)
                    {
                        controlEndPoint = ctrlEp;
                    }

                    if (bulkEp != null && bulkEp.bIn)
                    {
                        bulkInEndPoint = bulkEp;
                        CyUSBInterface ifc = node.Parent.Tag as CyUSBInterface;
                        if (ifc != null)
                        {
                            cyDevice.AltIntfc = ifc.bAlternateSetting;
                        }
                    }
                }
            }
        }

        private String GetFirmwareVer()
        {
            if (controlEndPoint == null)
            {
                return String.Empty;
            }
            var ctrlEp = controlEndPoint;
            ctrlEp.Target = CyConst.TGT_DEVICE;
            ctrlEp.Direction = CyConst.DIR_FROM_DEVICE;
            ctrlEp.ReqType = CyConst.REQ_VENDOR;
            ctrlEp.ReqCode = CMD_GET_FW_VERSION;
            ctrlEp.Value = 0x0000;
            ctrlEp.Index = 0x0000;
            ctrlEp.TimeOut = 1000;

            int len = 2;
            byte[] buf = new byte[len];

            bool isOk = ctrlEp.XferData(ref buf, ref len);
            return isOk ? String.Format("{0}.{1}", buf[0], buf[1]) : String.Empty;
        }

        private String GetRevision()
        {
            if (controlEndPoint == null)
            {
                return String.Empty;
            }
            var ctrlEp = controlEndPoint;
            ctrlEp.Target = CyConst.TGT_DEVICE;
            ctrlEp.Direction = CyConst.DIR_FROM_DEVICE;
            ctrlEp.ReqType = CyConst.REQ_VENDOR;
            ctrlEp.ReqCode = CMD_GET_REVID_VERSION;
            ctrlEp.Value = 0x0000;
            ctrlEp.Index = 0x0000;
            ctrlEp.TimeOut = 1000;

            int len = 1;
            byte[] buf = new byte[len];

            bool isOk = ctrlEp.XferData(ref buf, ref len);
            return isOk ? String.Format("{0}", buf[0]) : String.Empty;
        }

        public status_info_struct? GetStatusInfo()
        {
            if (controlEndPoint == null || bulkInEndPoint == null)
            {
                return null;
            }

            var ctrlEp = controlEndPoint;
            ctrlEp.Target = CyConst.TGT_DEVICE;
            ctrlEp.Direction = CyConst.DIR_FROM_DEVICE;
            ctrlEp.ReqType = CyConst.REQ_VENDOR;
            ctrlEp.ReqCode = CMD_GET_STATUS;
            ctrlEp.Value = 0x0000;
            ctrlEp.Index = 0x0000;
            ctrlEp.TimeOut = 2000;
           
            int len = 3;
            byte[] buf = new byte[len];

            bool isOk = ctrlEp.XferData(ref buf, ref len);
            if (isOk)
            {
                GCHandle h = GCHandle.Alloc(buf, GCHandleType.Pinned);
                status_info_struct infoStruct = (status_info_struct)Marshal.PtrToStructure(h.AddrOfPinnedObject(), typeof(status_info_struct));
                h.Free();
                return infoStruct;
            }
            return null;
        }

        private bool SetPortaA(byte value)
        {
            if (controlEndPoint == null)
            {
                return false;
            }
            var ctrlEp = controlEndPoint;
            ctrlEp.Target = CyConst.TGT_DEVICE;
            ctrlEp.Direction = CyConst.DIR_FROM_DEVICE;
            ctrlEp.ReqType = CyConst.REQ_VENDOR;
            ctrlEp.ReqCode = CMD_SET_PORTA;
            ctrlEp.Value = value;
            ctrlEp.Index = 0x0000;
            ctrlEp.TimeOut = 1000;

            int len = 1;
            byte[] buf = new byte[len];

            bool isOk = ctrlEp.XferData(ref buf, ref len);
            return isOk;
        }

        public bool SetOutputPort(byte value)
        {
            if (controlEndPoint == null)
            {
                return false;
            }
            var ctrlEp = controlEndPoint;
            ctrlEp.Target = CyConst.TGT_DEVICE;
            ctrlEp.Direction = CyConst.DIR_FROM_DEVICE;
            ctrlEp.ReqType = CyConst.REQ_VENDOR;
            ctrlEp.ReqCode = CMD_SET_OUTPUT;
            ctrlEp.Value = value;
            ctrlEp.Index = 0x0000;
            ctrlEp.TimeOut = 1000;

            int len = 1;
            byte[] buf = new byte[len];

            bool isOk = ctrlEp.XferData(ref buf, ref len);
            return isOk;
        }

        private bool StartCapture()
        {
            if (controlEndPoint == null || bulkInEndPoint == null)
            {
                return false;
            }

            var ctrlEp = controlEndPoint;
            ctrlEp.Target = CyConst.TGT_DEVICE;
            ctrlEp.Direction = CyConst.DIR_TO_DEVICE;
            ctrlEp.ReqType = CyConst.REQ_VENDOR;
            ctrlEp.ReqCode = CMD_START;
            ctrlEp.Value = 0x0000;
            ctrlEp.Index = 0x0000;
            ctrlEp.TimeOut = 2000;

            var initStruct = new cmd_start_acquisition_struct();
            initStruct.flags = CMD_START_FLAGS_SAMPLE_8BIT;// | CMD_START_FLAGS_INV_CLK;
            initStruct.sample_delay_h = 0;
            initStruct.sample_delay_l = 0;

            int len = 3;
            byte[] buf = new byte[len];

            GCHandle h = GCHandle.Alloc(initStruct, GCHandleType.Pinned);
            Marshal.Copy(h.AddrOfPinnedObject(), buf, 0, len);
            h.Free();

            bool isOk = ctrlEp.XferData(ref buf, ref len);
            return isOk;
        }

        private void ConfigureInEp()
        {
            BufSz = bulkInEndPoint.MaxPktSize * PPX;

            bulkInEndPoint.XferMode = XMODE.BUFFERED;
            bulkInEndPoint.XferSize = BufSz;
            bulkInEndPoint.TimeOut = 2000;
        }

        public bool StartReceiveData()
        {
            if (!IsReady || IsRunning)
            {
                return false;
            }

            continousMode = false;
            bRunning = true;
            xferRate = 0;
            XferBytes = 0;

            ConfigureInEp();

            resultLength = 0;
            resultBuffer = new byte[BufSz * QueueSz];

            if (!StartCapture())
            {
                bRunning = false;
                return false;
            }
            
            tCapture = new Thread(new ThreadStart(AutoReceiveDataThread));
            tCapture.IsBackground = true;
            tCapture.Priority = ThreadPriority.Highest;
            tCapture.Start();

            return true;
        }

        public bool StartReceiveDataContinous()
        {
            if (!IsReady || IsRunning)
            {
                return false;
            }

            continousMode = true;
            bRunning = true;
            xferRate = 0;
            XferBytes = 0;

            ConfigureInEp();

            resultLength = 0;
            resultBuffer = new byte[BufSz * QueueSz];

            if (!StartCapture())
            {
                bRunning = false;
                return false;
            }

            tCapture = new Thread(new ThreadStart(AutoReceiveDataThread));
            tCapture.IsBackground = true;
            tCapture.Priority = ThreadPriority.Highest;
            tCapture.Start();

            return true;
        }

        public void StopReceiving()
        {
            if (!IsReady || !IsRunning)
            {
                return;
            }
            if (tCapture != null && tCapture.IsAlive)
            {
                tCapture.Abort();
            }
            tCapture = null;
        }

        private void AutoReceiveDataThread()
        {
            byte[][] cmdBufs = new byte[QueueSz][];
            byte[][] xferBufs = new byte[QueueSz][];
            byte[][] ovLaps = new byte[QueueSz][];

            int xStart = 0;

            sw = new Stopwatch();

            try
            {
                LockNLoad(ref xStart, cmdBufs, xferBufs, ovLaps);
            }
            catch (Exception ex)
            {
                ex.GetBaseException();
                syncContext.Post(e => handleException.Invoke(), ex);
            }
        }

        private unsafe void LockNLoad(ref int j, byte[][] cBufs, byte[][] xBufs, byte[][] oLaps)
        {
            // Allocate one set of buffers for the queue. Buffered IO method require user to allocate a buffer as a part of command buffer,
            // the BeginDataXfer does not allocated it. BeginDataXfer will copy the data from the main buffer to the allocated while initializing the commands.
            cBufs[j] = new byte[CyConst.SINGLE_XFER_LEN + BufSz];
            xBufs[j] = new byte[BufSz];
            oLaps[j] = new byte[CyConst.OverlapSignalAllocSize];

            fixed (byte* tL0 = oLaps[j], tc0 = cBufs[j], tb0 = xBufs[j])  // Pin the buffers in memory
            {
                OVERLAPPED* ovLapStatus = (OVERLAPPED*)tL0;
                ovLapStatus->hEvent = (IntPtr)PInvoke.CreateEvent(0, 0, 0, 0);

                // Pre-load the queue with a request
                int len = BufSz;
                bulkInEndPoint.BeginDataXfer(ref cBufs[j], ref xBufs[j], ref len, ref oLaps[j]);

                j++;

                if (j < QueueSz)
                {
                    LockNLoad(ref j, cBufs, xBufs, oLaps); // Recursive call to pin next buffers in memory
                }
                else
                {
                    XferData(cBufs, xBufs, oLaps); // All loaded. Let's go!
                }
            }
        }

        private unsafe void XferData(byte[][] cBufs, byte[][] xBufs, byte[][] oLaps)
        {
            int k = 0;
            int len = 0;

            XferBytes = 0;
            sw.Restart();

            while (bRunning)
            {
                // WaitForXfer
                fixed (byte* tmpOvlap = oLaps[k])
                {
                    OVERLAPPED* ovLapStatus = (OVERLAPPED*)tmpOvlap;
                    if (!bulkInEndPoint.WaitForXfer(ovLapStatus->hEvent, 500))
                    {
                        bulkInEndPoint.Abort();
                        PInvoke.WaitForSingleObject(ovLapStatus->hEvent, CyConst.INFINITE);
                    }
                }

                // FinishDataXfer
                if (bulkInEndPoint.FinishDataXfer(ref cBufs[k], ref xBufs[k], ref len, ref oLaps[k]))
                {
                    XferBytes += len;
                }


                k++;
                if (k == QueueSz)  // Only update displayed stats once each time through the queue
                {
                    k = 0;
                    xferRate = (sw.ElapsedMilliseconds > 0) ? ((long)XferBytes / sw.ElapsedMilliseconds) : (long)XferBytes;
                    if (!continousMode)
                    {
                        sw.Stop();
                        bRunning = false;
                    }
                    /*if (continousMode)
                    {
                        Array.Clear(resultBuffer, 0, BufSz * QueueSz);
                    }*/
                    for (int i = 0; i < xBufs.Length; i++)
                    {
                        Array.Copy(xBufs[i], 0, resultBuffer, BufSz * i, BufSz);
                    }
                    resultLength = (long) XferBytes;

                    CaptureEventArgs args = new CaptureEventArgs();
                    args.BytesCaptures = resultLength;
                    args.IsError = false;
                    syncContext.Post(e => CaptureCompleted?.Invoke(this, args), args);
                    if (continousMode)
                    {
                        Thread.Sleep(1);
                    }
                }

                if (continousMode)
                {
                    len = BufSz;
                    Array.Clear(xBufs[k], 0, BufSz);
                    bulkInEndPoint.BeginDataXfer(ref cBufs[k], ref xBufs[k], ref len, ref oLaps[k]);
                }

            } // End infinite loop

        }

        /*Summary
          The callback routine delegated to handleException.
        */
        private void ThreadException()
        {
            sw.Stop();

            CaptureEventArgs args = new CaptureEventArgs();
            args.BytesCaptures = 0;
            args.IsError = true;
            CaptureCompleted?.Invoke(this, args);

            xferRate = 0;
            XferBytes = 0;
            resultLength = 0;
            bRunning = false;
        }

    }
}
