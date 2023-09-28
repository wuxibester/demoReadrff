using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System;
using System.Runtime.InteropServices;
using System.Collections;

namespace rff2csv
{
    public class GraphsContainer
    {
        private readonly Dictionary<int, SingleGraph> Graphs = new Dictionary<int, SingleGraph>();

        public decimal TimePerMeasurement { get; set; }

        public CombinedHeaderInformation CombinedHeaderInformation { get; }

        public CombinedHeaderInformationAst12 CombinedHeaderInformationAst12 { get; }

        public GraphsContainer(CombinedHeaderInformation combinedHeaderInformation)
        {
            CombinedHeaderInformation = combinedHeaderInformation;
        }

        public GraphsContainer(CombinedHeaderInformationAst12 combinedHeaderInformation)
        {
            CombinedHeaderInformationAst12 = combinedHeaderInformation;
        }

        public void AddNewValue(int uniqueId, GraphType type, int value, int internalGraphType)
        {
            SingleGraph singleGraph = new SingleGraph();
            singleGraph.InternalGraphType = "0x" + internalGraphType.ToString("X", CultureInfo.InvariantCulture);
            singleGraph.Type = type;
            if (Graphs.ContainsKey(uniqueId))
            {
                singleGraph = Graphs[uniqueId];
            }
            else
            {
                Graphs.Add(uniqueId, singleGraph);
            }

            singleGraph.AddRawValue(value);
        }

        public List<SingleGraph> AllGraphs()
        {
            return new List<SingleGraph>(Graphs.ValuesToList());
        }

        public override string ToString()
        {
            return string.Join(",", Graphs.Values.Select((SingleGraph c) => c.Type));
        }
    }

    public class CombinedHeaderInformation
    {
        public RawHeaderData HeaderData { get; set; }

        public RawMeasurementLineDescription RawLineDescription { get; set; }

        public MeasurementLineInterpretingInfo LineInterpretingInformation { get; set; }

        public int? HeaderVersion { get; set; }

        public int OffsetHeaderData { get; set; }

        public CombinedHeaderInformation()
        {
            HeaderData = new RawHeaderData();
            RawLineDescription = new RawMeasurementLineDescription();
            LineInterpretingInformation = new MeasurementLineInterpretingInfo();
        }
    }

    public class CombinedHeaderInformationAst12
    {
        public RawHeaderDataAst12 HeaderData { get; set; }

        public RawMeasurementLineDescription RawLineDescription { get; set; }

        public MeasurementLineInterpretingInfo LineInterpretingInformation { get; set; }

        public int? HeaderVersion { get; set; }

        public int OffsetHeaderData { get; set; }

        public CombinedHeaderInformationAst12()
        {
            HeaderData = new RawHeaderDataAst12();
            RawLineDescription = new RawMeasurementLineDescription();
            LineInterpretingInformation = new MeasurementLineInterpretingInfo();
        }
    }

    public class RawHeaderDataAst12
    {
        public string TypeScrewdriver { get; set; }

        public uint GearTransmission { get; set; }

        public ushort GearEfficacy { get; set; }

        public ushort ResolutionClass { get; set; }

        public int Increment { get; set; }

        public int TorqueCalibrationValue { get; set; }

        public int GraphCount { get; set; }

        public int RemainingLength { get; set; }

        public int TorqueShift { get; set; }

        public int OffsetGraphData { get; set; }
    }

    public class MeasurementLineInterpretingInfo
    {
        private readonly List<MeasurementSetDescription> MeasurementLine;

        public int LineLength { get; set; }

        public int NumberOfLines { get; set; }

        public MeasurementLineInterpretingInfo()
        {
            MeasurementLine = new List<MeasurementSetDescription>();
        }

        public void AddMeasurementSetDescription(MeasurementSetDescription measurementSetDescription)
        {
            MeasurementLine.Add(measurementSetDescription);
        }

        public List<MeasurementSetDescription> MeasurementLines()
        {
            return MeasurementLine.OrderBy((MeasurementSetDescription c) => c.Offset).ToList();
        }
    }

    public class MeasurementSetDescription
    {
        public GraphType GraphType { get; set; }

        public int DataTypeLength { get; set; }

        public int Offset { get; set; }

        public int InternalGraphType { get; set; }

        public override string ToString()
        {
            return $"{GraphType} Datalength: '{DataTypeLength}' Offset:'{Offset}'";
        }
    }

    public class RawMeasurementLineDescription
    {
        private readonly List<RawGraphDescription> Descriptions;

        public RawMeasurementLineDescription()
        {
            Descriptions = new List<RawGraphDescription>();
        }

        public IEnumerator GetEnumerator()
        {
            return Descriptions.GetEnumerator();
        }

        public void AddGraphData(RawGraphDescription graph)
        {
            Descriptions.Add(graph);
        }
    }

        public class RawGraphDescription
    {
        public int GraphVersion { get; set; }

        public int GraphType { get; set; }

        public override string ToString()
        {
            return $"Version '{GraphVersion}' - Type '{GraphType}'";
        }
    }


    public class RawHeaderData
    {
        public string TypeScrewdriver { get; set; }

        public int GearTransmission { get; set; }

        public int GearEfficacy { get; set; }

        public int Increment { get; set; }

        public int TorqueCalibrationValue { get; set; }

        public int GraphCount { get; set; }

        public int RemainingLength { get; set; }

        public int TorqueShift { get; set; }

        public int OffsetGraphData { get; set; }
    }

    public class SingleGraph
    {
        public GraphType Type { get; set; }

        public string InternalGraphType { get; set; }

        public List<int> RawValues { get; }

        public List<GraphPoint> CalculatedValues { get; set; }

        public SingleGraph()
        {
            RawValues = new List<int>();
            CalculatedValues = new List<GraphPoint>();
        }

        public void AddRawValue(int value)
        {
            RawValues.Add(value);
        }
    }

    public enum GraphType
    {
        Workload,
        RotationalSpeed,
        Torque,
        Angle,
        ErrorCode,
        TemperatureMotor,
        ProgramPosition,
        CurrentMotor,
        TemperaturePowerUnit,
        TorqueAdditionalSensor1,
        AngleAdditionalSensor1,
        TorqueMotor,
        AngleMotor,
        TorqueAdditionalSensor2,
        AngleAdditionalSensor2,
        AnalogStop,
        UnknownContent
    }

    public class GraphPoint
    {
        public IUnit Time { get; set; }

        public IUnit Value { get; set; }

        public GraphPoint(decimal timeInMillisconds, IUnit value)
        {
            Time = new Time(timeInMillisconds, TimeUnits.Milliseconds);
            Value = value;
        }

        public override string ToString()
        {
            return $"(t, x) => ({Time},{Value})";
        }
    }

    public interface IUnit
    {
        decimal ValueInBaseUnit { get; set; }

        UnitDescription BaseFormat { get; }

        FormatedUnit ConvertToFormat();

        FormatedUnit ConvertToFormat(UnitDescription targetFormat);

        IUnit Clone();
    }

     public class UnitDescription
    {
        private const int ShouldRoundToDecimals = 6;

        private readonly Func<decimal, decimal> ConvertFromBase;

        private readonly Func<decimal, decimal> ConvertToBase;

        public string Id { get; set; }

        public string TextName { get; set; }

        public int NumberOfDigitsToDisplay { get; set; }

        public List<UnitLocalization> UnitLocalizations { get; set; }

        public UnitDescription(string id, string textName, int numberOfDigitsToDisplay, Func<decimal, decimal> convertToBase, Func<decimal, decimal> convertFromBase, List<UnitLocalization> localizations)
        {
            ConvertToBase = convertToBase;
            ConvertFromBase = convertFromBase;
            Id = id;
            TextName = textName;
            NumberOfDigitsToDisplay = numberOfDigitsToDisplay;
            UnitLocalizations = localizations;
        }

        public decimal ToBaseUnit(decimal value)
        {
            decimal d = ConvertToBase(value);
            return Math.Round(d, 6, MidpointRounding.AwayFromZero);
        }

        public decimal FromBaseTo(decimal valueInBaseFormat)
        {
            decimal d = ConvertFromBase(valueInBaseFormat);
            return Math.Round(d, 6, MidpointRounding.AwayFromZero);
        }

        public UnitLocalization GetBestLocalizationForCurrentLanguage(CultureInfo yourCulture)
        {
            string twoLetterIso = yourCulture.TwoLetterISOLanguageName;
            UnitLocalization unitLocalization = UnitLocalizations.FirstOrDefault((UnitLocalization c) => c.UseForLanguage.IsTheSameIgnoreCase(twoLetterIso));
            if (unitLocalization != null)
            {
                return unitLocalization;
            }

            return DefaultFallbackLocalization();
        }

        private UnitLocalization DefaultFallbackLocalization()
        {
            return UnitLocalizations.First((UnitLocalization c) => c.UseForLanguage.IsTheSame("en"));
        }

        public override string ToString()
        {
            return TextName ?? "";
        }

        public static bool operator ==(UnitDescription value1, UnitDescription value2)
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

        public static bool operator !=(UnitDescription value1, UnitDescription value2)
        {
            return !(value1 == value2);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            UnitDescription unitDescription = (UnitDescription)obj;
            return Id.IsTheSameIgnoreCase(unitDescription.Id);
        }
        

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }

     public class UnitLocalization
    {
        public string Abbreviation { get; set; }

        public string UseForLanguage { get; set; }

        public UnitLocalization(string abbreviation, string useForLanguage)
        {
            Abbreviation = abbreviation;
            UseForLanguage = useForLanguage;
        }

        public override string ToString()
        {
            return "Language: '" + UseForLanguage + "', Abbreviation: '" + Abbreviation + "'";
        }
    }

     public static class StringExtensions
    {
        private const int NumberFallbackValue = 0;

        public static bool IsTheSame(this string s1, string s2)
        {
            return string.CompareOrdinal(s1, s2) == 0;
        }

        public static bool IsTheSameIgnoreCase(this string s1, string s2)
        {
            return string.Compare(s1, s2, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public static string Trimmed(this string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                return value.Trim();
            }

            return value;
        }

        public static string MaxLength(this string value, int length)
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (value.Length <= length)
                {
                    return value;
                }

                return value.Substring(0, length);
            }

            return value;
        }

        public static int ToInt(this string stringValue)
        {
            if (int.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }

            return 0;
        }

        public static double ToDouble(this string stringValue)
        {
            if (double.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }

            return 0.0;
        }

        public static double? ToNullableDouble(this string stringValue)
        {
            if (double.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }

            return null;
        }

        public static string ToLowerFirstChar(this string stringValue)
        {
            string text = stringValue;
            if (!string.IsNullOrEmpty(text) && char.IsUpper(text[0]))
            {
                text = char.ToLower(text[0], CultureInfo.InvariantCulture) + text.Substring(1);
            }

            return text;
        }

        public static string ToUpperFirstChar(this string stringValue)
        {
            string text = stringValue;
            if (!string.IsNullOrEmpty(text) && char.IsLower(text[0]))
            {
                text = char.ToUpper(text[0], CultureInfo.InvariantCulture) + text.Substring(1);
            }

            return text;
        }

        public static string[] SplitByLineBreaks(this string stringValue, StringSplitOptions options = StringSplitOptions.None)
        {
            return stringValue.Split(new string[3] { "\r\n", "\r", "\n" }, options);
        }
    }

      public class FormatedUnit
    {
        private readonly UnitDescription Format;

        public decimal ValueInBaseUnit { get; }

        public decimal ValueInTargetFormat => Format.FromBaseTo(ValueInBaseUnit);

        public FormatedUnit(UnitDescription format, decimal valueInBaseUnit)
        {
            Format = format;
            ValueInBaseUnit = valueInBaseUnit;
        }

        public string LocalizedToString(string cultureTwoLetterIsoCode)
        {
            UnitLocalization localizationOrDefault = GetLocalizationOrDefault(cultureTwoLetterIsoCode);
            decimal num = Math.Round(ValueInTargetFormat, Format.NumberOfDigitsToDisplay, MidpointRounding.AwayFromZero);
            return string.Format(new CultureInfo(cultureTwoLetterIsoCode), "{0} {1}", num, localizationOrDefault.Abbreviation);
        }

        private UnitLocalization GetLocalizationOrDefault(string cultureTwoLetterIsoCode)
        {
            UnitLocalization unitLocalization = Format.UnitLocalizations.FirstOrDefault((UnitLocalization c) => c.UseForLanguage.IsTheSameIgnoreCase(cultureTwoLetterIsoCode));
            if (unitLocalization == null)
            {
                unitLocalization = Format.UnitLocalizations.FirstOrDefault((UnitLocalization c) => c.UseForLanguage.IsTheSameIgnoreCase("en"));
            }

            return unitLocalization;
        }
    }

       public class Time : IUnit
    {
        public decimal ValueInBaseUnit { get; set; }

        public UnitDescription BaseFormat => TimeUnits.BaseFormat;

        public Time()
        {
        }

        public Time(decimal value)
            : this(value, TimeUnits.BaseFormat)
        {
        }

        public Time(decimal value, UnitDescription formatOfValue)
        {
            ValueInBaseUnit = formatOfValue.ToBaseUnit(value);
        }

        public FormatedUnit ConvertToFormat()
        {
            return new FormatedUnit(TimeUnits.BaseFormat, ValueInBaseUnit);
        }

        public FormatedUnit ConvertToFormat(UnitDescription targetFormat)
        {
            return new FormatedUnit(targetFormat, ValueInBaseUnit);
        }

        public IUnit Clone()
        {
            return new Time(ValueInBaseUnit, BaseFormat);
        }

        public static bool operator ==(Time value1, Time value2)
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

        public static bool operator !=(Time value1, Time value2)
        {
            return !(value1 == value2);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            Time time = (Time)obj;
            return ValueInBaseUnit == time.ValueInBaseUnit;
        }

        public override int GetHashCode()
        {
            return ValueInBaseUnit.GetHashCode();
        }

        public static Time operator -(Time value1, Time value2)
        {
            return new Time(value1.ValueInBaseUnit - value2.ValueInBaseUnit);
        }

        public static Time operator +(Time value1, Time value2)
        {
            return new Time(value1.ValueInBaseUnit + value2.ValueInBaseUnit);
        }

        public static Time operator *(Time value1, Time value2)
        {
            return new Time(value1.ValueInBaseUnit * value2.ValueInBaseUnit);
        }

        public static Time operator /(Time value1, Time value2)
        {
            return new Time(value1.ValueInBaseUnit / value2.ValueInBaseUnit);
        }

        public override string ToString()
        {
            return $"{ValueInBaseUnit}";
        }
    }

    public static class TimeUnits
    {
        public static List<UnitDescription> DisplayFormats
        {
            get
            {
                List<UnitDescription> list = new List<UnitDescription>();
                list.Add(new UnitDescription("angleMilliseconds", "Text_Millisekunden", 0, (decimal y) => y, (decimal x) => x, new List<UnitLocalization>
                {
                    new UnitLocalization("Ms", "en")
                }));
                return list;
            }
        }

        public static UnitDescription Milliseconds => new UnitDescription("angleMilliseconds", "Text_Millisekunden", 0, (decimal y) => y, (decimal x) => x, new List<UnitLocalization>
        {
            new UnitLocalization("Ms", "en")
        });

        public static UnitDescription BaseFormat => Milliseconds;
    }

       public static class DictonaryExtensions
    {
        public static IList<TKey> KeysToList<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
        {
            return dictionary.Select((KeyValuePair<TKey, TValue> c) => c.Key).ToList();
        }

        public static IList<TValue> ValuesToList<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
        {
            return dictionary.Select((KeyValuePair<TKey, TValue> c) => c.Value).ToList();
        }
    }
}