using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MapDataServer.Models
{
    public class ActivitySummary
    {
        public ActivitySummary()
        {
            Distances = new uint[HovTypesCount];
            Times = new TimeSpan[HovTypesCount];
            Counts = new int[HovTypesCount];
        }

        public static int HovTypesCount => Enum.GetValues(typeof(HovStatus)).Length;
        public uint[] Distances { get; set; }
        public TimeSpan[] Times { get; set; }
        public int[] Counts { get; set; }
        public int UnprocessedCount { get; set; }
        public int InsignificantCount { get; set; }

        public void AddTrip(Trip trip, PreprocessedTrip preprocessed)
        {
            if (preprocessed == null)
            {
                UnprocessedCount++;
                return;
            }
            if (preprocessed.DistanceMeters == 0)
            {
                InsignificantCount++;
                return;
            }
            var hovStatus = trip.HovStatus;
            Distances[(int)hovStatus] += preprocessed.DistanceMeters;
            Times[(int)hovStatus] += (preprocessed.ActualEndTime - preprocessed.ActualStartTime);
            Counts[(int)hovStatus]++;
        }

        public void AddSummary(ActivitySummary other)
        {
            UnprocessedCount += other.UnprocessedCount;
            InsignificantCount += other.InsignificantCount;
            for (int i = 0; i < HovTypesCount; i++)
            {
                Distances[i] += other.Distances[i];
                Times[i] += other.Times[i];
                Counts[i] += other.Counts[i];
            }
        }

        public override string ToString()
        {
            var totalDistance = Distances.Sum(val => val);
            var totalTime = TimeSpan.FromMilliseconds(Times.Sum(val => val.TotalMilliseconds));
            var totalCount = Counts.Sum();

            var resultBuilder = new StringBuilder();

            resultBuilder.AppendLine($"Total distance: {totalDistance} meters");
            resultBuilder.AppendLine($"Total time: {totalTime}");
            resultBuilder.AppendLine($"Total trips: {totalCount}");
            resultBuilder.AppendLine();

            for (int i = 0; i < HovTypesCount; i++)
            {

                var distance = Distances[i];
                var distFraction = (double)distance / (double)totalDistance;
                var time = Times[i];
                var timeFraction = time.TotalSeconds / totalTime.TotalSeconds;
                var count = Counts[i];
                var countFraction = (double)count / (double)totalCount;
                resultBuilder.AppendLine($"For {(HovStatus)i}:");
                resultBuilder.AppendLine($"Distance: {distance} meters ({distFraction.ToString("P")})");
                resultBuilder.AppendLine($"Time: {time} ({timeFraction.ToString("P")})");
                resultBuilder.AppendLine($"Trips: {count} ({countFraction.ToString("P")})");
                resultBuilder.AppendLine();
            }

            resultBuilder.AppendLine($"{UnprocessedCount} unprocessed trips and {InsignificantCount} zero-length trips not included in summary.");

            return resultBuilder.ToString();
        }
    }
}
