using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
//using SCADASystem.Model;
using SimpleJSON;
using ICSharpCode.SharpZipLib.Zip;
using System.Net.Http;
using static rff2csv.ReceiveMsg;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace rff2csv

{
    public enum toolType
    {
        //螺丝刀
        AST12,
        AST11,



    }
    public class toolInfo
    {
        public toolType tooltype;
        public object toolConnectStr;
        public string token;
    }

    public class convertrff
    {
        //工具信息
        public List<toolInfo> toolInfo;

        //public List<ReceiveMsg.CurveData> convertrff(string filepath)
        //{
        //    if (File.Exists(filepath))
        //    {
        //        return new GraphViewer(filepath).Read();
                
        //    }
        //    else
        //    {
        //        // Log.Debug($"[Error]曲线收集失败文件名{exData.curveFullName}不存在!SN:{uploadTem.ToolSN},ID:{uploadTem.TighteningID}");
        //        return null;
        //    }
        //}

        public static  string tocsv(string filepath)
        {
            try
            {
                string output = "时间,速度,扭矩,角度,程序步骤,电流,温度\n";
                if (File.Exists(filepath))
                {
                    var aa = new GraphViewer(filepath).Read();
                    
                    if(aa != null && aa.Count>0) 
                    {
                        foreach(var data in aa)
                        {
                            output += data.ts.ToString() + "," + data.value.MotorSpeed + "," + data.value.Torque + "," + data.value.Angle + "," + " ," + data.value.MotorEngine + "," + data.value.MotorTemperature + "\n";
                        }
                    }


                    return output;
                }
                else
                {
                    return ($"ERROR!曲线收集失败文件名{filepath}不存在!");

                }
            }
            catch(Exception e)
            {
                return "ERROR!" + e.Message;
            }


           
        }
    }
}
