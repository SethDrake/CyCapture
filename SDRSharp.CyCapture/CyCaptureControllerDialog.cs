using SDRSharp.Common;
using SDRSharp.Radio;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace SDRSharp.CyCapture
{
  public class CyCaptureControllerDialog : Form
  {
    private CyCaptureDevice _device;
    private IContainer components;
    private Timer refreshTimer;
    private Button closeButton;
    private Label label1;
    private Label label3;
    private ComboBox samplerateComboBox;
    private Label label5;
    private ComboBox samplingModeComboBox;

    public CyCaptureControllerDialog()
    {
      this.InitializeComponent();
      this.samplerateComboBox.SelectedIndex = Utils.GetIntSetting("cyCapture.sampleRate", 0);
      this.samplingModeComboBox.SelectedIndex = Utils.GetIntSetting("cyCapture.samplingMode", 0);
    }

    public ISharpControl Control { get; set; }

    public bool EnableEnumTimer
    {
      get
      {
        return this.refreshTimer.Enabled;
      }
      set
      {
        this.refreshTimer.Enabled = value;
      }
    }

    public CyCaptureDevice Device
    {
      get
      {
        return this._device;
      }
      set
      {
        if (value == this._device)
          return;
        this._device = value;
        this.InitDevice();
      }
    }

    private void SaveSettings()
    {
        Utils.SaveSetting("cyCapture.sampleRate", (object) this.samplerateComboBox.SelectedIndex);
        Utils.SaveSetting("cyCapture.samplingMode", (object) this.samplingModeComboBox.SelectedIndex);
    }

    private void closeButton_Click(object sender, EventArgs e)
    {
      this.Close();
    }

    private void CyCaptureControllerDialog_FormClosing(object sender, FormClosingEventArgs e)
    {
      if (e.CloseReason == CloseReason.UserClosing)
      {
        e.Cancel = true;
        this.Hide();
      }
      this.SaveSettings();
    }

    private void refreshTimer_Tick(object sender, EventArgs e)
    {
      bool flag = this._device != null && !this._device.IsStreaming;
      this.samplingModeComboBox.Enabled = flag;
    }

    private void samplerateComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (this._device == null)
        return;
      this._device.Samplerate = (uint) (double.Parse(this.samplerateComboBox.Items[this.samplerateComboBox.SelectedIndex].ToString().Split(' ')[0], (IFormatProvider) CultureInfo.InvariantCulture) * 1000000.0);
    }

    private void samplingModeComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (this._device == null)
        return;
    }

    public void InitDevice()
    {
      if (this._device == null)
        return;
     
      this.samplerateComboBox_SelectedIndexChanged((object) null, (EventArgs) null);
      this.samplingModeComboBox_SelectedIndexChanged((object) null, (EventArgs) null);
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing && this.components != null)
        this.components.Dispose();
      base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
      this.components = (IContainer) new Container();
      this.refreshTimer = new System.Windows.Forms.Timer(this.components);
      this.closeButton = new Button();
      this.label1 = new Label();
      this.label3 = new Label();
      this.samplerateComboBox = new ComboBox();
      this.label5 = new Label();
      this.samplingModeComboBox = new ComboBox();
      this.SuspendLayout();
      this.refreshTimer.Interval = 1000;
      this.refreshTimer.Tick += new EventHandler(this.refreshTimer_Tick);
      this.closeButton.DialogResult = DialogResult.Cancel;
      this.closeButton.Location = new Point(184, 306);
      this.closeButton.Name = "closeButton";
      this.closeButton.Size = new Size(75, 23);
      this.closeButton.TabIndex = 8;
      this.closeButton.Text = "Close";
      this.closeButton.UseVisualStyleBackColor = true;
      this.closeButton.Click += new EventHandler(this.closeButton_Click);
      this.label1.AutoSize = true;
      this.label1.Location = new Point(12, 9);
      this.label1.Name = "label1";
      this.label1.Size = new Size(41, 13);
      this.label1.TabIndex = 20;
      this.label1.Text = "Device";
      this.label3.AutoSize = true;
      this.label3.Location = new Point(12, 53);
      this.label3.Name = "label3";
      this.label3.Size = new Size(68, 13);
      this.label3.TabIndex = 24;
      this.label3.Text = "Sample Rate";
      this.samplerateComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
      this.samplerateComboBox.FormattingEnabled = true;
      this.samplerateComboBox.Items.AddRange(new object[2]
      {
        (object) "20 MSPS",
        (object) "10 MSPS",
      });
      this.samplerateComboBox.Location = new Point(12, 70);
      this.samplerateComboBox.Name = "samplerateComboBox";
      this.samplerateComboBox.Size = new Size(247, 21);
      this.samplerateComboBox.TabIndex = 1;
      this.samplerateComboBox.SelectedIndexChanged += new EventHandler(this.samplerateComboBox_SelectedIndexChanged);
      this.label5.AutoSize = true;
      this.label5.Location = new Point(12, 97);
      this.label5.Name = "label5";
      this.label5.Size = new Size(80, 13);
      this.label5.TabIndex = 31;
      this.label5.Text = "Sampling Mode";
      this.samplingModeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
      this.samplingModeComboBox.FormattingEnabled = true;
      this.samplingModeComboBox.Items.AddRange(new object[3]
      {
        (object) "Quadrature sampling",
        (object) "Direct sampling (I branch)",
        (object) "Direct sampling (Q branch)"
      });
      this.samplingModeComboBox.Location = new Point(12, 114);
      this.samplingModeComboBox.Name = "samplingModeComboBox";
      this.samplingModeComboBox.Size = new Size(247, 21);
      this.samplingModeComboBox.TabIndex = 2;
      this.samplingModeComboBox.SelectedIndexChanged += new EventHandler(this.samplingModeComboBox_SelectedIndexChanged);
      this.AutoScaleDimensions = new SizeF(6f, 13f);
      this.AutoScaleMode = AutoScaleMode.Font;
      this.CancelButton = (IButtonControl) this.closeButton;
      this.ClientSize = new Size(271, 342);
      this.Controls.Add((System.Windows.Forms.Control) this.label5);
      this.Controls.Add((System.Windows.Forms.Control) this.samplingModeComboBox);
      this.Controls.Add((System.Windows.Forms.Control) this.label3);
      this.Controls.Add((System.Windows.Forms.Control) this.samplerateComboBox);
      this.Controls.Add((System.Windows.Forms.Control) this.label1);
      this.Controls.Add((System.Windows.Forms.Control) this.closeButton);
      this.FormBorderStyle = FormBorderStyle.FixedDialog;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = nameof (CyCaptureControllerDialog);
      this.ShowIcon = false;
      this.ShowInTaskbar = false;
      this.StartPosition = FormStartPosition.CenterParent;
      this.Text = "CyCapture USB Controller";
      this.TopMost = true;
      this.FormClosing += new FormClosingEventHandler(this.CyCaptureControllerDialog_FormClosing);
      this.ResumeLayout(false);
      this.PerformLayout();
    }
  }
}
