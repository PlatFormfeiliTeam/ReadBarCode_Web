using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.IO;
using System.Data;

namespace ReadBarCode_Web
{
    class fn_share
    {
        public void systemLog(string filename, string msg)
        {
            if (File.Exists(filename.Trim()))
            {
                StreamWriter sw = File.AppendText(filename.Trim());
                sw.WriteLine(msg);
                sw.Close();
            }
            else
            {
                StreamWriter sw = new StreamWriter(filename, false, Encoding.Default);
                sw.Write(msg);
                sw.Close();

            }
        }

        public void WriteTXT_Unicode(string filename, string msg)
        {
            if (File.Exists(filename.Trim()))
            {
                File.Delete(filename);
            }
            StreamWriter sw = new StreamWriter(filename, false, Encoding.Unicode);
            sw.Write(msg);
            sw.Close();
        }
       
    }
}
