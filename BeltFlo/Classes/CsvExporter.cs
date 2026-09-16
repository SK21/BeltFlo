using System;
using System.Globalization;
using System.IO;
using System.Text;
using BeltFlo.Database;

namespace BeltFlo.Classes
{
    public static class CsvExporter
    {
        /// <summary>
        /// Exports all YieldDataPoints for a job to a CSV file.
        /// Returns the output file path, or null on error.
        /// </summary>
        public static string ExportJob(int jobId, string jobName, string path)
        {
            try
            {
                var points = Core.Database.YieldData.GetByJob(jobId);
                if (points == null || points.Count == 0) return null;

                // Same treatment the map gives them, so the exported file and the
                // picture on screen agree.
                PassTransients.Apply(points);

                // Digging width from the job's harvester and rows harvested.
                double widthM = 3.6576;
                foreach (var j in Core.Database.Jobs.GetAll())
                {
                    if (j.id != jobId) continue;
                    widthM = Core.JobWidthM(j.profileId, j.rowsHarvested);
                    break;
                }

                // lb/ac → kg/ha
                const double KgHaPerLbAc = 0.453592 / 0.404686;

                // Every number is written invariant, as the diagnostic log does. The
                // app runs in the culture of the chosen language, and seven of the
                // eight write decimals with a comma — which inside a comma-separated
                // file would split one value into two columns and leave the export
                // unreadable to anything, RateController included.
                var ci = CultureInfo.InvariantCulture;

                var sb = new StringBuilder();
                // Columns 0-5 match RC's YieldOverlayCreator expected format (parsed by position).
                // Extra columns follow for BeltFlo-specific data.
                sb.AppendLine("Timestamp,Latitude,Longitude,WidthMeters,Yield_kgha,ElevationMeters,Speed_kmh,Heading,HaAccumulated,PoundsInc,LoadId,BeltFtMin,CalRev");

                foreach (var p in points)
                {
                    double yieldKgHa = p.YieldRate * KgHaPerLbAc;
                    double haAcc     = p.AcresAccumulated * 0.404686;

                    sb.AppendLine(string.Join(",",
                        p.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", ci),
                        p.Latitude.ToString("F7", ci),
                        p.Longitude.ToString("F7", ci),
                        widthM.ToString("F3", ci),
                        yieldKgHa.ToString("F1", ci),
                        p.Elevation.ToString("F1", ci),
                        p.Speed.ToString("F2", ci),
                        p.Heading.ToString("F1", ci),
                        haAcc.ToString("F4", ci),
                        p.PoundsInc.ToString("F2", ci),
                        p.LoadId.ToString(ci),
                        p.BeltFtMin.ToString("F1", ci),
                        p.CalRev.ToString(ci)));
                }

                File.WriteAllText(path, sb.ToString());
                return path;
            }
            catch (Exception ex)
            {
                Props.WriteErrorLog("CsvExporter: " + ex.Message);
                return null;
            }
        }
    }
}
