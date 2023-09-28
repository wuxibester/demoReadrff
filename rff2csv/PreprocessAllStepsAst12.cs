using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace rff2csv
{
    public class PreprocessAllStepsAst12
    {
        private readonly byte[] File;

        public PreprocessAllStepsAst12(byte[] file)
        {
            File = file;
        }

        public CombinedHeaderInformationAst12 ReadMeasurementDataFile()
        {
            CombinedHeaderInformationAst12 combinedHeaderInformationAst = new CombinedHeaderInformationAst12();
            ValidateFile(combinedHeaderInformationAst);
            SkipInvalidHeader(combinedHeaderInformationAst);
            ReadHeaderData(combinedHeaderInformationAst);
            GetMeasurementLineInfo(combinedHeaderInformationAst);
            return combinedHeaderInformationAst;
        }

        private void ValidateFile(CombinedHeaderInformationAst12 headerInformation)
        {
            FileValidationAst12 fileValidationAst = new FileValidationAst12(File, headerInformation);
            fileValidationAst.VerifyHeaderOrThrowException();
        }

        private static void SkipInvalidHeader(CombinedHeaderInformationAst12 headerInformation)
        {
            TrimInvalidHeaderAst12 trimInvalidHeaderAst = new TrimInvalidHeaderAst12(headerInformation);
            trimInvalidHeaderAst.TrimHeader();
        }

        private void ReadHeaderData(CombinedHeaderInformationAst12 headerInformation)
        {
            ReadHeaderAst12 readHeaderAst = new ReadHeaderAst12(File, headerInformation);
            readHeaderAst.ReadData();
        }

        private void GetMeasurementLineInfo(CombinedHeaderInformationAst12 headerInformation)
        {
            CreateMeasurementLine createMeasurementLine = new CreateMeasurementLine(File, headerInformation);
            createMeasurementLine.DefineMeasurementLine();
        }
    }

      public class CreateMeasurementLine
    {
        private readonly byte[] File;

        private readonly CombinedHeaderInformation HeaderInformation;

        private readonly CombinedHeaderInformationAst12 HeaderInformationAst12;

        public CreateMeasurementLine(byte[] file, CombinedHeaderInformation headerInformation)
        {
            File = file;
            HeaderInformation = headerInformation;
        }

        public CreateMeasurementLine(byte[] file, CombinedHeaderInformationAst12 headerInformation)
        {
            File = file;
            HeaderInformationAst12 = headerInformation;
        }

        public void DefineMeasurementLine()
        {
            MeasurementLineInterpretingInfo measurementLineInterpretingInfo = new MeasurementLineInterpretingInfo();
            BuildMeasurementSetDescriptions(measurementLineInterpretingInfo);
            CalculateLineLength(measurementLineInterpretingInfo);
            CalculateNumberOfLines(measurementLineInterpretingInfo);
            if (HeaderInformation != null)
            {
                HeaderInformation.LineInterpretingInformation = measurementLineInterpretingInfo;
            }
            else
            {
                HeaderInformationAst12.LineInterpretingInformation = measurementLineInterpretingInfo;
            }
        }

        private void BuildMeasurementSetDescriptions(MeasurementLineInterpretingInfo interpretingInfo)
        {
            int num = 0;
            IEnumerator enumerator = ((HeaderInformation != null) ? HeaderInformation.RawLineDescription.GetEnumerator() : HeaderInformationAst12.RawLineDescription.GetEnumerator());
            while (enumerator.MoveNext())
            {
                RawGraphDescription rawGraphDescription = GetRawGraphDescription(enumerator);
                GraphType typeOfGraph = GetTypeOfGraph(rawGraphDescription);
                GraphTypeByteLengthMapping graphTypeByteLengthMapping = new GraphTypeByteLengthMapping();
                int num2 = ((HeaderInformation != null) ? graphTypeByteLengthMapping.GetLengthByType(typeOfGraph) : graphTypeByteLengthMapping.GetLengthByTypeForAst12(typeOfGraph));
                MeasurementSetDescription measurementSetDescription = new MeasurementSetDescription();
                measurementSetDescription.GraphType = typeOfGraph;
                measurementSetDescription.DataTypeLength = num2;
                measurementSetDescription.Offset = num;
                measurementSetDescription.InternalGraphType = rawGraphDescription.GraphType;
                num += num2;
                interpretingInfo.AddMeasurementSetDescription(measurementSetDescription);
            }
        }

        private static RawGraphDescription GetRawGraphDescription(IEnumerator linesEnumerator)
        {
            return (RawGraphDescription)GetEnumerableElement(linesEnumerator);
        }

        private static GraphType GetTypeOfGraph(RawGraphDescription rawGraphDescription)
        {
            if (rawGraphDescription.GraphVersion == 0)
            {
                return (GraphType)rawGraphDescription.GraphType;
            }

            if (rawGraphDescription.GraphVersion == 1)
            {
                return GraphType.UnknownContent;
            }

            throw new InvalidGraphVersionException($"{rawGraphDescription.GraphVersion} is not a valid Version. Valid Versions: 0 and 1");
        }

        private static void CalculateLineLength(MeasurementLineInterpretingInfo interpretingInfo)
        {
            int num = 0;
            foreach (MeasurementSetDescription item in interpretingInfo.MeasurementLines())
            {
                num += item.DataTypeLength;
            }

            interpretingInfo.LineLength = num;
        }

        private void CalculateNumberOfLines(MeasurementLineInterpretingInfo interpretingInfo)
        {
            int num = HeaderInformation?.HeaderData.OffsetGraphData ?? HeaderInformationAst12.HeaderData.OffsetGraphData;
            int num3 = (interpretingInfo.NumberOfLines = (File.Length - num) / interpretingInfo.LineLength);
        }

        private static object GetEnumerableElement(IEnumerator linesEnumerator)
        {
            if (linesEnumerator.Current == null)
            {
                throw new NullReferenceException("IEnumerator.Current is empty.");
            }

            return linesEnumerator.Current;
        }
    }

     [Serializable]
    public class InvalidGraphVersionException : Exception
    {
        public InvalidGraphVersionException()
        {
        }

        public InvalidGraphVersionException(string message)
            : base(message)
        {
        }

        public InvalidGraphVersionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected InvalidGraphVersionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    public class GraphTypeByteLengthMapping
    {
        private static volatile Dictionary<GraphType, int> GraphTypeByteLengthMappingDict;

        private static readonly object SyncRoot = new object();

        public GraphTypeByteLengthMapping()
        {
            if (GraphTypeByteLengthMappingDict != null)
            {
                return;
            }

            lock (SyncRoot)
            {
                if (GraphTypeByteLengthMappingDict == null)
                {
                    GraphTypeByteLengthMappingDict = new Dictionary<GraphType, int>
                    {
                        {
                            GraphType.Workload,
                            2
                        },
                        {
                            GraphType.RotationalSpeed,
                            2
                        },
                        {
                            GraphType.Torque,
                            2
                        },
                        {
                            GraphType.Angle,
                            4
                        },
                        {
                            GraphType.ErrorCode,
                            2
                        },
                        {
                            GraphType.TemperatureMotor,
                            2
                        },
                        {
                            GraphType.ProgramPosition,
                            2
                        },
                        {
                            GraphType.CurrentMotor,
                            2
                        },
                        {
                            GraphType.TemperaturePowerUnit,
                            2
                        },
                        {
                            GraphType.TorqueAdditionalSensor1,
                            2
                        },
                        {
                            GraphType.AngleAdditionalSensor1,
                            4
                        },
                        {
                            GraphType.TorqueMotor,
                            2
                        },
                        {
                            GraphType.AngleMotor,
                            4
                        },
                        {
                            GraphType.TorqueAdditionalSensor2,
                            2
                        },
                        {
                            GraphType.AngleAdditionalSensor2,
                            4
                        },
                        {
                            GraphType.AnalogStop,
                            2
                        },
                        {
                            GraphType.UnknownContent,
                            2
                        }
                    };
                }
            }
        }

        public int GetLengthByType(GraphType type)
        {
            if (!GraphTypeByteLengthMappingDict.ContainsKey(type))
            {
                throw new InvalidGraphTypeException($"ID {type} ist not a valid type.");
            }

            return GraphTypeByteLengthMappingDict[type];
        }

        public int GetLengthByTypeForAst12(GraphType type)
        {
            return 4;
        }
    }

    [Serializable]
    public class InvalidGraphTypeException : Exception
    {
        public InvalidGraphTypeException()
        {
        }

        public InvalidGraphTypeException(string message)
            : base(message)
        {
        }

        public InvalidGraphTypeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected InvalidGraphTypeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
     public class ReadHeaderAst12
    {
        private const int SizeOfHeaderVersionAst12 = 294;

        private const int SizeOfFieldRemainingLength = 4;

        private readonly CombinedHeaderInformationAst12 CombinedHeaderInformation;

        private readonly byte[] File;

        public ReadHeaderAst12(byte[] file, CombinedHeaderInformationAst12 combinedHeaderInformation)
        {
            File = file;
            CombinedHeaderInformation = combinedHeaderInformation;
        }

        public void ReadData()
        {
             MemoryStream input = new MemoryStream(File);
             BinaryReader reader = new BinaryReader(input);
            try
            {
                ReadData(reader);
            }
            catch (Exception innerException)
            {
                throw new InvalidFileFormatException("Error while reading header", innerException);
            }
        }

        private void ReadData(BinaryReader reader)
        {
            RawHeaderDataAst12 rawHeaderDataAst = new RawHeaderDataAst12();
            SkipHeader(reader);
            rawHeaderDataAst.TypeScrewdriver = ConvertToString(reader.ReadChars(32));
            rawHeaderDataAst.GearTransmission = reader.ReadUInt32();
            rawHeaderDataAst.GearEfficacy = reader.ReadUInt16();
            rawHeaderDataAst.ResolutionClass = reader.ReadUInt16();
            rawHeaderDataAst.Increment = reader.ReadInt32();
            rawHeaderDataAst.TorqueCalibrationValue = reader.ReadInt32();
            rawHeaderDataAst.GraphCount = reader.ReadInt32();
            RawMeasurementLineDescription rawLineDescription = ReadMeasurementLineMapping(rawHeaderDataAst.GraphCount, reader);
            ReadDataFromExtendetHeaderB(rawHeaderDataAst, reader);
            rawHeaderDataAst.OffsetGraphData = CalculateOffsetForMeasuredValues(rawHeaderDataAst);
            CombinedHeaderInformation.HeaderData = rawHeaderDataAst;
            CombinedHeaderInformation.RawLineDescription = rawLineDescription;
        }

        private void SkipHeader(BinaryReader reader)
        {
            reader.ReadBytes(CombinedHeaderInformation.OffsetHeaderData);
        }

        private static string ConvertToString(char[] readChars)
        {
            if (readChars == null || readChars.Length == 0)
            {
                return string.Empty;
            }

            return new string(readChars).TrimEnd(default(char));
        }

        private static RawMeasurementLineDescription ReadMeasurementLineMapping(int maxGraphCount, BinaryReader reader)
        {
            RawMeasurementLineDescriptionReaderAst12 rawMeasurementLineDescriptionReaderAst = new RawMeasurementLineDescriptionReaderAst12(reader, maxGraphCount);
            return rawMeasurementLineDescriptionReaderAst.ReadAllLines();
        }

        private static void ReadDataFromExtendetHeaderB(RawHeaderDataAst12 header, BinaryReader reader)
        {
            header.RemainingLength = reader.ReadInt32();
            header.TorqueShift = 0;
        }

        private int CalculateOffsetForMeasuredValues(RawHeaderDataAst12 header)
        {
            return GetOptionalHeaderLength() + 294 + 4 + header.RemainingLength;
        }

        private int GetOptionalHeaderLength()
        {
            if (CombinedHeaderInformation.OffsetHeaderData == 0)
            {
                return 0;
            }

            return CombinedHeaderInformation.OffsetHeaderData - 2;
        }
    }

     public class RawMeasurementLineDescriptionReaderAst12
    {
        private const int MaximumGraphsSupported = 30;

        private const int SizeOfOneLine = 4;

        private readonly int GraphCountInFile;

        private readonly BinaryReader Reader;

        public RawMeasurementLineDescriptionReaderAst12(BinaryReader reader, int graphCountInFile)
        {
            Reader = reader;
            GraphCountInFile = graphCountInFile;
        }

        public RawMeasurementLineDescription ReadAllLines()
        {
            RawMeasurementLineDescription result = ReadValues();
            SeekEndOfHeader();
            return result;
        }

        private RawMeasurementLineDescription ReadValues()
        {
            RawMeasurementLineDescription rawMeasurementLineDescription = new RawMeasurementLineDescription();
            for (int i = 0; i < GraphCountInFile; i++)
            {
                RawGraphDescription graph = ReadLine();
                rawMeasurementLineDescription.AddGraphData(graph);
            }

            return rawMeasurementLineDescription;
        }

        private RawGraphDescription ReadLine()
        {
            RawGraphDescription rawGraphDescription = new RawGraphDescription();
            rawGraphDescription.GraphVersion = Reader.ReadInt32();
            rawGraphDescription.GraphType = Reader.ReadInt32();
            return rawGraphDescription;
        }

        private void SeekEndOfHeader()
        {
            int count = (30 - GraphCountInFile) * 4;
            Reader.ReadBytes(count);
        }
    }

    [Serializable]
    public class InvalidFileFormatException : Exception
    {
        public InvalidFileFormatException()
        {
        }

        public InvalidFileFormatException(string message)
            : base(message)
        {
        }

        public InvalidFileFormatException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected InvalidFileFormatException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    internal class TrimInvalidHeaderAst12
    {
        private const int OffsetHeaderVersion = 2;

        private readonly CombinedHeaderInformationAst12 HeaderInformation;

        public TrimInvalidHeaderAst12(CombinedHeaderInformationAst12 headerInformation)
        {
            HeaderInformation = headerInformation;
        }

        public void TrimHeader()
        {
            HeaderInformation.OffsetHeaderData = 2;
        }
    }
    public class FileValidationAst12
    {
        private readonly CombinedHeaderInformationAst12 CombinedHeaderInformation;

        private readonly byte[] File;

        public FileValidationAst12(byte[] file, CombinedHeaderInformationAst12 combinedHeaderInformation)
        {
            File = file;
            CombinedHeaderInformation = combinedHeaderInformation;
        }

        public void VerifyHeaderOrThrowException()
        {
            int num = ReadHeaderVersionFromByte();
            ThrowExceptionOnInvalidHeader(num);
            CombinedHeaderInformation.HeaderVersion = num;
        }

        private int ReadHeaderVersionFromByte()
        {
             MemoryStream input = new MemoryStream(File);
             BinaryReader binaryReader = new BinaryReader(input);
            return binaryReader.ReadUInt16();
        }

        private static void ThrowExceptionOnInvalidHeader(int headerVersion)
        {
            int[] source = new int[1] { 4 };
            if (source.Contains(headerVersion))
            {
                return;
            }

            throw new InvalidHeaderVersionException($"Header Version '{headerVersion}' is not supported");
        }
    }

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