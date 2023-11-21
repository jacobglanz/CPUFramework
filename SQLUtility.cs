using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;

namespace CPUFramework
{
    public class SQLUtility
    {
        public static string ConnectionString = "";

        public static SqlCommand GetSQLCommand(string sproc)
        {
            SqlCommand cmd = new();

            using (SqlConnection conn = new(ConnectionString))
            {
                cmd = new() { Connection = conn, CommandType = CommandType.StoredProcedure, CommandText = sproc };
                conn.Open();
                SqlCommandBuilder.DeriveParameters(cmd);
            }
            
            return cmd;
        }

        public static DataTable GetDateTable(SqlCommand cmd)
        {
            Debug.Print($"{Environment.NewLine}{cmd.CommandText}--------");
            DataTable dt = new();
            using (SqlConnection conn = new(ConnectionString))
            {
                cmd.Connection = conn;
                conn.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                dt.Load(dr);
            }
            SetAllColumnsAllowNull(dt);
            return dt;
        }

        public static DataTable GetDateTable(string sqlstatment)
        {
            return GetDateTable(new SqlCommand(sqlstatment));
        }

        public static void ExecuteSQL(string sql)
        {
            GetDateTable(sql);
        }

        public static int GetFirstColumnFirstRowValue(string sql)
        {
            int n = 0;
            DataTable dt = GetDateTable(sql);
            if (dt.Columns.Count > 0 && dt.Rows.Count > 0)
            {
                if (dt.Rows[0][0] != DBNull.Value)
                {
                    int.TryParse(dt.Rows[0][0].ToString(), out n);
                }
            }
            return n;
        }

        private static void SetAllColumnsAllowNull(DataTable dt)
        {
            foreach (DataColumn c in dt.Columns)
            {
                c.AllowDBNull = true;
            }
        }

        public static void DebugPrintDataTable(DataTable dt)
        {
            foreach (DataRow r in dt.Rows)
            {
                foreach (DataColumn c in dt.Columns)
                {
                    Debug.Print(c.ColumnName + " = " + r[c.ColumnName]);
                }
            }
        }
    }
}