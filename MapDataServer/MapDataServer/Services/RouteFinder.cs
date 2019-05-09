using MapDataServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

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

        private static double Distance(double lon1, double lat1, double lon2, double lat2) =>
        Math.Sqrt((lon1 - lon2) * (lon1 - lon2) + (lat1 - lat2) * (lat1 - lat2));

        public IAsyncEnumerable<MapNode> FindClosestNodesToPoint(double lon, double lat)
        {
            return Database.MapNodes.OrderBy(
                node => Distance(node.Longitude, lon, node.Latitude, lat)).ToAsyncEnumerable();
            
        }

        class StepEnumerator : IAsyncEnumerator<(MapWay, MapNode)>
        {
            IDatabase Database;
            MapNode CurrentNode;
            double DestLon;
            double DestLat;

            IAsyncEnumerator<MapWay> WaysCrossingNode = null;
            IAsyncEnumerator<MapNode> NodesOnWay = null;

            public StepEnumerator(IDatabase database, MapNode currentNode, double destLon, double destLat)
            {
                Database = database;
                CurrentNode = currentNode;
                DestLon = destLon;
                DestLat = destLat;
            }

            public (MapWay, MapNode) Current
            {
                get
                {
                    if (WaysCrossingNode == null)
                        return (null, null);

                    return (WaysCrossingNode.Current, NodesOnWay.Current);
                }
            }

            public void Dispose()
            {
                WaysCrossingNode?.Dispose();
                NodesOnWay?.Dispose();
            }

            private IAsyncEnumerable<MapWay> GetWaysCrossingNode(MapNode node, MapWay exclude = null)
            {
                var query = from way in Database.MapWays
                            join link in Database.WayNodeLinks
                            on new { NodeId = node.Id, WayId = way.Id }
                            equals new { NodeId = link.NodeId, WayId = link.WayId }
                            select way;
                return (query).ToAsyncEnumerable();
            }

            private IAsyncEnumerable<MapNode> GetNodesOnWay(MapWay way, double destLon, double destLat, MapNode exclude = null)
            {
                return (from node in Database.MapNodes
                        join link in Database.WayNodeLinks
                        on new { WayId = way.Id, NodeId = node.Id }
                        equals new { WayId = link.WayId, NodeId = link.NodeId }
                        orderby Math.Sqrt((node.Longitude - destLon) * (node.Longitude - destLon) + (node.Latitude - destLat)) ascending // Distance(node.Longitude, node.Latitude, destLon, destLat) ascending
                        select node).ToAsyncEnumerable();
            }

            public async Task<bool> MoveNext(CancellationToken cancellationToken)
            {
                bool found = false;
                while (!found)
                {
                    if (WaysCrossingNode == null)
                    {
                        WaysCrossingNode = GetWaysCrossingNode(CurrentNode).GetEnumerator();
                    }
                    if (NodesOnWay == null)
                    {
                        if (!await WaysCrossingNode.MoveNext())
                            return false;
                        NodesOnWay = GetNodesOnWay(WaysCrossingNode.Current, DestLon, DestLat, CurrentNode).GetEnumerator();
                    }
                    if (!await NodesOnWay.MoveNext())
                    {
                        NodesOnWay.Dispose();
                        NodesOnWay = null;
                    }
                    else
                    {
                        found = (await (from link in Database.WayNodeLinks
                                        where link.NodeId == NodesOnWay.Current.Id
                                        && link.WayId != WaysCrossingNode.Current.Id
                                        select link).ToAsyncEnumerable().Count()) > 0;
                    }
                }
                return true;
            }
        }

        //public async Task<(MapWay, MapNode)> FindNextStep(MapNode current, MapWay way)
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
        //    MapWay selectedWay = null;
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
            var nextStep = new StepEnumerator(Database, await Database.MapNodes.Where(n => n.Id == 267814842).ToAsyncEnumerable().First(), -122.3690414429, 47.2863121033);

            while (await nextStep.MoveNext())
            {
                var cur = nextStep.Current;
            }
        }
        

        //public IAsyncEnumerable<(MapWay way, MapNode next)> FindNextSteps(double destLon, double destLat, IEnumerable<MapNode> previousNodes)

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
