namespace BeltFlo.Database
{
    /// <summary>
    /// A harvester. Root-crop harvesters are built for a fixed number of rows, so the
    /// digging width — rows × row spacing — belongs to the machine rather than to a
    /// swappable header. The conveyor settings and scale calibration hang off the same
    /// profile (conveyor_config.profile_id).
    /// </summary>
    public class HarvesterProfile
    {
        public const string Truck = "Truck";
        public const string Tank  = "Tank";

        public int    Id            { get; set; } = -1;
        public string Name          { get; set; } = "";
        public string HarvesterId   { get; set; } = "";
        public int    Rows          { get; set; } = 4;
        public double RowSpacingM   { get; set; } = 0.9144;   // 36 in

        // AgOpenGPS's convention: the distance from its pivot point to the digger,
        // positive ahead of the pivot and negative behind it (a towed harvester).
        public double AheadOfPivotM { get; set; } = 0;

        // Where the crop goes after the scale: straight into the truck, or into a
        // tank that is emptied into trucks. Decides per-load or whole-job correction.
        public string ScaleLocation { get; set; } = Truck;

        /// <summary>Digging width for a job picking up this many rows; 0 means the harvester's own.</summary>
        public double WidthM(int rowsHarvested) => (rowsHarvested > 0 ? rowsHarvested : Rows) * RowSpacingM;
    }
}
