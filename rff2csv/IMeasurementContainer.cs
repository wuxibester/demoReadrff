using System;
using System.Collections.Generic;
using System.Linq;

namespace rff2csv
{
   public interface IMeasurementContainer
    {
        byte[] OriginalFileData { get; set; }

        string ContainerName { get; set; }

        decimal MaximumXValue { get; }

        HardwareInformation HardwareInformation { get; set; }

        List<MeasuredSerie> MeasuredSeries { get; set; }

        Guid Guid { get; }

        IHeaderInformationService HeaderInformation { get; }

        IHeaderInformationService ScrewdriverInformation { get; }

        List<ProcessDataSet> ProcessData { get; }

        void AddSerie(MeasuredSerie measuredSerie);

        MeasuredSerie FindSeriesOrDefault(SeriesType type);
    }
    public enum SeriesType
    {
        None,
        Workload,
        RotationalSpeed,
        Torque,
        Angle,
        ErrorCode,
        TemperatureMotor,
        StepNumber,
        CurrentMotor,
        TemperaturePowerUnit,
        TorqueAdditionalSensor1,
        AngleAdditionalSensor1,
        TorqueMotor,
        AngleMotor,
        TorqueAdditionalSensor2,
        AngleAdditionalSensor2,
        AnalogStop,
        DownForce,
        Position,
        FeedRate,
        DetectionOfPenetration,
        StatusJaws,
        FeedMotionCurrent,
        DownHolderPosition,
        PositionFeedRate,
        FeedRateTargetValue,
        DownForceTargetValue,
        EngagementFound,
        DownHolderTargetValue,
        MotorCurrentScrewdriver,
        Friction,
        MonitoredArea
    }
    public class ProcessDataSet
    {
        public string TextIdentifier { get; set; }

        public object Value { get; set; }

        public bool IsInternal => !TextIdentifier.Contains("@TEXT");

        public override string ToString()
        {
            List<ProcessDataSet> list = Value as List<ProcessDataSet>;
            string text = ((list != null) ? $"List, Count: {list.Count}" : Value?.ToString());
            return TextIdentifier + ": " + text;
        }
    }
    public interface IHeaderInformationService
    {
        IList<HeaderInformation> CompleteHeader { get; }

        byte[] ImageToDisplay { get; set; }

        void Add(string key, object value);

        bool DoesValueExist(string key);

        T GetValue<T>(string key);
    }
    public class HeaderInformation
    {
        public string Key { get; set; }

        public object Value { get; set; }

        public HeaderInformation()
        {
        }

        public HeaderInformation(string key, object value)
        {
            Key = key;
            Value = value;
        }
    }
     public class MeasuredSerie
    {
        private DeSamplingOfSeries DeSamplingOfSeries;

        public Guid MeasuredSerieIdentifier { get; set; }

        public SeriesType SeriesType { get; set; }

        public IUnit UnitXValue { get; }

        public IUnit UnitYValue { get; }

        public bool IsInternalCurve { get; set; }

        public List<Annotation> AnnotationsForUser { get; set; }

        public List<MeasuredPoint> MeasuredPoints { get; set; }

        public string AlternativeDisplayName { get; set; }

        public MeasuredSerie(IUnit unitX, IUnit unitY)
        {
            MeasuredSerieIdentifier = Guid.NewGuid();
            MeasuredPoints = new List<MeasuredPoint>();
            AnnotationsForUser = new List<Annotation>();
            UnitXValue = unitX;
            UnitYValue = unitY;
        }

        public IUnit DiscreteYValueFor(decimal continuousXValue)
        {
            if (DeSamplingOfSeries == null)
            {
                DeSamplingOfSeries = new DeSamplingOfSeries(this);
            }

            return DeSamplingOfSeries.DiscreteYValueFor(continuousXValue);
        }

        public void AddPoint(MeasuredPoint point)
        {
            MeasuredPoints.Add(point);
        }

        public void AddRange(IEnumerable<MeasuredPoint> points)
        {
            MeasuredPoints.AddRange(points);
        }

        public override string ToString()
        {
            return $"{SeriesType}: {UnitXValue} {UnitYValue}";
        }

        public MeasuredSerie Clone()
        {
            MeasuredSerie measuredSerie = new MeasuredSerie(UnitXValue.Clone(), UnitYValue.Clone());
            List<MeasuredPoint> list = new List<MeasuredPoint>(MeasuredPoints.Count);
            foreach (MeasuredPoint measuredPoint in MeasuredPoints)
            {
                list.Add(measuredPoint.Clone());
            }

            measuredSerie.MeasuredPoints = list;
            measuredSerie.SeriesType = SeriesType;
            return measuredSerie;
        }
    }
     public class MeasuredPoint
    {
        public IUnit XValue { get; set; }

        public IUnit YValue { get; set; }

        public MeasuredPoint(IUnit x, IUnit y)
        {
            XValue = x;
            YValue = y;
        }

        public MeasuredPoint Clone()
        {
            IUnit x = XValue?.Clone();
            IUnit y = YValue?.Clone();
            return new MeasuredPoint(x, y);
        }

        public override string ToString()
        {
            return $"({XValue}, {YValue})";
        }
    }
    public class Annotation
    {
        public decimal ForValue { get; set; }

        public string LanguageTextIdentifier { get; set; }

        public Annotation(decimal forValue, string languageTextIdentifier)
        {
            ForValue = forValue;
            LanguageTextIdentifier = languageTextIdentifier;
        }
    }

    public class DeSamplingOfSeries
    {
        private readonly MeasuredSerie Serie;

        private MeasuredPoint LastPoint;

        private int NextValueAtIndex;

        private decimal LastContinousXValue;

        public DeSamplingOfSeries(MeasuredSerie serie)
        {
            Serie = serie;
            Reset();
        }

        public IUnit DiscreteYValueFor(decimal continousXValue)
        {
            if (IsNoLongerASequentialMethodCall(continousXValue))
            {
                Reset();
            }

            for (LastContinousXValue = continousXValue; NextValueAtIndex < Serie.MeasuredPoints.Count; NextValueAtIndex++)
            {
                MeasuredPoint measuredPoint = Serie.MeasuredPoints[NextValueAtIndex];
                if (IsPointBetweenLastValueAndNextValue(continousXValue, measuredPoint))
                {
                    return LastPoint.YValue;
                }

                LastPoint = measuredPoint;
            }

            return LastPoint?.YValue;
        }

        private bool IsPointBetweenLastValueAndNextValue(decimal value, MeasuredPoint nextPoint)
        {
            if (LastPoint.XValue.ValueInBaseUnit <= value)
            {
                return nextPoint.XValue.ValueInBaseUnit > value;
            }

            return false;
        }

        private void Reset()
        {
            LastPoint = Serie.MeasuredPoints.FirstOrDefault();
            NextValueAtIndex = 0;
        }

        private bool IsNoLongerASequentialMethodCall(decimal continousXValue)
        {
            return LastContinousXValue > continousXValue;
        }
    }

    public class HardwareInformation
    {
        public decimal? TorqueMaximalInNewtonMetre { get; set; }

        public decimal? SamplingTimeInMillisecondsBetweenTwoDiscreteValues { get; set; }

        public string ScrewDriverClearName { get; set; }

        public override string ToString()
        {
            return $"{TorqueMaximalInNewtonMetre}";
        }

        
    }
}