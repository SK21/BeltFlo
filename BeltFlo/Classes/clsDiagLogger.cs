using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace BeltFlo.Classes
{
    /// <summary>
    /// Per-session diagnostic CSV, one row per conveyor packet (5 Hz), written
    /// from the moment the app starts and independent of whether a job is
    /// recording. Its purpose is to make a bad number explainable afterwards: a
    /// field total that disagrees with the ticket, a map with a cold streak, a
    /// job that stopped recording.
    ///
    /// Columns:
    ///   PCTime        - wall clock at the packet, ms
    ///   CumLb         - module's cumulative pounds counter, tenths (wraps)
    ///   CumPulses     - module's cumulative belt pulse counter (wraps)
    ///   ScaleLb       - live weigh-section load after zero, lb
    ///   ScaleRaw      - raw converter counts
    ///   Flags         - status byte, hex (bit0 ScaleOK, bit1 BeltRunning, bit2 Tared,
    ///                   bit3 CalMismatch, bit4 Overload)
    ///   LbPerSec      - differenced flow, lightly smoothed
    ///   BeltFtMin     - belt speed from differenced pulses
    ///   SpeedKmh      - ground speed
    ///   YieldRate     - instantaneous lb/ac from the last drained position
    ///   JobId, LoadId - what the pounds were being credited to; -1 = none
    ///   Sections      - AOG section state: 1 = crop entering the machine
    ///   PipelineCount - positions still waiting for their crop to reach the scale.
    ///                   Reaches 0 exactly when the app stops attributing flow to a
    ///                   finished pass; ScaleLb still high past that point means the
    ///                   digging-to-scale delay is set too short.
    ///   Tail          - 1 while the machine is still emptying out after a pass
    ///   ScaleOk       - the module's flag for this packet
    ///   ScaleFault    - 1 while the collector is refusing to record because of it
    ///   NewFrac       - fraction of the last swath that was new ground
    ///
    /// Reading the file: CumLb and CumPulses are counters, so difference them —
    /// summing across rows double-counts. A counter that jumps backwards by more
    /// than half its range was reset (module reboot); a link dropout leaves a
    /// gap in PCTime and a larger-than-usual step, not a reset.
    /// </summary>
    public class clsDiagLogger
    {
        private readonly object cLock = new object();
        private StreamWriter cWriter;
        private string cFilePath;
        private bool cRunning;

        // Keep roughly a working week of sessions. Small enough that nobody has to
        // manage the folder, long enough that a fault reported days later is still
        // on disk.
        private const int KeepFiles = 20;

        public string FilePath { get { return cFilePath; } }
        public bool IsRunning { get { return cRunning; } }

        public void Start()
        {
            lock (cLock)
            {
                if (cRunning) return;
                try
                {
                    string folder = Path.Combine(Props.DataFolder, "DiagLogs");
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                    Prune(folder);

                    cFilePath = Path.Combine(folder,
                        "Diag_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv");
                    cWriter = new StreamWriter(cFilePath, false) { AutoFlush = true };
                    cWriter.WriteLine("PCTime,CumLb,CumPulses,ScaleLb,ScaleRaw,Flags," +
                                      "LbPerSec,BeltFtMin,SpeedKmh,YieldRate,JobId,LoadId," +
                                      "Sections,PipelineCount,Tail,ScaleOk,ScaleFault,NewFrac");
                    cRunning = true;
                }
                catch (Exception ex)
                {
                    Props.WriteErrorLog("DiagLogger/Start " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Called from Core.ApplyConveyorCounters. Reads live Core state rather than
        /// taking arguments, so both the CAN and UDP paths log identical columns
        /// without either having to know what the other carries.
        /// </summary>
        public void Log()
        {
            lock (cLock)
            {
                if (!cRunning || cWriter == null) return;
                try
                {
                    var ci = CultureInfo.InvariantCulture;
                    cWriter.WriteLine(string.Join(",",
                        DateTime.Now.ToString("HH:mm:ss.fff", ci),
                        Core.LastCumPoundsX10.ToString(ci),
                        Core.LastCumPulses.ToString(ci),
                        Core.LastScaleLb.ToString("0.#", ci),
                        Core.LastScaleRaw.ToString(ci),
                        "0x" + Core.LastFlags.ToString("X2"),
                        (Core.Yield?.CurrentLbPerSec ?? 0).ToString("0.###", ci),
                        (Core.Yield?.BeltFtPerMin ?? 0).ToString("0.#", ci),
                        (Core.GPS?.Speed ?? 0).ToString("0.##", ci),
                        (Core.Yield?.InstantYield ?? 0).ToString("0.#", ci),
                        (Core.Collector?.ActiveJobId ?? -1).ToString(ci),
                        (Core.Collector?.ActiveLoadId ?? -1).ToString(ci),
                        // 1/0 rather than True/False — these are meant to be plotted
                        // against the flow in a spreadsheet.
                        ((Core.GPS?.SectionsActive ?? false) ? 1 : 0).ToString(ci),
                        (Core.Collector?.PipelineCount ?? -1).ToString(ci),
                        ((Core.Collector?.IsDrainingTail ?? false) ? 1 : 0).ToString(ci),
                        (Core.LastScaleOk ? 1 : 0).ToString(ci),
                        ((Core.Collector?.ScaleFault ?? false) ? 1 : 0).ToString(ci),
                        (Core.Collector?.LastNewFraction ?? 1.0).ToString("0.###", ci)));
                }
                catch (Exception ex)
                {
                    Props.WriteErrorLog("DiagLogger/Log " + ex.Message);
                    // Stop after a write failure rather than logging an error per
                    // packet — a full disk would otherwise flood Errors.log at 5 Hz.
                    cRunning = false;
                }
            }
        }

        public void Stop()
        {
            lock (cLock)
            {
                if (!cRunning) return;
                try
                {
                    cWriter?.Flush();
                    cWriter?.Dispose();
                }
                catch (Exception ex)
                {
                    Props.WriteErrorLog("DiagLogger/Stop " + ex.Message);
                }
                cWriter = null;
                cRunning = false;
            }
        }

        private void Prune(string folder)
        {
            try
            {
                var old = new DirectoryInfo(folder).GetFiles("Diag_*.csv")
                    .OrderByDescending(f => f.LastWriteTimeUtc)
                    .Skip(KeepFiles - 1);
                foreach (var f in old) f.Delete();
            }
            catch (Exception ex)
            {
                // Pruning is housekeeping — never let it stop a session recording.
                Props.WriteErrorLog("DiagLogger/Prune " + ex.Message);
            }
        }
    }
}
