using System;

namespace BeltFlo.Database
{
    /// <summary>
    /// One calibration and geometry record for a profile's conveyor scale. Rows are
    /// append-only: a new save is a new row, and its id is the calibration revision
    /// carried in every packet and every yield_data row, so a point can always say
    /// which numbers produced it.
    /// </summary>
    public class ConveyorConfig
    {
        public int Id { get; set; }                     // the calibration revision
        public int ProfileId { get; set; }
        public double ZeroCounts { get; set; }          // raw counts with the belt running empty
        public double SpanLbPerCount { get; set; } = 1; // pounds per raw count above zero
        public DateTime? ZeroSetAt { get; set; }
        public int PulsesPerRev { get; set; }           // pulses in one full belt revolution; 0 = not measured
        public double InchesPerPulse { get; set; } = 1.0;
        public double SectionLenIn { get; set; } = 36;  // weighed section length
        public double FlowThresholdLbS { get; set; } = 0.05;
        public double BeltStopTimeoutS { get; set; } = 2.0;
        public int DelaySec { get; set; } = 10;         // digging-to-scale delay
        public DateTime CreatedAt { get; set; }
    }
}
