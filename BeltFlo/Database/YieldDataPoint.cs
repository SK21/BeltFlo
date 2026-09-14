using System;

namespace BeltFlo.Database
{
    public class YieldDataPoint
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        public int LoadId { get; set; } = -1;       // truck load this point was dug into; -1 = none open
        public DateTime Timestamp { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Elevation { get; set; }       // metres
        public float Speed { get; set; }            // km/h
        public float Heading { get; set; }          // degrees
        public double YieldRate { get; set; }       // lb/ac
        public double AcresAccumulated { get; set; }
        public double PoundsInc { get; set; }       // pounds over the scale since the previous row
        public int BeltPulses { get; set; }         // belt pulses since the previous row
        public double BeltFtMin { get; set; }       // belt speed at write time
        public double ScaleLb { get; set; }         // live weigh-section load at write time
        public int ScaleRaw { get; set; }           // raw converter counts at write time
        public int CalRev { get; set; }             // conveyor_config row the module was running
        public int RowsInUse { get; set; }          // 0 = full width
    }
}
