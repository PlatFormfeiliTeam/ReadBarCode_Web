using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadBarCode_Web
{
    public static class Extension
    {
        //订单的状态在草稿、文件已上传、订单已委托 三个状态发生时记录到订单状态变更日志
        public static void add_list_time(int status, string ordercode, Int32 CreateUserId, string CreateUserName)
        {
            if (status != 0 && status != 10)
            {
                return;
            }

            string sql_search = @"select count(*) from list_times where code = '{0}' and status = '{1}'";
            string sql_insert = @"insert into list_times(id,code,userid,realname,status,times,type,ispause) values(list_times_id.nextval,'{0}','{1}','{2}','{3}',sysdate,'0','0')";

            string sql = ""; int i = 0; //int CreateUserId = json_user.Value<Int32>("ID"); string CreateUserName = json_user.Value<string>("REALNAME");

            int[] status_array = new int[] { 0, 10 };
            foreach (int status_tmp in status_array)
            {
                if (status >= status_tmp)
                {
                    sql = string.Format(sql_search, ordercode, status_tmp);
                    i = Convert.ToInt32(DBMgr.GetDataTable(sql).Rows[0][0]);
                    if (i == 0)
                    {
                        sql = string.Format(sql_insert, ordercode, CreateUserId, CreateUserName, status_tmp);
                        DBMgr.ExecuteNonQuery(sql);
                    }
                }
            }

        }
    }
}
