using MessagingToolkit.Barcode;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ReadBarCode_Web
{
    public partial class Form1 : Form
    {
        string direc_pdf = ConfigurationManager.AppSettings["filedir"];
        string direc_img = ConfigurationManager.AppSettings["ImagePath"];

        public Form1()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (this.button1.Text.Trim() == "开 始 识 别")
            {
                this.button1.Text = "识 别 中..."; this.button1.BackColor = System.Drawing.Color.Yellow;
                System.Threading.Thread thread = new System.Threading.Thread(runTask);
                thread.IsBackground = true;
                thread.Start();
                this.button1.Enabled = true;
            }
            else if (this.button1.Text.Trim() == "识 别 中...")
            {
                this.button1.Text = "正 在 停 止"; this.button1.BackColor = System.Drawing.Color.Gray;
                this.button1.Enabled = false;
            }
        }

        private void runTask()
        {
            if (ConfigurationManager.AppSettings["AutoRun"].ToString().Trim() == "Y")
            {
                fn_share fn_share = new fn_share();
                string filedic = System.Environment.CurrentDirectory + @"\log\";
                if (!Directory.Exists(filedic))
                {
                    Directory.CreateDirectory(filedic);
                }
                while (true)
                {
                    if (this.button1.Text.Trim() == "正 在 停 止")
                    {
                        break;
                    }
                    System.Threading.Thread.Sleep(3000);
                    string filename = filedic + "barcode_web_log_" + DateTime.Now.ToString("yyyyMMddHH") + ".txt";
                    barcode_toolKit_web(fn_share, filename);
                }
                this.button1.Text = "开 始 识 别"; this.button1.BackColor = System.Drawing.Color.White;
                this.button1.Enabled = true;
            }
        }

        private void barcode_toolKit_web(fn_share fn_share, string filename)
        {
            string sql = ""; string guid = ""; string barcode = ""; 
            DataTable dt = new DataTable(); string id = ""; string filepath = ""; string originalname = ""; string filesuffix = "";
            string sql_insert = ""; DataTable dt_order = new DataTable(); string associateno = "";

            try
            {
                sql = "select * from list_filerecoginze where status='未关联' order by times";
                dt = DBMgr.GetDataTable(sql);
                foreach (DataRow dr in dt.Rows)
                {
                    fn_share.systemLog(filename, "-------------------------------------------------------------\r\n");

                    //先置空
                    guid = ""; barcode = "";
                    id = ""; filepath = ""; originalname = ""; filesuffix = "";
                    sql_insert = ""; dt_order.Clear(); associateno = "";

                    //赋值
                    id = dr["ID"].ToString();
                    filepath = dr["FILEPATH"].ToString();
                    originalname = dr["FILENAME"].ToString(); filesuffix = originalname.Substring(originalname.LastIndexOf(".") + 1).ToUpper();
                    FileInfo fi = new FileInfo(direc_pdf + filepath);

                    //---------------------------------------------------------------------------------------------------------------------
                    //再次判断状态，如果被删除，或者状态不是未关联，则跳到下一笔记录；否则 立刻更新为关联中
                    DataTable dt_status = DBMgr.GetDataTable("select * from list_filerecoginze where id=" + id);
                    if (dt_status == null) { continue; }
                    if (dt_status.Rows.Count <= 0) { continue; }
                    if (dt_status.Rows[0]["STATUS"].ToString() != "未关联") { continue; }
                    DBMgr.ExecuteNonQuery("update list_filerecoginze set status='关联中' where id=" + id);

                    if (filesuffix != "PDF")
                    {
                        DBMgr.ExecuteNonQuery("update list_filerecoginze set status='关联失败' where id=" + id);
                        continue;
                    }
                    DateTime d1 = DateTime.Now;
                    guid = Guid.NewGuid().ToString(); string imagefileName = direc_img + guid + ".Jpeg";
                    ConvertPDF.pdfToPic(direc_pdf + filepath, direc_img, guid, 1, 1, ImageFormat.Jpeg);
                    fn_share.systemLog(filename, "=== ConvertToImage——" + (DateTime.Now - d1) + "\r\n");

                    if (!File.Exists(imagefileName))//转图片失败
                    {
                        DBMgr.ExecuteNonQuery("update list_filerecoginze set status='关联失败' where id=" + id);
                        continue;
                    }

                    BarcodeDecoder barcodeDecoder = new BarcodeDecoder();
                    Image primaryImage = Image.FromFile(imagefileName);
                    Bitmap pImg = MakeGrayscale3((Bitmap)primaryImage);
                    Dictionary<DecodeOptions, object> decodingOptions = new Dictionary<DecodeOptions, object>();
                    List<BarcodeFormat> possibleFormats = new List<BarcodeFormat>(10);
                    possibleFormats.Add(BarcodeFormat.Code128);
                    possibleFormats.Add(BarcodeFormat.EAN13);
                    decodingOptions.Add(DecodeOptions.TryHarder, true);
                    decodingOptions.Add(DecodeOptions.PossibleFormats, possibleFormats);
                    DateTime d2 = DateTime.Now;
                    Result decodedResult = barcodeDecoder.Decode(pImg, decodingOptions);
                    fn_share.systemLog(filename, "===解析时长——" + (DateTime.Now - d2) + "\r\n");

                    if (decodedResult != null)//有些PDF文件并无条形码
                    {
                        barcode = decodedResult.Text;
                    }
                    //barcode = "17041000208";//测试用的单号

                    if (barcode == "")//条码解析失败，或无条码 都纳入 关联失败
                    {
                        DBMgr.ExecuteNonQuery("update list_filerecoginze set status='关联失败' where id=" + id);
                        continue;
                    }
                    //-------------------------------------------------
                    //根据识别出的订单号，查询是否存在此订单
                    dt_order = DBMgr.GetDataTable("select * from list_order a where a.code='" + barcode + "' and a.ISINVALID=0");
                    if (dt_order == null)
                    {
                        DBMgr.ExecuteNonQuery("update list_filerecoginze set status='关联失败',ordercode='" + barcode + "' where id=" + id);
                        continue;
                    }
                    if (dt_order.Rows.Count <= 0)
                    {
                        DBMgr.ExecuteNonQuery("update list_filerecoginze set status='关联失败',ordercode='" + barcode + "' where id=" + id);
                        continue;
                    }

                    //-------------------------------------------------识别成条码，关联到订单表
                    associateno = dt_order.Rows[0]["ASSOCIATENO"].ToString();

                    OracleConnection conn = null;
                    OracleTransaction ot = null;
                    conn = DBMgr.getOrclCon();
                    try
                    {
                        conn.Open();
                        ot = conn.BeginTransaction();

                        associateno = dt_order.Rows[0]["ASSOCIATENO"].ToString();
                        if (associateno != "")//两单关联
                        {
                            sql_insert = @"insert into LIST_ATTACHMENT (id
                                                ,filename,originalname,filetype,uploadtime,ordercode,sizes,filetypename
                                                ,filesuffix,IETYPE) 
                                            values(List_Attachment_Id.Nextval
                                                ,'{0}','{1}','{2}',sysdate,'{3}','{4}','{5}'
                                                ,'{6}','{7}')";
                            sql_insert = string.Format(sql_insert
                                    , "/44/" + barcode + "/" + filepath.Substring(filepath.LastIndexOf(@"/") + 1), originalname, "44", barcode, fi.Length, "订单文件"
                                    , filesuffix, dt_order.Rows[0]["BUSITYPE"].ToString() == "40" ? "仅出口" : "仅进口");
                            DBMgr.ExecuteNonQuery(sql_insert, conn);

                            DataTable dt_asOrder = new DataTable();
                            if (associateno != "")//两单关联
                            {
                                dt_asOrder = DBMgr.GetDataTable("select * from list_order a where a.ISINVALID=0 and ASSOCIATENO='" + associateno + "' and code!='" + barcode + "'");
                            }
                            if (dt_asOrder == null)
                            {

                            }

                            else if (dt_asOrder.Rows.Count < 0)
                            {

                            }
                            else
                            {
                                sql_insert = @"insert into LIST_ATTACHMENT (id
                                                ,filename,originalname,filetype,uploadtime,ordercode,sizes,filetypename
                                                ,filesuffix,IETYPE) 
                                            values(List_Attachment_Id.Nextval
                                                ,'{0}','{1}','{2}',sysdate,'{3}','{4}','{5}'
                                                ,'{6}','{7}')";
                                sql_insert = string.Format(sql_insert
                                        , "/44/" + dt_asOrder.Rows[0]["code"].ToString() + "/" + filepath.Substring(filepath.LastIndexOf(@"/") + 1), originalname, "44", dt_asOrder.Rows[0]["code"].ToString(), fi.Length, "订单文件"
                                        , filesuffix, dt_asOrder.Rows[0]["BUSITYPE"].ToString() == "40" ? "仅出口" : "仅进口");
                                DBMgr.ExecuteNonQuery(sql_insert, conn);
                            }

                        }
                        else
                        {
                            sql_insert = @"insert into LIST_ATTACHMENT (id
                                                ,filename,originalname,filetype,uploadtime,ordercode,sizes,filetypename
                                                ,filesuffix) 
                                            values(List_Attachment_Id.Nextval
                                                ,'{0}','{1}','{2}',sysdate,'{3}','{4}','{5}'
                                                ,'{6}')";
                            sql_insert = string.Format(sql_insert
                                    , "/44/" + barcode + "/" + filepath.Substring(filepath.LastIndexOf(@"/") + 1), originalname, "44", barcode, fi.Length, "订单文件"
                                    , filesuffix);
                            DBMgr.ExecuteNonQuery(sql_insert, conn);
                        }

                        //关联成功 ，文件挪到自动上传到文件服务器的目录，并删除原始目录的文件、修改原始路径为服务器新路径
                        DBMgr.ExecuteNonQuery("update list_filerecoginze set status='已关联',ordercode='" + barcode + "',cusno='" + dt_order.Rows[0]["CUSNO"].ToString()
                            + "',filepath='/44/" + barcode + "/" + filepath.Substring(filepath.LastIndexOf(@"/") + 1) + "' where id=" + id, conn);
                        ot.Commit();

                        fi.CopyTo(direc_pdf + @"/FileUpload/file/" + filepath.Substring(filepath.LastIndexOf(@"/") + 1));
                        fi.Delete();

                    }
                    catch (Exception ex)
                    {
                        ot.Rollback();
                        DBMgr.ExecuteNonQuery("update list_filerecoginze set status='关联失败' where id=" + id);
                        fn_share.systemLog(filename, "异常，id:" + id + ",filepath:" + filepath + "\r\n识别条码失败：" + ex.Message + "\r\n");
                    }
                    finally
                    {
                        conn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                DBMgr.ExecuteNonQuery("update list_filerecoginze set status='关联失败' where id=" + id);
                fn_share.systemLog(filename, "异常，id:" + id + ",filepath:" + filepath + "\r\n识别条码失败：" + ex.Message + "\r\n");
            }

        }

        public static Bitmap MakeGrayscale3(Bitmap original)
        {
            //截取文件右上方1/4部分处理、识别
            //Rectangle cloneRect = new Rectangle(original.Width / 2, 0, original.Width / 2, original.Height / 2);
            //Bitmap newBitmap = original.Clone(cloneRect, original.PixelFormat);
            //create a blank bitmap the same size as original
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);
            //newBitmap.Save(@"E:\test\44\2017-05-25\bit.jpg");
            //get a graphics object from the new image
            Graphics g = Graphics.FromImage(newBitmap);

            //create the grayscale ColorMatrix
            System.Drawing.Imaging.ColorMatrix colorMatrix = new System.Drawing.Imaging.ColorMatrix(
               new float[][] 
              {
                 new float[] {.3f, .3f, .3f, 0, 0},
                 new float[] {.59f, .59f, .59f, 0, 0},
                 new float[] {.11f, .11f, .11f, 0, 0},
                 new float[] {0, 0, 0, 1, 0},
                 new float[] {0, 0, 0, 0, 1}
              });

            //create some image attributes
            ImageAttributes attributes = new ImageAttributes();

            //set the color matrix attribute
            attributes.SetColorMatrix(colorMatrix);

            //draw the original image on the new image
            //using the grayscale color matrix
            g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
               0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);

            //dispose the Graphics object
            g.Dispose();
            return newBitmap;
        }


    }
}
