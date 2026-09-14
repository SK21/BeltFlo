using System;

namespace BeltFlo.Database
{
    /// <summary>
    /// One truck load. Opened from the run screen, closed when the truck leaves,
    /// reconciled later when its certified weight comes back from the scale.
    /// </summary>
    public class LoadRecord
    {
        public const string StatusActive    = "Active";
        public const string StatusWaiting   = "Waiting";     // closed, no certified weight yet
        public const string StatusCorrected = "Corrected";   // certified weight applied to its points
        public const string StatusComplete  = "Complete";    // certified weight saved, no correction

        public int Id { get; set; }
        public int JobId { get; set; }
        public string Truck { get; set; } = "";
        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public double MonitorLb { get; set; }        // what the scale said
        public double? CertifiedLb { get; set; }     // what the ticket said
        public double Factor { get; set; } = 1.0;    // certified ÷ monitor, once known
        public int CalRev { get; set; }              // calibration the load was weighed under
        public string Flag { get; set; } = "";       // wet, trashy, partial...
        public string Status { get; set; } = StatusActive;
    }
}
