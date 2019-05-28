using MapDataServer.Helpers;
using MapDataServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MapDataServer.Services
{
    public class RouteFinder : IRouteFinder
    {
        private double MaxLonDist { get; } = 0.0027;
        private double MaxLatDist { get; } = 0.0023;

        private IDatabase Database { get; }

        private IQueryable<MapNode> MapNodes { get; set; }
        private IQueryable<MapWay> MapWays { get; set; }
        private IQueryable<MapHighway> MapHighways { get; set; }
        private IQueryable<WayNodeLink> WayNodeLinks { get; set; }

        public RouteFinder(IDatabase database)
        {
            Database = database;
            MapNodes = Database.MapNodes;
            MapWays = database.MapWays;
            MapHighways = Database.MapHighways;
            WayNodeLinks = database.WayNodeLinks;
        }

        private async Task RestrictRegion(GeoPoint swCorner, GeoPoint neCorner)
        {
            var nodes = await Database.MapNodes.WithinBoundingBox(swCorner, neCorner).ToAsyncEnumerable().ToDictionary(node => node.Id);
            var links = await Database.WayNodeLinks.Where(link => nodes.ContainsKey(link.NodeId)).ToAsyncEnumerable().ToList();
            var wayIds = links.Where(link => link.Highway == false).Select(link => link.WayId).Distinct().ToArray();
            var highwayIds = links.Where(link => link.Highway == true).Select(link => link.WayId).Distinct().ToArray();
            var ways = await (from way in Database.MapWays where wayIds.Contains(way.Id) select way).ToAsyncEnumerable().ToList();
            var highways = await (from highway in Database.MapHighways where highwayIds.Contains(highway.Id) select highway).ToAsyncEnumerable().ToList();

            MapNodes = nodes.Values.AsQueryable();
            MapWays = ways.AsQueryable();
            MapHighways = highways.AsQueryable();
            WayNodeLinks = links.AsQueryable();
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
                    var query = RouteFinder.MapNodes.OrderBy(
                        node => Math.Sqrt((node.Longitude - lon) * (node.Longitude - lon) + (node.Latitude - lat) * (node.Latitude - lat)))
                        .Take(CurrentLimit);
                    Nodes.Clear();
                    Nodes.AddRange(await query.ToAsyncEnumerable().ToArray());
                }

                return (CurrentIndex < Nodes.Count);
            }
        }

        class StepEnumerator : IAsyncEnumerator<(MapHighway, MapNode, double)>
        {
            RouteFinder RouteFinder { get; }
            MapNode CurrentNode { get; }
            GeoPoint Destination { get; }

            public List<MapNode> ExcludedNodes { get; } = new List<MapNode>();
            public List<MapHighway> ExcludedWays { get; } = new List<MapHighway>();

            int currentWCN = -1;
            List<MapHighway> WaysCrossingNode = null;
            int currentNOW = -1;
            List<(MapNode, double)> NodesOnWay = null;

            public StepEnumerator(RouteFinder routeFinder, MapNode currentNode, GeoPoint destination)
            {
                RouteFinder = routeFinder;
                CurrentNode = currentNode;
                Destination = destination;
                ExcludedNodes.Add(currentNode);
            }

            public (MapHighway, MapNode, double) Current
            {
                get
                {
                    if (WaysCrossingNode == null)
                        return (null, null, 0);

                    return (WaysCrossingNode[currentWCN], NodesOnWay[currentNOW].Item1, NodesOnWay[currentNOW].Item2);
                }
            }

            public void Dispose()
            {
            }

            private async Task<List<MapHighway>> GetWaysCrossingNode(MapNode node, params MapHighway[] exclude)
            {
                var query = from way in RouteFinder.MapHighways
                            join link in RouteFinder.WayNodeLinks
                            on new { NodeId = node.Id, WayId = way.Id }
                            equals new { NodeId = link.NodeId, WayId = link.WayId }
                            select way;
                var result = await query.ToAsyncEnumerable().ToList();
                result.RemoveAll(way => exclude?.Contains(way) ?? false);
                return result;
            }

            private async Task<List<(MapNode, double)>> GetNodesOnWay(MapHighway way, GeoPoint destination, params MapNode[] exclude)
            {
                var query = from node in RouteFinder.MapNodes
                            join link in RouteFinder.WayNodeLinks
                            on new { WayId = way.Id, NodeId = node.Id }
                            equals new { WayId = link.WayId, NodeId = link.NodeId }
                            orderby link.ItemIndex
                            select node;
                var result = await query.ToAsyncEnumerable().Select(node => (node, 0.0)).ToList();
                var curIndex = result.FindIndex(n => n.Item1.Equals(CurrentNode));
                for (int i = 1; i < result.Count - curIndex; i++)
                {
                    result[curIndex + i] = (result[curIndex + i].Item1, result[curIndex + i - 1].Item2 + result[curIndex + i - 1].Item1.GetPoint().DistanceTo(result[curIndex + i].Item1.GetPoint()));
                }
                for (int i = 1; i < curIndex + 1; i++)
                {
                    result[curIndex - i] = (result[curIndex - i].Item1, result[curIndex - i + 1].Item2 + result[curIndex - i + 1].Item1.GetPoint().DistanceTo(result[curIndex - i].Item1.GetPoint()));
                }
                if (!way.OneWay.IsFalseNoBlankOrNull())
                {
                    if (way.OneWay == "-1")
                    {
                        result.RemoveRange(curIndex - 1, result.Count - curIndex);
                    }
                    else
                    {
                        result.RemoveRange(0, curIndex);
                    }
                }
                result.RemoveAll(node => exclude?.Contains(node.Item1) ?? false);
                Func<(MapNode, double), (MapNode, double), int> compare = (nodeX, nodeY) =>
                {
                    var dX = nodeX.Item1.GetPoint().DistanceTo(destination);
                    var dY = nodeX.Item1.GetPoint().DistanceTo(destination);
                    if (dX < dY)
                        return -1;
                    else if (dX == dY)
                        return 0;
                    return 1;
                };
                result.Sort(Comparer<(MapNode, double)>.Create(new Comparison<(MapNode, double)>(compare)));
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
                        NodesOnWay = await GetNodesOnWay(WaysCrossingNode[currentWCN], Destination, ExcludedNodes.ToArray());
                    }
                    if (++currentNOW >= NodesOnWay.Count)
                    {
                        currentNOW = -1;
                        NodesOnWay = null;
                    }
                    else
                    {
                        found = (await (from link in RouteFinder.WayNodeLinks
                                        where link.NodeId == NodesOnWay[currentNOW].Item1.Id
                                        && link.WayId != WaysCrossingNode[currentWCN].Id
                                        select link).ToAsyncEnumerable().Count()) > 0;
                    }
                }
                return true;
            }
        }

        class AStarNode
        {
            public AStarNode(MapNode currentNode, MapNode destinationNode)
            {
                CurrentNode = currentNode;
                DestinationNode = destinationNode;
            }

            public AStarNode(AStarNode parent, double lastTravelDistance, MapNode currentNode, MapNode destinationNode)
                : this(currentNode, destinationNode)
            {
                Parent = parent;
                LastTravelDistance = lastTravelDistance;
            }

            public IEnumerable<MapNode> Traverse()
            {
                List<MapNode> result = new List<MapNode>();
                var cur = this;
                while (cur != null)
                {
                    result.Add(cur.CurrentNode);
                    cur = cur.Parent;
                }
                result.Reverse();
                return result;
            }

            public string GetNodePositions()
            {
                StringBuilder result = new StringBuilder();
                foreach (var node in Traverse())
                {
                    result.AppendLine($"{node.Latitude}, {node.Longitude}");
                }

                return result.ToString();
            }

            public AStarNode Parent { get; }
            public MapNode CurrentNode { get; }
            public double LastTravelDistance { get; }
            public MapNode DestinationNode { get; }

            public double F => G + H;
            public double G => Parent == null ? 0 : Parent.G + LastTravelDistance;
            public double H => CurrentNode.GetPoint().DistanceTo(DestinationNode.GetPoint());

            public class AStarFComparer : IComparer<AStarNode>
            {
                public int Compare(AStarNode x, AStarNode y)
                {
                    if (x == null || y == null)
                    {
                        if (x == null)
                        {
                            return (y == null) ? 0 : 1;
                        }
                        if (y == null)
                        {
                            return (x == null) ? 0 : -1;
                        }
                    }
                    return x.F.CompareTo(y.F);
                }
            }
        }

        class AStarEnumerator : IAsyncEnumerator<AStarNode>
        {
            public AStarEnumerator(RouteFinder routeFinder, MapNode start, MapNode end)
            {
                RouteFinder = routeFinder;
                Start = start;
                End = end;
            }

            private RouteFinder RouteFinder { get; }

            private MapNode Start { get; }
            private MapNode End { get; }

            bool started = false;

            SortedList<double, AStarNode[]> Open { get;} = new SortedList<double, AStarNode[]>();
            List<AStarNode> Closed { get; } = new List<AStarNode>();

            private void AddToOpen(AStarNode node)
            {
                if (!Open.TryAdd(node.F, new AStarNode[] { node }))
                {
                    var cur = Open[node.F];
                    var next = new AStarNode[cur.Length + 1];
                    for (int i = 0; i < cur.Length; i++)
                    {
                        next[i] = cur[i];
                    }
                    next[cur.Length] = node;
                    Open[node.F] = next;
                }
            }

            private AStarNode PopFromOpen()
            {
                var result = Open.First().Value.First();
                if (Open.First().Value.Length == 1)
                    Open.RemoveAt(0);
                else
                {
                    var cur = Open.First().Value;
                    var next = new AStarNode[cur.Length - 1];
                    for (int i = 1; i < cur.Length; i++)
                    {
                        next[i - 1] = cur[i];
                    }
                    Open[Open.First().Key] = next;
                }
                return result;
            }

            bool OpenContainsEqualOrBetterPath(AStarNode node)
            {
                var index = Open.Keys.BinarySearch(node.F);
                while (Open.Count > index && Open.Keys[index] <= node.F)
                    index++;
                for (int i = 0; i < index; i++)
                {
                    if (Open.Values[i].Any(oNode => oNode.CurrentNode.GetPoint() == node.CurrentNode.GetPoint()))
                        return true;
                }
                return false;
            }

            public AStarNode Current => throw new NotImplementedException();

            public void Dispose()
            {
            }

            DateTime startTime;
            public async Task<bool> MoveNext(CancellationToken cancellationToken)
            {
                if (!started)
                {
                    AddToOpen(new AStarNode(Start, End));
                    startTime = DateTime.Now;
                    started = true;
                    return true;
                }
                else
                {
                    var currentPath = Open.First().Value.First().GetNodePositions();
                    if (Open.Count == 0)
                        return false;
                    var current = PopFromOpen();
                    if (current.CurrentNode.GetPoint() == End.GetPoint())
                    {
                        var list = current.GetNodePositions();
                        var elapsedTime = startTime - DateTime.Now;
                        return false;
                    }
                    var successors = new List<AStarNode>();
                    var nextEnumerator = new StepEnumerator(RouteFinder, current.CurrentNode, End.GetPoint());
                    while (await nextEnumerator.MoveNext())
                    {
                        var successor = new AStarNode(current, nextEnumerator.Current.Item3, nextEnumerator.Current.Item2, End);
                        var closedSame = Closed.Where(oNode => oNode.CurrentNode.GetPoint() == successor.CurrentNode.GetPoint());
                        if (!closedSame.Any() && !OpenContainsEqualOrBetterPath(successor))
                        {
                            AddToOpen(successor);
                        }
                    }
                    Closed.Add(current);
                }
                return true;
            }
        }

        public async Task<(MapHighway, MapNode)> FindNextStep(MapNode current, MapHighway way)
        {
            var nodesOnWay = from node in Database.MapNodes
                             join link in Database.WayNodeLinks
                             on new { nodeId = node.Id, wayId = way.Id }
                             equals new { nodeId = link.NodeId, wayId = link.WayId }
                             orderby link.ItemIndex
                             select node;
            var nodes = await nodesOnWay.ToAsyncEnumerable().ToList();
            var index = nodes.IndexOf(current);

            int up = 0;
            int down = 0;
            MapNode upNode = null;
            MapNode downNode = null;
            MapHighway selectedWay = null;
            while (up != -1 && down != -1)
            {
                if (up != -1)
                {
                    up++;
                    if (index + up < nodes.Count)
                    {
                        upNode = nodes[index + up];
                    }
                    else
                        up = -1;
                }
                if (down != -1)
                {
                    down++;
                    if (index - down >= 0)
                    {
                        downNode = nodes[index - down];
                    }
                    else
                        down = -1;
                }
            }
            throw new NotImplementedException();
        }

        public async Task Test()
        {
            await RestrictRegion(new GeoPoint(-122.30, 47.56), new GeoPoint(-122.14, 47.64));

            var start = await MapNodes.ClosestToPoint(new GeoPoint(-122.1430948723, 47.631465695), 1).ToAsyncEnumerable().ToList();
            var end = await MapNodes.ClosestToPoint(new GeoPoint(-122.2962537325, 47.5651499114), 1).ToAsyncEnumerable().ToList();

            var astar = new AStarEnumerator(this, start[0], end[0]);
            while (await astar.MoveNext()) { }

            //var nextPoint = new ClosestNodesToPointEnumerator(this, new GeoPoint(-122.2473373413, 47.3770446777));

            //while (await nextPoint.MoveNext(CancellationToken.None))
            //{
            //    var node = nextPoint.Current;
            //}

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
