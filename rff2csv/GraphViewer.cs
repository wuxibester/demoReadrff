using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SimpleJSON;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json.Linq;
using System.Globalization;
using ICSharpCode.SharpZipLib.Core;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace rff2csv
{
    public class GraphViewer
    {
       string FileFullPath;
       byte[] File;

       private readonly MapSeriesTypes MapSeriesTypes = new MapSeriesTypes();
    
        public GraphViewer(string filename)
        {
            FileFullPath = filename;
        }

        public List<ReceiveMsg.CurveData> Read()
        {
           if (!FileIsGraphViewerContainer(FileFullPath))
			{
                //不是.d-graph结尾
				 //LoadGraphFile(fileInfo);
                 if(FileFullPath.EndsWith(".DFE", StringComparison.CurrentCultureIgnoreCase)){
                     //.DEF结尾解析
                     //new AdfsFormat(info.Name, info.FullName).Read();
                 }else{
                    if(!FileFullPath.EndsWith(".RFF", StringComparison.CurrentCultureIgnoreCase)){
                        
                         //return  Ast10Format(FileFullPath, System.IO.File.ReadAllBytes(FileFullPath));
                    }else{
                        //.RFF解析
                        return Ast12Format(FileFullPath, System.IO.File.ReadAllBytes(FileFullPath));
                    }
                 }
                  
			}
            return null;
            //保留 TODO
            // GraphVisualisationParameters graphVisualisationParameters = new PersistContainerFile(new ZipFileAlgorithm()).Load(fileInfo.FullName);
			// ModifyMeasurementFileNameForCurveOverlayMode(graphVisualisationParameters);
			// return graphVisualisationParameters;
        }

        public List<ReceiveMsg.CurveData> Ast10Format(string filename, byte[] file)
        {
                File = file;
                IMeasurementContainer measurementContainer=null;
                try
                {
                    measurementContainer= ReadAst10();
                }
                catch (InvalidHeaderVersionException)
                {
                    //measurementContainer = ReadAst10();
                }
                measurementContainer.OriginalFileData = File;
                MeasurementCsvConverter measurementCsvConverter = new MeasurementCsvConverter(measurementContainer);
                //string outputPathFor = "";
                var aa = measurementCsvConverter.Save(toolType.AST11);
                return aa;
        }

        public List<ReceiveMsg.CurveData> Ast12Format(string filename, byte[] file)
        {
            File = ExtractContainedBinFileFromAst12File(file);
            IMeasurementContainer measurementContainer=null;
            try
            {
                measurementContainer= ReadAst12();
            }
            catch (InvalidHeaderVersionException)
            {
                //measurementContainer = ReadAst10();
            }
            //ReadAdditionalDataFromFile(measurementContainer);
            measurementContainer.OriginalFileData = File;
            // foreach (Ast12ContainerItem item in GetDecryptedFilesFromAst12ContainerFile())
            // {
            //    System.IO.File.WriteAllBytes(( FileFullPath + "_" + item.FileName), item.Content);
            // }
            MeasurementCsvConverter measurementCsvConverter = new MeasurementCsvConverter(measurementContainer);
			//string outputPathFor = "";
			var aa = measurementCsvConverter.Save(toolType.AST12);
            return aa;

        }

          private void ReadAdditionalDataFromFile(IMeasurementContainer measurementContainer)
        {
            List<Ast12ContainerItem> decryptedFilesFromAst12ContainerFile = GetDecryptedFilesFromAst12ContainerFile();
            Ast12ContainerItem ast12ContainerItem = decryptedFilesFromAst12ContainerFile.FirstOrDefault((Ast12ContainerItem x) => string.Compare(x.FileName, "program.json", StringComparison.Ordinal) == 0);
            if (ast12ContainerItem?.Content != null)
            {
                byte[] bytes = ast12ContainerItem.Content.ToArray();
                string @string = Encoding.UTF8.GetString(bytes);
                GetProgramDataSet(measurementContainer, @string);
            }

            Ast12ContainerItem ast12ContainerItem2 = decryptedFilesFromAst12ContainerFile.FirstOrDefault((Ast12ContainerItem x) => string.Compare(x.FileName, "systeminfo.json", StringComparison.Ordinal) == 0);
            if (ast12ContainerItem2?.Content != null)
            {
                byte[] bytes2 = ast12ContainerItem2.Content.ToArray();
                string string2 = Encoding.UTF8.GetString(bytes2);
                GetSystemDataSet(measurementContainer, string2);
            }
        }

        private static void GetSystemDataSet(IMeasurementContainer measurementContainer, string jsonString)
        {
            JArray val = JArray.Parse("[" + jsonString + "]");
            foreach (JToken item in val)
            {
                JObject val2 = (JObject)(object)((item is JObject) ? item : null);
                if (val2 == null)
                {
                    continue;
                }

                foreach (KeyValuePair<string, JToken> item2 in val2)
                {
                    measurementContainer.HeaderInformation.Add(item2.Key, item2.Value);
                }
            }
        }


        private static void GetProgramDataSet(IMeasurementContainer measurementContainer, string jsonString)
        {
            JsonToAst12ProgramConverter jsonToAst12ProgramConverter = new JsonToAst12ProgramConverter(jsonString);
            IEnumerable<ProcessDataSet> dataSet = jsonToAst12ProgramConverter.GetDataSet();
            foreach (ProcessDataSet item in dataSet)
            {
                measurementContainer.ProcessData.Add(item);
            }

            AppendStepNames(measurementContainer);
        }

        private static void AppendStepNames(IMeasurementContainer measurementContainer)
        {
            MeasuredSerie measuredSerie = measurementContainer.MeasuredSeries.FirstOrDefault((MeasuredSerie c) => c.SeriesType == SeriesType.StepNumber);
            if (measuredSerie == null)
            {
                return;
            }

            ProcessDataSet processDataSet = measurementContainer.ProcessData.FirstOrDefault((ProcessDataSet c) => c.TextIdentifier.IsTheSameIgnoreCase("@TEXT_Steps"));
            if (processDataSet == null)
            {
                return;
            }

            List<ProcessDataSet> list = (List<ProcessDataSet>)processDataSet.Value;
            if (list == null)
            {
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                ProcessDataSet processDataSet2 = list[i];
                int stepNumber = i + 1;
                MeasuredPoint measuredPoint = measuredSerie.MeasuredPoints.FirstOrDefault((MeasuredPoint c) => c.YValue.ValueInBaseUnit == (decimal)stepNumber);
                if (measuredPoint != null)
                {
                    string languageTextIdentifier = processDataSet2.TextIdentifier.Split(' ')[1];
                    Annotation item = new Annotation(stepNumber, languageTextIdentifier);
                    measuredSerie.AnnotationsForUser.Add(item);
                }
            }
        }

         public List<Ast12ContainerItem> GetDecryptedFilesFromAst12ContainerFile()
        {
            //IL_000d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0013: Expected O, but got Unknown
             MemoryStream memoryStream = new MemoryStream(File);
            ZipFile val = new ZipFile((Stream)memoryStream);
            try
            {
                ZipEntry val2 = HandleZipPassword(val);
                List<Ast12ContainerItem> list;
                using (Stream zipStream = val.GetInputStream(val2))
                {
                    list = ExtractFile(zipStream, File);
                }

                ReadFileContentFromContainerFileList(list, val);
                return list;
            }
            finally
            {
                ((IDisposable)val)?.Dispose();
            }
        }

        private static List<Ast12ContainerItem> ExtractFile(Stream zipStream, byte[] ast12File)
        {
             StreamReader streamReader = new StreamReader(zipStream);
            string text = streamReader.ReadToEnd();
            JObject val = JsonConvert.DeserializeObject<JObject>(text);
            return ChecksIfFilesExistsInZipContainer(((JToken)((JToken)val).Value<JArray>((object)"Content")).ToObject<List<Ast12ContainerItem>>(), ast12File);
        }
        private static List<Ast12ContainerItem> ChecksIfFilesExistsInZipContainer(IEnumerable<Ast12ContainerItem> unfilteredFileList, byte[] ast12File)
        {
            List<string> listOfFilesInZipFolder = GetListOfFilesInZipFolder(ast12File);
            List<Ast12ContainerItem> list = new List<Ast12ContainerItem>();
            foreach (Ast12ContainerItem unfilteredFile in unfilteredFileList)
            {
                if (listOfFilesInZipFolder.Contains(unfilteredFile.FileName) && !(unfilteredFile.FileName == "graph.bin") && !(unfilteredFile.FileName == "content.json"))
                {
                    list.Add(new Ast12ContainerItem
                    {
                        Content = null,
                        FileName = unfilteredFile.FileName
                    });
                }
            }

            return list;
        }
        private static List<string> GetListOfFilesInZipFolder(byte[] ast12File)
        {
            //IL_000e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0014: Expected O, but got Unknown
            //IL_0023: Unknown result type (might be due to invalid IL or missing references)
            //IL_002a: Expected O, but got Unknown
            List<string> list = new List<string>();
             MemoryStream memoryStream = new MemoryStream(ast12File);
            ZipFile val = new ZipFile((Stream)memoryStream);
            try
            {
                foreach (ZipEntry item in val)
                {
                    ZipEntry val2 = item;
                    if (val2.IsFile)
                    {
                        list.Add(val2.Name);
                    }
                }

                return list;
            }
            finally
            {
                ((IDisposable)val)?.Dispose();
            }
        }

        private static void ReadFileContentFromContainerFileList(IEnumerable<Ast12ContainerItem> files, ZipFile zf)
        {
            foreach (Ast12ContainerItem file in files)
            {
                ZipEntry entry = zf.GetEntry(file.FileName);
                byte[] buffer = new byte[entry.Size];
                 Stream stream = zf.GetInputStream(entry);
                 MemoryStream memoryStream = new MemoryStream(buffer);
                stream.CopyTo(memoryStream);
                file.Content = memoryStream.ToArray();
            }
        }


         public class Ast12ContainerItem
        {
            public string FileName { get; set; }

            public byte[] Content { get; set; }
        }

        public IMeasurementContainer ReadAst10()
        {
            GraphsContainer fileContent = ReadMeasuredValuesFromAst10File();
            IMeasurementContainer measurementContainer = ConvertFileContent(fileContent);
            WriteHeaderInformation(measurementContainer);
            return measurementContainer;
        }


        public IMeasurementContainer ReadAst12()
        {
            GraphsContainer fileContent = ReadMeasuredValuesFromAst12File();
            IMeasurementContainer measurementContainer = ConvertFileContent(fileContent);
            WriteHeaderInformation(measurementContainer);
            return measurementContainer;
        }

        private static void WriteHeaderInformation(IMeasurementContainer container)
        {
            Ast10HeaderInformations ast10HeaderInformations = new Ast10HeaderInformations(container);
            ast10HeaderInformations.Process();
        }

        public class Ast10HeaderInformations
        {
            private readonly IMeasurementContainer Container;

            public Ast10HeaderInformations(IMeasurementContainer container)
            {
                Container = container;
            }

            public void Process()
            {
                Container.HeaderInformation.Add("HeaderScrewdriverName", Container.HardwareInformation.ScrewDriverClearName);
            }
        }

        private IMeasurementContainer ConvertFileContent(GraphsContainer fileContent)
        {
            MeasurementContainer measurementContainer = new MeasurementContainer(FileFullPath, File);
            //measurementContainer.ContainerName = ContainerName;
            foreach (SingleGraph item in fileContent.AllGraphs())
            {
                MeasuredSerie measuredSerie = ToSingle(item);
                measurementContainer.AddSerie(measuredSerie);
            }

            string text = ((fileContent.CombinedHeaderInformation != null) ? fileContent.CombinedHeaderInformation.HeaderData.TypeScrewdriver : fileContent.CombinedHeaderInformationAst12.HeaderData.TypeScrewdriver);
            measurementContainer.HardwareInformation.ScrewDriverClearName = text;
            measurementContainer.HardwareInformation.TorqueMaximalInNewtonMetre = null;
            measurementContainer.HardwareInformation.SamplingTimeInMillisecondsBetweenTwoDiscreteValues = fileContent.TimePerMeasurement;
            return measurementContainer;
        }

        

        

        private MeasuredSerie ToSingle(SingleGraph singleGraph)
        {
            MeasuredSerie measuredSerie = new MeasuredSerie(new Time(), UnitY(singleGraph));
            SeriesType seriesType = (measuredSerie.SeriesType = MapSeriesTypes.GetMapping(singleGraph.Type));
            measuredSerie.AlternativeDisplayName = singleGraph.InternalGraphType.ToString(CultureInfo.InvariantCulture);
            foreach (GraphPoint calculatedValue in singleGraph.CalculatedValues)
            {
                measuredSerie.AddPoint(new MeasuredPoint(calculatedValue.Time, calculatedValue.Value));
            }

            return measuredSerie;
        }

        private static IUnit UnitY(SingleGraph singleGraph)
        {
            if (!singleGraph.CalculatedValues.Any())
            {
                return new NoUnit();
            }

            return singleGraph.CalculatedValues.FirstOrDefault().Value;
        }

        public GraphsContainer ReadMeasuredValuesFromAst10File()
        {
            CombinedHeaderInformation headerInformation = Preprocess10();
            return Calculate10(headerInformation);
        }

        public GraphsContainer ReadMeasuredValuesFromAst12File()
        {
            CombinedHeaderInformationAst12 headerInformation = Preprocess12();
            return Calculate12(headerInformation);
        }

        private GraphsContainer Calculate10(CombinedHeaderInformation headerInformation)
        {
            ReadMeasuredValues readMeasuredValues = new ReadMeasuredValues(File, headerInformation);
            return readMeasuredValues.GetMeasuredValues();
        }

        private GraphsContainer Calculate12(CombinedHeaderInformationAst12 headerInformation)
        {
            ReadMeasuredValuesAst12 readMeasuredValuesAst = new ReadMeasuredValuesAst12(File, headerInformation);
            return readMeasuredValuesAst.GetMeasuredValues();
        }

        private CombinedHeaderInformation Preprocess10()
        {
            PreprocessAllSteps preprocessAllSteps = new PreprocessAllSteps(File);
            return preprocessAllSteps.ReadMeasurementDataFile();
        }

        private CombinedHeaderInformationAst12 Preprocess12()
        {
            PreprocessAllStepsAst12 preprocessAllStepsAst = new PreprocessAllStepsAst12(File);
            return preprocessAllStepsAst.ReadMeasurementDataFile();
        }


        public static byte[] ExtractContainedBinFileFromAst12File(byte[] ast12File)
        {
            //IL_0008: Unknown result type (might be due to invalid IL or missing references)
            //IL_000e: Expected O, but got Unknown
             MemoryStream memoryStream = new MemoryStream(ast12File);
            ZipFile val = new ZipFile((Stream)memoryStream);
            try
            {
                HandleZipPassword(val);
                ZipEntry entry = val.GetEntry(KnownFileNames["GraphFile"]);
                byte[] array = new byte[entry.Size];
                 Stream stream = val.GetInputStream(entry);
                 MemoryStream memoryStream2 = new MemoryStream(array);
                StreamUtils.Copy(stream, (Stream)memoryStream2, array);
                return memoryStream2.ToArray();
            }
            catch (Exception ex)
            {
                //Log.Debug("[error]ExtractContainedBinFileFromAst12File error!"+ex.Message);
                return null;
            }
            finally
            {
                ((IDisposable)val)?.Dispose();
            }
        }

        private static ZipEntry HandleZipPassword(ZipFile zf)
        {
            ZipEntry entry = zf.GetEntry(KnownFileNames["ContentFile"]);
             Stream zipStream = zf.GetInputStream(entry);
            string password = ExtractPassword(zipStream);
            zf.Password=(password);
            return entry;
        }

        private static string ExtractPassword(Stream zipStream)
        {
             StreamReader streamReader = new StreamReader(zipStream);
            string text = streamReader.ReadToEnd();
            JObject val = JObject.Parse(text);
            JToken val2 = val["MacAddress"];
            string[] parts = ((object)val2).ToString().Split(':');
            return ReCreatePassword(parts);
        }

        public static string ReCreatePassword(string[] parts)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (string s in parts)
            {
                int num = byte.Parse(s, NumberStyles.HexNumber, CultureInfo.CurrentCulture);
                string value = $"{(byte)(~num):X2}";
                stringBuilder.Append(value);
            }

            return stringBuilder.ToString();
        }
    

        private bool FileIsGraphViewerContainer(string path)
		{
			return path.EndsWith(".d-graph", StringComparison.OrdinalIgnoreCase);
		}

        private static readonly IReadOnlyDictionary<string, string> KnownFileNames = new Dictionary<string, string>
        {
            { "ContentFile", "content.json" },
            { "GraphFile", "graph.bin" },
            { "ProgramFile", "program.json" },
            { "SystemInformationsFile", "systeminfo.json" },
            { "ScrewdriverInformationsFile", "screwdriverinfo.json" }
        };

        [Serializable]
        public class InvalidHeaderVersionException : Exception
        {
            public InvalidHeaderVersionException()
            {
            }

            public InvalidHeaderVersionException(string message)
                : base(message)
            {
            }

            public InvalidHeaderVersionException(string message, Exception innerException)
                : base(message, innerException)
            {
            }

            protected InvalidHeaderVersionException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }
        }

        
       
    }
}