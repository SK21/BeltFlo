using System;
using System.Drawing;
using System.Windows.Forms;
using BeltFlo.Classes;
using BeltFlo.Database;
using BeltFlo.Language;

namespace BeltFlo.Forms
{
    // Conveyor geometry and thresholds for the active harvester profile. What the
    // module needs to weigh (belt travel per pulse, weighed section length,
    // belt-stopped timeout) goes to it on Save; the delay and empty-belt threshold
    // stay in the app. Saved through Core.SaveConveyorConfig, which starts a new
    // calibration revision only when the pounds already recorded would change.
    // Refused while a job is running, so a job can't change conveyors halfway.
    public partial class frmMenuConveyor : Form
    {
        private bool _dragging;
        private Point _dragStart;
        private bool _padOpen;

        // Latest saved revision — kept so Save carries its zero and span forward
        private ConveyorConfig _cfg;

        // Values being edited, in stored units
        private double _inchesPerPulse;
        private int    _pulsesPerRev;
        private double _sectionLenIn;
        private double _minFlowLbS;
        private double _beltStopS;
        private int    _delaySec;

        // Live readout
        private readonly System.Windows.Forms.Timer _liveTimer = new System.Windows.Forms.Timer { Interval = 500 };
        private uint     _distanceStartPulses;
        private uint     _lastPulses;
        private DateTime _lastPulsesUtc;

        // Measure Belt
        private bool _measuring;
        private uint _measureStartPulses;

        private const double CM_PER_IN = 2.54;
        private const double IN_PER_FT = 12.0;
        private const double IN_PER_M  = 39.3700787;

        public frmMenuConveyor()
        {
            InitializeComponent();
        }

        private void frmMenuConveyor_Load(object sender, EventArgs e)
        {
            ApplyTheme();
            FormPositions.Restore(this);
            this.FormClosed += (s2, ev2) => FormPositions.Save(this);
            foreach (Control c in new Control[] { pnlTitle, lblTitle })
            {
                c.MouseDown += (s, ev) => { if (ev.Button == MouseButtons.Left) { _dragging = true; _dragStart = ev.Location; } };
                c.MouseMove += (s, ev) => { if (_dragging) { Left += ev.X - _dragStart.X; Top += ev.Y - _dragStart.Y; } };
                c.MouseUp   += (s, ev) => _dragging = false;
            }

            var profile = Core.Database.Profiles.GetById(Core.ActiveProfileId);
            if (profile != null) lblTitle.Text = $"{Lang.lgTitleConveyor} — {profile.Name}";

            _cfg = Core.Database.ConveyorConfigs.GetLatest(Core.ActiveProfileId)
                   ?? new ConveyorConfig { ProfileId = Core.ActiveProfileId };
            _inchesPerPulse = _cfg.InchesPerPulse;
            _pulsesPerRev   = _cfg.PulsesPerRev;
            _sectionLenIn   = _cfg.SectionLenIn;
            _minFlowLbS     = _cfg.FlowThresholdLbS;
            _beltStopS      = _cfg.BeltStopTimeoutS;
            _delaySec       = _cfg.DelaySec;

            _distanceStartPulses = _lastPulses = Core.LastCumPulses;
            _lastPulsesUtc = DateTime.UtcNow;

            ShowValues();
            UpdateLive();
            _liveTimer.Tick += (s, ev) => UpdateLive();
            _liveTimer.Start();
            this.FormClosed += (s, ev) => { _liveTimer.Stop(); _liveTimer.Dispose(); };
        }

        private void ApplyTheme()
        {
            var back = Properties.Settings.Default.MainBackColour;
            var fore = Properties.Settings.Default.MainForeColour;
            var ctrl = Color.FromArgb(60, 60, 60);
            pnlTitle.BackColor   = back;
            pnlContent.BackColor = back;
            lblTitle.ForeColor   = Color.FromArgb(180, 200, 220);
            foreach (Control c in pnlContent.Controls)
            {
                c.ForeColor = fore;
                if (c is Button b) { b.BackColor = ctrl; b.ForeColor = Color.White; }
            }
            foreach (var box in new[] { lblPulseVal, lblPprVal, lblSectionVal, lblMinFlowVal, lblStopVal, lblDelayVal })
            {
                box.BackColor = ctrl;
                box.ForeColor = fore;
            }
            btnSave.BackColor = Color.FromArgb(0, 90, 0);
        }

        // ── Units ─────────────────────────────────────────────────────────────
        // Lengths in inches or centimetres; the empty-belt threshold in the flow
        // unit the run screen shows (stored as lb/s).

        private static string LengthUnit => Props.IsMetric ? "cm" : "in";
        private static double InToDisplay(double inches) => Props.IsMetric ? inches * CM_PER_IN : inches;
        private static double InFromDisplay(double v)    => Props.IsMetric ? v / CM_PER_IN : v;

        private static string DistanceText(double inches) =>
            Props.IsMetric ? $"{inches / IN_PER_M:F1} m" : $"{inches / IN_PER_FT:F1} ft";

        private void ShowValues()
        {
            lblPulseVal.Text    = InToDisplay(_inchesPerPulse).ToString("F3");
            lblPulseUnit.Text   = LengthUnit;
            lblPprVal.Text      = _pulsesPerRev > 0 ? _pulsesPerRev.ToString() : "--";
            lblSectionVal.Text  = InToDisplay(_sectionLenIn).ToString("F1");
            lblSectionUnit.Text = LengthUnit;
            lblMinFlowVal.Text  = Props.DisplayFlow(_minFlowLbS * 60.0).ToString("F1");
            lblMinFlowUnit.Text = Props.FlowUnit;
            lblStopVal.Text     = _beltStopS.ToString("F1");
            lblDelayVal.Text    = _delaySec.ToString();
        }

        private void UpdateLive()
        {
            if (!Core.ModuleConnected)
            {
                lblLivePulses.Text     = "Pulses: --";
                lblLiveRate.Text       = "Rate: --";
                lblLiveBelt.Text       = "Belt: --";
                lblLiveDistance.Text   = "Distance: --";
                lblLiveState.Text      = "No module";
                lblLiveState.ForeColor = Color.Silver;
                return;
            }

            uint now = Core.LastCumPulses;
            DateTime t = DateTime.UtcNow;
            double dt = (t - _lastPulsesUtc).TotalSeconds;
            double rate = dt > 0 ? unchecked(now - _lastPulses) / dt : 0;
            _lastPulses = now;
            _lastPulsesUtc = t;

            uint since = unchecked(now - _distanceStartPulses);
            lblLivePulses.Text   = $"Pulses: {since}";
            lblLiveRate.Text     = $"Rate: {rate:F1} /s";
            lblLiveBelt.Text     = $"Belt: {Props.DisplayBeltSpeed(Core.Yield?.BeltFtPerMin ?? 0):F0} {Props.BeltSpeedUnit}";
            lblLiveDistance.Text = $"Distance: {DistanceText(since * _inchesPerPulse)}";

            bool running = Core.LastBeltRunning;
            lblLiveState.Text      = running ? "Belt running" : "Belt stopped";
            lblLiveState.ForeColor = running ? OkabeIto.BluishGreen : OkabeIto.Orange;

            if (_measuring)
                lblMeasure.Text = $"Measuring: {unchecked(now - _measureStartPulses)} pulses. Press Stop at the mark.";
        }

        // ── Editing ───────────────────────────────────────────────────────────

        private bool AskNumber(double min, double max, double current, int decimals, string title, out double value)
        {
            value = current;
            if (_padOpen) return false;
            _padOpen = true;
            try
            {
                using var pad = new frmNumpad(min, max, current, decimals, title);
                if (pad.ShowDialog(this) != DialogResult.OK) return false;
                value = Math.Min(max, Math.Max(min, pad.ReturnValue));
                return true;
            }
            finally { _padOpen = false; }
        }

        private void lblPulseVal_Click(object sender, EventArgs e)
        {
            double min = Props.IsMetric ? 0.03 : 0.01;
            double max = Props.IsMetric ? 127  : 50;
            if (AskNumber(min, max, Math.Round(InToDisplay(_inchesPerPulse), 3), 3, $"Belt per Pulse ({LengthUnit})", out double v))
            {
                _inchesPerPulse = InFromDisplay(v);
                ShowValues();
            }
        }

        private void lblPprVal_Click(object sender, EventArgs e)
        {
            if (AskNumber(0, 100000, _pulsesPerRev, 0, "Pulses per Belt Turn", out double v))
            {
                _pulsesPerRev = (int)Math.Round(v);
                ShowValues();
            }
        }

        private void lblSectionVal_Click(object sender, EventArgs e)
        {
            double min = Props.IsMetric ? 15  : 6;
            double max = Props.IsMetric ? 610 : 240;
            if (AskNumber(min, max, Math.Round(InToDisplay(_sectionLenIn), 1), 1, $"Weigh Section Length ({LengthUnit})", out double v))
            {
                _sectionLenIn = InFromDisplay(v);
                ShowValues();
            }
        }

        private void lblMinFlowVal_Click(object sender, EventArgs e)
        {
            if (AskNumber(0, 600, Math.Round(Props.DisplayFlow(_minFlowLbS * 60.0), 1), 1, $"Empty Belt Below ({Props.FlowUnit})", out double v))
            {
                _minFlowLbS = Props.LoadToLb(v) / 60.0;   // kg→lb in metric; lb/min → lb/s
                ShowValues();
            }
        }

        private void lblStopVal_Click(object sender, EventArgs e)
        {
            if (AskNumber(0.5, 25.5, _beltStopS, 1, "Belt Stopped After (s)", out double v))
            {
                _beltStopS = v;
                ShowValues();
            }
        }

        private void lblDelayVal_Click(object sender, EventArgs e)
        {
            if (AskNumber(0, 60, _delaySec, 0, "Dig to Scale Delay (s)", out double v))
            {
                _delaySec = (int)Math.Round(v);
                ShowValues();
            }
        }

        // ── Measure Belt ──────────────────────────────────────────────────────
        // Mark the belt, press Measure, run it one full turn back to the mark, press
        // Stop and enter the belt's length: belt per pulse = length ÷ pulses.

        private void btnMeasure_Click(object sender, EventArgs e)
        {
            if (!_measuring)
            {
                if (!Core.ModuleConnected)
                {
                    lblMeasure.Text = "No module connected — the pulses come from the module.";
                    return;
                }
                _measuring = true;
                _measureStartPulses = Core.LastCumPulses;
                btnMeasure.Text = Lang.lgStop;
                lblMeasure.Text = "Mark the belt, run it one full turn back to the mark, then press Stop.";
                return;
            }

            _measuring = false;
            btnMeasure.Text = Lang.lgMeasureBelt;
            uint pulses = unchecked(Core.LastCumPulses - _measureStartPulses);
            if (pulses == 0)
            {
                lblMeasure.Text = "No pulses counted — check the belt sensor.";
                return;
            }

            string unit = Props.IsMetric ? "m" : "ft";
            if (!AskNumber(0.5, Props.IsMetric ? 100 : 330, 0, 2, $"Belt Length ({unit}) for {pulses} pulses", out double len))
            {
                lblMeasure.Text = "";
                return;
            }

            double lengthIn = Props.IsMetric ? len * IN_PER_M : len * IN_PER_FT;
            _pulsesPerRev   = (int)pulses;
            _inchesPerPulse = lengthIn / pulses;
            lblMeasure.Text = $"{pulses} pulses over {len:F2} {unit} = {InToDisplay(_inchesPerPulse):F3} {LengthUnit} per pulse. Press Save to keep it.";
            ShowValues();
        }

        private void btnResetDist_Click(object sender, EventArgs e)
        {
            _distanceStartPulses = Core.LastCumPulses;
            UpdateLive();
        }

        // ── Save / Close ──────────────────────────────────────────────────────

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (Core.Collector.ActiveJobId > 0)
            {
                Props.ShowMessage(Lang.lgConveyorJobRunning, "", 3000, true);
                return;
            }

            var edited = new ConveyorConfig
            {
                ProfileId        = Core.ActiveProfileId,
                ZeroCounts       = _cfg.ZeroCounts,
                SpanLbPerCount   = _cfg.SpanLbPerCount,
                ZeroSetAt        = _cfg.ZeroSetAt,
                PulsesPerRev     = _pulsesPerRev,
                InchesPerPulse   = _inchesPerPulse,
                SectionLenIn     = _sectionLenIn,
                FlowThresholdLbS = _minFlowLbS,
                BeltStopTimeoutS = _beltStopS,
                DelaySec         = _delaySec
            };
            Core.SaveConveyorConfig(edited);
            _cfg = Core.Database.ConveyorConfigs.GetLatest(Core.ActiveProfileId) ?? edited;
            Props.ShowMessage(Lang.lgSettingsSaved);
        }

        private void btnClose_Click(object sender, EventArgs e) => this.Close();
    }
}
