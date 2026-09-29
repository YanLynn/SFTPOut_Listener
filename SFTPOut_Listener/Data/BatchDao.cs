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

        // Null-safe read every column except ID and TIMESTAMP is null
        private static string Str(IDataRecord r , int i )
        {
            return r.IsDBNull(i) ? "" : r.GetString(i).Trim();
        }

       // private static string Now() { return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); }

        public List<BatchInfo> GetByStatus(string status, int max)
        {
            var list = new List<BatchInfo>();
            const string sql =
                "SELECT TOP (@max) ID, ProcDate, IssBk, IssBr, BATCH, TRANTYPE, [SEQUENCE] " +
                "FROM dbo.tblCTSLiteBatch WHERE Status = @status " +
                "ORDER BY ID";
            using (var con = new SqlConnection(_cs))
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.Add("@max", SqlDbType.Int).Value = max;
                cmd.Parameters.Add("@status", SqlDbType.VarChar, 50).Value = status;
                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new BatchInfo
                        {
                            Id = r.GetInt32(0),
                            ProcDate = Str(r,1),
                            IssBk = Str(r,2),
                            IssBr = Str(r,3),
                            Batch = Str(r,4),
                            TranType = Str(r,5),
                            Sequence = Str(r,6)
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
                "UPDATE dbo.tblCTSLiteBatch SET Status = @to, [TimeStamp] = @ts WHERE ID = @id " +
                (fromStatus != null ? " AND Status = @from" : "");
            using (var con = new SqlConnection(_cs))
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.Add("@to", SqlDbType.VarChar, 50).Value = toStatus;
                cmd.Parameters.Add("@ts", SqlDbType.DateTime).Value = DateTime.Now;
                cmd.Parameters.Add("@id", SqlDbType.Int).Value = b.Id;
                if (fromStatus != null)
                    cmd.Parameters.Add("@from", SqlDbType.VarChar, 50).Value = fromStatus;
                con.Open();
                return cmd.ExecuteNonQuery();
            }
        }
    }
}