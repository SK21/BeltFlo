using System;
using System.Drawing;
using System.Windows.Forms;
using BeltFlo.Classes;
using BeltFlo.Language;

namespace BeltFlo.Forms
{
    // The run screen. The toolbar's ▶ ⏹ start and finish the truck load, and ⏸
    // stops all counting until ▶ — jobs are started and finished on the Jobs
    // screen, so a tap in the cab can never end a job by mistake. The tiles and
    // bars still use the grain app's layout: the second tile shows the open load,
    // the bars show flow and belt speed.
    public partial class frmMain : Form
    {
        // Status message fade timer
        private System.Windows.Forms.Timer _msgTimer;
        private int _msgCountdown;

        // Paused-with-sections-on alarm, counted in display ticks (1 s)
        private int _alarmTick;

        // Borderless drag
        private bool _dragging;
        private System.Drawing.Point _dragStart;

        // The load tile at its designed 32 pt fits six characters in 174 px, which
        // covers any truck ("47,500"). A seventh — a load left open far past a truck,
        // or a tank machine's running total — would be clipped at both ends by the
        // centred label, and a half-shown weight is worse than a smaller one.
        private static readonly Font LoadFontBig   = new Font("Microsoft Sans Serif", 32F, FontStyle.Bold);
        private static readonly Font LoadFontSmall = new Font("Microsoft Sans Serif", 26F, FontStyle.Bold);

        // Bar full scale. Per profile, set on the Conveyor Setup screen from what
        // the machine actually reaches; these are the fallbacks before a profile
        // has loaded.
        private const double DefaultMaxFlowLbPerMin = 3000.0;
        private const double DefaultMaxBeltFtPerMin = 400.0;

        private static double MaxFlowLbPerMin
        {
            get
            {
                double v = Core.ActiveConveyor?.BarMaxFlowLbMin ?? 0;
                return v > 0 ? v : DefaultMaxFlowLbPerMin;
            }
        }

        private static double MaxBeltFtPerMin
        {
            get
            {
                double v = Core.ActiveConveyor?.BarMaxBeltFtMin ?? 0;
                return v > 0 ? v : DefaultMaxBeltFtPerMin;
            }
        }

        public frmMain()
        {
            InitializeComponent();
        }

        // ── Startup / Shutdown ────────────────────────────────────────────────

        private void frmMain_Load(object sender, EventArgs e)
        {
            ApplyTheme();

            lblLoadTitle.Text = Lang.lgLoad.ToUpperInvariant();
            lblSensor1Title.Text  = Lang.lgFlow;
            lblSensor2Title.Text  = Lang.lgBelt;

            // No title bar and no title label to grab — wire dragging onto every
            // non-button surface (panels, labels) so the form is draggable from
            // anywhere except the toolbar's icon buttons.
            WireDragRecursive(this);

            RestorePosition();
            Core.Initialize(this);
            Core.UpdateDisplay += Core_UpdateDisplay;
            Core.JobStateChanged += Core_JobStateChanged;
            Core.ColorChanged += Core_ColorChanged;

            _msgTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _msgTimer.Tick += MsgTimer_Tick;

            UpdateStatusBar();
            SetLoadButtons();
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!Core.AppShutDown(e))
            {
                e.Cancel = true;
                return;
            }
            SavePosition();
        }

        private void SavePosition()
        {
            Properties.Settings.Default.MainFormX = this.Location.X;
            Properties.Settings.Default.MainFormY = this.Location.Y;
            Properties.Settings.Default.Save();
        }

        private void RestorePosition()
        {
            int x = Properties.Settings.Default.MainFormX;
            int y = Properties.Settings.Default.MainFormY;
            if (x < 0 || y < 0) return;

            var pt = new System.Drawing.Point(x, y);
            foreach (Screen s in Screen.AllScreens)
            {
                if (s.WorkingArea.Contains(pt))
                {
                    this.Location = pt;
                    return;
                }
            }
        }

        // ── Theme ─────────────────────────────────────────────────────────────

        private void ApplyTheme()
        {
            var back = Properties.Settings.Default.MainBackColour;
            var fore = Properties.Settings.Default.MainForeColour;
            var dispBack = Properties.Settings.Default.DisplayBackColour;
            var dispFore = Properties.Settings.Default.DisplayForeColour;

            // this.BackColor stays White — it shows through the 2px Padding as the border
            pnlToolbar.BackColor = back;
            pnlGauges.BackColor = back;
            pnlTotals.BackColor = back;
            pnlSensors.BackColor = back;

            lblYield.BackColor = dispBack;
            lblYield.ForeColor = dispFore;
            lblLoad.BackColor = dispBack;
            lblLoad.ForeColor = dispFore;

            lblYieldTitle.ForeColor = fore;
            lblLoadTitle.ForeColor = fore;
            lblTotArea.ForeColor = fore;
            lblTotTotal.ForeColor = fore;
            lblTotRate.ForeColor = fore;
            lblLoadUnit.ForeColor = System.Drawing.Color.White;

            // Dark tracks: a green track read as a full bar when the value was zero.
            pnlSensor1.BackColor = Color.FromArgb(50, 50, 50);
            pnlSensor2.BackColor = Color.FromArgb(50, 50, 50);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // Form BackColor = White shows through the 2px Padding as the border.
            // No drawing needed here — child controls can't paint outside Padding.
        }

        // ── Drag (no title bar) ───────────────────────────────────────────────
        private void Drag_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _dragging = true;
                _dragStart = e.Location;
            }
        }

        private void Drag_MouseMove(object sender, MouseEventArgs e)
        {
            if (_dragging)
            {
                this.Left += e.X - _dragStart.X;
                this.Top += e.Y - _dragStart.Y;
            }
        }

        private void Drag_MouseUp(object sender, MouseEventArgs e)
        {
            _dragging = false;
        }

        // Wires drag onto every descendant control except Buttons (the toolbar's
        // icon buttons need their Click behaviour, not drag). Recurses into panels
        // so labels/bars nested several levels deep are draggable too.
        private void WireDragRecursive(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (!(c is Button))
                {
                    c.MouseDown += Drag_MouseDown;
                    c.MouseMove += Drag_MouseMove;
                    c.MouseUp   += Drag_MouseUp;
                }
                if (c.HasChildren) WireDragRecursive(c);
            }
        }

        // ── Core events ───────────────────────────────────────────────────────

        private void Core_UpdateDisplay(object sender, EventArgs e)
        {
            UpdateGauges();
            UpdateStatusBar();
            CheckModuleTimeout();
            SoundAlarms();
        }

        // Alarms that repeat until the operator acts: sound and show every other
        // second.
        //   Paused with sections coming on (auto-resume off): crop is probably going
        //   over the scale uncounted — until ▶ or sections go off. SectionsActive
        //   goes stale 4 s after AOG stops sending, so closing AOG silences it too.
        //   No load open while crop crosses a scale that weighs into the truck —
        //   until ▶ opens a load, the flow stops, or ⏸. Silent without a module.
        private void SoundAlarms()
        {
            var col = Core.Collector;
            string message = null;
            if (col != null && col.IsPaused && col.PausedSectionsAlarm && Core.GPS.SectionsActive)
                message = Lang.lgPausedSectionsOn;
            else if (col != null && col.NoLoadAlarm && Core.ModuleConnected)
                message = Lang.lgNoLoadOpen;

            if (message == null)
            {
                _alarmTick = 0;
                // The operator has dealt with it, so take the message down now. Left
                // to expire on its own it would sit there for the rest of its ten
                // seconds, reading as an alarm that is still sounding. Judged from
                // the label rather than a flag, because ShowMessage posts the text
                // asynchronously: a repeat already in flight when the alarm stopped
                // would otherwise arrive after the clear and linger unnoticed.
                ClearAlarmMessage();
                return;
            }
            if (_alarmTick++ % 2 == 0)
                Props.ShowMessage(message, 3000, true);
        }

        /// <summary>Takes a repeating alarm's message down at once, rather than at its timeout.</summary>
        private void ClearAlarmMessage()
        {
            if (!lblStatusMsg.Visible) return;
            if (lblStatusMsg.Text != Lang.lgPausedSectionsOn &&
                lblStatusMsg.Text != Lang.lgNoLoadOpen) return;

            _msgCountdown = 0;
            _msgTimer?.Stop();
            lblStatusMsg.Visible = false;
            lblStatusMsg.Text = "";
        }

        private void Core_JobStateChanged(object sender, EventArgs e)
        {
            SetLoadButtons();
            UpdateStatusBar();
            UpdateGauges();
        }

        private void Core_ColorChanged(object sender, EventArgs e)
        {
            ApplyTheme();
            UpdateGauges();
            this.Invalidate();
        }

        // ── Gauge updates ─────────────────────────────────────────────────────

        private void UpdateGauges()
        {
            if (Core.IsShuttingDown) return;

            var col = Core.Collector;
            var yieldCalc = Core.Yield;

            // Numbers here use N rather than F, so thousands are grouped: a load is
            // read at a glance across the cab, and 47500 is easy to take for 4750.
            // N follows the running culture, so the separator is whatever the
            // operator's language uses.
            double yield = Props.DisplayRate(yieldCalc?.SmoothedYield ?? 0);
            lblYield.Text = yield.ToString("N1");
            lblYieldUnit.Text = Props.RateUnit;

            // Second tile — the open load, the number the operator watches to know
            // when the truck is full. Blank between loads. While paused it says so
            // in place of its unit, in the pause button's colour, so a pause left on
            // by mistake is hard to miss.
            bool hasLoad = col != null && col.ActiveLoadId > 0;
            bool paused  = col != null && col.IsPaused;
            string loadWord = Lang.lgLoad.ToUpperInvariant();
            lblLoadTitle.Text = hasLoad ? loadWord + " " + col.ActiveLoadNumber : loadWord;
            lblLoad.Text = hasLoad ? Props.TicketText(col.CurrentLoadLb) : "--";

            var loadFont = lblLoad.Text.Length > 6 ? LoadFontSmall : LoadFontBig;
            if (!ReferenceEquals(lblLoad.Font, loadFont)) lblLoad.Font = loadFont;

            lblLoad.ForeColor = paused ? OkabeIto.Orange : Properties.Settings.Default.DisplayForeColour;
            lblLoadUnit.Text = paused ? Lang.lgPause.ToUpperInvariant() : Props.TicketUnit;
            lblLoadUnit.ForeColor = paused ? OkabeIto.Orange : Color.White;

            // Bar 1 — flow over the scale
            double flowLbMin = (yieldCalc?.CurrentLbPerSec ?? 0) * 60.0;
            double flow = Math.Min(1.0, Math.Max(0, flowLbMin / MaxFlowLbPerMin));
            pnlSensor1Fill.Width = (int)(pnlSensor1.Width * flow);
            lblSensor1Value.Text = $"{Props.DisplayFlow(flowLbMin):N0} {Props.FlowUnit}";

            // Bar 2 — belt speed
            double beltFtMin = yieldCalc?.BeltFtPerMin ?? 0;
            double belt = Math.Min(1.0, Math.Max(0, beltFtMin / MaxBeltFtPerMin));
            pnlSensor2Fill.Width = (int)(pnlSensor2.Width * belt);
            lblSensor2Value.Text = $"{Props.DisplayBeltSpeed(beltFtMin):N0} {Props.BeltSpeedUnit}";
            pnlSensor2Fill.BackColor = Core.LastBeltRunning
                ? Color.FromArgb(50, 150, 220)
                : Color.FromArgb(80, 80, 80);

            // The fourth cell, under the average yield, is deliberately empty. It used
            // to repeat the flow already on the Flow bar below it, and area per hour
            // — the obvious replacement — is on the AgOpenGPS screen beside this one.
            double area = Props.DisplayArea(col?.TotalAcres ?? 0);
            double total = Props.DisplayMass(col?.TotalPounds ?? 0);
            double avg = Props.DisplayRate(col?.AverageYield ?? 0);

            lblTotArea.Text = $"{area:N1} {Props.AreaUnit}";
            lblTotTotal.Text = $"{total:N1} {Props.MassUnit}";
            lblTotRate.Text = $"{avg:N1} {Props.RateUnit}";
        }

        // Okabe-Ito colorblind-safe palette: bluish-green / vermillion instead of
        // plain LimeGreen / Red, which are hard to tell apart under red-green color
        // vision deficiency (the most common type). Separated by hue AND luminance
        // so the difference survives protanopia/deuteranopia/tritanopia.
        private static readonly Color StatusOk  = OkabeIto.BluishGreen;
        private static readonly Color StatusBad = OkabeIto.Vermillion;

        private void UpdateStatusBar()
        {
            bool gpsOk = Core.GPS.IsConnected;
            bool modOk = Core.ModuleConnected
                && (DateTime.UtcNow - Core.LastModuleReceive).TotalSeconds < 5;

            lblStatusGPS.Text = Lang.lgGPS;
            lblStatusGPS.ForeColor = gpsOk ? StatusOk : StatusBad;

            // Green only when the link works both ways, as in RateController: packets
            // arriving, and the module saying it is receiving the settings. Orange
            // when it sends but doesn't hear the PC.
            lblStatusModule.Text = Lang.lgModule;
            lblStatusModule.ForeColor = !modOk ? StatusBad
                                      : Core.ModuleReceiving ? StatusOk
                                      : OkabeIto.Orange;

            // Scale — the module's verdict on its converter and cells, plus the one
            // belt-sensor fault the app can infer for itself. The module field
            // reports the link only; this reports whether what comes over the link
            // can be believed. Worst first, so the label always names the thing
            // most in need of attention.
            //
            // Unlike the grain app's flow sensor this is not gated on harvesting: a
            // converter that has stopped answering, or cells at their limit, are
            // hardware faults whether or not the machine is moving, and worth
            // catching in the yard.
            //
            // The verdict needs a recent status packet, not merely a live link: the
            // flags ride the status packet alone, and on CAN that is a separate frame
            // from the counters, so counters by themselves could otherwise hold the
            // label at its last reading. Without one there is nothing to report and
            // the label goes altogether, rather than sitting there greyed — the
            // Module label beside it already says why.
            bool scaleKnown = modOk
                && (DateTime.UtcNow - Core.LastStatusReceive).TotalSeconds < 5;

            lblStatusScale.Visible = scaleKnown;

            if (scaleKnown)
            {
                if (!Core.LastScaleOk || Core.LastOverload)
                {
                    lblStatusScale.Text = Lang.lgStatusScale;
                    lblStatusScale.ForeColor = StatusBad;
                }
                else if (Core.BeltSensorSuspect)
                {
                    // Weight moving over the section with no belt pulses: the module
                    // multiplies weight by belt travel, so it is recording nothing.
                    lblStatusScale.Text = Lang.lgBelt;
                    lblStatusScale.ForeColor = OkabeIto.Orange;
                }
                else if (!Core.LastTared)
                {
                    lblStatusScale.Text = Lang.lgStatusZero;
                    lblStatusScale.ForeColor = OkabeIto.Orange;
                }
                else
                {
                    lblStatusScale.Text = Lang.lgStatusScale;
                    lblStatusScale.ForeColor = StatusOk;
                }
            }

            if (Core.Collector.ActiveJobId > 0)
            {
                bool recording = Core.Collector.IsRecording;
                string jobName = Core.Collector.ActiveJobName.Length > 0 ? Core.Collector.ActiveJobName : "Active Job";
                if (recording && !Core.LastDataWriteOk)
                {
                    lblStatusJob.Text = jobName + Lang.lgDataWriteError;
                    lblStatusJob.ForeColor = StatusBad;
                }
                else if (Core.Collector.IsPaused)
                {
                    lblStatusJob.Text = jobName + Lang.lgJobStatusPaused;
                    lblStatusJob.ForeColor = OkabeIto.Orange;
                }
                else
                {
                    lblStatusJob.Text = recording ? jobName + Lang.lgJobStatusOn : jobName + Lang.lgJobStatusOff;
                    lblStatusJob.ForeColor = recording ? StatusOk : OkabeIto.Orange;
                }
            }
            else
            {
                lblStatusJob.Text = Lang.lgNoActiveJob;
                lblStatusJob.ForeColor = Color.Silver;
            }
        }

        private void CheckModuleTimeout()
        {
            if (Core.ModuleConnected &&
                (DateTime.UtcNow - Core.LastModuleReceive).TotalSeconds > 5)
            {
                Core.ModuleConnected = false;
                // Module-reported state does not outlive the module.
                Core.ClearModuleReportedState();
            }
        }

        // ── Toolbar buttons ───────────────────────────────────────────────────

        private void btnMenu_Click(object sender, EventArgs e)
        {
            FormManager.ShowForm(new frmMenu());
        }

        // ▶ — resume counting after a pause, or start a load.
        private void btnStart_Click(object sender, EventArgs e)
        {
            var col = Core.Collector;
            if (col.ActiveJobId <= 0)
            {
                Props.ShowMessage(Lang.lgNoActiveJob, 3000, true);
                return;
            }

            if (col.IsPaused)
            {
                col.ResumeJob();
                Props.ShowMessage(Lang.lgResumed, 2000);
            }
            else if (col.ActiveLoadId <= 0)
            {
                col.StartLoad();
                // Opening a load is the answer to "No load open" — clear it on the tap
                // rather than waiting for the next counter packet and display tick.
                ClearAlarmMessage();
            }
            SetLoadButtons();
        }

        // ⏸ — stop counting altogether: no weight to the job or the load and no
        // map points, for cleaning the belt, clearing a jam or dumping on purpose.
        // Allowed with or without an open load.
        private void btnPause_Click(object sender, EventArgs e)
        {
            Core.Collector.PauseJob();
            Props.ShowMessage(Lang.lgPaused, 3000);
            SetLoadButtons();
        }

        // ⏹ — the truck has left. Confirmed, so a stray tap doesn't split a load;
        // the Loads screen can reopen it if the truck comes back.
        private void btnStop_Click(object sender, EventArgs e)
        {
            using var dlg = new frmMsgBox(Lang.lgFinishLoadPrompt);
            dlg.ShowDialog(this);
            if (!dlg.Result) return;

            Core.Collector.FinishLoad();
            SetLoadButtons();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Core.RequestUserExit();
        }

        private void btnMini_Click(object sender, EventArgs e)
        {
            this.Hide();
            new frmMini().Show();
        }

        // The LOAD tile title is a shortcut to the loads list, as on
        // RateController's run screen.
        private void lblLoadTitle_Click(object sender, EventArgs e) => FormManager.ShowForm(new frmMenuLoads());

        // Full-strength colours for each load button when it is available.
        // Okabe-Ito: same hues used for the status-bar labels, so "good/active",
        // "warning/paused" and "bad/stopped" mean the same color everywhere.
        private static readonly Color StartActive = OkabeIto.BluishGreen;
        private static readonly Color PauseActive = OkabeIto.Orange;
        private static readonly Color StopActive  = OkabeIto.Vermillion;
        // Shared "unavailable" look — clearly off, not just a dimmed word.
        private static readonly Color BtnOffBack   = Color.FromArgb(40, 40, 40);
        private static readonly Color BtnOffFore   = Color.FromArgb(95, 95, 95);
        private static readonly Color BtnOffBorder = Color.FromArgb(70, 70, 70);

        private void SetLoadButtons()
        {
            // Derived from the load's lifecycle, not from whether the job happens to
            // be recording this second — recording toggles with AOG sections on every
            // headland turn, and the buttons must not flicker with it.
            var col = Core.Collector;
            bool hasJob  = col != null && col.ActiveJobId > 0;
            bool hasLoad = hasJob && col.ActiveLoadId > 0;
            bool paused  = hasJob && col.IsPaused;

            // All three are dark with no job: the status bar already says so, and
            // jobs are started on the Jobs screen.
            btnStart.Enabled = hasJob && (paused || !hasLoad);   // resume, or new load
            btnPause.Enabled = hasJob && !paused;
            btnStop.Enabled  = hasLoad;

            StyleButton(btnStart, StartActive);
            StyleButton(btnPause, PauseActive);
            StyleButton(btnStop,  StopActive);
        }

        // Make availability obvious at a glance: an enabled button shows its
        // full colour with a bright border; a disabled one goes flat dark grey
        // so it plainly reads as "off" rather than a slightly greyed word.
        private static void StyleButton(Button btn, Color activeBack)
        {
            if (btn.Enabled)
            {
                btn.BackColor = activeBack;
                btn.ForeColor = Color.White;
                btn.FlatAppearance.BorderColor = Color.FromArgb(230, 230, 230);
                btn.FlatAppearance.BorderSize  = 2;
            }
            else
            {
                btn.BackColor = BtnOffBack;
                btn.ForeColor = BtnOffFore;
                btn.FlatAppearance.BorderColor = BtnOffBorder;
                btn.FlatAppearance.BorderSize  = 1;
            }
        }

        // ── Status message ────────────────────────────────────────────────────

        public void ShowStatusMessage(string message, bool isError, int durationMs = 3000)
        {
            lblStatusMsg.BackColor = pnlStatus.BackColor;
            lblStatusMsg.Text = message;
            lblStatusMsg.ForeColor = isError ? Color.Red : Color.Yellow;
            lblStatusMsg.Visible = true;
            lblStatusMsg.BringToFront();
            // The timer ticks once a second, so the duration rounds up to whole
            // seconds, and never to none.
            _msgCountdown = Math.Max(1, (int)Math.Ceiling(durationMs / 1000.0));
            _msgTimer.Start();
        }

        private void MsgTimer_Tick(object sender, EventArgs e)
        {
            _msgCountdown--;
            if (_msgCountdown <= 0)
            {
                lblStatusMsg.Visible = false;
                lblStatusMsg.Text = "";
                _msgTimer.Stop();
            }
        }
    }
}
