using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;

namespace DGCore.CSV
{
    public class TestCsvConnection : DbConnection
    {

        private string _filename;
        private ConnectionState _state = ConnectionState.Closed;

        public TestCsvConnection(string fileName)
        {
            _filename = fileName;
        }
        protected override DbCommand CreateDbCommand() => new TestCsvCommand(this);
        public override void Open()
        {
            //_state = ConnectionState.Open;
        }

        public override void Close()
        {
            //_state = ConnectionState.Closed;
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotImplementedException();
        public override void ChangeDatabase(string databaseName) => throw new NotImplementedException();
        public override string ConnectionString { get; set; }
        public override string Database { get; }
        public override ConnectionState State => _state;
        public override string DataSource { get; }
        public override string ServerVersion { get; }
    }

    //=========================================
    public class TestCsvCommand : DbCommand
    {
        public TestCsvCommand(TestCsvConnection connection)
        {
            DbConnection = connection;
        }

        public override void Prepare()
        {
            throw new NotImplementedException();
        }

        public override string CommandText { get; set; }
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType
        {
            get => CommandType.TableDirect;
            set { }
        }

        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection DbConnection { get; set; }
        protected override DbParameterCollection DbParameterCollection { get; } = new TestCsvParameterCollection();
        protected override DbTransaction DbTransaction { get; set; }
        public override bool DesignTimeVisible { get; set; }

        public override void Cancel()
        {
            throw new NotImplementedException();
        }

        protected override DbParameter CreateDbParameter()
        {
            throw new NotImplementedException();
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            // throw new NotImplementedException();
            return new TestCsvDataReader(this);
        }

        public override int ExecuteNonQuery()
        {
            throw new NotImplementedException();
        }

        public override object ExecuteScalar()
        {
            throw new NotImplementedException();
        }
    }

    //==========================================
    public class TestCsvParameter : DbParameter
    {
        public override void ResetDbType()
        {
            throw new NotImplementedException();
        }

        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; }
        public override bool IsNullable { get; set; }
        public override string ParameterName { get; set; }
        public override string SourceColumn { get; set; }
        public override object Value { get; set; }
        public override bool SourceColumnNullMapping { get; set; }
        public override int Size { get; set; }
    }

    //========================
    public class TestCsvParameterCollection : DbParameterCollection
    {
        public override int Add(object value)
        {
            throw new NotImplementedException();
        }

        public override bool Contains(object value)
        {
            throw new NotImplementedException();
        }

        public override void Clear()
        {
            // throw new NotImplementedException();
        }

        public override int IndexOf(object value)
        {
            throw new NotImplementedException();
        }

        public override void Insert(int index, object value)
        {
            throw new NotImplementedException();
        }

        public override void Remove(object value)
        {
            throw new NotImplementedException();
        }

        public override void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public override void RemoveAt(string parameterName)
        {
            throw new NotImplementedException();
        }

        protected override void SetParameter(int index, DbParameter value)
        {
            throw new NotImplementedException();
        }

        protected override void SetParameter(string parameterName, DbParameter value)
        {
            throw new NotImplementedException();
        }

        public override int Count { get; }
        public override object SyncRoot { get; }

        public override int IndexOf(string parameterName)
        {
            throw new NotImplementedException();
        }

        public override IEnumerator GetEnumerator()
        {
            yield break;
        }

        protected override DbParameter GetParameter(int index)
        {
            throw new NotImplementedException();
        }

        protected override DbParameter GetParameter(string parameterName)
        {
            throw new NotImplementedException();
        }

        public override bool Contains(string value)
        {
            throw new NotImplementedException();
        }

        public override void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public override void AddRange(Array values)
        {
            throw new NotImplementedException();
        }
    }

    //===============================================
    //===============================================
    public class TestCsvDataReader : DbDataReader
    {
        private readonly TestCsvCommand _cmd;
        private string[] _headers;
        private MultilineMyCsvEscapedFileReader _reader;
        private readonly List<string> _data = new List<string>();

        public TestCsvDataReader(TestCsvCommand cmd)
        {
            _cmd = cmd;
        }

        public override DataTable GetSchemaTable() // !!! ToDo
        {
            if (_headers == null)
                using (var reader = new MultilineMyCsvEscapedFileReader(_cmd.CommandText))
                    if (reader.ReadRow(_data))
                        _headers = _data.ToArray();
                    else
                        _headers = new string[0];

            var dt = new DataTable();
            dt.Columns.Add(new DataColumn("ColumnName", typeof(string)));
            dt.Columns.Add(new DataColumn("ColumnOrdinal", typeof(int)));
            dt.Columns.Add(new DataColumn("ColumnSize", typeof(int)));
            dt.Columns.Add(new DataColumn("BaseTableName", typeof(string)));
            dt.Columns.Add(new DataColumn("BaseColumnName", typeof(string)));
            dt.Columns.Add(new DataColumn("DataType", typeof(Type)));
            dt.Columns.Add(new DataColumn("AllowDBNull", typeof(bool)));
            dt.Columns.Add(new DataColumn("IsKey", typeof(bool)));

            for (var k = 0; k < _headers.Length; k++)
                dt.Rows.Add(_headers[k], k, int.MaxValue, Path.GetFileName(_cmd.CommandText), _headers[k], typeof(string), true, false);

            return dt;
        }
        public override bool Read()
        {
            if (_reader == null)
            {
                _reader = new MultilineMyCsvEscapedFileReader(_cmd.CommandText);
                // Read header
                _reader.ReadRow(_data);
            }

            return _reader.ReadRow(_data);
        }

        public override bool IsDBNull(int ordinal) => string.IsNullOrEmpty(_data[ordinal]);

        public override string GetString(int ordinal) => _data[ordinal];

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _reader?.Dispose();
        }

        public override string GetName(int ordinal) => throw new NotImplementedException();
        public override int GetValues(object[] values) => throw new NotImplementedException();
        public override int FieldCount { get; }
        public override object this[int ordinal] => throw new NotImplementedException();
        public override object this[string name] => throw new NotImplementedException();
        public override bool HasRows { get; }
        public override bool IsClosed { get; }
        public override int RecordsAffected { get; }
        public override bool NextResult() => throw new NotImplementedException();
        public override int Depth { get; }
        public override int GetOrdinal(string name) => throw new NotImplementedException();
        public override bool GetBoolean(int ordinal) => throw new NotImplementedException();
        public override byte GetByte(int ordinal) => throw new NotImplementedException();
        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length) => throw new NotImplementedException();
        public override char GetChar(int ordinal) => throw new NotImplementedException();
        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length) => throw new NotImplementedException();
        public override Guid GetGuid(int ordinal) => throw new NotImplementedException();
        public override short GetInt16(int ordinal) => throw new NotImplementedException();
        public override int GetInt32(int ordinal) => throw new NotImplementedException();
        public override long GetInt64(int ordinal) => throw new NotImplementedException();
        public override DateTime GetDateTime(int ordinal) => throw new NotImplementedException();
        public override decimal GetDecimal(int ordinal) => throw new NotImplementedException();
        public override double GetDouble(int ordinal) => throw new NotImplementedException();
        public override float GetFloat(int ordinal) => throw new NotImplementedException();
        public override string GetDataTypeName(int ordinal) => throw new NotImplementedException();
        public override Type GetFieldType(int ordinal) => throw new NotImplementedException();
        public override object GetValue(int ordinal) => throw new NotImplementedException();
        public override IEnumerator GetEnumerator() => throw new NotImplementedException();
    }
}
