using MapDataServer.Helpers;
using MapDataServer.Models;
using Priority_Queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using SQF = MapDataServer.Helpers.SqlableFunctions;

namespace MapDataServer.Services
{
    public class RouteFinder : IRouteFinder
    {
        private double MaxLonDist { get; } = 0.0027;
        private double MaxLatDist { get; } = 0.0023;

        private IDatabase Database { get; }

        public RouteFinder(IDatabase database)
        {
            Database = database;
        }

        private bool IsCloseToDestination(double testLon, double testLat, double destLon, double destLat) =>
            Math.Abs(testLon - destLon) <= MaxLonDist && Math.Abs(testLat - destLat) <= MaxLatDist;

        class ClosestNodesToPointEnumerator : LinqToDB.Async.IAsyncEnumerator<MapNode>
        {
            private RouteFinder RouteFinder { get; }
            private List<MapNode> Nodes { get; } = new List<MapNode>();
            private GeoPoint Point { get; }
            private int CurrentIndex { get; set; } = -1;
            private int CurrentLimit { get; set; } = 0;

            public ClosestNodesToPointEnumerator(RouteFinder routeFinder, GeoPoint point)
            {
                RouteFinder = routeFinder;
                Point = point;
            }

            public MapNode Current => Nodes[CurrentIndex];

            public void Dispose() { }

            public async Task<bool> MoveNext(CancellationToken cancellationToken)
            {
                if (++CurrentIndex >= Nodes.Count)
                {
                    CurrentLimit = CurrentLimit == 0 ? 8 : CurrentLimit * 2;
                    double lon = Point.Longitude;
                    double lat = Point.Latitude;
                    var query = RouteFinder.Database.MapNodes.OrderBy(
                        node => Math.Sqrt((node.Longitude - lon) * (node.Longitude - lon) + (node.Latitude - lat) * (node.Latitude - lat)))
                        .Take(CurrentLimit);
                    Nodes.Clear();
                    Nodes.AddRange(await query.ToAsyncEnumerable().ToArray());
                }

                return (CurrentIndex < Nodes.Count);
            }
        }

        class StepEnumerator : IAsyncEnumerator<(MapHighway, MapNode)>
        {
            IDatabase Database;
            MapNode CurrentNode;
            double DestLon;
            double DestLat;

            public List<MapNode> ExcludedNodes { get; } = new List<MapNode>();
            public List<MapHighway> ExcludedWays { get; } = new List<MapHighway>();

            int currentWCN = -1;
            List<MapHighway> WaysCrossingNode = null;
            int currentNOW = -1;
            List<MapNode> NodesOnWay = null;

            public StepEnumerator(IDatabase database, MapNode currentNode, double destLon, double destLat)
            {
                Database = database;
                CurrentNode = currentNode;
                DestLon = destLon;
                DestLat = destLat;
                ExcludedNodes.Add(currentNode);
            }

            public (MapHighway, MapNode) Current
            {
                get
                {
                    if (WaysCrossingNode == null)
                        return (null, null);

                    return (WaysCrossingNode[currentWCN], NodesOnWay[currentNOW]);
                }
            }

            public void Dispose()
            {
            }

            private async Task<List<MapHighway>> GetWaysCrossingNode(MapNode node, params MapHighway[] exclude)
            {
                var query = from way in Database.MapHighways
                            join link in Database.WayNodeLinks
                            on new { NodeId = node.Id, WayId = way.Id }
                            equals new { NodeId = link.NodeId, WayId = link.WayId }
                            select way;
                var result = await query.ToAsyncEnumerable().ToList();
                result.RemoveAll(way => exclude?.Contains(way) ?? false);
                return result;
            }

            private async Task<List<MapNode>> GetNodesOnWay(MapHighway way, double destLon, double destLat, params MapNode[] exclude)
            {
                var query = from node in Database.MapNodes
                            join link in Database.WayNodeLinks
                            on new { WayId = way.Id, NodeId = node.Id }
                            equals new { WayId = link.WayId, NodeId = link.NodeId }
                            orderby Math.Sqrt((node.Longitude - destLon) * (node.Longitude - destLon) + (node.Latitude - destLat)) ascending // Distance(node.Longitude, node.Latitude, destLon, destLat) ascending
                            select node;
                var result = await query.ToAsyncEnumerable().ToList();
                result.RemoveAll(node => exclude?.Contains(node) ?? false);
                return result;
            }

            public async Task<bool> MoveNext(CancellationToken cancellationToken)
            {
                bool found = false;
                while (!found)
                {
                    if (WaysCrossingNode == null)
                    {
                        WaysCrossingNode = await GetWaysCrossingNode(CurrentNode, ExcludedWays.ToArray());
                        ExcludedWays.AddRange(WaysCrossingNode);
                    }
                    if (NodesOnWay == null)
                    {
                        if (++currentWCN >= WaysCrossingNode.Count)
                            return false;
                        NodesOnWay = await GetNodesOnWay(WaysCrossingNode[currentWCN], DestLon, DestLat, ExcludedNodes.ToArray());
                    }
                    if (++currentNOW >= NodesOnWay.Count)
                    {
                        currentNOW = -1;
                        NodesOnWay = null;
                    }
                    else
                    {
                        found = (await (from link in Database.WayNodeLinks
                                        where link.NodeId == NodesOnWay[currentNOW].Id
                                        && link.WayId != WaysCrossingNode[currentWCN].Id
                                        select link).ToAsyncEnumerable().Count()) > 0;
                    }
                }
                return true;
            }
        }

        //public async Task<(MapHighway, MapNode)> FindNextStep(MapNode current, MapHighway way)
        //{
        //    var nodesOnWay = from node in Database.MapNodes
        //                     join link in Database.WayNodeLinks
        //                     on new { nodeId = node.Id, wayId = way.Id }
        //                     equals new { nodeId = link.NodeId, wayId = link.WayId }
        //                     orderby link.ItemIndex
        //                     select node;
        //    var nodes = await nodesOnWay.ToAsyncEnumerable().ToList();
        //    var index = nodes.IndexOf(current);

        //    int up = 0;
        //    int down = 0;
        //    MapNode upNode = null;
        //    MapNode downNode = null;
        //    MapHighway selectedWay = null;
        //    while (up != -1 && down != -1)
        //    {
        //        if (up != -1)
        //        {
        //            up++;
        //            if (index + up < nodes.Count)
        //            {
        //                upNode = nodes[index + up];
        //            }
        //            else
        //                up = -1;
        //        }
        //        if (down != -1)
        //        {
        //            down++;
        //            if (index - down >= 0)
        //            {
        //                downNode = nodes[index - down];
        //            }
        //            else
        //                down = -1;
        //        }
        //    }
        //    return;
        //}

        public async Task Test()
        {
            var nextPoint = new ClosestNodesToPointEnumerator(this, new GeoPoint(-122.2473373413, 47.3770446777));

            while (await nextPoint.MoveNext(CancellationToken.None))
            {
                var node = nextPoint.Current;
            }

            //var nextStep = new StepEnumerator(Database, await Database.MapNodes.Where(n => n.Id == 267814842).ToAsyncEnumerable().First(), -122.3690414429, 47.2863121033);

            //string list = "";
            //while (await nextStep.MoveNext())
            //{
            //    var currentNode = nextStep.Current.Item2;
            //    list += currentNode.Latitude.ToString() + "," + currentNode.Longitude.ToString() + "\n";
            //    var nextNextStep = new StepEnumerator(Database, currentNode, -122.3690414429, 47.2863121033);
            //    nextNextStep.ExcludedWays.Add(nextStep.Current.Item1);
            //    while (await nextNextStep.MoveNext())
            //    {
            //        var currentNextNode = nextNextStep.Current.Item2;
            //        list += currentNextNode.Latitude.ToString() + "," + currentNextNode.Longitude.ToString() + "\n";
            //        var nnnStep = new StepEnumerator(Database, currentNextNode, -122.3690414429, 47.2863121033);
            //        nnnStep.ExcludedWays.Add(nextNextStep.Current.Item1);
            //        nnnStep.ExcludedWays.Add(nextStep.Current.Item1);
            //        while (await nnnStep.MoveNext())
            //        {
            //            var currentNNNode = nnnStep.Current.Item2;
            //            list += currentNNNode.Latitude.ToString() + "," + currentNNNode.Longitude.ToString() + "\n";
            //        }
            //    }
            //}
        }


        //public IAsyncEnumerable<(MapHighway way, MapNode next)> FindNextSteps(double destLon, double destLat, IEnumerable<MapNode> previousNodes)

        public async Task<IEnumerable<WaySegment>> FindRoute(IEnumerable<(double lon, double lat)> approximatePath)
        {
            List<IEnumerator<MapNode>> points = new List<IEnumerator<MapNode>>();

            while (true)
            {

            }

            throw new NotImplementedException();
        }
    }
}
