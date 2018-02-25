
using SDRSharp.Common;
using SDRSharp.Radio;
using System;
using System.Windows.Forms;

namespace SDRSharp.CyCapture
{
  public class CyCaptureIO : IFrontendController, IIQStreamController, ITunableSource, IFloatingConfigDialogProvider, ISampleRateChangeSource, IControlAwareObject, ISpectrumProvider, IDisposable
  {
    private double DefaultSampleRate = 2400000.0;
    private uint _frequency = 105500000;
    private CyCaptureControllerDialog _gui;
    private CyCaptureDevice _device;
    private Radio.SamplesAvailableDelegate _callback;

    public event EventHandler SampleRateChanged;

    public CyCaptureIO()
    {
      this._gui = new CyCaptureControllerDialog();
    }

    ~CyCaptureIO()
    {
      this.Dispose();
    }

    public void SetControl(object control)
    {
      this._gui.Control = (ISharpControl) control;
    }

    public void Dispose()
    {
      this.CloseDevice();
      GC.SuppressFinalize((object) this);
    }

    public void SelectDevice()
    {
      this.CloseDevice();
      this._device = new CyCaptureDevice();
      this._device.Frequency = this._frequency;
      this._gui.Device = this._device;
      this._device.SamplesAvailable += new SamplesAvailableDelegate(this.captDevice_SamplesAvailable);
      this._device.SampleRateChanged += new EventHandler(this.captDevice_SampleRateChanged);
    }

    public void Open()
    {
        this._gui.EnableEnumTimer = true;
        try
        {
            this.SelectDevice();
            return;
        }
        catch (ApplicationException ex)
        {
        }
        throw new ApplicationException("No compatible devices found");
    }

    private void CloseDevice()
    {
      if (this._device == null)
        return;
      this._device.Stop();
      this._device.SamplesAvailable -= new SamplesAvailableDelegate(this.captDevice_SamplesAvailable);
      this._device.SampleRateChanged -= new EventHandler(this.captDevice_SampleRateChanged);
      this._device.Dispose();
      this._device = (CyCaptureDevice) null;
    }

    public void Close()
    {
      this.CloseDevice();
      this._gui.EnableEnumTimer = false;
    }

    public void Start(SDRSharp.Radio.SamplesAvailableDelegate callback)
    {
      if (this._device == null)
        throw new ApplicationException("No device selected");
      this._callback = callback;
      try
      {
        this._device.Start();
      }
      catch
      {
        this.Open();
        this._device.Start();
      }
    }

    public void Stop()
    {
      if (this._device == null)
        return;
      this._device.Stop();
    }

    public bool CanTune
    {
      get
      {
        return true;
      }
    }

    public long MinimumTunableFrequency
    {
      get
      {
        return 0;
      }
    }

    public long MaximumTunableFrequency
    {
      get
      {
        return 2500000000;
      }
    }

    public void ShowSettingGUI(IWin32Window parent)
    {
      this._gui.Show();
    }

    public void HideSettingGUI()
    {
      this._gui.Hide();
    }

    public double Samplerate
    {
      get
      {
        if (this._device != null)
          return (double) this._device.Samplerate;
        return this.DefaultSampleRate;
      }
    }

    public long Frequency
    {
      get
      {
        return (long) this._frequency;
      }
      set
      {
        this._frequency = (uint) value;
        if (this._device == null)
          return;
        this._device.Frequency = this._frequency;
      }
    }

    public float UsableSpectrumRatio
    {
      get
      {
        return 0.8f;
      }
    }

    private unsafe void captDevice_SamplesAvailable(object sender, SamplesAvailableEventArgs e)
    {
      this._callback((IFrontendController) this, e.Buffer, e.Length);
    }

    private void captDevice_SampleRateChanged(object sender, EventArgs e)
    {
      // ISSUE: reference to a compiler-generated field
      EventHandler sampleRateChanged = this.SampleRateChanged;
      if (sampleRateChanged == null)
        return;
      sampleRateChanged((object) this, e);
    }
  }
}
