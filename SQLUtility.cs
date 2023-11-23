﻿using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Text;

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
            return DoExecuteSQL(cmd, true);
        }

        private static DataTable DoExecuteSQL(SqlCommand cmd, bool loadDataTable)
        {
            DataTable dt = new();
            using (SqlConnection conn = new(ConnectionString))
            {
                cmd.Connection = conn;
                conn.Open();
                Debug.Print(GetSQL(cmd));
                try
                {
                    SqlDataReader dr = cmd.ExecuteReader();
                    if (loadDataTable)
                    {
                        dt.Load(dr);
                    }
                }
                catch (SqlException ex)
                {
                    string msg = ParseConstraintMsg(ex.Message);
                    throw new Exception(msg);
                }
                catch (InvalidCastException ex)
                {
                    throw new Exception($"{cmd.CommandText}: {ex.Message}");
                }
            }
            SetAllColumnsAllowNull(dt);
            return dt;
        }

        public static DataTable GetDataTable(string sqlstatment)
        {
            return DoExecuteSQL(new SqlCommand(sqlstatment), true);
        }

        public static void ExecuteSQL(string sql)
        {
            GetDataTable(sql);
        }

        public static void ExecuteSQL(SqlCommand cmd)
        {
            DoExecuteSQL(cmd, false);
        }

        public static void SetParamValue(SqlCommand cmd, string paramName, object value)
        {
            try
            {
                cmd.Parameters[paramName].Value = value;
            }
            catch (Exception ex)
            {
                throw new Exception($"{cmd.CommandText}: {ex.Message}", ex);
            }
        }

        private static string ParseConstraintMsg(string msg)
        {
            string origMsg = msg;
            string prefix = "ck_";
            string msgEnd = "";
            if (!msg.Contains(prefix))
            {
                if (msg.Contains("u_"))
                {
                    prefix = "u_";
                    msgEnd = " must be unique.";
                }
                else if (msg.Contains("f_"))
                {
                    prefix = "f_";
                }
            }
            if (msg.Contains(prefix))
            {
                msg = msg.Replace("\"", "'");
                int position = msg.IndexOf(prefix) + prefix.Length;
                msg = msg.Substring(position);
                position = msg.IndexOf("'");
                if (position == -1)
                {
                    msg = origMsg;
                }
                else
                {
                    msg = msg.Substring(0, position);
                    msg = msg.Replace("_", " ") + msgEnd;
                    if (prefix == "f_")
                    {
                        var words = msg.Split(" ");
                        if(words.Length > 1)
                        {
                            msg = $"Cannot delete {words[0]} becaue it has a related {words[1]} record";
                        }
                    }
                }
            }
            return msg;
        }

        public static int GetFirstColumnFirstRowValue(string sql)
        {
            int n = 0;
            DataTable dt = GetDataTable(sql);
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

        public static string GetSQL(SqlCommand cmd)
        {
#if DEBUG
            string val = "";
            StringBuilder sb = new();
            if (cmd.Connection != null)
            {
                sb.AppendLine($"--{cmd.Connection.DataSource}");
                sb.AppendLine($"use {cmd.Connection.Database}");
                sb.AppendLine("go");
            }
            if (cmd.CommandType == CommandType.StoredProcedure)
            {
                sb.AppendLine($"exec {cmd.CommandText}");
                int countdown = cmd.Parameters.Count;
                string comma = ",";
                foreach (SqlParameter p in cmd.Parameters)
                {
                    if (p.Direction != ParameterDirection.ReturnValue)
                    {
                        comma = countdown == 1 ? "" : comma;
                        sb.AppendLine($"{p.ParameterName} = {(p.Value == null ? "null" : p.Value.ToString())}{comma}");
                    }
                    countdown--;
                }
            }
            else
            {
                sb.AppendLine(cmd.CommandText);
            }
            val = sb.ToString();
#endif
            return val;

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