using System;
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

                var sb = new StringBuilder();
                // Columns 0-5 match RC's YieldOverlayCreator expected format (parsed by position).
                // Extra columns follow for BeltFlo-specific data.
                sb.AppendLine("Timestamp,Latitude,Longitude,WidthMeters,Yield_kgha,ElevationMeters,Speed_kmh,Heading,HaAccumulated,PoundsInc,LoadId,BeltFtMin,CalRev");

                foreach (var p in points)
                {
                    double yieldKgHa = p.YieldRate * KgHaPerLbAc;
                    double haAcc     = p.AcresAccumulated * 0.404686;

                    sb.AppendLine(
                        $"{p.Timestamp:yyyy-MM-dd HH:mm:ss}," +
                        $"{p.Latitude:F7},{p.Longitude:F7}," +
                        $"{widthM:F3},{yieldKgHa:F1},{p.Elevation:F1}," +
                        $"{p.Speed:F2},{p.Heading:F1}," +
                        $"{haAcc:F4}," +
                        $"{p.PoundsInc:F2},{p.LoadId},{p.BeltFtMin:F1},{p.CalRev}");
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
