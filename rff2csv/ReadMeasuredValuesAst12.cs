using System.Collections.Generic;
using System.Collections.Concurrent;
using System;
using System.Linq;
using System.Reflection;
using System.IO;

namespace rff2csv
{
    internal class ReadMeasuredValuesAst12
    {
        private readonly byte[] File;

        private readonly CombinedHeaderInformationAst12 HeaderInformation;

        public ReadMeasuredValuesAst12(byte[] file, CombinedHeaderInformationAst12 headerInformation)
        {
            File = file;
            HeaderInformation = headerInformation;
        }

        public GraphsContainer GetMeasuredValues()
        {
            ReadMeasurementLine readMeasurementLine = new ReadMeasurementLine(File, HeaderInformation);
            GraphsContainer graphsContainer = readMeasurementLine.DoWork();
            CalculateTimePerRowAst12 calculateTimePerRowAst = new CalculateTimePerRowAst12(graphsContainer, HeaderInformation);
            calculateTimePerRowAst.DoCalculation();
            CalculateMeasuredValuesAst12 calculateMeasuredValuesAst = new CalculateMeasuredValuesAst12(graphsContainer, HeaderInformation);
            calculateMeasuredValuesAst.DoCalculation();
            return graphsContainer;
        }
    }
      public class CalculateMeasuredValuesAst12
    {
        private static volatile Dictionary<GraphType, ICanConvertAst12> Converters;

        private static readonly object SyncRoot = new object();

        private readonly GraphsContainer GraphCollection;

        private readonly CombinedHeaderInformationAst12 HeaderInformation;

        public CalculateMeasuredValuesAst12(GraphsContainer graphCollection, CombinedHeaderInformationAst12 headerInformation)
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

        private static Dictionary<GraphType, ICanConvertAst12> GetConverters()
        {
            Dictionary<GraphType, ICanConvertAst12> dictionary = new Dictionary<GraphType, ICanConvertAst12>();
            List<ICanConvertAst12> list = new ActivatorTypeLoader().LoadAndResolve<ICanConvertAst12>();
            foreach (ICanConvertAst12 item in list)
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
            //CacheConvertersIfNecessary();
            foreach (SingleGraph item in GraphCollection.AllGraphs())
            {
                switch(item.Type)
                {
                    case GraphType.Torque:
                    case GraphType.TorqueMotor:
                    case GraphType.TorqueAdditionalSensor1:
                    case GraphType.TorqueAdditionalSensor2:
                        TorqueConverterAst12 canConvertAstTorque = new TorqueConverterAst12();
                        item.CalculatedValues =canConvertAstTorque.Convert(GraphCollection, item, HeaderInformation.HeaderData);
                    break;
                    
                    case GraphType.Angle:
                    case GraphType.AngleMotor:
                    case GraphType.AngleAdditionalSensor1:
                    case GraphType.AngleAdditionalSensor2:
                        AngleConverterAst12 canConvertAstAngle = new AngleConverterAst12();
                        item.CalculatedValues =canConvertAstAngle.Convert(GraphCollection, item, HeaderInformation.HeaderData);
                    break;

                    case GraphType.RotationalSpeed:
                         RotationalSpeedConverterAst12 canConvertAstSpeed = new RotationalSpeedConverterAst12();
                         item.CalculatedValues =canConvertAstSpeed.Convert(GraphCollection, item, HeaderInformation.HeaderData);
                    break;

                    case GraphType.CurrentMotor:
                        CurrentEngineConverterAst12 canConvertAstEngine = new CurrentEngineConverterAst12();
                         item.CalculatedValues =canConvertAstEngine.Convert(GraphCollection, item, HeaderInformation.HeaderData);
                    break;
                    case GraphType.TemperaturePowerUnit:
                    case GraphType.TemperatureMotor:
                        TemperaturePowerUnitConverterAst12 canConvertAstTemperature = new TemperaturePowerUnitConverterAst12();
                         item.CalculatedValues =canConvertAstTemperature.Convert(GraphCollection, item, HeaderInformation.HeaderData);
                    break;
                    default:
                        CopyRawValueOneToOne copyRawValueOneToOne = new CopyRawValueOneToOne();
                        item.CalculatedValues = copyRawValueOneToOne.Convert(GraphCollection, item);
                    break;

                }
               
            }
        
		}
    }

    public class CopyRawValueOneToOne
    {
        public IUnit UnitYAxis => new NoUnit();

        public List<GraphPoint> Convert(GraphsContainer graphCollection, SingleGraph graph)
        {
            GraphPoints graphPoints = new GraphPoints(graphCollection.TimePerMeasurement);
            foreach (int rawValue in graph.RawValues)
            {
                graphPoints.Add(new NoUnit(rawValue));
            }

            return graphPoints.AsList();
        }
    }

    public class GraphPoints
    {
        private readonly decimal DurationOfOneValue;

        private readonly List<GraphPoint> Points = new List<GraphPoint>();

        private decimal CurrentTime;

        private IUnit LastValue;

        private bool WasLastPointAdded;

        public GraphPoints(decimal durationOfOneValue)
        {
            DurationOfOneValue = durationOfOneValue;
            CurrentTime = default(decimal);
        }

        public void Add(IUnit value)
        {
            if (LastValue == null || LastValue.ValueInBaseUnit != value.ValueInBaseUnit)
            {
                AddPoint(value);
                WasLastPointAdded = true;
            }
            else
            {
                WasLastPointAdded = false;
            }

            LastValue = value;
            IncreaseTime();
        }

        private void AddPoint(IUnit value)
        {
            GraphPoint item = new GraphPoint(CurrentTime, value);
            Points.Add(item);
        }

        private void IncreaseTime()
        {
            CurrentTime += DurationOfOneValue;
        }

        public List<GraphPoint> AsList()
        {
            if (!WasLastPointAdded)
            {
                WasLastPointAdded = true;
                AddPoint(LastValue);
            }

            return Points;
        }
    }

    public class NoUnit : IUnit
    {
        public decimal ValueInBaseUnit { get; set; }

        public UnitDescription BaseFormat => NoUnits.BaseFormat;

        public NoUnit()
        {
        }

        public NoUnit(decimal value)
            : this(value, NoUnits.BaseFormat)
        {
        }

        public NoUnit(decimal value, UnitDescription formatOfValue)
        {
            ValueInBaseUnit = formatOfValue.ToBaseUnit(value);
        }

        public FormatedUnit ConvertToFormat()
        {
            return new FormatedUnit(NoUnits.BaseFormat, ValueInBaseUnit);
        }

        public FormatedUnit ConvertToFormat(UnitDescription targetFormat)
        {
            return new FormatedUnit(targetFormat, ValueInBaseUnit);
        }

        public IUnit Clone()
        {
            return new NoUnit(ValueInBaseUnit, BaseFormat);
        }

        public static bool operator ==(NoUnit value1, NoUnit value2)
        {
            if ((object)value1 == value2)
            {
                return true;
            }

            if ((object)value1 == null)
            {
                return false;
            }

            if ((object)value2 == null)
            {
                return false;
            }

            return value1.Equals(value2);
        }

        public static bool operator !=(NoUnit value1, NoUnit value2)
        {
            return !(value1 == value2);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            NoUnit noUnit = (NoUnit)obj;
            return ValueInBaseUnit == noUnit.ValueInBaseUnit;
        }

        public override int GetHashCode()
        {
            return ValueInBaseUnit.GetHashCode();
        }

        public static NoUnit operator -(NoUnit value1, NoUnit value2)
        {
            return new NoUnit(value1.ValueInBaseUnit - value2.ValueInBaseUnit);
        }

        public static NoUnit operator +(NoUnit value1, NoUnit value2)
        {
            return new NoUnit(value1.ValueInBaseUnit + value2.ValueInBaseUnit);
        }

        public static NoUnit operator *(NoUnit value1, NoUnit value2)
        {
            return new NoUnit(value1.ValueInBaseUnit * value2.ValueInBaseUnit);
        }

        public static NoUnit operator /(NoUnit value1, NoUnit value2)
        {
            return new NoUnit(value1.ValueInBaseUnit / value2.ValueInBaseUnit);
        }

        public override string ToString()
        {
            return $"{ValueInBaseUnit}";
        }
    }

     public static class NoUnits
    {
        public static List<UnitDescription> DisplayFormats
        {
            get
            {
                List<UnitDescription> list = new List<UnitDescription>();
                list.Add(new UnitDescription("noUnit", "Text_NoUnit", 2, (decimal y) => y, (decimal x) => x, new List<UnitLocalization>
                {
                    new UnitLocalization("", "en")
                }));
                return list;
            }
        }

        public static UnitDescription NoUnit => new UnitDescription("noUnit", "Text_NoUnit", 2, (decimal y) => y, (decimal x) => x, new List<UnitLocalization>
        {
            new UnitLocalization("", "en")
        });

        public static UnitDescription BaseFormat => NoUnit;
    }

    public class ActivatorTypeLoader 
    {
        private readonly CachedTypeLoader CachedTypeLoader = new CachedTypeLoader();

        public string AssemblyFilter
        {
            get
            {
                return CachedTypeLoader.AssemblyFilter;
            }
            set
            {
                CachedTypeLoader.AssemblyFilter = value;
            }
        }

        public List<TypeToLoad> LoadAndResolve<TypeToLoad>() where TypeToLoad : class
        {
            return LoadAndResolve<TypeToLoad>(null);
        }

        public List<TypeToLoad> LoadAndResolve<TypeToLoad>(params object[] args) where TypeToLoad : class
        {
            List<TypeToLoad> list = new List<TypeToLoad>();
            List<Type> source = CachedTypeLoader.Load<TypeToLoad>();
            list.AddRange(source.Select((Type type) => (args != null) ? ((TypeToLoad)Activator.CreateInstance(type, args)) : ((TypeToLoad)Activator.CreateInstance(type))));
            return list;
        }
    }

     public class CachedTypeLoader
    {
        private static readonly ConcurrentDictionary<string, List<Type>> CachedTypes = new ConcurrentDictionary<string, List<Type>>(StringComparer.OrdinalIgnoreCase);

        public string AssemblyFilter { get; set; } = "Deprag";


        public List<Type> Load<TypeToLoad>() where TypeToLoad : class
        {
            Type typeFromHandle = typeof(TypeToLoad);
            return Load(typeFromHandle);
        }

        public List<Type> Load(Type typeOfT)
        {
            string key = GetKey(typeOfT);
            if (CachedTypes.ContainsKey(key))
            {
                return CachedTypes[key];
            }

            AddToCache(typeOfT);
            return CachedTypes[key];
        }

        private void AddToCache(Type typeOfT)
        {
            string key = GetKey(typeOfT);
            if (!CachedTypes.ContainsKey(key))
            {
                List<Assembly> inAssemblies = (from x in AppDomain.CurrentDomain.GetAssemblies()
                                               where !x.FullName.StartsWith("Microsoft.", StringComparison.InvariantCultureIgnoreCase) && !x.FullName.StartsWith("System.", StringComparison.InvariantCultureIgnoreCase)
                                               select x).ToList();
                List<Type> value = FindAllImplementations(typeOfT, AssemblyFilter, inAssemblies);
                CachedTypes.TryAdd(key, value);
            }
        }

        private string GetKey(Type type)
        {
            return type?.ToString() + AssemblyFilter;
        }

        private static List<Type> FindAllImplementations(Type type, string filter, IEnumerable<Assembly> inAssemblies)
        {
            List<Type> list = new List<Type>();
            foreach (Assembly inAssembly in inAssemblies)
            {
                try
                {
                    if (!string.IsNullOrEmpty(filter) && !inAssembly.ToString().Contains(filter))
                    {
                        continue;
                    }

                    Type[] exportedTypes = inAssembly.GetExportedTypes();
                    IEnumerable<Type> collection = from t in exportedTypes
                                                   let interfaces = t.GetInterfaces()
                                                   where (!t.IsAbstract && interfaces.Contains(type)) || (t.BaseType != null && t.BaseType == type) || interfaces.Any((Type c) => c.IsGenericType && c.GetGenericTypeDefinition() == type)
                                                   select t;
                    list.AddRange(collection);
                }
                catch (ReflectionTypeLoadException ex)
                {
                    IEnumerable<string> values = ex.LoaderExceptions.Select((Exception c) => c.Message);
                    string text = string.Join(", ", values);
                    throw new Exception(inAssembly.FullName + ": " + text, ex);
                }
                catch (Exception ex2)
                {
                    string text2 = "CurrentAssembly: " + inAssembly.FullName + ", Filter: " + filter;
                    text2 += ex2;
                    throw new Exception(text2);
                }
            }

            return list;
        }
    }

    public interface ICanConvertAst12
    {
        List<GraphType> CanConvert { get; }

        List<GraphPoint> Convert(GraphsContainer graphCollection, SingleGraph graph, RawHeaderDataAst12 header);
    }

    public class TorqueConverterAst12 : ICanConvertAst12
	{
		public List<GraphType> CanConvert => new List<GraphType>
		{
			GraphType.Torque,
			GraphType.TorqueMotor,
			GraphType.TorqueAdditionalSensor1,
			GraphType.TorqueAdditionalSensor2
		};

		public List<GraphPoint> Convert(GraphsContainer graphCollection, SingleGraph graph, RawHeaderDataAst12 header)
		{
			GraphPoints graphPoints = new GraphPoints(graphCollection.TimePerMeasurement);
			decimal num = (decimal)header.GearTransmission / 10000.0m * (decimal)header.GearEfficacy / 100.0m * 1.0m / ((decimal)header.TorqueCalibrationValue * 1000.0m) / (decimal)Math.Pow(2.0, header.TorqueShift);
			foreach (int rawValue in graph.RawValues)
			{
				decimal d = (decimal)rawValue * num;
				decimal value = Math.Round(d, 6, MidpointRounding.AwayFromZero);
				graphPoints.Add(new Torque(value, new UnitDescription("torqueNewtonMeter","Text_NewtonMeter",3,(decimal y) => y, (decimal x) => x, new List<UnitLocalization>
                {
                    new UnitLocalization("", "en")
                })));
			}
			return graphPoints.AsList();
		}
	}

    public class AngleConverterAst12 : ICanConvertAst12
	{
		// Token: 0x17000051 RID: 81
		// (get) Token: 0x060000EC RID: 236 RVA: 0x000040B1 File Offset: 0x000022B1
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

		// Token: 0x060000ED RID: 237 RVA: 0x000040D8 File Offset: 0x000022D8
		public List<GraphPoint> Convert(GraphsContainer graphCollection, SingleGraph graph, RawHeaderDataAst12 header)
		{
			GraphPoints graphPoints = new GraphPoints(graphCollection.TimePerMeasurement);
			int num = graph.RawValues[0];
			decimal d = -360.0m / (header.Increment * (header.GearTransmission / 10000.0m) * 4.0m);
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

    public static class TorqueUnits
    {
        public static List<UnitDescription> DisplayFormats { get; }
        public static UnitDescription FootPound { get; }
        public static UnitDescription InchPound { get; }
        public static UnitDescription KilogrammMeter { get; }
        public static UnitDescription KilogrammZentimeter { get; }
        public static UnitDescription NewtonCentimeter { get; }
        public static UnitDescription NewtonMeter { get; }
        public static UnitDescription BaseFormat { get; }
    }

    public class Torque : IUnit
	{
		// Token: 0x0600013C RID: 316 RVA: 0x00004AB8 File Offset: 0x00002CB8
		public Torque()
		{
		}

		// Token: 0x0600013D RID: 317 RVA: 0x00004AC0 File Offset: 0x00002CC0
		public Torque(decimal value) : this(value, TorqueUnits.BaseFormat)
		{
		}

		// Token: 0x0600013E RID: 318 RVA: 0x00004ACE File Offset: 0x00002CCE
		public Torque(decimal value, UnitDescription formatOfValue)
		{
			this.ValueInBaseUnit = formatOfValue.ToBaseUnit(value);
		}

		// Token: 0x17000053 RID: 83
		// (get) Token: 0x0600013F RID: 319 RVA: 0x00004AE3 File Offset: 0x00002CE3
		// (set) Token: 0x06000140 RID: 320 RVA: 0x00004AEB File Offset: 0x00002CEB
		public decimal ValueInBaseUnit { get; set; }

		// Token: 0x06000141 RID: 321 RVA: 0x00004AF4 File Offset: 0x00002CF4
		public FormatedUnit ConvertToFormat()
		{
			return new FormatedUnit(TorqueUnits.BaseFormat, this.ValueInBaseUnit);
		}

		// Token: 0x06000142 RID: 322 RVA: 0x00004B06 File Offset: 0x00002D06
		public FormatedUnit ConvertToFormat(UnitDescription targetFormat)
		{
			return new FormatedUnit(targetFormat, this.ValueInBaseUnit);
		}

		// Token: 0x17000054 RID: 84
		// (get) Token: 0x06000143 RID: 323 RVA: 0x00004B14 File Offset: 0x00002D14
		public UnitDescription BaseFormat
		{
			get
			{
				return TorqueUnits.BaseFormat;
			}
		}

		// Token: 0x06000144 RID: 324 RVA: 0x00004B1B File Offset: 0x00002D1B
		public IUnit Clone()
		{
			return new Torque(this.ValueInBaseUnit, this.BaseFormat);
		}

		// Token: 0x06000145 RID: 325 RVA: 0x00004B2E File Offset: 0x00002D2E
		public static bool operator ==(Torque value1, Torque value2)
		{
			return value1 == value2 || (value1 != null && value2 != null && value1.Equals(value2));
		}

		// Token: 0x06000146 RID: 326 RVA: 0x00004B47 File Offset: 0x00002D47
		public static bool operator !=(Torque value1, Torque value2)
		{
			return !(value1 == value2);
		}

		// Token: 0x06000147 RID: 327 RVA: 0x00004B54 File Offset: 0x00002D54
		public override bool Equals(object obj)
		{
			if (obj == null || base.GetType() != obj.GetType())
			{
				return false;
			}
			Torque torque = (Torque)obj;
			return this.ValueInBaseUnit == torque.ValueInBaseUnit;
		}

		// Token: 0x06000148 RID: 328 RVA: 0x00004B94 File Offset: 0x00002D94
		public override int GetHashCode()
		{
			return this.ValueInBaseUnit.GetHashCode();
		}

		// Token: 0x06000149 RID: 329 RVA: 0x00004BAF File Offset: 0x00002DAF
		public static Torque operator -(Torque value1, Torque value2)
		{
			return new Torque(value1.ValueInBaseUnit - value2.ValueInBaseUnit);
		}

		// Token: 0x0600014A RID: 330 RVA: 0x00004BC7 File Offset: 0x00002DC7
		public static Torque operator +(Torque value1, Torque value2)
		{
			return new Torque(value1.ValueInBaseUnit + value2.ValueInBaseUnit);
		}

		// Token: 0x0600014B RID: 331 RVA: 0x00004BDF File Offset: 0x00002DDF
		public static Torque operator *(Torque value1, Torque value2)
		{
			return new Torque(value1.ValueInBaseUnit * value2.ValueInBaseUnit);
		}

		// Token: 0x0600014C RID: 332 RVA: 0x00004BF7 File Offset: 0x00002DF7
		public static Torque operator /(Torque value1, Torque value2)
		{
			return new Torque(value1.ValueInBaseUnit / value2.ValueInBaseUnit);
		}

		// Token: 0x0600014D RID: 333 RVA: 0x00004C0F File Offset: 0x00002E0F
		public override string ToString()
		{
			return string.Format("{0}", this.ValueInBaseUnit);
		}
	}

    public static class AngleUnits
	{
		// Token: 0x17000019 RID: 25
		// (get) Token: 0x06000062 RID: 98 RVA: 0x00002CB8 File Offset: 0x00000EB8
		public static List<UnitDescription> DisplayFormats
		{
			get
			{
				List<UnitDescription> list = new List<UnitDescription>();
				list.Add(new UnitDescription("angleGrad", "Text_Grad", 2, (decimal y) => y, (decimal x) => x, new List<UnitLocalization>
				{
					new UnitLocalization("°", "en")
				}));
				return list;
			}
		}

		// Token: 0x1700001A RID: 26
		// (get) Token: 0x06000063 RID: 99 RVA: 0x00002D3C File Offset: 0x00000F3C
		public static UnitDescription Degrees
		{
			get
			{
				return new UnitDescription("angleGrad", "Text_Grad", 2, (decimal y) => y, (decimal x) => x, new List<UnitLocalization>
				{
					new UnitLocalization("°", "en")
				});
			}
		}

		// Token: 0x1700001B RID: 27
		// (get) Token: 0x06000064 RID: 100 RVA: 0x00002DB1 File Offset: 0x00000FB1
		public static UnitDescription BaseFormat
		{
			get
			{
				return AngleUnits.Degrees;
			}
		}
	}
    public class Angle : IUnit
	{
		// Token: 0x06000050 RID: 80 RVA: 0x00002B48 File Offset: 0x00000D48
		public Angle()
		{
		}

		// Token: 0x06000051 RID: 81 RVA: 0x00002B50 File Offset: 0x00000D50
		public Angle(decimal value) : this(value, AngleUnits.BaseFormat)
		{
		}

		// Token: 0x06000052 RID: 82 RVA: 0x00002B5E File Offset: 0x00000D5E
		public Angle(decimal value, UnitDescription formatOfValue)
		{
			this.ValueInBaseUnit = formatOfValue.ToBaseUnit(value);
		}

		// Token: 0x17000017 RID: 23
		// (get) Token: 0x06000053 RID: 83 RVA: 0x00002B73 File Offset: 0x00000D73
		// (set) Token: 0x06000054 RID: 84 RVA: 0x00002B7B File Offset: 0x00000D7B
		public decimal ValueInBaseUnit { get; set; }

		// Token: 0x06000055 RID: 85 RVA: 0x00002B84 File Offset: 0x00000D84
		public FormatedUnit ConvertToFormat()
		{
			return new FormatedUnit(AngleUnits.BaseFormat, this.ValueInBaseUnit);
		}

		// Token: 0x06000056 RID: 86 RVA: 0x00002B96 File Offset: 0x00000D96
		public FormatedUnit ConvertToFormat(UnitDescription targetFormat)
		{
			return new FormatedUnit(targetFormat, this.ValueInBaseUnit);
		}

		// Token: 0x17000018 RID: 24
		// (get) Token: 0x06000057 RID: 87 RVA: 0x00002BA4 File Offset: 0x00000DA4
		public UnitDescription BaseFormat
		{
			get
			{
				return AngleUnits.BaseFormat;
			}
		}

		// Token: 0x06000058 RID: 88 RVA: 0x00002BAB File Offset: 0x00000DAB
		public IUnit Clone()
		{
			return new Angle(this.ValueInBaseUnit, this.BaseFormat);
		}

		// Token: 0x06000059 RID: 89 RVA: 0x00002BBE File Offset: 0x00000DBE
		public static bool operator ==(Angle value1, Angle value2)
		{
			return value1 == value2 || (value1 != null && value2 != null && value1.Equals(value2));
		}

		// Token: 0x0600005A RID: 90 RVA: 0x00002BD7 File Offset: 0x00000DD7
		public static bool operator !=(Angle value1, Angle value2)
		{
			return !(value1 == value2);
		}

		// Token: 0x0600005B RID: 91 RVA: 0x00002BE4 File Offset: 0x00000DE4
		public override bool Equals(object obj)
		{
			if (obj == null || base.GetType() != obj.GetType())
			{
				return false;
			}
			Angle angle = (Angle)obj;
			return this.ValueInBaseUnit == angle.ValueInBaseUnit;
		}

		// Token: 0x0600005C RID: 92 RVA: 0x00002C24 File Offset: 0x00000E24
		public override int GetHashCode()
		{
			return this.ValueInBaseUnit.GetHashCode();
		}

		// Token: 0x0600005D RID: 93 RVA: 0x00002C3F File Offset: 0x00000E3F
		public static Angle operator -(Angle value1, Angle value2)
		{
			return new Angle(value1.ValueInBaseUnit - value2.ValueInBaseUnit);
		}

		// Token: 0x0600005E RID: 94 RVA: 0x00002C57 File Offset: 0x00000E57
		public static Angle operator +(Angle value1, Angle value2)
		{
			return new Angle(value1.ValueInBaseUnit + value2.ValueInBaseUnit);
		}

		// Token: 0x0600005F RID: 95 RVA: 0x00002C6F File Offset: 0x00000E6F
		public static Angle operator *(Angle value1, Angle value2)
		{
			return new Angle(value1.ValueInBaseUnit * value2.ValueInBaseUnit);
		}

		// Token: 0x06000060 RID: 96 RVA: 0x00002C87 File Offset: 0x00000E87
		public static Angle operator /(Angle value1, Angle value2)
		{
			return new Angle(value1.ValueInBaseUnit / value2.ValueInBaseUnit);
		}

		// Token: 0x06000061 RID: 97 RVA: 0x00002C9F File Offset: 0x00000E9F
		public override string ToString()
		{
			return string.Format("{0}", this.ValueInBaseUnit);
		}
	}

    public class RotationalSpeedConverterAst12 : ICanConvertAst12
	{
		// Token: 0x17000053 RID: 83
		// (get) Token: 0x060000F2 RID: 242 RVA: 0x00004278 File Offset: 0x00002478
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

		// Token: 0x060000F3 RID: 243 RVA: 0x00004288 File Offset: 0x00002488
		public List<GraphPoint> Convert(GraphsContainer graphCollection, SingleGraph graph, RawHeaderDataAst12 header)
		{
			GraphPoints graphPoints = new GraphPoints(graphCollection.TimePerMeasurement);
			decimal d = -1.0m / (header.GearTransmission / 10000.0m);
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

    public static class RotationalSpeedUnits
	{
		// Token: 0x1700003F RID: 63
		// (get) Token: 0x060000F8 RID: 248 RVA: 0x00004078 File Offset: 0x00002278
		public static List<UnitDescription> DisplayFormats
		{
			get
			{
				List<UnitDescription> list = new List<UnitDescription>();
				list.Add(new UnitDescription("rotationalSpeed", "Text_RevolutionsPerMinute", 0, (decimal y) => y, (decimal x) => x, new List<UnitLocalization>
				{
					new UnitLocalization("rpm", "en"),
					new UnitLocalization("Min⁻¹", "de")
				}));
				return list;
			}
		}

		// Token: 0x17000040 RID: 64
		// (get) Token: 0x060000F9 RID: 249 RVA: 0x00004110 File Offset: 0x00002310
		public static UnitDescription RevolutionsPerMinute
		{
			get
			{
				return new UnitDescription("rotationalSpeed", "Text_RevolutionsPerMinute", 0, (decimal y) => y, (decimal x) => x, new List<UnitLocalization>
				{
					new UnitLocalization("rpm", "en"),
					new UnitLocalization("Min⁻¹", "de")
				});
			}
		}

		// Token: 0x17000041 RID: 65
		// (get) Token: 0x060000FA RID: 250 RVA: 0x0000419A File Offset: 0x0000239A
		public static UnitDescription BaseFormat
		{
			get
			{
				return RotationalSpeedUnits.RevolutionsPerMinute;
			}
		}
	}
    public class RotationalSpeed : IUnit
	{
		// Token: 0x060000E6 RID: 230 RVA: 0x00003F08 File Offset: 0x00002108
		public RotationalSpeed()
		{
		}

		// Token: 0x060000E7 RID: 231 RVA: 0x00003F10 File Offset: 0x00002110
		public RotationalSpeed(decimal value) : this(value, RotationalSpeedUnits.BaseFormat)
		{
		}

		// Token: 0x060000E8 RID: 232 RVA: 0x00003F1E File Offset: 0x0000211E
		public RotationalSpeed(decimal value, UnitDescription formatOfValue)
		{
			this.ValueInBaseUnit = formatOfValue.ToBaseUnit(value);
		}

		// Token: 0x1700003D RID: 61
		// (get) Token: 0x060000E9 RID: 233 RVA: 0x00003F33 File Offset: 0x00002133
		// (set) Token: 0x060000EA RID: 234 RVA: 0x00003F3B File Offset: 0x0000213B
		public decimal ValueInBaseUnit { get; set; }

		// Token: 0x060000EB RID: 235 RVA: 0x00003F44 File Offset: 0x00002144
		public FormatedUnit ConvertToFormat()
		{
			return new FormatedUnit(RotationalSpeedUnits.BaseFormat, this.ValueInBaseUnit);
		}

		// Token: 0x060000EC RID: 236 RVA: 0x00003F56 File Offset: 0x00002156
		public FormatedUnit ConvertToFormat(UnitDescription targetFormat)
		{
			return new FormatedUnit(targetFormat, this.ValueInBaseUnit);
		}

		// Token: 0x1700003E RID: 62
		// (get) Token: 0x060000ED RID: 237 RVA: 0x00003F64 File Offset: 0x00002164
		public UnitDescription BaseFormat
		{
			get
			{
				return RotationalSpeedUnits.BaseFormat;
			}
		}

		// Token: 0x060000EE RID: 238 RVA: 0x00003F6B File Offset: 0x0000216B
		public IUnit Clone()
		{
			return new RotationalSpeed(this.ValueInBaseUnit, this.BaseFormat);
		}

		// Token: 0x060000EF RID: 239 RVA: 0x00003F7E File Offset: 0x0000217E
		public static bool operator ==(RotationalSpeed value1, RotationalSpeed value2)
		{
			return value1 == value2 || (value1 != null && value2 != null && value1.Equals(value2));
		}

		// Token: 0x060000F0 RID: 240 RVA: 0x00003F97 File Offset: 0x00002197
		public static bool operator !=(RotationalSpeed value1, RotationalSpeed value2)
		{
			return !(value1 == value2);
		}

		// Token: 0x060000F1 RID: 241 RVA: 0x00003FA4 File Offset: 0x000021A4
		public override bool Equals(object obj)
		{
			if (obj == null || base.GetType() != obj.GetType())
			{
				return false;
			}
			RotationalSpeed rotationalSpeed = (RotationalSpeed)obj;
			return this.ValueInBaseUnit == rotationalSpeed.ValueInBaseUnit;
		}

		// Token: 0x060000F2 RID: 242 RVA: 0x00003FE4 File Offset: 0x000021E4
		public override int GetHashCode()
		{
			return this.ValueInBaseUnit.GetHashCode();
		}

		// Token: 0x060000F3 RID: 243 RVA: 0x00003FFF File Offset: 0x000021FF
		public static RotationalSpeed operator -(RotationalSpeed value1, RotationalSpeed value2)
		{
			return new RotationalSpeed(value1.ValueInBaseUnit - value2.ValueInBaseUnit);
		}

		// Token: 0x060000F4 RID: 244 RVA: 0x00004017 File Offset: 0x00002217
		public static RotationalSpeed operator +(RotationalSpeed value1, RotationalSpeed value2)
		{
			return new RotationalSpeed(value1.ValueInBaseUnit + value2.ValueInBaseUnit);
		}

		// Token: 0x060000F5 RID: 245 RVA: 0x0000402F File Offset: 0x0000222F
		public static RotationalSpeed operator *(RotationalSpeed value1, RotationalSpeed value2)
		{
			return new RotationalSpeed(value1.ValueInBaseUnit * value2.ValueInBaseUnit);
		}

		// Token: 0x060000F6 RID: 246 RVA: 0x00004047 File Offset: 0x00002247
		public static RotationalSpeed operator /(RotationalSpeed value1, RotationalSpeed value2)
		{
			return new RotationalSpeed(value1.ValueInBaseUnit / value2.ValueInBaseUnit);
		}

		// Token: 0x060000F7 RID: 247 RVA: 0x0000405F File Offset: 0x0000225F
		public override string ToString()
		{
			return string.Format("{0}", this.ValueInBaseUnit);
		}
	}


    public class CurrentEngineConverterAst12 : ICanConvertAst12
	{
		// Token: 0x17000052 RID: 82
		// (get) Token: 0x060000EF RID: 239 RVA: 0x000041CC File Offset: 0x000023CC
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

		// Token: 0x060000F0 RID: 240 RVA: 0x000041DC File Offset: 0x000023DC
		public List<GraphPoint> Convert(GraphsContainer graphCollection, SingleGraph graph, RawHeaderDataAst12 header)
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

    public class Current : IUnit
	{
		// Token: 0x0600007A RID: 122 RVA: 0x00003028 File Offset: 0x00001228
		public Current()
		{
		}

		// Token: 0x0600007B RID: 123 RVA: 0x00003030 File Offset: 0x00001230
		public Current(decimal value) : this(value, CurrentUnits.BaseFormat)
		{
		}

		// Token: 0x0600007C RID: 124 RVA: 0x0000303E File Offset: 0x0000123E
		public Current(decimal value, UnitDescription formatOfValue)
		{
			this.ValueInBaseUnit = formatOfValue.ToBaseUnit(value);
		}

		// Token: 0x17000021 RID: 33
		// (get) Token: 0x0600007D RID: 125 RVA: 0x00003053 File Offset: 0x00001253
		// (set) Token: 0x0600007E RID: 126 RVA: 0x0000305B File Offset: 0x0000125B
		public decimal ValueInBaseUnit { get; set; }

		// Token: 0x0600007F RID: 127 RVA: 0x00003064 File Offset: 0x00001264
		public FormatedUnit ConvertToFormat()
		{
			return new FormatedUnit(CurrentUnits.BaseFormat, this.ValueInBaseUnit);
		}

		// Token: 0x06000080 RID: 128 RVA: 0x00003076 File Offset: 0x00001276
		public FormatedUnit ConvertToFormat(UnitDescription targetFormat)
		{
			return new FormatedUnit(targetFormat, this.ValueInBaseUnit);
		}

		// Token: 0x17000022 RID: 34
		// (get) Token: 0x06000081 RID: 129 RVA: 0x00003084 File Offset: 0x00001284
		public UnitDescription BaseFormat
		{
			get
			{
				return CurrentUnits.BaseFormat;
			}
		}

		// Token: 0x06000082 RID: 130 RVA: 0x0000308B File Offset: 0x0000128B
		public IUnit Clone()
		{
			return new Current(this.ValueInBaseUnit, this.BaseFormat);
		}

		// Token: 0x06000083 RID: 131 RVA: 0x0000309E File Offset: 0x0000129E
		public static bool operator ==(Current value1, Current value2)
		{
			return value1 == value2 || (value1 != null && value2 != null && value1.Equals(value2));
		}

		// Token: 0x06000084 RID: 132 RVA: 0x000030B7 File Offset: 0x000012B7
		public static bool operator !=(Current value1, Current value2)
		{
			return !(value1 == value2);
		}

		// Token: 0x06000085 RID: 133 RVA: 0x000030C4 File Offset: 0x000012C4
		public override bool Equals(object obj)
		{
			if (obj == null || base.GetType() != obj.GetType())
			{
				return false;
			}
			Current current = (Current)obj;
			return this.ValueInBaseUnit == current.ValueInBaseUnit;
		}

		// Token: 0x06000086 RID: 134 RVA: 0x00003104 File Offset: 0x00001304
		public override int GetHashCode()
		{
			return this.ValueInBaseUnit.GetHashCode();
		}

		// Token: 0x06000087 RID: 135 RVA: 0x0000311F File Offset: 0x0000131F
		public static Current operator -(Current value1, Current value2)
		{
			return new Current(value1.ValueInBaseUnit - value2.ValueInBaseUnit);
		}

		// Token: 0x06000088 RID: 136 RVA: 0x00003137 File Offset: 0x00001337
		public static Current operator +(Current value1, Current value2)
		{
			return new Current(value1.ValueInBaseUnit + value2.ValueInBaseUnit);
		}

		// Token: 0x06000089 RID: 137 RVA: 0x0000314F File Offset: 0x0000134F
		public static Current operator *(Current value1, Current value2)
		{
			return new Current(value1.ValueInBaseUnit * value2.ValueInBaseUnit);
		}

		// Token: 0x0600008A RID: 138 RVA: 0x00003167 File Offset: 0x00001367
		public static Current operator /(Current value1, Current value2)
		{
			return new Current(value1.ValueInBaseUnit / value2.ValueInBaseUnit);
		}

		// Token: 0x0600008B RID: 139 RVA: 0x0000317F File Offset: 0x0000137F
		public override string ToString()
		{
			return string.Format("{0}", this.ValueInBaseUnit);
		}
	}

    public static class CurrentUnits
	{
		// Token: 0x17000023 RID: 35
		// (get) Token: 0x0600008C RID: 140 RVA: 0x00003198 File Offset: 0x00001398
		public static List<UnitDescription> DisplayFormats
		{
			get
			{
				List<UnitDescription> list = new List<UnitDescription>();
				list.Add(new UnitDescription("Current", "Text_Ampere", 2, (decimal y) => y, (decimal x) => x, new List<UnitLocalization>
				{
					new UnitLocalization("A", "en")
				}));
				return list;
			}
		}

		// Token: 0x17000024 RID: 36
		// (get) Token: 0x0600008D RID: 141 RVA: 0x0000321C File Offset: 0x0000141C
		public static UnitDescription Ampere
		{
			get
			{
				return new UnitDescription("Current", "Text_Ampere", 2, (decimal y) => y, (decimal x) => x, new List<UnitLocalization>
				{
					new UnitLocalization("A", "en")
				});
			}
		}

		// Token: 0x17000025 RID: 37
		// (get) Token: 0x0600008E RID: 142 RVA: 0x00003291 File Offset: 0x00001491
		public static UnitDescription BaseFormat
		{
			get
			{
				return CurrentUnits.Ampere;
			}
		}
	}


    public class TemperaturePowerUnitConverterAst12 : ICanConvertAst12
	{
		// Token: 0x17000054 RID: 84
		// (get) Token: 0x060000F5 RID: 245 RVA: 0x00004348 File Offset: 0x00002548
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

		// Token: 0x060000F6 RID: 246 RVA: 0x00004360 File Offset: 0x00002560
		public List<GraphPoint> Convert(GraphsContainer graphCollection, SingleGraph graph, RawHeaderDataAst12 header)
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

    public class Temperature : IUnit
	{
		// Token: 0x06000111 RID: 273 RVA: 0x000044F4 File Offset: 0x000026F4
		public Temperature()
		{
		}

		// Token: 0x06000112 RID: 274 RVA: 0x000044FC File Offset: 0x000026FC
		public Temperature(decimal value) : this(value, TemperatureUnits.BaseFormat)
		{
		}

		// Token: 0x06000113 RID: 275 RVA: 0x0000450A File Offset: 0x0000270A
		public Temperature(decimal value, UnitDescription formatOfValue)
		{
			this.ValueInBaseUnit = formatOfValue.ToBaseUnit(value);
		}

		// Token: 0x17000048 RID: 72
		// (get) Token: 0x06000114 RID: 276 RVA: 0x0000451F File Offset: 0x0000271F
		// (set) Token: 0x06000115 RID: 277 RVA: 0x00004527 File Offset: 0x00002727
		public decimal ValueInBaseUnit { get; set; }

		// Token: 0x06000116 RID: 278 RVA: 0x00004530 File Offset: 0x00002730
		public FormatedUnit ConvertToFormat()
		{
			return new FormatedUnit(TemperatureUnits.BaseFormat, this.ValueInBaseUnit);
		}

		// Token: 0x06000117 RID: 279 RVA: 0x00004542 File Offset: 0x00002742
		public FormatedUnit ConvertToFormat(UnitDescription targetFormat)
		{
			return new FormatedUnit(targetFormat, this.ValueInBaseUnit);
		}

		// Token: 0x17000049 RID: 73
		// (get) Token: 0x06000118 RID: 280 RVA: 0x00004550 File Offset: 0x00002750
		public UnitDescription BaseFormat
		{
			get
			{
				return TemperatureUnits.BaseFormat;
			}
		}

		// Token: 0x06000119 RID: 281 RVA: 0x00004557 File Offset: 0x00002757
		public IUnit Clone()
		{
			return new Temperature(this.ValueInBaseUnit, this.BaseFormat);
		}

		// Token: 0x0600011A RID: 282 RVA: 0x0000456A File Offset: 0x0000276A
		public static bool operator ==(Temperature value1, Temperature value2)
		{
			return value1 == value2 || (value1 != null && value2 != null && value1.Equals(value2));
		}

		// Token: 0x0600011B RID: 283 RVA: 0x00004583 File Offset: 0x00002783
		public static bool operator !=(Temperature value1, Temperature value2)
		{
			return !(value1 == value2);
		}

		// Token: 0x0600011C RID: 284 RVA: 0x00004590 File Offset: 0x00002790
		public override bool Equals(object obj)
		{
			if (obj == null || base.GetType() != obj.GetType())
			{
				return false;
			}
			Temperature temperature = (Temperature)obj;
			return this.ValueInBaseUnit == temperature.ValueInBaseUnit;
		}

		// Token: 0x0600011D RID: 285 RVA: 0x000045D0 File Offset: 0x000027D0
		public override int GetHashCode()
		{
			return this.ValueInBaseUnit.GetHashCode();
		}

		// Token: 0x0600011E RID: 286 RVA: 0x000045EB File Offset: 0x000027EB
		public static Temperature operator -(Temperature value1, Temperature value2)
		{
			return new Temperature(value1.ValueInBaseUnit - value2.ValueInBaseUnit);
		}

		// Token: 0x0600011F RID: 287 RVA: 0x00004603 File Offset: 0x00002803
		public static Temperature operator +(Temperature value1, Temperature value2)
		{
			return new Temperature(value1.ValueInBaseUnit + value2.ValueInBaseUnit);
		}

		// Token: 0x06000120 RID: 288 RVA: 0x0000461B File Offset: 0x0000281B
		public static Temperature operator *(Temperature value1, Temperature value2)
		{
			return new Temperature(value1.ValueInBaseUnit * value2.ValueInBaseUnit);
		}

		// Token: 0x06000121 RID: 289 RVA: 0x00004633 File Offset: 0x00002833
		public static Temperature operator /(Temperature value1, Temperature value2)
		{
			return new Temperature(value1.ValueInBaseUnit / value2.ValueInBaseUnit);
		}

		// Token: 0x06000122 RID: 290 RVA: 0x0000464B File Offset: 0x0000284B
		public override string ToString()
		{
			return string.Format("{0}", this.ValueInBaseUnit);
		}
	}

    public static class TemperatureUnits
	{
		// Token: 0x1700004A RID: 74
		// (get) Token: 0x06000123 RID: 291 RVA: 0x00004664 File Offset: 0x00002864
		public static List<UnitDescription> DisplayFormats
		{
			get
			{
				List<UnitDescription> list = new List<UnitDescription>();
				list.Add(new UnitDescription("temperatureDegreeFahrenheit", "Text_GradFahrenheit", 1, (decimal y) => (y - 32m) / 1.8m, (decimal x) => x * 1.8m + 32m, new List<UnitLocalization>
				{
					new UnitLocalization("°F", "en")
				}));
				list.Add(new UnitDescription("temperatureDegreeCelsius", "Text_TemperaturGrad", 1, (decimal y) => y, (decimal x) => x, new List<UnitLocalization>
				{
					new UnitLocalization("°C", "en")
				}));
				return list;
			}
		}

		// Token: 0x1700004B RID: 75
		// (get) Token: 0x06000124 RID: 292 RVA: 0x00004754 File Offset: 0x00002954
		public static UnitDescription DegreeCelsius
		{
			get
			{
				return new UnitDescription("temperatureDegreeCelsius", "Text_TemperaturGrad", 1, (decimal y) => y, (decimal x) => x, new List<UnitLocalization>
				{
					new UnitLocalization("°C", "en")
				});
			}
		}

		// Token: 0x1700004C RID: 76
		// (get) Token: 0x06000125 RID: 293 RVA: 0x000047CC File Offset: 0x000029CC
		public static UnitDescription DegreeFahrenheit
		{
			get
			{
				return new UnitDescription("temperatureDegreeFahrenheit", "Text_GradFahrenheit", 1, (decimal y) => (y - 32m) / 1.8m, (decimal x) => x * 1.8m + 32m, new List<UnitLocalization>
				{
					new UnitLocalization("°F", "en")
				});
			}
		}

		// Token: 0x1700004D RID: 77
		// (get) Token: 0x06000126 RID: 294 RVA: 0x00004841 File Offset: 0x00002A41
		public static UnitDescription BaseFormat
		{
			get
			{
				return TemperatureUnits.DegreeCelsius;
			}
		}
	}


     public class ReadMeasurementLine
    {
        private readonly byte[] File;

        private readonly CombinedHeaderInformation HeaderInformation;

        private readonly CombinedHeaderInformationAst12 HeaderInformationAst12;

        public ReadMeasurementLine(byte[] file, CombinedHeaderInformation headerInformation)
        {
            File = file;
            HeaderInformation = headerInformation;
        }

        public ReadMeasurementLine(byte[] file, CombinedHeaderInformationAst12 headerInformation)
        {
            File = file;
            HeaderInformationAst12 = headerInformation;
        }

        public GraphsContainer DoWork()
        {
            RawMeasurementLines rawMeasurementLines = GetRawMeasurementLines();
            return CreateGraphs(rawMeasurementLines);
        }

        private RawMeasurementLines GetRawMeasurementLines()
        {
            RawMeasurementLines rawMeasurementLines = new RawMeasurementLines();
            int count = HeaderInformation?.HeaderData.OffsetGraphData ?? HeaderInformationAst12.HeaderData.OffsetGraphData;
            MeasurementLineInterpretingInfo measurementLineInterpretingInfo = HeaderInformation?.LineInterpretingInformation ?? HeaderInformationAst12.LineInterpretingInformation;
             MemoryStream input = new MemoryStream(File);
             BinaryReader binaryReader = new BinaryReader(input);
            binaryReader.ReadBytes(count);
            for (int i = 0; i < measurementLineInterpretingInfo.NumberOfLines; i++)
            {
                rawMeasurementLines.AddLine(binaryReader.ReadBytes(measurementLineInterpretingInfo.LineLength));
            }

            return rawMeasurementLines;
        }

        private GraphsContainer CreateGraphs(RawMeasurementLines rawLines)
        {
            MeasurementLineInterpretingInfo measurementLineInterpretingInfo = HeaderInformation?.LineInterpretingInformation ?? HeaderInformationAst12.LineInterpretingInformation;
            List<MeasurementSetDescription> list = measurementLineInterpretingInfo.MeasurementLines();
            GraphsContainer graphsContainer = ((HeaderInformation != null) ? new GraphsContainer(HeaderInformation) : new GraphsContainer(HeaderInformationAst12));
            foreach (byte[] measurementLine in rawLines.MeasurementLines)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    MeasurementSetDescription measurementSetDescription = list[i];
                    graphsContainer.AddNewValue(value: (measurementSetDescription.DataTypeLength != 2) ? BitConverter.ToInt32(measurementLine, measurementSetDescription.Offset) : BitConverter.ToInt16(measurementLine, measurementSetDescription.Offset), uniqueId: i, type: measurementSetDescription.GraphType, internalGraphType: measurementSetDescription.InternalGraphType);
                }
            }

            return graphsContainer;
        }
    }

    public class RawMeasurementLines
    {
        public List<byte[]> MeasurementLines { get; set; }

        public RawMeasurementLines()
        {
            MeasurementLines = new List<byte[]>();
        }

        public void AddLine(byte[] line)
        {
            MeasurementLines.Add(line);
        }

        public override string ToString()
        {
            return $"Line length: {MeasurementLines.Count}";
        }
    }
    public class CalculateTimePerRowAst12
    {
        private readonly GraphsContainer GraphCollection;

        private readonly CombinedHeaderInformationAst12 HeaderInformation;

        public CalculateTimePerRowAst12(GraphsContainer graphCollection, CombinedHeaderInformationAst12 headerInformation)
        {
            GraphCollection = graphCollection;
            HeaderInformation = headerInformation;
        }

        public void DoCalculation()
        {
            int lineLength = HeaderInformation.LineInterpretingInformation.LineLength;
            decimal timePerMeasurement = (decimal)lineLength / 4.0m * 0.025m;
            GraphCollection.TimePerMeasurement = timePerMeasurement;
        }
    }

}