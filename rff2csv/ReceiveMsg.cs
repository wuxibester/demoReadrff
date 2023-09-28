using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rff2csv
{
  
    //给外部的数据
  
    public class ReceiveMsg
    {
        //曲线数据
        public class CurveData
        {
            public float ts{ get; set; }
            public CurveItem value{ get; set; }

        }

        public class CurveItem
        {
            //扭矩曲线
            //public float Tq{ get; set; }
            public float Torque{ get; set; }
           
            //角度曲线
            //public float Ag{ get; set; }
            public float Angle{ get; set; }

            //额外的数据，不重要
            //电机速度
            //public float Ms{ get; set; }
            public float MotorSpeed{ get; set; }          
            //电机电流
            //public float Me{ get; set; }
            public float MotorEngine{ get; set; }
            //电机温度
            //public float Mt{get;set;}
            public float MotorTemperature{get;set;}
        }


        //每次扭矩的信息
        public class ReceiveData
        {
            public string SN { get; set; }

            public string FriendName{get;set;}
            public string ToolName { get; set; }
            public string ConnectString { get; set; }
            public string DeviceID { get; set; }
            public string ToolType { get; set; }
            public string ToolSN { get; set; }
            public string TightingTime { get; set; }//
            public long TightingID { get; set; }//
            public string VinNumber { get; set; }//
            public string ParamterID { get; set; }//
            public string TighteningStatus { get; set; } //
            public double Torque { get; set; }
            public double Angle { get; set; }
            public string ToolSerialNumber { get; set; }
            public string TorqueUnit { get; set; }
            public string AngleUnit { get; set; }
            //public List<CurveData> CurveData {get;set;}
            //厂家自己的数据
            public object  ExtraData{get;set;}

        }
        //数据类型
        public enum ReceiveMsgType
        {
            //LOG信息
            ReceiveMsgStr,
            //拧紧数据
            ReceiveMsgData,

            ReceiveMsgPset,
            ReceiveMsgCom,
            ReceiveMsgLost,

        }


        public ReceiveMsgType nType { get; set; }//类型

        public string strMsg { get; set; }//输出的文本

        public ReceiveData sData { get; set; }//输出的数据

        public class toolState
        {
            public bool state;
        }
    }
}
