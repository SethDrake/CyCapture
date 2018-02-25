using SDRSharp.Radio;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using CaptureDevice;

namespace SDRSharp.CyCapture
{
  public sealed unsafe class CyCaptureDevice : IDisposable
  {
    private readonly Device captureDevice;
    private uint _centerFrequency = 105500000;
    private uint _sampleRate = 20000000;
    private readonly SamplesAvailableEventArgs _eventArgs = new SamplesAvailableEventArgs();
    private static readonly UnsafeBuffer _lutBuffer = UnsafeBuffer.Create(256, 4);
    private static readonly uint _readLength = (uint) Utils.GetIntSetting("CyCaptureBufferLength", 16384);
    private static readonly unsafe float* _lutPtr = (float*) (void*) CyCaptureDevice._lutBuffer;
    private const uint DefaultFrequency = 105500000;
    private const int DefaultSamplerate = 20000000;
    private const float TimeConst = 0.005f;
    private readonly string _name;
    private float _iavg;
    private float _qavg;
    private float _alpha;
    private GCHandle _gcHandle;
    private UnsafeBuffer _iqBuffer;
    private unsafe Complex* _iqPtr;
    private Thread _worker;
    public EventHandler SampleRateChanged;

    static unsafe CyCaptureDevice()
    {
      for (int index = 0; index < 256; ++index)
        CyCaptureDevice._lutPtr[index] = (float) (index - 128) * (1f / (float) sbyte.MaxValue);
    }

    public CyCaptureDevice()
    {
        captureDevice = new Device();
        captureDevice.CaptureCompleted += new EventHandler<CaptureEventArgs>(SdrSamplesAvailable);
        captureDevice.OpenDevice();

        this._alpha = (float) (1.0 - Math.Exp(-1.0 / ((double) this._sampleRate * 0.00499999988824129)));
        this._name = String.Format("{0} ver.{1} rev.{2}", captureDevice.Name, captureDevice.Version, captureDevice.Revision);
        this._gcHandle = GCHandle.Alloc((object) this);
    }

    ~CyCaptureDevice()
    {
      this.Dispose();
    }

    public void Dispose()
    {
      this.Stop();
      captureDevice.CloseDevice();
        if (this._gcHandle.IsAllocated)
        {
            this._gcHandle.Free();
        }
        GC.SuppressFinalize((object) this);
    }

    public event SamplesAvailableDelegate SamplesAvailable;

    public void Start()
    {
      if (this._worker != null)
        throw new ApplicationException("Already running");
      this._iavg = 0.0f;
      this._qavg = 0.0f;
      this._worker = new Thread(new ThreadStart(this.StreamProc));
      this._worker.Priority = ThreadPriority.Highest;
      this._worker.Start();
    }

    public void Stop()
    {
      if (this._worker == null)
        return;
      captureDevice.StopReceiving();
      this._worker.Join();
      this._worker = (Thread) null;
    }

    public string Name
    {
      get
      {
        return this._name;
      }
    }

    public uint Samplerate
    {
      get
      {
        return this._sampleRate;
      }
      set
      {
        if ((int) value == (int) this._sampleRate)
          return;
        this._sampleRate = value;
        //NativeMethods.rtlsdr_set_sample_rate(this._dev, this._sampleRate);
        this._alpha = (float) (1.0 - Math.Exp(-1.0 / ((double) this._sampleRate * 0.00499999988824129)));
        EventHandler sampleRateChanged = this.SampleRateChanged;
        if (sampleRateChanged == null)
          return;
        sampleRateChanged((object) this, EventArgs.Empty);
      }
    }

    public uint Frequency
    {
      get
      {
            return this._centerFrequency;
      }
      set
      {
            this._centerFrequency = value;
          //captureDevice.SetOutputPort((byte)this._centerFrequency);
      }
    }

    public bool IsStreaming
    {
      get
      {
        return this._worker != null;
      }
    }

    public bool IsHung
    {
      get
      {
        if (this._worker != null)
          return this._worker.ThreadState == ThreadState.Stopped;
        return false;
      }
    }

    private void StreamProc()
    {
        captureDevice.PPX = 1;
        captureDevice.QueueSz = (int)CyCaptureDevice._readLength / 512;
        captureDevice.StartReceiveDataContinous();
        //NativeMethods.rtlsdr_read_async(this._dev, CyCaptureDevice._readAsyncCallback, (IntPtr) this._gcHandle, 0U, CyCaptureDevice._readLength);
    }

    private unsafe void ComplexSamplesAvailable(Complex* buffer, int length)
    {
      // ISSUE: reference to a compiler-generated field
      if (this.SamplesAvailable == null)
        return;
      this._eventArgs.Buffer = buffer;
      this._eventArgs.Length = length;
      // ISSUE: reference to a compiler-generated field
      this.SamplesAvailable((object) this, this._eventArgs);
    }

    private unsafe void SdrSamplesAvailable(object sender, CaptureEventArgs e)
    {
      /*GCHandle gcHandle = this._gcHandle;
      if (!gcHandle.IsAllocated)
        return;*/
      //CyCaptureDevice target = (CyCaptureDevice) gcHandle.Target;
      CyCaptureDevice target = this;
      int length = (int)e.BytesCaptures;
      if (target._iqBuffer == null || target._iqBuffer.Length != length)
      {
        target._iqBuffer = UnsafeBuffer.Create(length, sizeof (Complex));
        target._iqPtr = (Complex*) (void*) target._iqBuffer;
      }
      float iavg = target._iavg;
      float qavg = target._qavg;
      float alpha = target._alpha;
      Complex* iqPtr = target._iqPtr;
      for (int index = 0; index < length; index++)
      {
        //iqPtr->Real = CyCaptureDevice._lutPtr[*buf++];
        //iqPtr->Imag = CyCaptureDevice._lutPtr[*buf++];
        iqPtr->Real = captureDevice.ResultBuffer[index];
        iqPtr->Imag = captureDevice.ResultBuffer[index];
        iavg += alpha * (iqPtr->Real - iavg);
        qavg += alpha * (iqPtr->Imag - qavg);
        iqPtr->Real -= iavg;
        iqPtr->Imag -= qavg;
        ++iqPtr;
      }
      target._iavg = iavg;
      target._qavg = qavg;
      target.ComplexSamplesAvailable(target._iqPtr, target._iqBuffer.Length);
    }
  }
}
