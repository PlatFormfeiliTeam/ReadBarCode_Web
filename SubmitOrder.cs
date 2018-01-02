using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReadBarCode_Web
{
    public static class SubmitOrder
    {
        public static string Submit(string ordercode, string CreateUserId, string CreateUserName)
        {
            string rtnstr = "{success:true}";
            DataTable dt_order = DBMgr.GetDataTable("select * from list_order a where a.code='" + ordercode + "' and a.ISINVALID=0");

            string busitype = dt_order.Rows[0]["BUSITYPE"].ToString();
            string entrusttype = dt_order.Rows[0]["ENTRUSTTYPE"].ToString();

            if (busitype == "30" || busitype == "31")//陆运业务
            {
                string status = "10", declstatus = "", inspstatus = "";
                if (entrusttype == "01") {
                    if (dt_order.Rows[0]["DECLSTATUS"].ToString() != "")
                    {
                        if (Convert.ToInt32(dt_order.Rows[0]["DECLSTATUS"].ToString()) >= 10)
                        {
                            return rtnstr;
                        }
                    }
                    declstatus = status; 
                }
                if (entrusttype == "02") {
                    if (dt_order.Rows[0]["INSPSTATUS"].ToString() != "")
                    {
                        if (Convert.ToInt32(dt_order.Rows[0]["INSPSTATUS"].ToString()) >= 10)
                        {
                            return rtnstr;
                        }
                    }
                    inspstatus = status; 
                }
                if (entrusttype == "03") {
                    if (dt_order.Rows[0]["DECLSTATUS"].ToString() != "")
                    {
                        if (Convert.ToInt32(dt_order.Rows[0]["DECLSTATUS"].ToString()) >= 10)
                        {
                            return rtnstr;
                        }
                    }
                    if (dt_order.Rows[0]["INSPSTATUS"].ToString() != "")
                    {
                        if (Convert.ToInt32(dt_order.Rows[0]["INSPSTATUS"].ToString()) >= 10)
                        {
                            return rtnstr;
                        }
                    }
                    declstatus = status; inspstatus = status; 
                }

                string sql = @"UPDATE LIST_ORDER SET STATUS='{1}',DECLSTATUS='{2}',INSPSTATUS='{3}',SUBMITUSERID='{4}',SUBMITUSERNAME='{5}'  
                                    ,SUBMITTIME=sysdate,BUSIKIND='001',ORDERWAY='1'                                    
                            WHERE CODE = '{0}' ";
                sql = string.Format(sql, ordercode, status, declstatus, inspstatus, CreateUserId, CreateUserName);

                int result = DBMgr.ExecuteNonQuery(sql);
                if (result == 1)
                {                    
                    Extension.add_list_time(10, ordercode,Convert.ToInt32(CreateUserId), CreateUserName);//插入订单状态变更日志
                }
                else
                {
                    rtnstr = "{success:false}";
                }
            }
            return rtnstr;
           
        }


    }
}
