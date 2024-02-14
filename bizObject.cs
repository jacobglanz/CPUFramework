using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Reflection.PortableExecutable;

namespace CPUFramework
{
    public class bizObject : INotifyPropertyChanged
    {
        string _tableName = ""; string _getSproc = ""; string _updateSproc = ""; string _deleteSproc = ""; string _primayKeyName = ""; string _primayKeyParamName = "";
        DataTable _dataTable = new();
        List<PropertyInfo> _properties = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public bizObject()
        {
            Type t = this.GetType();
            _tableName = t.Name;
            if (_tableName.ToLower().StartsWith("biz")) { _tableName = _tableName.Substring(3); }
            _getSproc = _tableName + "Get";
            _updateSproc = _tableName + "Update";
            _deleteSproc = _tableName + "Delete";
            _primayKeyName = _tableName + "Id";
            _primayKeyParamName = "@" + _primayKeyName;
            _properties = t.GetProperties().ToList<PropertyInfo>();
        }

        public DataTable Load(int primaryKeyValue)
        {
            DataTable dt = new();
            SqlCommand cmd = SQLUtility.GetSQLCommand(_getSproc);
            SQLUtility.SetParamValue(cmd, _primayKeyParamName, primaryKeyValue);
            dt = SQLUtility.GetDataTable(cmd);
            if (dt.Rows.Count > 0)
            {
                LoadProps(dt.Rows[0]);
            }
            _dataTable = dt;
            return dt;
        }

        private void LoadProps(DataRow dr)
        {
            foreach (DataColumn col in dr.Table.Columns)
            {
                SetProp(col.ColumnName, dr[col.ColumnName]);
            }
        }

        public void Delete()
        {
            PropertyInfo? prop = GetProp(_primayKeyName, true, false);
            if (prop != null)
            {
                int? id = (int?)prop.GetValue(this);
                if (id != null)
                {
                    this.Delete((int)id);
                }
            }
        }

        public void Delete(int id)
        {
            SqlCommand cmd = SQLUtility.GetSQLCommand(_deleteSproc);
            SQLUtility.SetParamValue(cmd, _primayKeyParamName, id);
            SQLUtility.ExecuteSQL(cmd);
        }

        public void Delete(DataTable dataTable)
        {
            int id = (int)dataTable.Rows[0][_primayKeyName];
            this.Delete(id);
        }

        public void Save()
        {
            SqlCommand cmd = SQLUtility.GetSQLCommand(_updateSproc);
            foreach (SqlParameter param in cmd.Parameters)
            {
                var prop = GetProp(param.ParameterName, true, false);
                if (prop != null)
                {
                    object? val = prop.GetValue(this);
                    param.Value = val != null ? val : DBNull.Value;
                }
            }
            SQLUtility.ExecuteSQL(cmd);
            foreach (SqlParameter param in cmd.Parameters)
            {
                if (param.Direction == ParameterDirection.InputOutput)
                {
                    SetProp(param.ParameterName, param.Value);
                }
            }
        }

        public void Save(DataTable dataTable)
        {
            if (dataTable.Rows.Count == 0)
            {
                throw new Exception($"Cannot call {_updateSproc} save method because there are no rows in the table");
            }
            SQLUtility.SaveDateRow(dataTable.Rows[0], _updateSproc);
        }

        private PropertyInfo? GetProp(string propName, bool forRead, bool forWrite)
        {
            if (propName.StartsWith("@")) { propName = propName.Substring(1); }
            return _properties.FirstOrDefault(
                 p => p.Name.ToLower() == propName.ToLower()
                 && (!forRead || p.CanRead)
                 && (!forWrite || p.CanWrite)
             );

        }

        private void SetProp(string propName, object? value)
        {
            var prop = GetProp(propName, false, true);
            if (prop != null)
            {
                if (value == DBNull.Value) { value = null; }
                prop.SetValue(this, value);
            }
        }

        protected void InvokePropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
