using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Models
{
    [Table(Name = "MapHighways")]
    public class MapHighway : MapWay
    {
        [Column(Name = nameof(HighwayType)), DataType("VARCHAR(32)")]
        public string HighwayType { get; set; }


        [Column(Name = nameof(Sidewalk)), DataType("VARCHAR(4)")]
        public string Sidewalk { get; set; }


        [Column(Name = nameof(CyclewayType)), DataType("VARCHAR(32)")]
        public string CyclewayType { get; set; }


        [Column(Name = nameof(BusWay)), DataType(LinqToDB.DataType.Boolean)]
        public bool BusWay { get; set; } = false;


        [Column(Name = nameof(Abutters)), DataType("VARCHAR(20)")]
        public string Abutters { get; set; }


        [Column(Name = nameof(BicycleRoad)), DataType(LinqToDB.DataType.Boolean)]
        public bool BicycleRoad { get; set; }


        [Column(Name = nameof(Incline)), DataType("VARCHAR(16)")]
        public string Incline { get; set; }


        [Column(Name = nameof(Junction)), DataType("VARCHAR(16)")]
        public string Junction { get; set; }


        [Column(Name = nameof(Lanes)), DataType(LinqToDB.DataType.Byte)]
        public byte Lanes { get; set; }


        [Column(Name = nameof(MotorRoad)), DataType("VARCHAR(3)")]
        public string MotorRoad { get; set; }


        [Column(Name = nameof(ParkingCondition)), DataType("VARCHAR(16)")]
        public string ParkingCondition { get; set; }


        [Column(Name = nameof(ParkingLane)), DataType("VARCHAR(16)")]
        public string ParkingLane { get; set; }


        [Column(Name = nameof(Service)), DataType("VARCHAR(16)")]
        public string Service { get; set; }


        [Column(Name = nameof(Surface)), DataType("VARCHAR(16)")]
        public string Surface { get; set; }


        [Column(Name = nameof(MaxWidth)), DataType("VARCHAR(8)")]
        public string MaxWidth { get; set; }


        [Column(Name = nameof(MaxHeight)), DataType("VARCHAR(8)")]
        public string MaxHeight { get; set; }


        [Column(Name = nameof(MaxSpeed)), DataType(LinqToDB.DataType.Single)]
        public float MaxSpeed { get; set; }


        [Column(Name = nameof(MaxWeight)), DataType("VARCHAR(8)")]
        public string MaxWeight { get; set; }


        [Column(Name = nameof(OneWay)), DataType("VARCHAR(16)")]
        public string OneWay { get; set; }


        [Column(Name = nameof(TurnLanes)), DataType("VARCHAR(200)")]
        public string TurnLanes { get; set; }


        [Column(Name = nameof(DestinationLanes)), DataType("VARCHAR(64)")]
        public string DestinationLanes { get; set; }


        [Column(Name = nameof(WidthLanes)), DataType("VARCHAR(64)")]
        public string WidthLanes { get; set; }


        [Column(Name = nameof(HovLanes)), DataType("VARCHAR(64)")]
        public string HovLanes { get; set; }


        [Column(Name = nameof(TurnLanesForward)), DataType("VARCHAR(200)")]
        public string TurnLanesForward { get; set; }


        [Column(Name = nameof(DestinationLanesForward)), DataType("VARCHAR(64)")]
        public string DestinationLanesForward { get; set; }


        [Column(Name = nameof(WidthLanesForward)), DataType("VARCHAR(64)")]
        public string WidthLanesForward { get; set; }


        [Column(Name = nameof(HovLanesForward)), DataType("VARCHAR(64)")]
        public string HovLanesForward { get; set; }


        [Column(Name = nameof(TurnLanesBackward)), DataType("VARCHAR(200)")]
        public string TurnLanesBackward { get; set; }


        [Column(Name = nameof(DestinationLanesBackward)), DataType("VARCHAR(64)")]
        public string DestinationLanesBackward { get; set; }


        [Column(Name = nameof(WidthLanesBackward)), DataType("VARCHAR(64)")]
        public string WidthLanesBackward { get; set; }


        [Column(Name = nameof(HovLanesBackward)), DataType("VARCHAR(64)")]
        public string HovLanesBackward { get; set; }
    }
}
