using System.Collections.Generic;

namespace rff2csv
{
     public class MapSeriesTypes
    {
        private static volatile Dictionary<GraphType, SeriesType> Mapping;

        private static readonly object SyncRoot = new object();

        public SeriesType GetMapping(GraphType type)
        {
            if (Mapping == null)
            {
                lock (SyncRoot)
                {
                    if (Mapping == null)
                    {
                        Mapping = Map();
                    }
                }
            }

            if (type == GraphType.UnknownContent)
            {
                return SeriesType.None;
            }

            return Mapping[type];
        }

        private static Dictionary<GraphType, SeriesType> Map()
        {
            Dictionary<GraphType, SeriesType> dictionary = new Dictionary<GraphType, SeriesType>();
            dictionary.Add(GraphType.Workload, SeriesType.Workload);
            dictionary.Add(GraphType.RotationalSpeed, SeriesType.RotationalSpeed);
            dictionary.Add(GraphType.Torque, SeriesType.Torque);
            dictionary.Add(GraphType.Angle, SeriesType.Angle);
            dictionary.Add(GraphType.ErrorCode, SeriesType.ErrorCode);
            dictionary.Add(GraphType.TemperatureMotor, SeriesType.TemperatureMotor);
            dictionary.Add(GraphType.ProgramPosition, SeriesType.StepNumber);
            dictionary.Add(GraphType.CurrentMotor, SeriesType.CurrentMotor);
            dictionary.Add(GraphType.TemperaturePowerUnit, SeriesType.TemperaturePowerUnit);
            dictionary.Add(GraphType.TorqueAdditionalSensor1, SeriesType.TorqueAdditionalSensor1);
            dictionary.Add(GraphType.AngleAdditionalSensor1, SeriesType.AngleAdditionalSensor1);
            dictionary.Add(GraphType.TorqueMotor, SeriesType.TorqueMotor);
            dictionary.Add(GraphType.AngleMotor, SeriesType.AngleMotor);
            dictionary.Add(GraphType.TorqueAdditionalSensor2, SeriesType.TorqueAdditionalSensor2);
            dictionary.Add(GraphType.AngleAdditionalSensor2, SeriesType.AngleAdditionalSensor2);
            dictionary.Add(GraphType.AnalogStop, SeriesType.AnalogStop);
            return dictionary;
        }
    }
}