using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadBarCode_Web
{
    public class DBMgr
    {
        private static readonly string ConnectionString = ConfigurationManager.AppSettings["strconn"];
        public static OracleConnection getOrclCon()
        {
            OracleConnection orclCon = new OracleConnection(ConnectionString);
            return orclCon;
        }

        public static DataSet GetDataSet(string sql)
        {
            DataSet ds = new DataSet();
            try
            {
                using (OracleConnection orclCon = new OracleConnection(ConnectionString))
                {
                    DbCommand oc = orclCon.CreateCommand();
                    oc.CommandText = sql;
                    if (orclCon.State.ToString().Equals("Open"))
                    {
                        orclCon.Close();
                    }
                    orclCon.Open();
                    DbDataAdapter adapter = new OracleDataAdapter();
                    adapter.SelectCommand = oc;
                    adapter.Fill(ds);
                }
            }
            catch (Exception e)
            {
                //log.Error(e.Message + e.StackTrace);
            }
            return ds;
        }

        public static DataTable GetDataTable(string sql)
        {
            DataSet ds = new DataSet();
            try
            {
                using (OracleConnection orclCon = new OracleConnection(ConnectionString))
                {
                    DbCommand oc = orclCon.CreateCommand();
                    oc.CommandText = sql;
                    if (orclCon.State.ToString().Equals("Open"))
                    {
                        orclCon.Close();
                    }
                    orclCon.Open();
                    DbDataAdapter adapter = new OracleDataAdapter();
                    adapter.SelectCommand = oc;
                    adapter.Fill(ds);
                }
            }
            catch (Exception e)
            {
                //log.Error(e.Message + e.StackTrace);
            }
            return ds.Tables[0];
        }

        public static int ExecuteNonQuery(string sql)
        {
            int retcount = -1;
            using (OracleConnection orclCon = new OracleConnection(ConnectionString))
            {
                OracleCommand oc = new OracleCommand(sql, orclCon);
                if (orclCon.State.ToString().Equals("Open"))
                {
                    orclCon.Close();
                }
                orclCon.Open();
                retcount = oc.ExecuteNonQuery();
                oc.Parameters.Clear();
            }
            return retcount;
        }

        public static int ExecuteNonQuery(string sql, OracleConnection orclCon)
        {
            int retcount = -1;
            OracleCommand oc = new OracleCommand(sql, orclCon);
            retcount = oc.ExecuteNonQuery();
            oc.Parameters.Clear();
            return retcount;
        }
    }
}
