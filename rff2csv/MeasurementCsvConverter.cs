using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Security;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Reflection;

namespace rff2csv
{
     public class MeasurementCsvConverter : IDisposable
    {
        private readonly IMeasurementContainer MeasurementContainer;

        //private ExcelWorkBook ExcelWorkBook = new ExcelWorkBook();

        //private IExcelSheet ExcelSheet;

        private readonly TimeDetection TimeDetection;

        private bool Disposed;

        public MeasurementCsvConverter(IMeasurementContainer measurementContainer)
        {
            MeasurementContainer = measurementContainer;

            //ExcelSheet = ExcelWorkBook.Sheets.AddSheet("DEPRAG");
            TimeDetection = new TimeDetection(measurementContainer);
        }

        public List<ReceiveMsg.CurveData> Save(toolType toolType)
        {
            //MeasurementCsvConvertOption measurementCsvConvertOption = new MeasurementCsvConvertOption();
            //if (measurementCsvConvertOption.SaveMetadata)
            {
                //SaveMetaData(outputFileName);
            }

           return SaveData(toolType);
        }

        // public void Save(string outputFileName, Action<MeasurementCsvConvertOption> config)
        // {
        //     MeasurementCsvConvertOption measurementCsvConvertOption = new MeasurementCsvConvertOption();
        //     config?.Invoke(measurementCsvConvertOption);
        //     if (measurementCsvConvertOption.SaveMetadata)
        //     {
        //         SaveMetaData(outputFileName);
        //     }

        //     SaveData(outputFileName);
        // }

        // private void SaveMetaData(string outputFileName)
        // {
        //     if (outputFileName == null)
        //     {
        //         throw new ArgumentNullException("outputFileName");
        //     }

        //     JObject metaDataAsJson = GetMetaDataAsJson();
        //     File.WriteAllText(Path.ChangeExtension(outputFileName, ".json"), ((object)metaDataAsJson).ToString());
        // }

        private JObject GetMetaDataAsJson()
        {
            //IL_0056: Unknown result type (might be due to invalid IL or missing references)
            //IL_005b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0067: Expected O, but got Unknown
            JObject val = JObject.FromObject((object)MeasurementContainer.HeaderInformation);
            List<MetaDataItem> processData = MeasurementContainer.ProcessData.Select((ProcessDataSet x) => new MetaDataItem
            {
                TextIdentifier = x.TextIdentifier,
                IsInternal = x.IsInternal,
                Value = x.Value
            }).ToList();
            MetaDataContainer metaDataContainer = new MetaDataContainer(processData);
            JObject val2 = JObject.FromObject((object)metaDataContainer);
            JsonMergeSettings val3 = new JsonMergeSettings();
            val3.MergeArrayHandling=((MergeArrayHandling)0);
            ((JContainer)val).Merge((object)val2, val3);
            return val;
        }

        private List<ReceiveMsg.CurveData> SaveData(toolType toolType)
        {
            //WriteHeader();
            int num = 1;
            List<MeasuredSerie> list = MeasurementContainer.MeasuredSeries.Where((MeasuredSerie c) => !c.IsInternalCurve).ToList();
            List<ReceiveMsg.CurveData> curveList = new List<ReceiveMsg.CurveData>();
            //List<string> lineData = new List<string>();
            while (TimeDetection.CurrentTime <= MeasurementContainer.MaximumXValue)
            {
                switch(toolType)
                {
                    case toolType.AST12:
                        curveList.Add(new ReceiveMsg.CurveData(){
                            ts=(float)TimeDetection.CurrentTime,
                            value = new ReceiveMsg.CurveItem(){
                                Torque =list.Count()>0? (float)list[0].DiscreteYValueFor(TimeDetection.CurrentTime).ValueInBaseUnit:-1,
                                Angle =list.Count()>1? (float)list[1].DiscreteYValueFor(TimeDetection.CurrentTime).ValueInBaseUnit:-1,
                                MotorSpeed =list.Count()>2? (float)list[2].DiscreteYValueFor(TimeDetection.CurrentTime).ValueInBaseUnit:-1,
                                //程序号，没卵用，不睬(float)list[3].DiscreteYValueFor(TimeDetection.CurrentTime).ValueInBaseUnit,
                                MotorEngine =list.Count()>3? (float)list[4].DiscreteYValueFor(TimeDetection.CurrentTime).ValueInBaseUnit:-1,
                                MotorTemperature = list.Count()>4? (float)list[5].DiscreteYValueFor(TimeDetection.CurrentTime).ValueInBaseUnit:-1
                            },
                        });
                    break;
                    case toolType.AST11:
                        curveList.Add(new ReceiveMsg.CurveData(){
                            ts=(float)TimeDetection.CurrentTime,
                            value = new ReceiveMsg.CurveItem(){
                                MotorSpeed =list.Count()>0? (float)list[0].DiscreteYValueFor(TimeDetection.CurrentTime).ValueInBaseUnit:-1,
                                Torque =list.Count()>1? (float)list[1].DiscreteYValueFor(TimeDetection.CurrentTime).ValueInBaseUnit:-1,
                                Angle =list.Count()>2? (float)list[2].DiscreteYValueFor(TimeDetection.CurrentTime).ValueInBaseUnit:-1,
                                //程序号，没卵用，不睬(float)list[3].DiscreteYValueFor(TimeDetection.CurrentTime).ValueInBaseUnit,
                                MotorEngine =list.Count()>3? (float)list[4].DiscreteYValueFor(TimeDetection.CurrentTime).ValueInBaseUnit:-1,
                                MotorTemperature = list.Count()>4? (float)list[5].DiscreteYValueFor(TimeDetection.CurrentTime).ValueInBaseUnit:-1
                            },
                        });
                    break;
                }   
                
                // string lineTem =TimeDetection.CurrentTime+",";
                // //ExcelSheet.Cells[num, 0].Value = TimeDetection.CurrentTime;
                // for (int i = 0; i < list.Count; i++)
                // {
                //     MeasuredSerie measuredSerie = list[i];
                //     decimal valueInBaseUnit = measuredSerie.DiscreteYValueFor(TimeDetection.CurrentTime).ValueInBaseUnit;
                //     //ExcelSheet.Cells[num, i + 1].Value = valueInBaseUnit;
                //     lineTem += valueInBaseUnit +",";
                // }

                num++;
                //lineData.Add(lineTem);
                TimeDetection.Next();
                if (TimeDetection.CurrentTime == MeasurementContainer.MaximumXValue)
                {
                    break;
                }
            }
            //File.WriteAllLines(outputFileName,lineData);
            //ExcelWorkBook.Save(outputFileName);
            return curveList;
        }

        

        // private void WriteHeader()
        // {
        //     string value = ("TimeCsvName");
        //     ExcelSheet.Cells[0, 0].Value = value;
        //     SeriesVisualisationSuggestion seriesVisualisationSuggestion = new SeriesVisualisationSuggestion();
        //     List<MeasuredSerie> list = MeasurementContainer.MeasuredSeries.Where((MeasuredSerie c) => !c.IsInternalCurve).ToList();
        //     for (int i = 0; i < list.Count; i++)
        //     {
        //         MeasuredSerie measuredSerie = list[i];
        //         string text = (seriesVisualisationSuggestion.Get(measuredSerie.SeriesType).TextIdentifier);
        //         string abbreviation = measuredSerie.UnitYValue.BaseFormat.GetBestLocalizationForCurrentLanguage(new CultureInfo("en-US")).Abbreviation;
        //         if (!string.IsNullOrEmpty(abbreviation))
        //         {
        //             text = text + " (" + abbreviation + ")";
        //         }

        //         ExcelSheet.Cells[0, i + 1].Value = text;
        //     }
        // }

        protected virtual void Dispose(bool disposing)
        {
            if (!Disposed && disposing)
            {
                // ExcelWorkBook.Dispose();
                // ExcelWorkBook = null;
                // ExcelSheet = null;
            }

            Disposed = true;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    // public class SeriesVisualisationSuggestion
    // {
    //     private static volatile Dictionary<SeriesType, VisualisationDescription> Descriptions;

    //     private static readonly object SyncRoot = new object();

    //     // public VisualisationDescription Get(SeriesType type)
    //     // {
    //     //     if (Descriptions == null)
    //     //     {
    //     //         lock (SyncRoot)
    //     //         {
    //     //             if (Descriptions == null)
    //     //             {
    //     //                 Descriptions = ReadAllDescriptions();
    //     //             }
    //     //         }
    //     //     }

    //     //     if (Descriptions.ContainsKey(type))
    //     //     {
    //     //         VisualisationDescription visualisationDescription = Descriptions[type];
    //     //         if (type == SeriesType.None)
    //     //         {
    //     //             visualisationDescription = visualisationDescription.Clone();
    //     //             visualisationDescription.StrokeColor = RandomColorForNone.GetNextColor();
    //     //         }

    //     //         return visualisationDescription;
    //     //     }

    //     //     throw new UnknownSeriesTypeException();
    //     // }

    //     // private static Dictionary<SeriesType, VisualisationDescription> ReadAllDescriptions()
    //     // {
    //     //     using MemoryStream stream = new MemoryStream(GetFileContent());
    //     //     using ExcelReader excelReader = new ExcelReader(stream);
    //     //     List<ExcelEntry> excelRows = excelReader.Read<ExcelEntry>();
    //     //     return ConvertRows(excelRows);
    //     // }

    //     // private static Dictionary<SeriesType, VisualisationDescription> ConvertRows(List<ExcelEntry> excelRows)
    //     // {
    //     //     Dictionary<SeriesType, VisualisationDescription> dictionary = new Dictionary<SeriesType, VisualisationDescription>();
    //     //     foreach (ExcelEntry excelRow in excelRows)
    //     //     {
    //     //         VisualisationDescription visualisationDescription = new VisualisationDescription();
    //     //         visualisationDescription.IsVisiblePerDefault = excelRow.IsVisiblePerDefault;
    //     //         visualisationDescription.StrokeThicknessInPixel = excelRow.StrokeThickness;
    //     //         visualisationDescription.StrokeColor = excelRow.Color;
    //     //         visualisationDescription.TextIdentifier = excelRow.TextIdentifier;
    //     //         visualisationDescription.ScalingFactorWhenDisplayedAgainstTime = excelRow.ScalingFactorWhenDisplayedAgainstTime;
    //     //         visualisationDescription.ScalingFactorWhenDisplayedAgainstAngle = excelRow.ScalingFactorWhenDisplayedAgainstAngle;
    //     //         visualisationDescription.DisplayPriority = excelRow.DisplayPriority;
    //     //         SeriesType key = (SeriesType)Enum.Parse(typeof(SeriesType), excelRow.SeriesType, ignoreCase: true);
    //     //         dictionary.Add(key, visualisationDescription);
    //     //     }

    //     //     return dictionary;
    //     // }

    //     // private static byte[] GetFileContent()
    //     // {
    //     //     string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SeriesVisualisationDescription.xlsx");
    //     //     if (File.Exists(path))
    //     //     {
    //     //         return File.ReadAllBytes(path);
    //     //     }

    //     //     return Resources.SeriesVisualisationDescription;
    //     // }
    // }

    //   public class ExcelWorkBook 
    // {
    //     private readonly Workbook Workbook;

    //     private bool Disposed;

    //     public ExcelFormat FileFormat => (ExcelFormat)Workbook.get_FileFormat();

    //     public IExcelWorkSheetCollection Sheets => new ExcelWorkSheetCollection(Workbook);

    //     public void Save(string fileName)
    //     {
    //         Workbook.Save(fileName);
    //     }

    //     public void Save(string fileName, ExcelFormat format)
    //     {
    //         Workbook.Save(fileName, (SaveFormat)format);
    //     }

    //     public void CalculateFormula()
    //     {
    //         Workbook.CalculateFormula();
    //     }

    //     public MemoryStream SaveToStream()
    //     {
    //         return Workbook.SaveToStream();
    //     }

    //     public MemoryStream SaveToStream(ExcelFormat format)
    //     {
    //         MemoryStream memoryStream = new MemoryStream();
    //         Workbook.Save((Stream)memoryStream, (SaveFormat)format);
    //         memoryStream.Position = 0L;
    //         return memoryStream;
    //     }

    //     public ExcelWorkBook()
    //     {
    //         //IL_000c: Unknown result type (might be due to invalid IL or missing references)
    //         //IL_0016: Expected O, but got Unknown
    //         AsposeLicence.SetLicence();
    //         Workbook = new Workbook();
    //         Workbook.get_Worksheets().RemoveAt(0);
    //     }

    //     public ExcelWorkBook(string file)
    //         : this(file, new LoadOptions())
    //     {
    //     }

    //     public ExcelWorkBook(Stream stream)
    //         : this(stream, new LoadOptions())
    //     {
    //     }

    //     public ExcelWorkBook(string file, LoadOptions loadOptions)
    //         : this()
    //     {
    //         //IL_000e: Unknown result type (might be due to invalid IL or missing references)
    //         //IL_0018: Expected O, but got Unknown
    //         Workbook = new Workbook(file, loadOptions.AsAsposeLoadOptions);
    //     }

    //     public ExcelWorkBook(Stream stream, LoadOptions loadOptions)
    //         : this()
    //     {
    //         //IL_000e: Unknown result type (might be due to invalid IL or missing references)
    //         //IL_0018: Expected O, but got Unknown
    //         Workbook = new Workbook(stream, loadOptions.AsAsposeLoadOptions);
    //     }

    //     protected virtual void Dispose(bool disposing)
    //     {
    //         if (!Disposed && disposing)
    //         {
    //             Workbook.Dispose();
    //         }

    //         Disposed = true;
    //     }

    //     public void Dispose()
    //     {
    //         Dispose(disposing: true);
    //         GC.SuppressFinalize(this);
    //     }
    // }

    // public enum ExcelFormat
    // {
    //     Auto = 0,
    //     Csv = 1,
    //     Excel97To2003 = 5,
    //     Xlsx = 6,
    //     TabDelimited = 11,
    //     Html = 12,
    //     Pdf = 13
    // }

    // public class ExcelEntry
    // {
    //     [Order(0)]
    //     public string SeriesType { get; set; }

    //     [Order(1)]
    //     public string TextIdentifier { get; set; }

    //     [Order(2)]
    //     public int StrokeThickness { get; set; }

    //     [Order(3)]
    //     public string Color { get; set; }

    //     [Order(4)]
    //     public bool IsVisiblePerDefault { get; set; }

    //     [Order(5)]
    //     public decimal ScalingFactorWhenDisplayedAgainstTime { get; set; }

    //     [Order(6)]
    //     public decimal ScalingFactorWhenDisplayedAgainstAngle { get; set; }

    //     [Order(7)]
    //     public int DisplayPriority { get; set; }
    // }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class OrderAttribute : Attribute
    {
        public int Index { get; set; }

        public string Name { get; set; }

        public bool ConvertUsingDataConverter { get; set; }

        public DisplayFormat DisplayFormat { get; set; }

        public OrderAttribute()
        {
            DisplayFormat = DisplayFormat.General;
            ConvertUsingDataConverter = true;
        }

        public OrderAttribute(int index)
            : this()
        {
            Index = index;
        }

        public OrderAttribute(string name, int index)
            : this()
        {
            Index = index;
            Name = name;
        }

        public OrderAttribute(string name, int index, DisplayFormat displayFormat)
            : this()
        {
            Index = index;
            DisplayFormat = displayFormat;
            Name = name;
        }

        public OrderAttribute(string name, int index, bool convertUsingDataConverter)
            : this()
        {
            Index = index;
            Name = name;
            ConvertUsingDataConverter = convertUsingDataConverter;
        }
    }

     public enum DisplayFormat
    {
        General = 0,
        Decimal1 = 1,
        Decimal2 = 2,
        Decimal3 = 3,
        Decimal4 = 4,
        CurrencySymbol1 = 5,
        CurrencySymbol2 = 6,
        CurrencySymbol3 = 7,
        CurrencySymbol4 = 8,
        Percentage1 = 9,
        Percentage2 = 10,
        Scientific1 = 11,
        Fraction1 = 12,
        Fraction2 = 13,
        DateShort = 14,
        DatEdmmmyyy = 0xF,
        DatEdmmm = 0x10,
        DatEmmmyy = 17,
        TimEhmmAmpm = 18,
        TimEhmmssAmpm = 19,
        TimEhmm = 20,
        TimEhmmss = 21,
        TimEmdyyhmm = 22,
        Currency1 = 37,
        Currency2 = 38,
        Currency3 = 39,
        Currency4 = 40,
        Accounting1 = 41,
        Accounting2 = 42,
        Accounting3 = 43,
        Accounting4 = 44,
        TimEmmss = 45,
        TimEhmmss2 = 46,
        TimEmmss0 = 47,
        Scientific2 = 48,
        Text = 49
    }

    public static class Resources
    {
        private const string ResourceFolder = "Deprag.FileFormats.Core.Resources.";

        public static byte[] SeriesVisualisationDescription => ResourceReader.GetBytes("Deprag.FileFormats.Core.Resources.SeriesVisualisationDescription.xlsx", typeof(Resources).Assembly);

        public static byte[] Language => ResourceReader.GetBytes("Deprag.FileFormats.Core.Resources.Language.xlsx", typeof(Resources).Assembly);

        public static byte[] Language_AST_Files => ResourceReader.GetBytes("Deprag.FileFormats.Core.Resources.Language-AST_Files.xlsx", typeof(Resources).Assembly);

        public static byte[] Language_ADFS => ResourceReader.GetBytes("Deprag.FileFormats.Core.Resources.Language-ADFS.xlsx", typeof(Resources).Assembly);

        public static byte[] FlowFormScrewImage => ResourceReader.GetBytes("Deprag.FileFormats.Core.Resources.Images.Schraube_FlowForm.png", typeof(Resources).Assembly);

        public static byte[] FlowPushScrewImage => ResourceReader.GetBytes("Deprag.FileFormats.Core.Resources.Images.Schraube_FlowPush.png", typeof(Resources).Assembly);
    }

    public static class ResourceReader
    {
        public static string GetFile(string fileName, Assembly executingAssembly)
        {
             Stream stream = executingAssembly.GetManifestResourceStream(fileName);
            if (stream == null)
            {
                throw new Exception("Resource " + fileName + " not found in " + executingAssembly.FullName + ".  Valid resources are: " + string.Join(", ", executingAssembly.GetManifestResourceNames()) + ".");
            }

             StreamReader streamReader = new StreamReader(stream);
            return streamReader.ReadToEnd();
        }

        public static byte[] GetBytes(string fileName, Assembly executingAssembly)
        {
             Stream stream = executingAssembly.GetManifestResourceStream(fileName);
            if (stream == null)
            {
                throw new Exception("Resource " + fileName + " not found in " + executingAssembly.FullName + ".  Valid resources are: " + string.Join(", ", executingAssembly.GetManifestResourceNames()) + ".");
            }

            return ToByteArray(stream);
        }

        public static List<string> GetAllFilesInNamespace(string namespaceString, Assembly executingAssembly)
        {
            List<string> list = new List<string>();
            IEnumerable<string> enumerable = from x in executingAssembly.GetManifestResourceNames()
                                             where x.StartsWith(namespaceString)
                                             select x;
            foreach (string item in enumerable)
            {
                string file = GetFile(item, executingAssembly);
                list.Add(file);
            }

            return list;
        }

        public static List<byte[]> GetAllFilesAsByteArrayInNamespace(string namespaceString, Assembly executingAssembly)
        {
            List<byte[]> list = new List<byte[]>();
            IEnumerable<string> enumerable = from x in executingAssembly.GetManifestResourceNames()
                                             where x.StartsWith(namespaceString)
                                             select x;
            foreach (string item in enumerable)
            {
                byte[] bytes = GetBytes(item, executingAssembly);
                list.Add(bytes);
            }

            return list;
        }

        private static byte[] ToByteArray(Stream stream)
        {
             MemoryStream memoryStream = new MemoryStream();
            byte[] array = new byte[1024];
            int count;
            while ((count = stream.Read(array, 0, array.Length)) > 0)
            {
                memoryStream.Write(array, 0, count);
            }

            return memoryStream.ToArray();
        }
    }

    [Serializable]
    public class UnknownSeriesTypeException : Exception
    {
        public UnknownSeriesTypeException()
        {
        }

        public UnknownSeriesTypeException(string message)
            : base(message)
        {
        }

        public UnknownSeriesTypeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected UnknownSeriesTypeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

     
     public static class RandomColorForNone
    {
        private static readonly List<string> Colors = new List<string>
        {
            "#660099", "#006633", "#0000CC", "#990000", "#FF0000", "#FF00CC", "#660033", "#009933", "#CC0000", "#6633CC",
            "#9900CC", "#663399", "#CC00CC", "#FF0099", "#0000FF", "#FF77FF", "#778888"
        };

        private static int CurrentIndex;

        public static string GetNextColor()
        {
            string result = Colors[CurrentIndex];
            CurrentIndex++;
            if (CurrentIndex >= Colors.Count)
            {
                CurrentIndex = 0;
            }

            return result;
        }
    }

      public class VisualisationDescription
    {
        public string TextIdentifier { get; set; }

        public int StrokeThicknessInPixel { get; set; }

        public string StrokeColor { get; set; }

        public bool IsVisiblePerDefault { get; set; }

        public decimal ScalingFactorWhenDisplayedAgainstTime { get; set; }

        public decimal ScalingFactorWhenDisplayedAgainstAngle { get; set; }

        public int DisplayPriority { get; set; }

        public VisualisationDescription Clone()
        {
            VisualisationDescription visualisationDescription = new VisualisationDescription();
            visualisationDescription.TextIdentifier = TextIdentifier;
            visualisationDescription.StrokeThicknessInPixel = StrokeThicknessInPixel;
            visualisationDescription.StrokeColor = StrokeColor;
            visualisationDescription.IsVisiblePerDefault = IsVisiblePerDefault;
            visualisationDescription.ScalingFactorWhenDisplayedAgainstTime = ScalingFactorWhenDisplayedAgainstTime;
            visualisationDescription.ScalingFactorWhenDisplayedAgainstAngle = ScalingFactorWhenDisplayedAgainstAngle;
            visualisationDescription.DisplayPriority = DisplayPriority;
            return visualisationDescription;
        }
    }
    public class MetaDataContainer
    {
        public IList<MetaDataItem> ProcessData { get; }

        public MetaDataContainer(IList<MetaDataItem> processData)
        {
            ProcessData = processData;
        }
    }
     public class MetaDataItem
    {
        public string TextIdentifier { get; set; }

        public object Value { get; set; }

        public bool IsInternal { get; set; }
    }
     public class TimeDetection
    {
        private readonly IMeasurementContainer MeasurementContainer;

        public decimal CurrentTime { get; private set; }

        public TimeDetection(IMeasurementContainer measurementContainer)
        {
            MeasurementContainer = measurementContainer;
        }

        public void Next()
        {
            decimal? samplingTimeInMillisecondsBetweenTwoDiscreteValues = MeasurementContainer.HardwareInformation.SamplingTimeInMillisecondsBetweenTwoDiscreteValues;
            CurrentTime += samplingTimeInMillisecondsBetweenTwoDiscreteValues.Value;
            if (CurrentTime >= MeasurementContainer.MaximumXValue)
            {
                CurrentTime = MeasurementContainer.MaximumXValue;
            }
        }
    }
}
