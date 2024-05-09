using System.Data;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Text;
using System.Reflection;
using System.Runtime.CompilerServices;


namespace CPUFramework;

public class SQLUtility
{
    private static string ConnectionString = "";

    public static void SetConnectionString(string connString, bool tryOpen, string userId = "", string password = "")
    {
        if (!string.IsNullOrEmpty(userId))
        {
            SqlConnectionStringBuilder s = new();
            s.ConnectionString = connString;
            s.UserID = userId;
            s.Password = password;
            connString = s.ConnectionString;
        }
        ConnectionString = connString;

        if (tryOpen)
        {
            using (SqlConnection conn = new(ConnectionString))
            {
                conn.Open();
            }
        }
    }

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

    public static DataTable GetDataTable(SqlCommand cmd)
    {
        return DoExecuteSQL(cmd, true);
    }

    public static DataTable GetDataTable(string sqlStatement)
    {
        return DoExecuteSQL(new SqlCommand(sqlStatement), true);
    }

    public static void ExecuteSQL(SqlCommand cmd)
    {
        DoExecuteSQL(cmd, false);
    }

    public static void ExecuteSQL(string sql)
    {
        GetDataTable(sql);
    }

    public static void SaveDataTable(DataTable dt, string sprocName)
    {
        var rows = dt.Select("", "", DataViewRowState.Added | DataViewRowState.ModifiedCurrent);
        foreach (DataRow r in rows)
        {
            SaveDateRow(r, sprocName, false);
        }
        dt.AcceptChanges();
    }

    public static void SaveDateRow(DataRow dr, string sprocName, bool acceptChanges = true)
    {
        SqlCommand cmd = GetSQLCommand(sprocName);
        foreach (DataColumn column in dr.Table.Columns)
        {
            string paramName = "@" + column;
            if (cmd.Parameters.Contains(paramName))
            {
                SetParamValue(cmd, paramName, dr[column.ColumnName]);
            }
        }
        DoExecuteSQL(cmd, false);
        foreach (SqlParameter p in cmd.Parameters)
        {
            if (p.Direction == ParameterDirection.InputOutput)
            {
                string col = p.ParameterName.Substring(1);
                if (dr.Table.Columns.Contains(col))
                {
                    dr[col] = p.Value;
                }
            }
        }
        if (acceptChanges)
        {
            dr.Table.AcceptChanges();
        }
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
                CheckReturnValue(cmd);
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
        SetAllColumnsProperties(dt);
        return dt;
    }

    private static void CheckReturnValue(SqlCommand cmd)
    {
        int returnValue = 0;
        string msg = "";
        if (cmd.CommandType == CommandType.StoredProcedure)
        {

            foreach (SqlParameter p in cmd.Parameters)
            {
                if (p.Direction == ParameterDirection.ReturnValue)
                {
                    if (p.Value != null)
                    {
                        returnValue = (int)p.Value;
                    }
                }
                else if (p.ParameterName.ToLower() == "@message")
                {
                    if (p.Value != null)
                    {
                        msg = p.Value.ToString();
                    }
                }
            }
            if (msg == "")
            {
                msg = $"{cmd.CommandText} did not do requested action";
            }
            if (returnValue == 1)
            {
                throw new Exception(msg);
            }
        }
    }

    public static void SetParamValue(SqlCommand cmd, string paramName, object value)
    {
        if (!paramName.StartsWith("@")) { paramName = "@" + paramName; }
        if (cmd.Parameters.Contains(paramName))
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
    }

    private static string ParseConstraintMsg(string msg)
    {
        string originalMsg = msg;
        string prefix = "ck_";
        string msgEnd = "";
        string notNullPrefix = "Cannot insert the value NULL into column ";
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
            else if (msg.Contains(notNullPrefix))
            {
                prefix = "Cannot insert the value NULL into column '";
                msgEnd = " cannot be blank";
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
                msg = originalMsg;
            }
            else
            {
                msg = msg.Substring(0, position);
                msg = msg.Replace("_", " ") + msgEnd;
                if (prefix == "f_")
                {
                    var words = msg.Split(" ");
                    if (words.Length > 1)
                    {
                        msg = $"Cannot delete {words[0]} because it has a related {words[1]} record";
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
                _ = int.TryParse(dt.Rows[0][0].ToString(), out n);
            }
        }
        return n;
    }

    public static int GetValueFromFirstRowAsInt(DataTable dt, string columnName)
    {
        int value = 0;
        if (dt.Rows.Count > 0)
        {
            DataRow r = dt.Rows[0];
            if (r[columnName] != null && r[columnName] is int)
            {
                value = (int)r[columnName];

            }
        }
        return value;
    }

    public static string GetValueFromFirstRowAsString(DataTable dt, string columnName)
    {
        string value = "";
        if (dt.Rows != null && dt.Rows.Count > 0)
        {
            DataRow r = dt.Rows[0];
            if (r[columnName] != null)
            {
                if (r[columnName] is string s)
                {
                    value = s;
                }
                else if(r[columnName] is Guid guid)
                {
                    value = guid.ToString();
                }
            }
        }
        return value;
    }

    private static void SetAllColumnsProperties(DataTable dt)
    {
        foreach (DataColumn c in dt.Columns)
        {
            c.AllowDBNull = true;
            c.AutoIncrement = false;
        }
    }

    public static bool TableHasChanges(DataTable dt)
    {
        bool b = false;
        if (dt.GetChanges() != null)
        {
            b = true;
        }
        return b;
    }

    public static string GetSQL(SqlCommand cmd)
    {
        string val = "";
#if DEBUG
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

    public static List<T> TryGetListFromDataTable<T>(DataTable dt) where T : new()
    {
        List<T> list = new();
        List<PropertyInfo> properties = typeof(T).GetType().GetProperties().ToList();

        foreach (DataRow dr in dt.Rows)
        {
            T t = new();
            list.Add(t);
            foreach (DataColumn dc in dt.Columns)
            {
                PropertyInfo? property = properties.FirstOrDefault(p =>
                    p.Name.ToLower() == dc.ColumnName.ToLower()
                    && p.CanWrite
                );

                if (property != null)
                {
                    object? value = dr[dc];
                    if (value == DBNull.Value) { value = null; }
                    try
                    {
                        property.SetValue(t, value);
                    }
                    catch { }
                }
            }
        }

        return list;
    }
}