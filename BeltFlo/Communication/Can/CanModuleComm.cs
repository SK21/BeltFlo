using System;
using System.Timers;
using BeltFlo.Classes;

namespace BeltFlo.Communication.Can
{
    /// <summary>
    /// Manages CAN communication with the BeltFlo conveyor module.
    /// Receive-only. The 19-byte UDP packet does not fit one CAN frame, so the
    /// module sends it as two: counters and status. Both are extended IDs
    /// (Priority=6, PF=0xFF ProprietaryB, SA=0xF8); 0x18FF00F8 and 0x18FF01F8
    /// stay with the grain module so the two can share a bus without confusion.
    /// </summary>
    public class CanModuleComm : IDisposable
    {
        private const uint CountersFrameId = 0x18FF02F8u;
        private const uint StatusFrameId   = 0x18FF03F8u;
        private const int AdapterTimeoutMs = 4000;
        private const int ModuleTimeoutMs = 2000;

        private ICanInterface _driver;
        private Timer _timeoutTimer;
        private DateTime _lastFrameAny = DateTime.MinValue;
        private DateTime _lastModuleFrame = DateTime.MinValue;

        /// <summary>True if the CAN adapter is open and any frame was received within 4 s.</summary>
        public bool AdapterConnected =>
            _driver != null && _driver.IsOpen &&
            (DateTime.UtcNow - _lastFrameAny).TotalMilliseconds < AdapterTimeoutMs;

        /// <summary>True if a counters frame was received within 2 s.</summary>
        public bool ModuleReceiving =>
            (DateTime.UtcNow - _lastModuleFrame).TotalMilliseconds < ModuleTimeoutMs;

        public bool Start(CanDriver driver, string port)
        {
            Stop();

            switch (driver)
            {
                case CanDriver.InnoMaker: _driver = new InnoMakerInterface(); break;
                case CanDriver.PCAN: _driver = new PcanInterface(); break;
                default: _driver = new SlcanInterface(); break;
            }

            _driver.FrameReceived += OnFrameReceived;

            if (!_driver.Open(port, 250000))
            {
                _driver.FrameReceived -= OnFrameReceived;
                _driver.Dispose();
                _driver = null;
                return false;
            }

            _timeoutTimer = new Timer(500) { AutoReset = true };
            _timeoutTimer.Elapsed += OnTimerElapsed;
            _timeoutTimer.Start();
            return true;
        }

        public void Stop()
        {
            _timeoutTimer?.Stop();
            _timeoutTimer?.Dispose();
            _timeoutTimer = null;

            if (_driver != null)
            {
                _driver.FrameReceived -= OnFrameReceived;
                _driver.Dispose();
                _driver = null;
            }
        }

        private void OnFrameReceived(object sender, CanFrameEventArgs e)
        {
            _lastFrameAny = DateTime.UtcNow;

            if (e.Frame.Data == null || e.Frame.Dlc != 8) return;

            var mf = Core.MainForm;
            if (mf == null || !mf.IsHandleCreated || mf.IsDisposed || Core.IsShuttingDown) return;

            if (e.Frame.Id == CountersFrameId)
            {
                _lastModuleFrame = DateTime.UtcNow;
                byte[] data = e.Frame.Data;
                try { mf.BeginInvoke((Action)(() => ParseCounters(data))); }
                catch (InvalidOperationException) { }
            }
            else if (e.Frame.Id == StatusFrameId)
            {
                byte[] data = e.Frame.Data;
                try { mf.BeginInvoke((Action)(() => ParseStatus(data))); }
                catch (InvalidOperationException) { }
            }
        }

        private void ParseCounters(byte[] d)
        {
            // Counters frame (0x18FF02F8), DLC=8 — same fields as UDP bytes [3-10]:
            // [0-3] cum_pounds_x10  uint32 LE  delivered weight, tenths of a pound; wraps
            // [4-7] cum_pulses      uint32 LE  belt pulses; wraps
            uint cumLbX10  = BitConverter.ToUInt32(d, 0);
            uint cumPulses = BitConverter.ToUInt32(d, 4);
            Core.ApplyConveyorCounters(cumLbX10, cumPulses);
        }

        private void ParseStatus(byte[] d)
        {
            // Status frame (0x18FF03F8), DLC=8 — same fields as UDP bytes [2] and [11-17]:
            // [0]   flags  bit0=ScaleOK, bit1=BeltRunning, bit2=Tared, bit3=CalMismatch, bit4=Overload
            // [1-2] scale_lb_x10  int16 LE   live weigh-section load after zero, tenths
            // [3-6] scale_raw     int32 LE   raw converter counts
            // [7]   cal_rev       uint8
            byte flags     = d[0];
            short scaleX10 = BitConverter.ToInt16(d, 1);
            int scaleRaw   = BitConverter.ToInt32(d, 3);
            int calRev     = d[7];
            Core.ApplyConveyorStatus(flags, scaleX10 / 10.0, scaleRaw, calRev);
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (!ModuleReceiving && Core.ModuleConnected)
            {
                Core.ModuleConnected = false;
                // Module-reported state does not outlive the module — see the
                // matching clear in frmMain.CheckModuleTimeout.
                Core.LastBeltRunning = false;
            }
        }

        public void Dispose() => Stop();
    }
}
