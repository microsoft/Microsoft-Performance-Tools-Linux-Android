// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using Perfetto.Protos;
using Utilities;

namespace PerfettoProcessor
{
    /// <summary>
    /// https://perfetto.dev/docs/analysis/sql-tables#package_list
    /// </summary>
    public class PerfettoPackageListEvent : PerfettoSqlEvent
    {
        public const string Key = "PerfettoPackageListEvent";

        public const string SqlQuery = "select id, type, package_name, uid, debuggable, profileable_from_shell, version_code from package_list order by id";
        public int Id { get; set; }
        public string Type { get; set; }

        /// <summary>
        /// name of the package, e.g. com.google.android.gm.
        /// </summary>
        public string PackageName { get; set; }

        /// <summary>
        /// UID processes of this package run as.
        /// </summary>
        public long Uid { get; set; }

        /// <summary>
        /// bool whether this app is debuggable.
        /// </summary>
        public bool Debuggable { get; set; }

        /// <summary>
        /// bool whether this app is profileable.
        /// </summary>
        public bool ProfileableFromShell { get; set; }

        /// <summary>
        /// versionCode from the APK.
        /// </summary>
        public long VersionCode { get; set; }

        public override string GetSqlQuery()
        {
            return SqlQuery;
        }

        public override string GetEventKey()
        {
            return Key;
        }

        public override void ProcessCell(string colName,
            QueryResult.Types.CellsBatch.Types.CellType cellType,
            QueryResult.Types.CellsBatch batch,
            string[] stringCells,
            CellCounters counters)
        {
            var col = colName.ToLower();
            switch (cellType)
            {
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellInvalid:
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellNull:
                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellVarint:
                    var longVal = batch.VarintCells[counters.IntCounter++];
                    switch (col)
                    {
                        case "id":
                            Id = (int)longVal;
                            break;
                        case "uid":
                            Uid = longVal;
                            break;
                        case "debuggable":
                            Debuggable = Convert.ToBoolean(longVal);
                            break;
                        case "profileable_from_shell":
                            ProfileableFromShell = Convert.ToBoolean(longVal);
                            break;
                        case "version_code":
                            VersionCode = longVal;
                            break;
                    }

                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellFloat64:
                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellString:
                    var strVal = Common.StringIntern(stringCells[counters.StringCounter++]);
                    switch (col)
                    {
                        case "type":
                            Type = strVal;
                            break;
                        case "package_name":
                            PackageName = strVal;
                            break;
                    }
                    break;
                case Perfetto.Protos.QueryResult.Types.CellsBatch.Types.CellType.CellBlob:
                    break;
                default:
                    throw new Exception("Unexpected CellType");
            }
        }
    }
}
