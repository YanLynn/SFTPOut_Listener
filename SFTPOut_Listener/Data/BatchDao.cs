using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace SFTPOut_Listener
{
    internal sealed class BatchDao
    {
        private readonly string _cs;

        public BatchDao(ConfigData cfg) { _cs = cfg.DbConnString; }

        private static string Now() { return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); }

        public List<BatchInfo> GetByStatus(string status, int max)
        {
            var list = new List<BatchInfo>();
            const string sql =
                "SELECT TOP (@max) ProcDate, IssBk, IssBr, Batch, TranType, [Sequence] " +
                "FROM dbo.tblCTSLiteBatch WHERE Status = @status " +
                "ORDER BY ProcDate, Batch, [Sequence]";
            using (var con = new SqlConnection(_cs))
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.Add("@max", SqlDbType.Int).Value = max;
                cmd.Parameters.Add("@status", SqlDbType.VarChar, 20).Value = status;
                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new BatchInfo
                        {
                            ProcDate = r.GetString(0).Trim(),
                            IssBk = r.GetString(1).Trim(),
                            IssBr = r.GetString(2).Trim(),
                            Batch = r.GetString(3).Trim(),
                            TranType = r.GetString(4).Trim(),
                            Sequence = r.GetString(5).Trim()
                        });
                    }
                }
            }
            return list;
        }

        public int Count(string status)
        {
            using (var con = new SqlConnection(_cs))
            using (var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM dbo.tblCTSLiteBatch WHERE Status = @status", con))
            {
                cmd.Parameters.Add("@status", SqlDbType.VarChar, 20).Value = status;
                con.Open();
                return (int)cmd.ExecuteScalar();
            }
        }

        public bool TryChangeStatus(BatchInfo b, string fromStatus, string toStatus)
        {
            return Update(b, fromStatus, toStatus) == 1;
        }

        public void SetStatus(BatchInfo b, string toStatus)
        {
            Update(b, null, toStatus);
        }

        private int Update(BatchInfo b, string fromStatus, string toStatus)
        {
            string sql =
                "UPDATE dbo.tblCTSLiteBatch SET Status = @to, [TimeStamp] = @ts " +
                "WHERE ProcDate = @pd AND IssBk = @ib AND IssBr = @ir AND Batch = @bt " +
                "AND TranType = @tt AND [Sequence] = @sq" +
                (fromStatus != null ? " AND Status = @from" : "");
            using (var con = new SqlConnection(_cs))
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.Add("@to", SqlDbType.VarChar, 20).Value = toStatus;
                cmd.Parameters.Add("@ts", SqlDbType.VarChar, 20).Value = Now();
                cmd.Parameters.Add("@pd", SqlDbType.VarChar, 8).Value = b.ProcDate;
                cmd.Parameters.Add("@ib", SqlDbType.VarChar, 4).Value = b.IssBk;
                cmd.Parameters.Add("@ir", SqlDbType.VarChar, 3).Value = b.IssBr;
                cmd.Parameters.Add("@bt", SqlDbType.VarChar, 6).Value = b.Batch;
                cmd.Parameters.Add("@tt", SqlDbType.VarChar, 4).Value = b.TranType;
                cmd.Parameters.Add("@sq", SqlDbType.VarChar, 4).Value = b.Sequence;
                if (fromStatus != null)
                    cmd.Parameters.Add("@from", SqlDbType.VarChar, 20).Value = fromStatus;
                con.Open();
                return cmd.ExecuteNonQuery();
            }
        }
    }
}