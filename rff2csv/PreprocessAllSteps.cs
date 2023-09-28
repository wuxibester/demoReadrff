using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace rff2csv
{
      public class PreprocessAllSteps
    {
        private readonly byte[] File;

        public PreprocessAllSteps(byte[] file)
        {
            File = file;
        }

        public CombinedHeaderInformation ReadMeasurementDataFile()
        {
            CombinedHeaderInformation combinedHeaderInformation = new CombinedHeaderInformation();
            ValidateFile(combinedHeaderInformation);
            SkipInvalidHeader(combinedHeaderInformation);
            ReadHeaderData(combinedHeaderInformation);
            GetMeasurementLineInfo(combinedHeaderInformation);
            return combinedHeaderInformation;
        }

        private void ValidateFile(CombinedHeaderInformation headerInformation)
        {
            FileValidation fileValidation = new FileValidation(File, headerInformation);
            fileValidation.VerifyHeaderOrThrowException();
        }

        private void SkipInvalidHeader(CombinedHeaderInformation headerInformation)
        {
            TrimInvalidHeader trimInvalidHeader = new TrimInvalidHeader(File, headerInformation);
            trimInvalidHeader.TrimHeader();
        }

        private void ReadHeaderData(CombinedHeaderInformation headerInformation)
        {
            ReadHeader readHeader = new ReadHeader(File, headerInformation);
            readHeader.ReadData();
        }

        private void GetMeasurementLineInfo(CombinedHeaderInformation headerInformation)
        {
            CreateMeasurementLine createMeasurementLine = new CreateMeasurementLine(File, headerInformation);
            createMeasurementLine.DefineMeasurementLine();
        }
    }

      public class RawMeasurementLineDescriptionReader
    {
        private const int MaximumGraphsSupported = 15;

        private const int SizeOfOneLine = 4;

        private readonly int GraphCountInFile;

        private readonly BinaryReader Reader;

        public RawMeasurementLineDescriptionReader(BinaryReader reader, int graphCountInFile)
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
            rawGraphDescription.GraphVersion = Reader.ReadInt16();
            rawGraphDescription.GraphType = Reader.ReadInt16();
            return rawGraphDescription;
        }

        private void SeekEndOfHeader()
        {
            int count = (15 - GraphCountInFile) * 4;
            Reader.ReadBytes(count);
        }
    }

    public class ReadHeader
    {
        private const int HeaderA = 0;

        private const int HeaderC = 1;

        private const int SizeOfHeaderVersionA = 94;

        private const int SizeOfFieldRemainingLength = 4;

        private readonly CombinedHeaderInformation CombinedHeaderInformation;

        private readonly byte[] File;

        public ReadHeader(byte[] file, CombinedHeaderInformation combinedHeaderInformation)
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
            RawHeaderData rawHeaderData = new RawHeaderData();
            SkipHeader(reader);
            rawHeaderData.TypeScrewdriver = ConvertToString(reader.ReadChars(16));
            rawHeaderData.GearTransmission = reader.ReadInt32();
            rawHeaderData.GearEfficacy = reader.ReadInt16();
            rawHeaderData.Increment = reader.ReadInt32();
            rawHeaderData.TorqueCalibrationValue = reader.ReadInt32();
            rawHeaderData.GraphCount = reader.ReadInt16();
            RawMeasurementLineDescription rawLineDescription = ReadMeasurementLineMapping(rawHeaderData.GraphCount, reader);
            ReadDataFromExtendetHeaderB(rawHeaderData, reader);
            rawHeaderData.OffsetGraphData = CalculateOffsetForMeasuredValues(rawHeaderData);
            CombinedHeaderInformation.HeaderData = rawHeaderData;
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
            RawMeasurementLineDescriptionReader rawMeasurementLineDescriptionReader = new RawMeasurementLineDescriptionReader(reader, maxGraphCount);
            return rawMeasurementLineDescriptionReader.ReadAllLines();
        }

        private void ReadDataFromExtendetHeaderB(RawHeaderData header, BinaryReader reader)
        {
            if (CombinedHeaderInformation.HeaderVersion == 0 || CombinedHeaderInformation.HeaderVersion == 1)
            {
                header.RemainingLength = 0;
                header.TorqueShift = 0;
            }
            else
            {
                header.RemainingLength = reader.ReadInt32();
                header.TorqueShift = reader.ReadInt16();
            }
        }

        private int CalculateOffsetForMeasuredValues(RawHeaderData header)
        {
            if (CombinedHeaderInformation.HeaderVersion == 0 || CombinedHeaderInformation.HeaderVersion == 1)
            {
                return GetOptionalHeaderLength() + 94;
            }

            return GetOptionalHeaderLength() + 94 + 4 + header.RemainingLength;
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

    internal class TrimInvalidHeader
    {
        private const int OffsetHeaderVersion = 2;

        private const int FirstBytesToSkip = 30;

        private const int LastBytesToSkip = 4;

        private const int SizeOfMarkerInformation = 588;

        private const int SizeOfGraphInformation = 4;

        private readonly byte[] File;

        private readonly CombinedHeaderInformation HeaderInformation;

        private readonly int HeaderVersion;

        public TrimInvalidHeader(byte[] file, CombinedHeaderInformation headerInformation)
        {
            File = file;
            HeaderInformation = headerInformation;
            if (headerInformation.HeaderVersion.HasValue)
            {
                HeaderVersion = headerInformation.HeaderVersion.Value;
            }
        }

        public void TrimHeader()
        {
            if (!AnyHeaderToTrim())
            {
                HeaderInformation.OffsetHeaderData = 2;
                return;
            }

            int num = 30;
            num += CalculateSizeOfNextBlock(num, 588);
            num += CalculateSizeOfNextBlock(num, 4);
            num += 4;
            HeaderInformation.OffsetHeaderData = num;
        }

        private int CalculateSizeOfNextBlock(int offset, int sizeOfSingleEntry)
        {
            short num = BitConverter.ToInt16(File, offset);
            int num2 = 2;
            return num2 + num * sizeOfSingleEntry;
        }

        private bool AnyHeaderToTrim()
        {
            int[] source = new int[2] { 1, 3 };
            return source.Contains(HeaderVersion);
        }
    }

     public class FileValidation
    {
        private readonly CombinedHeaderInformation CombinedHeaderInformation;

        private readonly byte[] File;

        public FileValidation(byte[] file, CombinedHeaderInformation combinedHeaderInformation)
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
            int[] source = new int[2] { 4, 5 };
            int[] source2 = new int[4] { 0, 1, 2, 3 };
            if (source.Contains(headerVersion))
            {
                throw new InvalidHeaderVersionException($"File was opened in Graph 10. The header with version '{headerVersion}' is not supported.");
            }

            if (source2.Contains(headerVersion))
            {
                return;
            }

            throw new InvalidHeaderVersionException($"Header Version '{headerVersion}' is not supported");
        }
    }


}