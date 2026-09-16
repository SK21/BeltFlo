using System;
using System.Drawing;
using System.IO.Ports;
using System.Windows.Forms;
using BeltFlo.Classes;
using BeltFlo.Language;

namespace BeltFlo.Forms
{
    public partial class frmMenuSettings : Form
    {
        private bool _dragging;
        private Point _dragStart;
        private string _originalCommType;
        private string _originalCanDriver;
        private string _originalCanPort;
        private int _unitChoice;   // 0 = cwt/ac, 1 = tons/ac, 2 = metric t/ha

        private static readonly Color ActiveColour = Color.FromArgb(0, 80, 160);
        private static readonly Color InactiveColour = Color.FromArgb(60, 60, 60);

        public frmMenuSettings()
        {
            InitializeComponent();
        }

        private void frmMenuSettings_Load(object sender, EventArgs e)
        {
            ApplyTheme();
            FormPositions.Restore(this);
            this.FormClosed += (s2, ev2) => FormPositions.Save(this);
            foreach (Control c in new System.Windows.Forms.Control[] { pnlTitle, lblTitle })
            {
                c.MouseDown += (s, ev) => { if (ev.Button == MouseButtons.Left) { _dragging = true; _dragStart = ev.Location; } };
                c.MouseMove += (s, ev) => { if (_dragging) { Left += ev.X - _dragStart.X; Top += ev.Y - _dragStart.Y; } };
                c.MouseUp += (s, ev) => _dragging = false;
            }
            LoadCurrentSettings();
            _originalCommType = Properties.Settings.Default.ModuleCommType;
            _originalCanDriver = Properties.Settings.Default.CanDriver;
            _originalCanPort = Properties.Settings.Default.CanPort;

            if (Core.UDPmodule.ModuleIP != "")
            {
                btnEthernet.Text = Lang.lgWiFi + "  (" + Core.UDPmodule.ModuleIP + ")";
            }
            else
            {
                btnEthernet.Text = Lang.lgWiFi;
            }
        }

        private void ApplyTheme()
        {
            var back = Properties.Settings.Default.MainBackColour;
            var fore = Properties.Settings.Default.MainForeColour;
            var ctrl = Color.FromArgb(60, 60, 60);

            pnlTitle.BackColor = back;
            pnlContent.BackColor = back;
            lblTitle.ForeColor = Color.FromArgb(180, 200, 220);

            foreach (Control c in pnlContent.Controls)
            {
                c.ForeColor = fore;
                if (c is Button btn && btn != btnSaveSettings)
                {
                    btn.BackColor = InactiveColour;
                    btn.ForeColor = Color.White;
                }
                if (c is ComboBox cb)
                {
                    cb.BackColor = ctrl;
                    cb.ForeColor = Color.White;
                }
            }

            btnSaveSettings.BackColor = Color.FromArgb(0, 110, 0);
            btnSaveSettings.ForeColor = Color.White;
            btnSettingsClose.BackColor = InactiveColour;
            btnSettingsClose.ForeColor = Color.White;
        }

        private void LoadCurrentSettings()
        {
            // Units. Metric yield is always t/ha, so three buttons cover every
            // combination the app can be in: the two imperial ticket units, and
            // metric. Picking one sets both Units and YieldUnit.
            SetUnitChoice(Properties.Settings.Default.Units == "Metric" ? 2
                        : Props.YieldInTons ? 1 : 0);

            // Network mode
            bool isEthernet = Properties.Settings.Default.ModuleCommType != "CAN";
            SetToggle(btnEthernet, btnCAN, isEthernet);

            // Resume Job on Start
            bool resume = Properties.Settings.Default.ResumeJobOnStart;
            SetToggle(btnResumeOn, btnResumeOff, resume);

            // Resume from Pause when sections come on
            SetToggle(btnAutoResumeOn, btnAutoResumeOff, Properties.Settings.Default.AutoResumePause);

            // CAN driver
            cbCanDriver.SelectedIndex = cbCanDriver.FindStringExact(Properties.Settings.Default.CanDriver);
            if (cbCanDriver.SelectedIndex < 0) cbCanDriver.SelectedIndex = 0;

            // CAN port
            LoadCanPortCombo();
            cbCanPort.SelectedIndex = cbCanPort.FindStringExact(Properties.Settings.Default.CanPort);

            UpdateNetworkControls(isEthernet);
        }

        private void LoadCanPortCombo()
        {
            cbCanPort.Items.Clear();
            try
            {
                foreach (string port in SerialPort.GetPortNames())
                    cbCanPort.Items.Add(port);
            }
            catch { }
        }

        private void UpdateNetworkControls(bool isWifi)
        {
            lblWifiInfo.Visible = isWifi;
            lblCanDriver.Visible = !isWifi;
            cbCanDriver.Visible = !isWifi;
            lblCanPort.Visible = !isWifi;
            cbCanPort.Visible = !isWifi;
            btnRescanPorts.Visible = !isWifi;
            lblCanStatus.Visible = !isWifi;
        }

        // Reflects the CAN adapter actually running (started from previously
        // saved settings), not whatever driver/port is currently selected in
        // the combo boxes — those only take effect after Save + restart.
        private void tmrCanStatus_Tick(object sender, EventArgs e)
        {
            if (!lblCanStatus.Visible) return;

            bool connected = Core.CanModule.AdapterConnected;
            lblCanStatus.Text = "Adapter: " + (connected ? "Connected" : "Not Connected");
            lblCanStatus.ForeColor = connected ? OkabeIto.BluishGreen : OkabeIto.Vermillion;
        }

        private void btnRescanPorts_Click(object sender, EventArgs e)
        {
            string current = cbCanPort.SelectedItem?.ToString() ?? "";
            LoadCanPortCombo();
            cbCanPort.SelectedIndex = cbCanPort.FindStringExact(current);
        }

        private void SetToggle(Button active, Button inactive, bool firstActive)
        {
            active.BackColor = firstActive ? ActiveColour : InactiveColour;
            inactive.BackColor = firstActive ? InactiveColour : ActiveColour;
        }

        private void SetUnitChoice(int choice)
        {
            _unitChoice = choice;
            btnUnitCwt.BackColor    = choice == 0 ? ActiveColour : InactiveColour;
            btnUnitTons.BackColor   = choice == 1 ? ActiveColour : InactiveColour;
            btnUnitMetric.BackColor = choice == 2 ? ActiveColour : InactiveColour;
        }

        private void btnUnitCwt_Click(object sender, EventArgs e) => SetUnitChoice(0);
        private void btnUnitTons_Click(object sender, EventArgs e) => SetUnitChoice(1);
        private void btnUnitMetric_Click(object sender, EventArgs e) => SetUnitChoice(2);

        private void btnResumeOn_Click(object sender, EventArgs e) => SetToggle(btnResumeOn, btnResumeOff, true);
        private void btnResumeOff_Click(object sender, EventArgs e) => SetToggle(btnResumeOn, btnResumeOff, false);
        private void btnAutoResumeOn_Click(object sender, EventArgs e) => SetToggle(btnAutoResumeOn, btnAutoResumeOff, true);
        private void btnAutoResumeOff_Click(object sender, EventArgs e) => SetToggle(btnAutoResumeOn, btnAutoResumeOff, false);

        private void btnEthernet_Click(object sender, EventArgs e)
        {
            SetToggle(btnEthernet, btnCAN, true);
            UpdateNetworkControls(true);
        }

        private void btnCAN_Click(object sender, EventArgs e)
        {
            SetToggle(btnEthernet, btnCAN, false);
            UpdateNetworkControls(false);
        }

        private void btnSaveSettings_Click(object sender, EventArgs e)
        {
            // Units. Metric is always t/ha, so the imperial yield unit is left
            // as it was — switching to metric and back keeps the earlier choice.
            Properties.Settings.Default.Units = _unitChoice == 2 ? "Metric" : "Imperial";
            if (_unitChoice != 2)
                Properties.Settings.Default.YieldUnit = _unitChoice == 1 ? "tons/ac" : "cwt/ac";

            // Network / comm type
            bool isEthernet = btnEthernet.BackColor == ActiveColour;
            Properties.Settings.Default.ModuleCommType = isEthernet ? "UDP" : "CAN";

            if (isEthernet)
            {
                // WiFi mode — module broadcasts to its own subnet automatically; no endpoint config needed
            }
            else
            {
                string driver = cbCanDriver.SelectedItem?.ToString() ?? "SLCAN";
                string port = cbCanPort.SelectedItem?.ToString() ?? "";
                Properties.Settings.Default.CanDriver = driver;
                Properties.Settings.Default.CanPort = port;
                Props.CurrentCanDriver = Enum.TryParse(driver, out CanDriver cd) ? cd : CanDriver.SLCAN;
                Props.CanPort = port;
            }

            Props.CanEnabled = !isEthernet;

            // Resume Job on Start
            Properties.Settings.Default.ResumeJobOnStart = btnResumeOn.BackColor == ActiveColour;

            // Resume from Pause when sections come on
            Properties.Settings.Default.AutoResumePause = btnAutoResumeOn.BackColor == ActiveColour;

            Properties.Settings.Default.Save();
            Core.RaiseColorChanged();
            Props.ShowMessage(Lang.lgSettingsSaved);

            bool commChanged = Properties.Settings.Default.ModuleCommType != _originalCommType
                            || Properties.Settings.Default.CanDriver != _originalCanDriver
                            || Properties.Settings.Default.CanPort != _originalCanPort;

            if (commChanged)
            {
                var result = MessageBox.Show(
                    "Comm settings changed. Restart now to apply?",
                    "Restart Required",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                    Core.RequestRestart();
                else
                    _originalCommType = _originalCanDriver = _originalCanPort = null;
            }
        }

        private void btnSettingsClose_Click(object sender, EventArgs e) => this.Close();
    }
}
