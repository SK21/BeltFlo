using System.Data.SQLite;
using System;
using System.Collections.Generic;
using BeltFlo.Classes;

namespace BeltFlo.Database
{
    // Thrown when a delete is blocked by a foreign-key constraint (the row is
    // still referenced by a job or a load) so callers can show a friendly
    // message instead of letting the raw SQLiteException surface.
    public class ItemInUseException : Exception
    {
        public ItemInUseException() : base("Item is referenced by one or more jobs.") { }
    }

    // ── JobRepo ───────────────────────────────────────────────────────────────
    public class JobRepo
    {
        private readonly string _cs;
        public JobRepo(string connectionString) { _cs = connectionString; }

        public int Create(string name, int profileId, int cropId, int headerId, int fieldId = -1)
        {
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            // A newly created job is not yet recording — it becomes 'Active' only
            // when Loaded (JobRepo.Reopen / Collector.LoadJob), which closes any
            // previously active job. Creating it 'Active' here produced a second
            // active row alongside the job actually recording, so the DB could
            // hold two 'Active' jobs and resume the wrong one on next start.
            using var cmd = new SQLiteCommand(
                "INSERT INTO jobs (name, profile_id, crop_id, header_id, field_id, status) " +
                "VALUES (@n, @p, @c, @h, @f, 'New'); SELECT last_insert_rowid();", conn);
            cmd.Parameters.AddWithValue("@n", name);
            cmd.Parameters.AddWithValue("@p", profileId);
            cmd.Parameters.AddWithValue("@c", cropId);
            cmd.Parameters.AddWithValue("@h", headerId);
            cmd.Parameters.AddWithValue("@f", fieldId > 0 ? (object)fieldId : DBNull.Value);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void UpdateTotals(int jobId, double acres, double pounds)
        {
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            using var cmd = new SQLiteCommand(
                "UPDATE jobs SET total_acres=@a, total_pounds=@p WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@a", acres);
            cmd.Parameters.AddWithValue("@p", pounds);
            cmd.Parameters.AddWithValue("@id", jobId);
            cmd.ExecuteNonQuery();
        }

        public void Close(int jobId)
        {
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            using var cmd = new SQLiteCommand(
                "UPDATE jobs SET status='Complete', ended_at=datetime('now') WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@id", jobId);
            cmd.ExecuteNonQuery();
        }

        // `volume` is total pounds. The name survives from the grain code so the
        // forms that read the tuple did not all have to change at once.
        public List<(int id, string name, string status, string startedAt, double acres, double volume, int profileId, int cropId, int headerId, int fieldId, string notes)> GetAll()
        {
            var result = new List<(int, string, string, string, double, double, int, int, int, int, string)>();
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            using var cmd = new SQLiteCommand(
                "SELECT id, name, status, started_at, total_acres, total_pounds, profile_id, crop_id, header_id, field_id, notes FROM jobs ORDER BY id DESC", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                result.Add((reader.GetInt32(0), reader.GetString(1), reader.GetString(2),
                            reader.GetString(3), reader.GetDouble(4), reader.GetDouble(5),
                            reader.IsDBNull(6) ? -1 : reader.GetInt32(6),
                            reader.IsDBNull(7) ? -1 : reader.GetInt32(7),
                            reader.IsDBNull(8) ? -1 : reader.GetInt32(8),
                            reader.IsDBNull(9) ? -1 : reader.GetInt32(9),
                            reader.IsDBNull(10) ? "" : reader.GetString(10)));
            return result;
        }

        public void Update(int jobId, string name, int cropId, int headerId, int profileId, int fieldId = -1, string notes = "")
        {
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            using var cmd = new SQLiteCommand(
                "UPDATE jobs SET name=@n, crop_id=@c, header_id=@h, profile_id=@p, field_id=@f, notes=@nt WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@n",  name);
            cmd.Parameters.AddWithValue("@c",  cropId);
            cmd.Parameters.AddWithValue("@h",  headerId);
            cmd.Parameters.AddWithValue("@p",  profileId);
            cmd.Parameters.AddWithValue("@f",  fieldId > 0 ? (object)fieldId : DBNull.Value);
            cmd.Parameters.AddWithValue("@nt", notes ?? "");
            cmd.Parameters.AddWithValue("@id", jobId);
            cmd.ExecuteNonQuery();
        }

        public void Reopen(int jobId)
        {
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            using var cmd = new SQLiteCommand(
                "UPDATE jobs SET status='Active', ended_at=NULL WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@id", jobId);
            cmd.ExecuteNonQuery();
        }

        public void Delete(int jobId)
        {
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            using var tx = conn.BeginTransaction();
            // Deleting a job takes its recorded readings and its loads with it — with
            // FK enforcement on, either would otherwise block this delete.
            using (var cmd = new SQLiteCommand("DELETE FROM yield_data WHERE job_id=@id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", jobId);
                cmd.ExecuteNonQuery();
            }
            using (var cmd = new SQLiteCommand("DELETE FROM loads WHERE job_id=@id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", jobId);
                cmd.ExecuteNonQuery();
            }
            using (var cmd = new SQLiteCommand("DELETE FROM jobs WHERE id=@id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", jobId);
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }
    }

    // ── YieldDataRepo ─────────────────────────────────────────────────────────
    public class YieldDataRepo
    {
        private readonly string _cs;
        public YieldDataRepo(string connectionString) { _cs = connectionString; }

        public bool Insert(YieldDataPoint pt)
        {
            try
            {
                using var conn = new SQLiteConnection(_cs);
                conn.Open();
                using var cmd = new SQLiteCommand(@"
INSERT INTO yield_data
    (job_id, load_id, timestamp, latitude, longitude, elevation, speed, heading,
     yield_rate, acres_accumulated, pounds_inc, belt_pulses, belt_ft_min,
     scale_lb, scale_raw, cal_rev, rows_in_use)
VALUES
    (@jid, @lid, @ts, @lat, @lon, @elev, @spd, @hdg,
     @yr, @ac, @lb, @bp, @bs,
     @sl, @sr, @cr, @ru)", conn);
                cmd.Parameters.AddWithValue("@jid", pt.JobId);
                cmd.Parameters.AddWithValue("@lid", pt.LoadId);
                cmd.Parameters.AddWithValue("@ts", pt.Timestamp.ToString("o"));
                cmd.Parameters.AddWithValue("@lat", pt.Latitude);
                cmd.Parameters.AddWithValue("@lon", pt.Longitude);
                cmd.Parameters.AddWithValue("@elev", pt.Elevation);
                cmd.Parameters.AddWithValue("@spd", pt.Speed);
                cmd.Parameters.AddWithValue("@hdg", pt.Heading);
                cmd.Parameters.AddWithValue("@yr", pt.YieldRate);
                cmd.Parameters.AddWithValue("@ac", pt.AcresAccumulated);
                cmd.Parameters.AddWithValue("@lb", pt.PoundsInc);
                cmd.Parameters.AddWithValue("@bp", pt.BeltPulses);
                cmd.Parameters.AddWithValue("@bs", pt.BeltFtMin);
                cmd.Parameters.AddWithValue("@sl", pt.ScaleLb);
                cmd.Parameters.AddWithValue("@sr", pt.ScaleRaw);
                cmd.Parameters.AddWithValue("@cr", pt.CalRev);
                cmd.Parameters.AddWithValue("@ru", pt.RowsInUse);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                Props.WriteErrorLog("YieldDataRepo/Insert: " + ex.Message);
                return false;
            }
        }

        public List<YieldDataPoint> GetByJob(int jobId)
        {
            var result = new List<YieldDataPoint>();
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            // Name every column: the reader below is positional, and SELECT * would
            // make those positions depend on the schema the file happens to carry.
            using var cmd = new SQLiteCommand(
                "SELECT id, job_id, load_id, timestamp, latitude, longitude, elevation, speed, heading, " +
                "yield_rate, acres_accumulated, pounds_inc, belt_pulses, belt_ft_min, " +
                "scale_lb, scale_raw, cal_rev, rows_in_use " +
                "FROM yield_data WHERE job_id=@jid ORDER BY timestamp", conn);
            cmd.Parameters.AddWithValue("@jid", jobId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new YieldDataPoint
                {
                    Id = reader.GetInt32(0),
                    JobId = reader.GetInt32(1),
                    LoadId = reader.GetInt32(2),
                    Timestamp = DateTime.Parse(reader.GetString(3)),
                    Latitude = reader.GetDouble(4),
                    Longitude = reader.GetDouble(5),
                    Elevation = reader.GetDouble(6),
                    Speed = reader.GetFloat(7),
                    Heading = reader.GetFloat(8),
                    YieldRate = reader.GetDouble(9),
                    AcresAccumulated = reader.GetDouble(10),
                    PoundsInc = reader.GetDouble(11),
                    BeltPulses = reader.GetInt32(12),
                    BeltFtMin = reader.GetDouble(13),
                    ScaleLb = reader.GetDouble(14),
                    ScaleRaw = reader.GetInt32(15),
                    CalRev = reader.GetInt32(16),
                    RowsInUse = reader.GetInt32(17)
                });
            }
            return result;
        }

        // Rescales the yield and the incremental pounds of a job's points by one
        // proportional factor — the whole job, or only the points dug into one load
        // when loadId is given. This is the per-load correction from a certified
        // weight: the span carries the whole measurement chain, so a ticket that
        // says the monitor read 3% high corrects every point of that load by 3%.
        // Brings jobs.total_pounds along by the same pounds. Returns the rows
        // rewritten and the job's new total.
        public (int rows, double totalPounds) RescaleJob(int jobId, double factor, int loadId = -1)
        {
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            using var tx = conn.BeginTransaction();

            string where = loadId > 0 ? "job_id=@jid AND load_id=@lid" : "job_id=@jid";

            double poundsBefore = 0;
            int rows = 0;
            using (var sumCmd = new SQLiteCommand(
                "SELECT COALESCE(SUM(pounds_inc), 0), COUNT(*) FROM yield_data WHERE " + where, conn, tx))
            {
                sumCmd.Parameters.AddWithValue("@jid", jobId);
                if (loadId > 0) sumCmd.Parameters.AddWithValue("@lid", loadId);
                using var r = sumCmd.ExecuteReader();
                if (r.Read()) { poundsBefore = r.GetDouble(0); rows = r.GetInt32(1); }
            }

            using (var updCmd = new SQLiteCommand(
                "UPDATE yield_data SET yield_rate = yield_rate * @f, pounds_inc = pounds_inc * @f WHERE " + where, conn, tx))
            {
                updCmd.Parameters.AddWithValue("@f", factor);
                updCmd.Parameters.AddWithValue("@jid", jobId);
                if (loadId > 0) updCmd.Parameters.AddWithValue("@lid", loadId);
                updCmd.ExecuteNonQuery();
            }

            double newTotal = 0;
            using (var jobCmd = new SQLiteCommand(
                "UPDATE jobs SET total_pounds = total_pounds + @d WHERE id=@jid; " +
                "SELECT total_pounds FROM jobs WHERE id=@jid", conn, tx))
            {
                jobCmd.Parameters.AddWithValue("@d", poundsBefore * (factor - 1.0));
                jobCmd.Parameters.AddWithValue("@jid", jobId);
                object o = jobCmd.ExecuteScalar();
                if (o != null && o != DBNull.Value) newTotal = Convert.ToDouble(o);
            }

            tx.Commit();
            return (rows, newTotal);
        }
    }

    // ── ProfileRepo ───────────────────────────────────────────────────────────
    public class ProfileRepo
    {
        private readonly string _cs;
        public ProfileRepo(string connectionString) { _cs = connectionString; }

        public int Create(string name, string harvesterId = "")
        {
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            using var cmd = new SQLiteCommand(
                "INSERT INTO profiles (name, harvester_id) VALUES (@n, @h); SELECT last_insert_rowid();", conn);
            cmd.Parameters.AddWithValue("@n", name);
            cmd.Parameters.AddWithValue("@h", harvesterId);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public List<(int id, string name, string harvesterId)> GetAll()
        {
            var result = new List<(int, string, string)>();
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            using var cmd = new SQLiteCommand("SELECT id, name, harvester_id FROM profiles ORDER BY name", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                result.Add((reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));
            return result;
        }

        public void Update(int id, string name, string harvesterId)
        {
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            using var cmd = new SQLiteCommand(
                "UPDATE profiles SET name=@n, harvester_id=@h WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@n",  name);
            cmd.Parameters.AddWithValue("@h",  harvesterId);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public void Delete(int id)
        {
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            using var tx = conn.BeginTransaction();
            // Deleting a profile takes its conveyor configuration history with it —
            // with FK enforcement on, conveyor_config.profile_id would otherwise
            // block this delete on every profile, since each is seeded with one.
            using (var cmd = new SQLiteCommand("DELETE FROM conveyor_config WHERE profile_id=@id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
            using (var cmd = new SQLiteCommand("DELETE FROM profiles WHERE id=@id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", id);
                try { cmd.ExecuteNonQuery(); }
                catch (SQLiteException ex) when (ex.ResultCode == SQLiteErrorCode.Constraint)
                { tx.Rollback(); throw new ItemInUseException(); }
            }
            tx.Commit();
        }
    }

    // ── CropRepo ──────────────────────────────────────────────────────────────
    public class CropRepo
    {
        private readonly string _cs;
        public CropRepo(string connectionString) { _cs = connectionString; }

        public int Create(string name)
        {
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            using var cmd = new SQLiteCommand(
                "INSERT INTO crops (name) VALUES (@n); SELECT last_insert_rowid();", conn);
            cmd.Parameters.AddWithValue("@n", name);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public List<(int id, string name)> GetAll()
        {
            var result = new List<(int, string)>();
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            using var cmd = new SQLiteCommand("SELECT id, name FROM crops ORDER BY name", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                result.Add((reader.GetInt32(0), reader.GetString(1)));
            return result;
        }

        public void Update(int id, string name)
        {
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            using var cmd = new SQLiteCommand("UPDATE crops SET name=@n WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@n",  name);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public void Delete(int id)
        {
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            using var cmd = new SQLiteCommand("DELETE FROM crops WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            try { cmd.ExecuteNonQuery(); }
            catch (SQLiteException ex) when (ex.ResultCode == SQLiteErrorCode.Constraint)
            { throw new ItemInUseException(); }
        }
    }

    // ── HeaderRepo ────────────────────────────────────────────────────────────
    public class HeaderRepo
    {
        private readonly string _cs;
        public HeaderRepo(string connectionString) { _cs = connectionString; }

        public int Create(string name, string headerType, double cutWidthM, double fwdOffsetM = 0)
        {
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            using var cmd = new SQLiteCommand(
                "INSERT INTO headers (name, header_type, cut_width, fwd_offset) VALUES (@n, @t, @w, @o); SELECT last_insert_rowid();", conn);
            cmd.Parameters.AddWithValue("@n", name);
            cmd.Parameters.AddWithValue("@t", headerType);
            cmd.Parameters.AddWithValue("@w", cutWidthM);
            cmd.Parameters.AddWithValue("@o", fwdOffsetM);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public List<(int id, string name, string type, double widthM, double fwdOffsetM)> GetAll()
        {
            var result = new List<(int, string, string, double, double)>();
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            using var cmd = new SQLiteCommand(
                "SELECT id, name, header_type, cut_width, fwd_offset FROM headers ORDER BY name", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                result.Add((reader.GetInt32(0), reader.GetString(1),
                            reader.GetString(2), reader.GetDouble(3), reader.GetDouble(4)));
            return result;
        }

        public void Update(int id, string name, string headerType, double cutWidthM, double fwdOffsetM = 0)
        {
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            using var cmd = new SQLiteCommand(
                "UPDATE headers SET name=@n, header_type=@t, cut_width=@w, fwd_offset=@o WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@n",  name);
            cmd.Parameters.AddWithValue("@t",  headerType);
            cmd.Parameters.AddWithValue("@w",  cutWidthM);
            cmd.Parameters.AddWithValue("@o",  fwdOffsetM);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public void Delete(int id)
        {
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            using var cmd = new SQLiteCommand("DELETE FROM headers WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            try { cmd.ExecuteNonQuery(); }
            catch (SQLiteException ex) when (ex.ResultCode == SQLiteErrorCode.Constraint)
            { throw new ItemInUseException(); }
        }
    }

    // ── FieldRepo ─────────────────────────────────────────────────────────────
    public class FieldRepo
    {
        private readonly string _cs;
        public FieldRepo(string connectionString) { _cs = connectionString; }

        public int Create(string name, string boundaryGeoJson = "")
        {
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            using var cmd = new SQLiteCommand(
                "INSERT INTO fields (name, boundary) VALUES (@n, @b); SELECT last_insert_rowid();", conn);
            cmd.Parameters.AddWithValue("@n", name);
            cmd.Parameters.AddWithValue("@b", boundaryGeoJson);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public List<(int id, string name)> GetAll()
        {
            var result = new List<(int, string)>();
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            using var cmd = new SQLiteCommand("SELECT id, name FROM fields ORDER BY name", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                result.Add((reader.GetInt32(0), reader.GetString(1)));
            return result;
        }

        public void Update(int id, string name)
        {
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            using var cmd = new SQLiteCommand("UPDATE fields SET name=@n WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@n",  name);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public void Delete(int id)
        {
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            using var cmd = new SQLiteCommand("DELETE FROM fields WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            try { cmd.ExecuteNonQuery(); }
            catch (SQLiteException ex) when (ex.ResultCode == SQLiteErrorCode.Constraint)
            { throw new ItemInUseException(); }
        }
    }

    // ── ConveyorConfigRepo ────────────────────────────────────────────────────
    public class ConveyorConfigRepo
    {
        private readonly string _cs;
        public ConveyorConfigRepo(string connectionString) { _cs = connectionString; }

        /// <summary>Appends a new revision and returns its id.</summary>
        public int Save(ConveyorConfig c)
        {
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            using var cmd = new SQLiteCommand(@"
INSERT INTO conveyor_config
    (profile_id, zero_counts, span_lb_per_count, zero_set_at, pulses_per_rev,
     inches_per_pulse, section_len_in, flow_threshold_lb_s, belt_stop_timeout_s, delay_sec)
VALUES (@p, @z, @s, @za, @ppr, @ipp, @sl, @ft, @bst, @d);
SELECT last_insert_rowid();", conn);
            cmd.Parameters.AddWithValue("@p",   c.ProfileId);
            cmd.Parameters.AddWithValue("@z",   c.ZeroCounts);
            cmd.Parameters.AddWithValue("@s",   c.SpanLbPerCount);
            cmd.Parameters.AddWithValue("@za",  c.ZeroSetAt.HasValue ? (object)c.ZeroSetAt.Value.ToString("o") : DBNull.Value);
            cmd.Parameters.AddWithValue("@ppr", c.PulsesPerRev);
            cmd.Parameters.AddWithValue("@ipp", c.InchesPerPulse);
            cmd.Parameters.AddWithValue("@sl",  c.SectionLenIn);
            cmd.Parameters.AddWithValue("@ft",  c.FlowThresholdLbS);
            cmd.Parameters.AddWithValue("@bst", c.BeltStopTimeoutS);
            cmd.Parameters.AddWithValue("@d",   c.DelaySec);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public ConveyorConfig GetLatest(int profileId)
        {
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            using var cmd = new SQLiteCommand(
                SelectColumns + " FROM conveyor_config WHERE profile_id=@p ORDER BY id DESC LIMIT 1", conn);
            cmd.Parameters.AddWithValue("@p", profileId);
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Map(reader) : null;
        }

        /// <summary>The revision a stored point or packet names, or null if unknown.</summary>
        public ConveyorConfig GetById(int id)
        {
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            using var cmd = new SQLiteCommand(SelectColumns + " FROM conveyor_config WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Map(reader) : null;
        }

        private const string SelectColumns =
            "SELECT id, profile_id, zero_counts, span_lb_per_count, zero_set_at, pulses_per_rev, " +
            "inches_per_pulse, section_len_in, flow_threshold_lb_s, belt_stop_timeout_s, delay_sec, created_at";

        private static ConveyorConfig Map(SQLiteDataReader r)
        {
            return new ConveyorConfig
            {
                Id               = r.GetInt32(0),
                ProfileId        = r.GetInt32(1),
                ZeroCounts       = r.GetDouble(2),
                SpanLbPerCount   = r.GetDouble(3),
                ZeroSetAt        = r.IsDBNull(4) ? (DateTime?)null : DateTime.Parse(r.GetString(4)),
                PulsesPerRev     = r.GetInt32(5),
                InchesPerPulse   = r.GetDouble(6),
                SectionLenIn     = r.GetDouble(7),
                FlowThresholdLbS = r.GetDouble(8),
                BeltStopTimeoutS = r.GetDouble(9),
                DelaySec         = r.GetInt32(10),
                CreatedAt        = DateTime.TryParse(r.GetString(11), out var dt) ? dt : DateTime.MinValue
            };
        }
    }

    // ── LoadRepo ──────────────────────────────────────────────────────────────
    public class LoadRepo
    {
        private readonly string _cs;
        public LoadRepo(string connectionString) { _cs = connectionString; }

        public int Create(int jobId, string truck, int calRev)
        {
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            using var cmd = new SQLiteCommand(
                "INSERT INTO loads (job_id, truck, cal_rev) VALUES (@j, @t, @c); SELECT last_insert_rowid();", conn);
            cmd.Parameters.AddWithValue("@j", jobId);
            cmd.Parameters.AddWithValue("@t", truck ?? "");
            cmd.Parameters.AddWithValue("@c", calRev);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        /// <summary>The truck has left: freeze the monitor weight and wait for its ticket.</summary>
        public void Close(int id, double monitorLb)
        {
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            using var cmd = new SQLiteCommand(
                "UPDATE loads SET monitor_lb=@m, closed_at=datetime('now'), status=@s WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@m",  monitorLb);
            cmd.Parameters.AddWithValue("@s",  LoadRecord.StatusWaiting);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        /// <summary>Periodic crash-safety write of the running weight on the open load.</summary>
        public void UpdateMonitorLb(int id, double monitorLb)
        {
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            using var cmd = new SQLiteCommand("UPDATE loads SET monitor_lb=@m WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@m",  monitorLb);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public void Rename(int id, string truck)
        {
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            using var cmd = new SQLiteCommand("UPDATE loads SET truck=@t WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@t",  truck ?? "");
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        /// <summary>Records the ticket. The caller decides whether to also rescale the load's points.</summary>
        public void SetCertified(int id, double certifiedLb, double factor, string status, string flag = "")
        {
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            using var cmd = new SQLiteCommand(
                "UPDATE loads SET certified_lb=@c, factor=@f, status=@s, flag=@fl WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@c",  certifiedLb);
            cmd.Parameters.AddWithValue("@f",  factor);
            cmd.Parameters.AddWithValue("@s",  status);
            cmd.Parameters.AddWithValue("@fl", flag ?? "");
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        /// <summary>The load still being filled on this job, or null.</summary>
        public LoadRecord GetOpen(int jobId)
        {
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            using var cmd = new SQLiteCommand(
                SelectColumns + " FROM loads WHERE job_id=@j AND status=@s ORDER BY id DESC LIMIT 1", conn);
            cmd.Parameters.AddWithValue("@j", jobId);
            cmd.Parameters.AddWithValue("@s", LoadRecord.StatusActive);
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Map(reader) : null;
        }

        public LoadRecord GetById(int id)
        {
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            using var cmd = new SQLiteCommand(SelectColumns + " FROM loads WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? Map(reader) : null;
        }

        /// <summary>All loads on a job, newest first; every load when jobId is -1.</summary>
        public List<LoadRecord> GetAll(int jobId = -1)
        {
            var result = new List<LoadRecord>();
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            using var cmd = new SQLiteCommand(
                SelectColumns + " FROM loads" + (jobId > 0 ? " WHERE job_id=@j" : "") + " ORDER BY id DESC", conn);
            if (jobId > 0) cmd.Parameters.AddWithValue("@j", jobId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) result.Add(Map(reader));
            return result;
        }

        public void Delete(int id)
        {
            using var conn = new SQLiteConnection(_cs);
            conn.Open();
            using var tx = conn.BeginTransaction();
            // The points stay with the job; they just stop belonging to a load.
            using (var cmd = new SQLiteCommand("UPDATE yield_data SET load_id=-1 WHERE load_id=@id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
            using (var cmd = new SQLiteCommand("DELETE FROM loads WHERE id=@id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }

        private const string SelectColumns =
            "SELECT id, job_id, truck, opened_at, closed_at, monitor_lb, certified_lb, factor, cal_rev, flag, status";

        private static LoadRecord Map(SQLiteDataReader r)
        {
            return new LoadRecord
            {
                Id          = r.GetInt32(0),
                JobId       = r.GetInt32(1),
                Truck       = r.GetString(2),
                OpenedAt    = DateTime.TryParse(r.GetString(3), out var o) ? o : DateTime.MinValue,
                ClosedAt    = r.IsDBNull(4) ? (DateTime?)null : (DateTime.TryParse(r.GetString(4), out var c) ? c : (DateTime?)null),
                MonitorLb   = r.GetDouble(5),
                CertifiedLb = r.IsDBNull(6) ? (double?)null : r.GetDouble(6),
                Factor      = r.GetDouble(7),
                CalRev      = r.GetInt32(8),
                Flag        = r.GetString(9),
                Status      = r.GetString(10)
            };
        }
    }
}
