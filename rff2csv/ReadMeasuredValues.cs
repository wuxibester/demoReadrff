using System.Collections.Generic;
using System.Collections.Concurrent;
using System;
using System.Linq;
using System.Reflection;
using System.IO;

namespace rff2csv
{
    internal class ReadMeasuredValues
    {
        private readonly byte[] File;

        private readonly CombinedHeaderInformation HeaderInformation;

        public ReadMeasuredValues(byte[] file, CombinedHeaderInformation headerInformation)
        {
            File = file;
            HeaderInformation = headerInformation;
        }

        public GraphsContainer GetMeasuredValues()
        {
            ReadMeasurementLine readMeasurementLine = new ReadMeasurementLine(File, HeaderInformation);
            GraphsContainer graphsContainer = readMeasurementLine.DoWork();
            CalculateTimePerRow calculateTimePerRow = new CalculateTimePerRow(graphsContainer, HeaderInformation);
            calculateTimePerRow.DoCalculation();
            CalculateMeasuredValues calculateMeasuredValues = new CalculateMeasuredValues(graphsContainer, HeaderInformation);
            calculateMeasuredValues.DoCalculation();
            return graphsContainer;
        }
    }

    public interface ICanConvert
    {
        List<GraphType> CanConvert { get; }

        List<GraphPoint> Convert(GraphsContainer graphCollection, SingleGraph graph, RawHeaderData header);
    }

    public class CalculateMeasuredValues
    {
        private static volatile Dictionary<GraphType, ICanConvert> Converters;

        private static readonly object SyncRoot = new object();

        private readonly GraphsContainer GraphCollection;

        private readonly CombinedHeaderInformation HeaderInformation;

        public CalculateMeasuredValues(GraphsContainer graphCollection, CombinedHeaderInformation headerInformation)
        {
            GraphCollection = graphCollection;
            HeaderInformation = headerInformation;
        }

        private static void CacheConvertersIfNecessary()
        {
            if (Converters != null)
            {
                return;
            }

            lock (SyncRoot)
            {
                if (Converters == null)
                {
                    Converters = GetConverters();
                }
            }
        }

        private static Dictionary<GraphType, ICanConvert> GetConverters()
        {
            Dictionary<GraphType, ICanConvert> dictionary = new Dictionary<GraphType, ICanConvert>();
            List<ICanConvert> list = new ActivatorTypeLoader().LoadAndResolve<ICanConvert>();
            foreach (ICanConvert item in list)
            {
                foreach (GraphType item2 in item.CanConvert)
                {
                    dictionary.Add(item2, item);
                }
            }

            return dictionary;
        }

        public void DoCalculation()
        {
            foreach (SingleGraph item in GraphCollection.AllGraphs())
            {
                switch(item.Type)
                {
                    case GraphType.Torque:
                    case GraphType.TorqueMotor:
                    case GraphType.TorqueAdditionalSensor1:
                    case GraphType.TorqueAdditionalSensor2:
                        TorqueConverter canConvertAstTorque = new TorqueConverter();
                        item.CalculatedValues =canConvertAstTorque.Convert(GraphCollection, item, HeaderInformation.HeaderData);
                    break;
                    
                    case GraphType.Angle:
                    case GraphType.AngleMotor:
                    case GraphType.AngleAdditionalSensor1:
                    case GraphType.AngleAdditionalSensor2:
                        AngleConverter canConvertAstAngle = new AngleConverter();
                        item.CalculatedValues =canConvertAstAngle.Convert(GraphCollection, item, HeaderInformation.HeaderData);
                    break;

                    case GraphType.RotationalSpeed:
                         RotationalSpeedConverter canConvertAstSpeed = new RotationalSpeedConverter();
                         item.CalculatedValues =canConvertAstSpeed.Convert(GraphCollection, item, HeaderInformation.HeaderData);
                    break;

                    case GraphType.CurrentMotor:
                        CurrentEngineConverter canConvertAstEngine = new CurrentEngineConverter();
                         item.CalculatedValues =canConvertAstEngine.Convert(GraphCollection, item, HeaderInformation.HeaderData);
                    break;
                    case GraphType.TemperaturePowerUnit:
                    case GraphType.TemperatureMotor:
                        TemperaturePowerUnitConverter canConvertAstTemperature = new TemperaturePowerUnitConverter();
                         item.CalculatedValues =canConvertAstTemperature.Convert(GraphCollection, item, HeaderInformation.HeaderData);
                    break;
                    default:
                        CopyRawValueOneToOne copyRawValueOneToOne = new CopyRawValueOneToOne();
                        item.CalculatedValues = copyRawValueOneToOne.Convert(GraphCollection, item);
                    break;

                }
               
            }
            // CacheConvertersIfNecessary();
            // foreach (SingleGraph item in GraphCollection.AllGraphs())
            // {
            //     if (Converters.ContainsKey(item.Type))
            //     {
            //         ICanConvert canConvert = Converters[item.Type];
            //         item.CalculatedValues = canConvert.Convert(GraphCollection, item, HeaderInformation.HeaderData);
            //     }
            //     else
            //     {
            //         CopyRawValueOneToOne copyRawValueOneToOne = new CopyRawValueOneToOne();
            //         item.CalculatedValues = copyRawValueOneToOne.Convert(GraphCollection, item);
            //     }
            // }
        }
    }

    public class TemperaturePowerUnitConverter : ICanConvert
	{
		// Token: 0x1700006A RID: 106
		// (get) Token: 0x06000163 RID: 355 RVA: 0x00005970 File Offset: 0x00003B70
		public List<GraphType> CanConvert
		{
			get
			{
				return new List<GraphType>
				{
					GraphType.TemperaturePowerUnit,
					GraphType.TemperatureMotor
				};
			}
		}

		// Token: 0x06000164 RID: 356 RVA: 0x00005988 File Offset: 0x00003B88
		public List<GraphPoint> Convert(GraphsContainer graphCollection, SingleGraph graph, RawHeaderData header)
		{
			GraphPoints graphPoints = new GraphPoints(graphCollection.TimePerMeasurement);
			foreach (int value in graph.RawValues)
			{
				decimal d = value / 10.0m;
				decimal value2 = Math.Round(d, 6, MidpointRounding.AwayFromZero);
				graphPoints.Add(new Temperature(value2, new UnitDescription("temperatureDegreeCelsius","Text_TemperaturGrad",1,(decimal y) => y, (decimal x) => x, new List<UnitLocalization>
                {
                    new UnitLocalization("", "en")
                })));
			}
			return graphPoints.AsList();
		}
	}

    public class CurrentEngineConverter : ICanConvert
	{
		// Token: 0x17000068 RID: 104
		// (get) Token: 0x0600015D RID: 349 RVA: 0x000057F4 File Offset: 0x000039F4
		public List<GraphType> CanConvert
		{
			get
			{
				return new List<GraphType>
				{
					GraphType.CurrentMotor
				};
			}
		}

		// Token: 0x0600015E RID: 350 RVA: 0x00005804 File Offset: 0x00003A04
		public List<GraphPoint> Convert(GraphsContainer graphCollection, SingleGraph graph, RawHeaderData header)
		{
			GraphPoints graphPoints = new GraphPoints(graphCollection.TimePerMeasurement);
			foreach (int value in graph.RawValues)
			{
				decimal d = value / 1000.0m;
				decimal value2 = Math.Round(d, 6, MidpointRounding.AwayFromZero);
				graphPoints.Add(new Current(value2, new UnitDescription("Current","Text_Ampere",2,(decimal y) => y, (decimal x) => x, new List<UnitLocalization>
                {
                    new UnitLocalization("", "en")
                })));
			}
			return graphPoints.AsList();
		}
	}

    public class RotationalSpeedConverter : ICanConvert
	{
		// Token: 0x17000069 RID: 105
		// (get) Token: 0x06000160 RID: 352 RVA: 0x000058A0 File Offset: 0x00003AA0
		public List<GraphType> CanConvert
		{
			get
			{
				return new List<GraphType>
				{
					GraphType.RotationalSpeed
				};
			}
		}

		// Token: 0x06000161 RID: 353 RVA: 0x000058B0 File Offset: 0x00003AB0
		public List<GraphPoint> Convert(GraphsContainer graphCollection, SingleGraph graph, RawHeaderData header)
		{
			GraphPoints graphPoints = new GraphPoints(graphCollection.TimePerMeasurement);
			decimal d = -2.5m / (header.GearTransmission / 100.0m);
			foreach (int value in graph.RawValues)
			{
				decimal d2 = value * d;
				decimal value2 = Math.Round(d2, 6, MidpointRounding.AwayFromZero);
				graphPoints.Add(new RotationalSpeed(value2, new UnitDescription("rotationalSpeed","Text_RevolutionsPerMinute",0,(decimal y) => y, (decimal x) => x, new List<UnitLocalization>
                {
                    new UnitLocalization("", "en")
                })));
			}
			return graphPoints.AsList();
		}
	}

    public class AngleConverter : ICanConvert
	{
		// Token: 0x17000066 RID: 102
		// (get) Token: 0x06000157 RID: 343 RVA: 0x00005658 File Offset: 0x00003858
		public List<GraphType> CanConvert
		{
			get
			{
				return new List<GraphType>
				{
					GraphType.Angle,
					GraphType.AngleMotor,
					GraphType.AngleAdditionalSensor1,
					GraphType.AngleAdditionalSensor2
				};
			}
		}

		// Token: 0x06000158 RID: 344 RVA: 0x00005680 File Offset: 0x00003880
		public List<GraphPoint> Convert(GraphsContainer graphCollection, SingleGraph graph, RawHeaderData header)
		{
			GraphPoints graphPoints = new GraphPoints(graphCollection.TimePerMeasurement);
			int num = graph.RawValues[0];
			decimal d = -360.0m / (header.Increment * (header.GearTransmission / 100.0m) * 4.0m);
			foreach (int num2 in graph.RawValues)
			{
				decimal d2 = (num2 - num) * d;
				decimal value = Math.Round(d2, 6, MidpointRounding.AwayFromZero);
				graphPoints.Add(new Angle(value, new UnitDescription("angleGrad","Text_Grad",2,(decimal y) => y, (decimal x) => x, new List<UnitLocalization>
                {
                    new UnitLocalization("", "en")
                })));
			}
			return graphPoints.AsList();
		}
	}

    public class TorqueConverter : ICanConvert
	{
		// Token: 0x1700006B RID: 107
		// (get) Token: 0x06000166 RID: 358 RVA: 0x00005A24 File Offset: 0x00003C24
		public List<GraphType> CanConvert
		{
			get
			{
				return new List<GraphType>
				{
					GraphType.Torque,
					GraphType.TorqueMotor,
					GraphType.TorqueAdditionalSensor1,
					GraphType.TorqueAdditionalSensor2
				};
			}
		}

		// Token: 0x06000167 RID: 359 RVA: 0x00005A4C File Offset: 0x00003C4C
		public List<GraphPoint> Convert(GraphsContainer graphCollection, SingleGraph graph, RawHeaderData header)
		{
			GraphPoints graphPoints = new GraphPoints(graphCollection.TimePerMeasurement);
			decimal d = header.GearTransmission / 100.0m * header.GearEfficacy / 100.0m * 1.0m / (header.TorqueCalibrationValue * 10.0m) / (decimal)Math.Pow(2.0, (double)header.TorqueShift);
			foreach (int value in graph.RawValues)
			{
				decimal d2 = value * d;
				decimal value2 = Math.Round(d2, 6, MidpointRounding.AwayFromZero);
				graphPoints.Add(new Torque(value2, new UnitDescription("torqueNewtonMeter","Text_NewtonMeter",3,(decimal y) => y, (decimal x) => x, new List<UnitLocalization>
                {
                    new UnitLocalization("", "en")
                })));
			}
			return graphPoints.AsList();
		}
	}

     public class CalculateTimePerRow
    {
        private readonly GraphsContainer GraphCollection;

        private readonly CombinedHeaderInformation HeaderInformation;

        public CalculateTimePerRow(GraphsContainer graphCollection, CombinedHeaderInformation headerInformation)
        {
            GraphCollection = graphCollection;
            HeaderInformation = headerInformation;
        }

        public void DoCalculation()
        {
            int lineLength = HeaderInformation.LineInterpretingInformation.LineLength;
            decimal timePerMeasurement = (decimal)lineLength / 2.0m * 0.025m;
            GraphCollection.TimePerMeasurement = timePerMeasurement;
        }
    }
}