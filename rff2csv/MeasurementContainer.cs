using System;
using System.Collections.Generic;
using System.Linq;

namespace rff2csv
{
     public class MeasurementContainer : IMeasurementContainer
    {
        private decimal? _MaximumXValue;

        public byte[] OriginalFileData { get; set; }

        public string ContainerName { get; set; }

        public decimal MaximumXValue
        {
            get
            {
                if (_MaximumXValue.HasValue)
                {
                    return _MaximumXValue.Value;
                }

                FindMaximumXValue();
                return _MaximumXValue.Value;
            }
        }

        public HardwareInformation HardwareInformation { get; set; }

        public List<MeasuredSerie> MeasuredSeries { get; set; } = new List<MeasuredSerie>();


        public Guid Guid { get; }

        public IHeaderInformationService HeaderInformation { get; }

        public IHeaderInformationService ScrewdriverInformation { get; }

        public List<ProcessDataSet> ProcessData { get; }

        public MeasurementContainer(string containerName, byte[] originalFileData)
        {
            HardwareInformation = new HardwareInformation();
            HeaderInformation = new HeaderInformationService();
            ScrewdriverInformation = new HeaderInformationService();
            ProcessData = new List<ProcessDataSet>();
            ContainerName = containerName;
            OriginalFileData = originalFileData;
            Guid = Guid.NewGuid();
        }

        public void AddSerie(MeasuredSerie measuredSerie)
        {
            MeasuredSeries.Add(measuredSerie);
        }

        private void FindMaximumXValue()
        {
            decimal num = default(decimal);
            foreach (MeasuredSerie item in MeasuredSeries)
            {
                MeasuredPoint measuredPoint = item.MeasuredPoints.LastOrDefault();
                if (measuredPoint != null && measuredPoint.XValue.ValueInBaseUnit > num)
                {
                    num = measuredPoint.XValue.ValueInBaseUnit;
                }
            }

            _MaximumXValue = num;
        }

        public MeasuredSerie FindSeriesOrDefault(SeriesType type)
        {
            return MeasuredSeries.FirstOrDefault((MeasuredSerie c) => c.SeriesType == type);
        }

        public override string ToString()
        {
            return ContainerName ?? "";
        }
    }

     public class HeaderInformationService : IHeaderInformationService
    {
        private readonly Dictionary<string, HeaderInformation> Info = new Dictionary<string, HeaderInformation>();

        public IList<HeaderInformation> CompleteHeader => Info.ValuesToList();

        public byte[] ImageToDisplay { get; set; }

        public void Add(string key, object value)
        {
            HeaderInformation value2 = new HeaderInformation(key, value);
            Info.Add(key, value2);
        }

        public bool DoesValueExist(string key)
        {
            return Info.ContainsKey(key);
        }

        public T GetValue<T>(string key)
        {
            return (T)Info[key].Value;
        }
    }
}