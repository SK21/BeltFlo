using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;

namespace ModuleSimulator
{
    /// <summary>
    /// Simulates a BeltFlo conveyor module — sends the conveyor packet (PGN 40010,
    /// 5 Hz) over UDP to the BeltFlo PC app on port 30300.
    ///
    /// The sliders set what a real machine would present to the scale: the load
    /// sitting on the weighed section and the belt speed. The module's job is to
    /// integrate load against belt travel, and that is done here the same way:
    /// pounds per inch of belt × inches of belt that passed this tick.
    /// </summary>
    public partial class frmSimulator : Form
    {
        private const int PC_RECV_PORT  = 30300;
        private const int SIM_SEND_PORT = 30301;

        // Geometry — must match the PC app's default conveyor configuration so the
        // belt speed it derives from pulses agrees with the slider.
        private const double InchesPerPulse = 1.0;
        private const double SectionLenIn   = 36.0;

        // The calibration revision the simulated module claims to be running. The
        // PC app seeds each profile with one conveyor_config row, so 1 matches a
        // fresh database.
        private const byte CalRev = 1;

        private System.Windows.Forms.Timer _sendTimer;
        private UdpClient  _udp;
        private IPEndPoint _target;
        private double _simAngle = 0;
        private int    _ticks    = 0;

        // Cumulative counters, kept as doubles so fractional pounds and pulses
        // carry over between ticks, and truncated to uint32 on the wire so they
        // wrap the way the module's will.
        private double _cumLb     = 0;
        private double _cumPulses = 0;
        private double _lbPerSec  = 0;

        public frmSimulator()
        {
            InitializeComponent();
        }

        private static readonly string _posFile =
            Path.Combine(Application.LocalUserAppDataPath, "simpos.txt");

        private void frmSimulator_Load(object sender, EventArgs e)
        {
            RestorePosition();

            try
            {
                _udp    = new UdpClient(SIM_SEND_PORT);
                _target = new IPEndPoint(IPAddress.Loopback, PC_RECV_PORT);
            }
            catch (Exception ex)
            {
                lblStatus.Text = "UDP Error: " + ex.Message;
                return;
            }

            _sendTimer = new System.Windows.Forms.Timer { Interval = 100 };  // 10 Hz integration, 5 Hz send
            _sendTimer.Tick += SendTimer_Tick;
            _sendTimer.Start();
            lblStatus.Text = "Sending to 127.0.0.1:" + PC_RECV_PORT;
        }

        private void RestorePosition()
        {
            try
            {
                if (!File.Exists(_posFile)) return;
                var parts = File.ReadAllText(_posFile).Split(',');
                if (parts.Length == 2 && int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y))
                {
                    var pt = new System.Drawing.Point(x, y);
                    foreach (Screen s in Screen.AllScreens)
                        if (s.WorkingArea.Contains(pt)) { Location = pt; return; }
                }
            }
            catch { }
        }

        private void SavePosition()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_posFile));
                File.WriteAllText(_posFile, $"{Location.X},{Location.Y}");
            }
            catch { }
        }

        private void chkSections_CheckedChanged(object sender, EventArgs e)
        {
            chkSections.ForeColor = chkSections.Checked
                ? System.Drawing.Color.DarkGreen
                : System.Drawing.Color.Red;
        }

        private void SendTimer_Tick(object sender, EventArgs e)
        {
            // A module that has lost power or its wiring sends nothing at all. The
            // PC app has no packet to read a fault out of — it times out after 5 s
            // of silence, which is the only path to a red Module label.
            if (chkModuleOffline.Checked)
            {
                lblFlow.Text        = "Flow: --";
                lblStatus.Text      = "Module offline — sending nothing";
                lblStatus.ForeColor = System.Drawing.Color.Red;
                return;
            }

            _simAngle += 0.05;
            _ticks++;

            double loadLb    = trkLoad.Value / 10.0;      // 0.0 – 50.0 lb on the section
            double beltFtMin = trkBelt.Value;             // 0 – 300 ft/min
            double variation = trkVariation.Value / 100.0;

            lblVariationSlider.Text = $"Variation: {trkVariation.Value}%";

            // Nothing on the belt unless the digger is in the ground.
            if (!chkSections.Checked) loadLb = 0;
            if (chkBeltStopped.Checked)
            {
                // Crop left sitting on a stopped belt: the section reads steady.
                beltFtMin = 0;
            }
            else
            {
                if (chkSineWave.Checked) loadLb *= 1.0 + variation * Math.Sin(_simAngle);
                // Crop moving over the section is lumpy, and that lumpiness is what
                // the PC app uses to tell a running belt from a stopped one when no
                // pulses arrive.
                loadLb *= 1.0 + 0.15 * Math.Sin(_simAngle * 9.7) + 0.08 * Math.Sin(_simAngle * 23.3);
            }

            // Integrate. lb per inch of belt × inches that passed this tick. With a
            // dead belt sensor the belt still moves but the module hears no pulses,
            // so it integrates nothing — the failure the app's Belt warning exists for.
            const double dt = 0.1;
            double beltInPerSec   = beltFtMin * 12.0 / 60.0;
            double sensedInPerSec = chkBeltSensorDead.Checked ? 0 : beltInPerSec;
            _lbPerSec   = loadLb / SectionLenIn * sensedInPerSec;
            _cumLb     += _lbPerSec * dt;
            _cumPulses += sensedInPerSec * dt / InchesPerPulse;

            if (_ticks % 2 == 0)
            {
                // bit0 ScaleOK, bit1 BeltRunning, bit2 Tared. Clearing bit0 is the
                // module diagnosing its own converter or cells, which the PC app
                // acts on immediately.
                byte flags = 0;
                if (!chkNotZeroed.Checked)  flags |= 0x04;
                if (!chkScaleFault.Checked) flags |= 0x01;
                if (sensedInPerSec > 0)     flags |= 0x02;

                uint cumLbX10  = unchecked((uint)(long)(_cumLb * 10.0));
                uint cumPulses = unchecked((uint)(long)_cumPulses);
                short scaleX10 = (short)Math.Max(-32768, Math.Min(32767, loadLb * 10.0));
                int scaleRaw   = 100000 + (int)(loadLb * 2000.0);   // arbitrary counts, for the calibration screen

                SendConveyorPacket(flags, cumLbX10, cumPulses, scaleX10, scaleRaw);
            }

            // Update UI labels
            lblFlow.Text   = $"Flow: {_lbPerSec:F2} lb/s  ({_lbPerSec * 60:F0} lb/min)";
            lblTotal.Text  = $"Total: {_cumLb:F1} lb";
            lblPulses.Text = $"Pulses: {(long)_cumPulses}  ({beltFtMin:F0} ft/min)";

            if (chkScaleFault.Checked)
            {
                lblStatus.Text      = "Sending ScaleOK = 0 — app should show NO SCALE now";
                lblStatus.ForeColor = System.Drawing.Color.DarkOrange;
            }
            else if (chkBeltSensorDead.Checked)
            {
                lblStatus.Text      = "Belt sensor dead — app shows Belt after 3 s with crop on";
                lblStatus.ForeColor = System.Drawing.Color.DarkOrange;
            }
            else if (chkBeltStopped.Checked)
            {
                lblStatus.Text      = "Belt stopped — pulses and pounds frozen";
                lblStatus.ForeColor = System.Drawing.Color.DarkOrange;
            }
            else if (chkNotZeroed.Checked)
            {
                lblStatus.Text      = "Not zeroed — app shows Zero";
                lblStatus.ForeColor = System.Drawing.Color.DarkOrange;
            }
            else
            {
                lblStatus.Text      = "Sending to 127.0.0.1:" + PC_RECV_PORT;
                lblStatus.ForeColor = System.Drawing.Color.DarkGreen;
            }
        }

        private void SendConveyorPacket(byte flags, uint cumLbX10, uint cumPulses, short scaleX10, int scaleRaw)
        {
            // Conveyor packet — 19 bytes:
            // [0-1]   PGN 40010 LE (0x4A 0x9C)
            // [2]     flags  bit0=ScaleOK, bit1=BeltRunning, bit2=Tared, bit3=CalMismatch, bit4=Overload
            // [3-6]   cum_pounds_x10  uint32 LE
            // [7-10]  cum_pulses      uint32 LE
            // [11-12] scale_lb_x10    int16 LE
            // [13-16] scale_raw       int32 LE
            // [17]    cal_rev         uint8
            // [18]    CRC8 — byte sum of everything before it
            byte[] pkt = new byte[19];
            pkt[0] = 0x4A;
            pkt[1] = 0x9C;
            pkt[2] = flags;
            Array.Copy(BitConverter.GetBytes(cumLbX10),  0, pkt, 3,  4);
            Array.Copy(BitConverter.GetBytes(cumPulses), 0, pkt, 7,  4);
            Array.Copy(BitConverter.GetBytes(scaleX10),  0, pkt, 11, 2);
            Array.Copy(BitConverter.GetBytes(scaleRaw),  0, pkt, 13, 4);
            pkt[17] = CalRev;

            int ck = 0;
            for (int i = 0; i < 18; i++) ck += pkt[i];
            pkt[18] = (byte)ck;

            try { _udp.Send(pkt, pkt.Length, _target); } catch { }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            SavePosition();
            _sendTimer?.Stop();
            _udp?.Close();
            base.OnFormClosed(e);
        }
    }
}
