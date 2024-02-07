using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPUFramework
{
    public class bizObject
    {
        string _tableName = ""; string _getSproc = ""; string _updateSproc = ""; string _deleteSproc = ""; string _primayKeyName = ""; string _primayKeyParamName = "";
        DataTable _dataTable = new();
        public bizObject(string tableName)
        {
            _tableName = tableName;
            _getSproc = tableName + "Get";
            _updateSproc = tableName + "Update";
            _deleteSproc = tableName + "Delete";
            _primayKeyName = tableName + "Id";
            _primayKeyParamName = "@" + _primayKeyName;

        }
        public DataTable Load(int primaryKeyValue)
        {
            SqlCommand cmd = SQLUtility.GetSQLCommand(_getSproc);
            SQLUtility.SetParamValue(cmd, _primayKeyParamName, primaryKeyValue);
            DataTable dt = SQLUtility.GetDataTable(cmd);
            _dataTable = dt;
            return dt;
        }

        public void Delete(DataTable dataTable)
        {
            int id = (int)dataTable.Rows[0][_primayKeyName];
            SqlCommand cmd = SQLUtility.GetSQLCommand(_deleteSproc);
            SQLUtility.SetParamValue(cmd, _primayKeyParamName, id);
            SQLUtility.ExecuteSQL(cmd);
        }

        public void Save(DataTable dataTable)
        {
            if (dataTable.Rows.Count == 0)
            {
                throw new Exception($"Cannot call {_updateSproc} save method because there are no rows in the table");
            }
            SQLUtility.SaveDateRow(dataTable.Rows[0], _updateSproc);
        }
    }
}
