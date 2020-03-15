using System;
using System.Collections.Generic;
using System.Text;

namespace MapDataServer.Models
{
    public class TripSummary
    {
        public TripSummary() { }
        public TripSummary(Trip trip, PreprocessedTrip preprocessed)
        {
            Id = trip.Id;
            HovStatus = trip.HovStatus;
            VehicleType = trip.VehicleType;
            IsProcessed = preprocessed != null;
            StartTime = preprocessed?.ActualStartTime ?? trip.StartTime;
            EndTime = preprocessed?.ActualEndTime ?? trip.EndTime;
            DistanceMeters = preprocessed?.DistanceMeters;
        }

        public long Id { get; set; }
        public HovStatus HovStatus { get; set; }
        public string VehicleType { get; set; }
        public bool IsProcessed { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public uint? DistanceMeters { get; set; }
    }
}
