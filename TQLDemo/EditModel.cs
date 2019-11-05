using CK.Core;
using CK.SqlServer.Parser;
using System;
namespace TQLDemo
{
    public class EditModel
    {
        public IActivityMonitor _m;
        public EditModel()
        {
            _m = new ActivityMonitor();
            _m.Output.RegisterClient( new ActivityMonitorTextWriterClient( w => Logs += w ) );
        }
        public SqlServerParser p = new SqlServerParser();
        public string _tql;
        public ISqlServerParserResult<ISqlServerTransformer> _parsedTql;
        public string _sql;
        public ISqlServerParserResult<ISqlServerParsedText> _parsedSql;
        public string Tql
        {
            get => _tql;
            set
            {
                _tql = value;
                _parsedTql = p.ParseTransformer( Tql );
            }
        }
        public string Sql
        {
            get => _sql;
            set
            {
                _sql = value;
                _parsedSql = p.Parse( Sql );
            }
        }
        public string TqlError => _parsedTql?.ErrorMessage;
        public string SqlError => _parsedSql?.ErrorMessage;
        public bool TqlResultIsError => _parsedTql?.IsError ?? true;
        public bool SqlResultIsError => _parsedSql?.IsError ?? true;
        public string Logs { get; set; }
        public string Transformed
        {
            get
            {
                bool error =
                       string.IsNullOrWhiteSpace( Tql )
                    || string.IsNullOrWhiteSpace( Sql )
                    || (_parsedSql?.IsError ?? true)
                    || (_parsedTql?.IsError ?? true)
                    || _parsedSql?.Result == null
                    || _parsedTql?.Result == null;
                if( error ) return "Please fill the inputs or correct the errors.";

                return (_parsedTql.Result.SafeTransform( _m, _parsedSql.Result )?.ToFullString()) ?? "Error while applying TQL on SQL.";
            }
        }
    }
}
