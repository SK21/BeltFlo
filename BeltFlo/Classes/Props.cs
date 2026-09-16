using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace BeltFlo.Classes
{
    public enum CanDriver { SLCAN, InnoMaker, PCAN }

    public static class Props
    {
        public static readonly string AppName = "BeltFlo";
        public static readonly string AppVersion = "1.0.0";
        public static readonly string AppDate = "14-Sep-2026";

        private static string cApplicationFolder;
        private static string cDataFolder;
        private static string cLogsFolder;
        private static string cExportFolder;
        private static bool cCanEnabled = false;
        private static CanDriver cCurrentCanDriver = CanDriver.SLCAN;
        private static string cCanPort = "";

        public static bool CanEnabled
        {
            get { return cCanEnabled; }
            set { cCanEnabled = value; }
        }

        public static string Version { get { return AppVersion; } }

        public static CanDriver CurrentCanDriver
        {
            get { return cCurrentCanDriver; }
            set { cCurrentCanDriver = value; }
        }

        public static string CanPort
        {
            get { return cCanPort; }
            set { cCanPort = value; }
        }

        // ── Units ─────────────────────────────────────────────────────────────
        // Internal values are always pounds, acres and lb/ac. The scale delivers
        // pounds, so nothing stored depends on a crop constant; every other unit is
        // a display conversion applied on the way out.
        //
        // Imperial users choose between hundredweight and tons per acre — the two
        // units potato and beet tickets are written in. Metric is always t/ha.
        private const double LB_PER_CWT = 100.0;
        private const double LB_PER_TON = 2000.0;
        private const double LB_PER_KG  = 2.20462;
        private const double HA_PER_AC  = 0.404686;
        private const double M_PER_FT   = 0.3048;

        public static bool IsMetric => Properties.Settings.Default.Units == "Metric";
        public static bool YieldInTons => Properties.Settings.Default.YieldUnit == "tons/ac";

        public static string AreaUnit  => IsMetric ? "ha"   : "ac";
        public static string MassUnit  => IsMetric ? "t"    : (YieldInTons ? "tons"    : "cwt");
        public static string RateUnit  => IsMetric ? "t/ha" : (YieldInTons ? "tons/ac" : "cwt/ac");
        public static string LoadUnit  => IsMetric ? "kg"   : "lb";        // what the scale itself weighs in
        public static string FlowUnit  => IsMetric ? "kg/min" : "lb/min";
        public static string SpeedUnit => IsMetric ? "km/h" : "mph";
        public static string BeltSpeedUnit => IsMetric ? "m/min" : "ft/min";

        private static double LbPerMassUnit => IsMetric ? 1000.0 * LB_PER_KG : (YieldInTons ? LB_PER_TON : LB_PER_CWT);

        public static double DisplayArea(double acres)      => IsMetric ? acres * HA_PER_AC : acres;
        public static double DisplayMass(double lb)         => lb / LbPerMassUnit;
        public static double DisplayRate(double lbPerAc)    => IsMetric ? lbPerAc / LbPerMassUnit / HA_PER_AC : lbPerAc / LbPerMassUnit;
        public static double DisplayLoad(double lb)         => IsMetric ? lb / LB_PER_KG : lb;
        public static double DisplayFlow(double lbPerMin)   => IsMetric ? lbPerMin / LB_PER_KG : lbPerMin;
        public static double DisplaySpeed(double kmh)       => IsMetric ? kmh : kmh * 0.621371;
        public static double DisplayBeltSpeed(double ftMin) => IsMetric ? ftMin * M_PER_FT : ftMin;

        /// <summary>A weight typed in the display load unit, back to pounds.</summary>
        public static double LoadToLb(double display)       => IsMetric ? display * LB_PER_KG : display;

        /// <summary>A belt speed typed in the display unit, back to ft/min.</summary>
        public static double BeltSpeedToFtMin(double display) => IsMetric ? display / M_PER_FT : display;

        /// <summary>A flow typed in the display unit, back to lb/min.</summary>
        public static double FlowToLbMin(double display) => IsMetric ? display * LB_PER_KG : display;

        // ── Ticket weights ────────────────────────────────────────────────────
        // A truck load is read against a certified weigh ticket, so it follows the
        // unit those tickets are written in: pounds for potatoes, short tons for
        // sugar beet, kilograms in metric. That is the imperial yield unit's choice,
        // which is why this tracks YieldInTons while LoadUnit above does not — the
        // scale's own readings (the live section load, a known test weight, pounds
        // per count) stay in lb or kg, where a 20 lb test weight reading "0.01 tons"
        // would be useless.

        public static string TicketUnit => IsMetric ? "kg" : (YieldInTons ? "tons" : "lb");

        /// <summary>Decimals a ticket weight needs: tons want two, whole lb or kg none.</summary>
        public static int TicketDecimals => (!IsMetric && YieldInTons) ? 2 : 0;

        private static double LbPerTicketUnit =>
            IsMetric ? LB_PER_KG : (YieldInTons ? LB_PER_TON : 1.0);

        public static double DisplayTicket(double lb)  => lb / LbPerTicketUnit;

        /// <summary>A weight typed in the ticket unit, back to pounds.</summary>
        public static double TicketToLb(double display) => display * LbPerTicketUnit;

        /// <summary>A ticket weight ready to show: grouped thousands, the right decimals.</summary>
        public static string TicketText(double lb) =>
            DisplayTicket(lb).ToString("N" + TicketDecimals);

        /// <summary>As TicketText, but always signed — for a difference against a ticket.</summary>
        public static string TicketSigned(double lb)
        {
            string n = TicketDecimals > 0 ? "#,##0.00" : "#,##0";
            return DisplayTicket(lb).ToString($"+{n};-{n};0");
        }

        public static string ApplicationFolder { get { return cApplicationFolder; } }
        public static string DataFolder { get { return cDataFolder; } }
        public static string LogsFolder { get { return cLogsFolder; } }
        public static string ExportFolder { get { return cExportFolder; } }

        public static void CheckFolders()
        {
            cApplicationFolder = AppDomain.CurrentDomain.BaseDirectory;
            cDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                AppName);
            cLogsFolder = Path.Combine(cDataFolder, "Logs");
            cExportFolder = Path.Combine(cDataFolder, "Exports");

            Directory.CreateDirectory(cDataFolder);
            Directory.CreateDirectory(cLogsFolder);
            Directory.CreateDirectory(cExportFolder);
        }

        public static void WriteErrorLog(string message)
        {
            try
            {
                string path = Path.Combine(cLogsFolder ?? AppDomain.CurrentDomain.BaseDirectory, "Errors.log");
                File.AppendAllText(path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {message}{Environment.NewLine}");
            }
            catch { }
        }

        public static void WriteActivityLog(string message, bool newFile = false)
        {
            try
            {
                string path = Path.Combine(cLogsFolder ?? AppDomain.CurrentDomain.BaseDirectory, "Activity.log");
                if (newFile && File.Exists(path))
                    File.Delete(path);
                File.AppendAllText(path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {message}{Environment.NewLine}");
            }
            catch { }
        }

        public static void WriteLog(string fileName, string content, bool append = true, bool newFile = false)
        {
            try
            {
                string path = Path.Combine(cLogsFolder ?? AppDomain.CurrentDomain.BaseDirectory, fileName);
                if (newFile && File.Exists(path))
                    File.Delete(path);
                if (append)
                    File.AppendAllText(path, content);
                else
                    File.WriteAllText(path, content);
            }
            catch { }
        }

        public static void SaveFormLocation(Form frm)
        {
            try
            {
                Properties.Settings.Default[$"Form_{frm.Name}_X"] = frm.Location.X;
                Properties.Settings.Default[$"Form_{frm.Name}_Y"] = frm.Location.Y;
                Properties.Settings.Default.Save();
            }
            catch { }
        }

        public static void LoadFormLocation(Form frm)
        {
            try
            {
                object x = Properties.Settings.Default[$"Form_{frm.Name}_X"];
                object y = Properties.Settings.Default[$"Form_{frm.Name}_Y"];
                if (x != null && y != null)
                {
                    int px = Convert.ToInt32(x);
                    int py = Convert.ToInt32(y);
                    Rectangle screen = Screen.GetWorkingArea(frm);
                    if (screen.Contains(px, py))
                        frm.Location = new Point(px, py);
                }
            }
            catch { }
        }

        /// <summary>
        /// Shows a message across the run screen's status bar for durationMs, with a
        /// warning sound when isError. Carried a title and a topMost flag until the
        /// status bar replaced the message box; neither reached the screen, and every
        /// caller passed an empty title, so they are gone rather than left lying.
        /// </summary>
        public static void ShowMessage(string message, int durationMs = 3000, bool isError = false)
        {
            try
            {
                if (Core.MainForm == null || Core.MainForm.IsDisposed) return;

                if (isError)
                    System.Media.SystemSounds.Exclamation.Play();

                Core.MainForm.BeginInvoke((Action)(() =>
                {
                    Core.MainForm.ShowStatusMessage(message, isError, durationMs);
                }));
            }
            catch { }
        }

        public static void ApplyTheme(Control control)
        {
            control.BackColor = Properties.Settings.Default.MainBackColour;
            control.ForeColor = Properties.Settings.Default.MainForeColour;
            foreach (Control child in control.Controls)
                ApplyTheme(child);
        }
    }
}
