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
        private double MaxDist { get; } = 0.0025;

        private IDatabase Database { get; }

        private MemoryDatabase MemoryDatabase { get; }

        public RouteFinder(IDatabase database)
        {
            Database = database;
            MemoryDatabase = new MemoryDatabase(Database);
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
                var query = from way in RouteFinder.MemoryDatabase.MapHighways
                            join link in RouteFinder.MemoryDatabase.WayNodeLinks
                            on new { NodeId = node.Id, WayId = way.Id }
                            equals new { NodeId = link.NodeId, WayId = link.WayId }
                            select way;
                var result = await query.ToAsyncEnumerable().ToList();
                result.RemoveAll(way => exclude?.Contains(way) ?? false);
                return result;
            }

            private async Task<List<(MapNode, double)>> GetNodesOnWay(MapHighway way, GeoPoint destination, params MapNode[] exclude)
            {
                var query = from node in RouteFinder.MemoryDatabase.MapNodes
                            join link in RouteFinder.MemoryDatabase.WayNodeLinks
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
                        found = NodesOnWay[currentNOW].Item1.GetPoint() == Destination ||
                            (await (from link in RouteFinder.MemoryDatabase.WayNodeLinks
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

            public AStarNode GetFirstParent()
            {
                var parent = this;
                while (parent.Parent != null)
                    parent = parent.Parent;
                return parent;
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
            bool disposed = false;

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

            public AStarNode Current => Open.Values.FirstOrDefault()?[0];

            public bool Complete { get; private set; } = false;

            public void Dispose()
            {
                disposed = true;
            }

            DateTime startTime;
            public async Task<bool> MoveNext(CancellationToken cancellationToken)
            {
                if (disposed)
                    return true;
                if (!started)
                {
                    AddToOpen(new AStarNode(Start, End));
                    startTime = DateTime.Now;
                    started = true;
                    return true;
                }
                else
                {
                    if (Open.Count == 0)
                        return false;
                    var currentPath = Open.First().Value.First().GetNodePositions();
                    if (Open.Count == 0)
                        return false;
                    var current = PopFromOpen();
                    if (current.CurrentNode.GetPoint() == End.GetPoint())
                    {
                        var list = current.GetNodePositions();
                        var elapsedTime = startTime - DateTime.Now;
                        Complete = true;
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
            var nodesOnWay = from node in MemoryDatabase.MapNodes
                             join link in MemoryDatabase.WayNodeLinks
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

        public class PathMatcher
        {
            private RouteFinder RouteFinder { get; }
            private int NodesPerPoint { get; }
            private int PointCount { get; }

            public static List<List<int>> GetIntegerSums(int count, int sum, int max)
            {
                if (count < 1)
                    return new List<List<int>>();
                var limit = Math.Min(max, sum);
                if (count == 1)
                {
                    if (sum > limit)
                        return new List<List<int>>();
                    return new List<List<int>>() { new List<int>() { sum } };
                }
                var result = new List<List<int>>();
                for (int i = 0; i <= limit; i++)
                {
                    foreach (var sub in GetIntegerSums(count - 1, sum - i, max))
                    {
                        sub.Insert(0, i);
                        result.Add(sub);
                    }
                }
                return result;
            }

            public PathMatcher(RouteFinder routeFinder, int nodesPerPoint, int pointCount)
            {
                RouteFinder = routeFinder;
                NodesPerPoint = nodesPerPoint;
                PointCount = pointCount;
            }

            private List<MapNode[]> ClosestNodes { get; } = new List<MapNode[]>();
            private List<GeoPoint> CurrentPoints { get; } = new List<GeoPoint>();

            private Dictionary<(int, int, int), AStarEnumerator> TripPairFinders { get; } = new Dictionary<(int, int, int), AStarEnumerator>();

            public List<int[]> CurrentPaths { get; } = new List<int[]>();

            private async Task SetMemoryRegion()
            {
                double minLat = 0;
                double minLon = 0;
                double maxLat = 0;
                double maxLon = 0;

                bool first = true;
                foreach (var point in CurrentPoints)
                {
                    if (first)
                    {
                        minLat = maxLat = point.Latitude;
                        minLon = maxLon = point.Longitude;
                        first = false;
                    }
                    else
                    {
                        if (point.Latitude < minLat)
                            minLat = point.Latitude;
                        if (point.Latitude > maxLat)
                            maxLat = point.Latitude;
                        if (point.Longitude < minLon)
                            minLat = point.Latitude;
                        if (point.Longitude > maxLon)
                            maxLat = point.Latitude;
                    }
                }

                minLat -= 2 * RouteFinder.MaxDist;
                maxLat += 2 * RouteFinder.MaxDist;
                minLon -= 2 * RouteFinder.MaxDist;
                maxLon += 2 * RouteFinder.MaxDist;
                await RouteFinder.MemoryDatabase.SetFromRegion(RouteFinder.Database, new GeoPoint(minLon, minLat), new GeoPoint(maxLon, maxLat));
            }

            private async Task<MapNode[]> GetClosestNodes(GeoPoint point) =>
                (await RouteFinder.MemoryDatabase.GetClosestNodes(point, NodesPerPoint))
                //.Where(node => node.GetPoint().DistanceTo(point) < RouteFinder.MaxDist)
                .ToArray();

            async Task<bool?> AttemptAndCheckPath(int[] path)
            {
                for (int i = 0; i < path.Length - 1; i++)
                {
                    var key = (i, path[i], path[i + 1]);
                    var finder = TripPairFinders[key];
                    if (finder == null)
                        return null;
                    var continues = await finder.MoveNext();
                    if (continues)
                        return false;
                    if (!finder.Complete)
                    {
                        TripPairFinders[key] = null;
                        return null;
                    }
                }
                return true;
            }

            async Task<int[]> FirstCompletedPathOrDefault()
            {
                for (int i = 0; i < CurrentPaths.Count; i++)
                {
                    var status = await AttemptAndCheckPath(CurrentPaths[i]);
                    switch (status)
                    {
                        case true:
                            return CurrentPaths[i];
                        case null:
                            CurrentPaths.RemoveAt(i);
                            i--;
                            break;
                    }
                }
                return null;
            }

            async void AddFindersForPath(int[] path)
            {
                CurrentPaths.Add(path);
                for (int i = 0; i < path.Length - 1; i++)
                {
                    var key = (i, path[i], path[i + 1]);
                    if (!TripPairFinders.ContainsKey(key))
                    {
                        var startPoint = ClosestNodes[i][path[i]];
                        var endPoint = ClosestNodes[i + 1][path[i + 1]];
                        TripPairFinders.Add(key, new AStarEnumerator(RouteFinder, startPoint, endPoint));
                    }
                }
            }

            public async Task<bool> PushPoint(GeoPoint point)
            {
                CurrentPoints.Add(point);
                GeoPoint? oldPoint = null;
                if (CurrentPoints.Count > PointCount)
                {
                    oldPoint = CurrentPoints[0];
                    CurrentPoints.RemoveAt(0);
                }
                await SetMemoryRegion();
                var closest = await GetClosestNodes(point);
                foreach (var arr in ClosestNodes)
                {
                    foreach (var node in arr)
                    {
                        if (closest.Contains(node))
                        {
                            // Points seem to be clustering, and this point is likely to not contribue any valuable information & should be dropped
                            CurrentPoints.Remove(point);
                            if (oldPoint != null)
                                CurrentPoints.Insert(0, oldPoint.Value);
                            return false;
                        }
                    }
                }
                ClosestNodes.Add(closest);
                if (oldPoint != null)
                    ClosestNodes.RemoveAt(0);

                return true;
            }

            public async Task CalculatePath()
            {
                int attempt = 0;
                List<AStarNode> results = new List<AStarNode>();
                int[] pathResult;

                CurrentPaths.Clear();
                TripPairFinders.Clear();

                var attemptsPerLevel = 20;
                var pairLevels = (NodesPerPoint - 1) * PointCount + 1;

                do
                {
                    if (attempt % attemptsPerLevel == 0)
                    {
                        var nextPaths = GetIntegerSums(PointCount, attempt / attemptsPerLevel, NodesPerPoint - 1);
                        foreach (var path in nextPaths)
                        {
                            AddFindersForPath(path.ToArray());
                        }
                    }
                    attempt++;
                }
                while ((pathResult = await FirstCompletedPathOrDefault()) == null &&
                    attempt < pairLevels * attemptsPerLevel * 2 &&
                    CurrentPaths.Count > 0);

                for (int i = 0; i < pathResult.Length - 1; i++)
                {
                    var key = (i, pathResult[i], pathResult[i + 1]);
                    var finder = TripPairFinders[key];
                    Console.WriteLine(finder.Current.GetNodePositions());
                }
            }
        }

        public async Task Test()
        {

            var trip1 = await Database.GetFullTrip(8042057989057450465);
            //var trip2 = await Database.GetFullTrip(4819942918563353813);

            //foreach (var step in trip1)
            //{
            //    var distance = step.CurrentPoint.GetPoint().DistanceTo(step.PreviousPoint?.GetPoint());
            //    var timeBetween = step.CurrentPoint.Time - step.PreviousPoint?.Time;
            //    step.Drop();
            //}

            var matcher = new PathMatcher(this, 4, 4);

            StringBuilder coords = new StringBuilder();
            int numPoints = 0;
            int pt = 0;
            foreach (var point in trip1)
            {
                if (pt % 2 == 0)
                {
                    if (await matcher.PushPoint(point.CurrentPoint.GetPoint()))
                        numPoints++;
                    if (numPoints == 4)
                        break;
                }
                pt++;
            }

            await matcher.CalculatePath();

            //var path = await FindRoute(trip1);

            //foreach (var node in path)
            //{
            //    coords.AppendLine($"{node.Latitude}, {node.Longitude}");
            //}

            return;
            await MemoryDatabase.SetFromRegion(Database, new GeoPoint(-122.30, 47.56), new GeoPoint(-122.14, 47.64));

            var start = await MemoryDatabase.MapNodes.ClosestToPoint(new GeoPoint(-122.1430948723, 47.631465695), 1).ToAsyncEnumerable().ToList();
            var end = await MemoryDatabase.MapNodes.ClosestToPoint(new GeoPoint(-122.2962537325, 47.5651499114), 1).ToAsyncEnumerable().ToList();

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

        public async Task<List<MapNode>> FindRoute(FullTrip trip)
        {
            List<MapNode> pathResult = new List<MapNode>();

            List<IEnumerator<MapNode>> points = new List<IEnumerator<MapNode>>();

            MapNode[] closestNodesPrevious = null;
            MapNode[] closestNodesCurrent = null;

            bool AreNodesClose(MapNode start, MapNode end) => start.GetPoint().DistanceTo(end.GetPoint()) <= MaxDist;

            async Task<AStarNode> FirstCompletedPathOrDefault(List<AStarEnumerator> pathFinders)
            {
                for (int i = 0; i < pathFinders.Count; i++)
                {
                    if (!(await pathFinders[i].MoveNext()))
                    {
                        if (pathFinders[i].Complete)
                            return pathFinders[i].Current;
                        else
                        {
                            pathFinders.RemoveAt(i);
                            i--;
                        }
                    }
                }
                return null;
            }

            async Task<AStarNode> FindPathMultiple(MapNode[] startPoints, MapNode[] endPoints)
            {
                int attempt = 0;
                var pathFinders = new List<AStarEnumerator>();
                pathFinders.Add(new AStarEnumerator(this, startPoints[0], endPoints[0]));
                AStarNode result;

                while ((result = await FirstCompletedPathOrDefault(pathFinders)) == null && attempt < 150 && pathFinders.Count > 0)
                {
                    attempt++;
                    if (attempt == 5)
                    {
                        if (AreNodesClose(startPoints[1], endPoints[0]))
                        {
                            pathFinders.Add(new AStarEnumerator(this, startPoints[1], endPoints[0]));
                            pathFinders.Add(new AStarEnumerator(this, startPoints[0], endPoints[1]));
                        }
                    }
                    if (attempt == 10)
                    {
                        if (AreNodesClose(startPoints[1], endPoints[1]))
                            pathFinders.Add(new AStarEnumerator(this, startPoints[1], endPoints[1]));
                        if (AreNodesClose(startPoints[2], endPoints[0]))
                        {
                            pathFinders.Add(new AStarEnumerator(this, startPoints[2], endPoints[0]));
                            pathFinders.Add(new AStarEnumerator(this, startPoints[0], endPoints[2]));
                        }
                    }
                    if (attempt == 15)
                    {
                        if (AreNodesClose(startPoints[3], endPoints[0]))
                        {
                            pathFinders.Add(new AStarEnumerator(this, startPoints[3], endPoints[0]));
                            pathFinders.Add(new AStarEnumerator(this, startPoints[0], endPoints[3]));
                        }
                        if (AreNodesClose(startPoints[1], endPoints[2]))
                        {
                            pathFinders.Add(new AStarEnumerator(this, startPoints[2], endPoints[1]));
                            pathFinders.Add(new AStarEnumerator(this, startPoints[1], endPoints[2]));
                        }
                    }
                    if (attempt == 20)
                    {
                        if (AreNodesClose(startPoints[4], endPoints[0]))
                        {
                            pathFinders.Add(new AStarEnumerator(this, startPoints[4], endPoints[0]));
                            pathFinders.Add(new AStarEnumerator(this, startPoints[0], endPoints[4]));
                        }
                        if (AreNodesClose(startPoints[1], endPoints[3]))
                        {
                            pathFinders.Add(new AStarEnumerator(this, startPoints[3], endPoints[1]));
                            pathFinders.Add(new AStarEnumerator(this, startPoints[1], endPoints[3]));
                        }
                        if (AreNodesClose(startPoints[2], endPoints[2]))
                            pathFinders.Add(new AStarEnumerator(this, startPoints[2], endPoints[2]));
                    }
                    if (attempt == 25)
                    {
                        if (AreNodesClose(startPoints[1], endPoints[4]))
                        {
                            pathFinders.Add(new AStarEnumerator(this, startPoints[4], endPoints[1]));
                            pathFinders.Add(new AStarEnumerator(this, startPoints[1], endPoints[4]));
                        }
                        if (AreNodesClose(startPoints[2], endPoints[3]))
                        {
                            pathFinders.Add(new AStarEnumerator(this, startPoints[3], endPoints[2]));
                            pathFinders.Add(new AStarEnumerator(this, startPoints[2], endPoints[3]));
                        }
                    }
                    if (attempt == 30)
                    {
                        if (AreNodesClose(startPoints[2], endPoints[4]))
                        {
                            pathFinders.Add(new AStarEnumerator(this, startPoints[4], endPoints[2]));
                            pathFinders.Add(new AStarEnumerator(this, startPoints[2], endPoints[4]));
                        }
                        if (AreNodesClose(startPoints[3], endPoints[3]))
                            pathFinders.Add(new AStarEnumerator(this, startPoints[3], endPoints[3]));
                    }
                    if (attempt == 35)
                    {
                        if (AreNodesClose(startPoints[3], endPoints[4]))
                        {
                            pathFinders.Add(new AStarEnumerator(this, startPoints[4], endPoints[3]));
                            pathFinders.Add(new AStarEnumerator(this, startPoints[3], endPoints[4]));
                        }
                    }
                    if (attempt == 40)
                    {
                        if (AreNodesClose(startPoints[4], endPoints[4]))
                            pathFinders.Add(new AStarEnumerator(this, startPoints[4], endPoints[4]));
                    }
                }

                return result;
            }

            async Task<AStarNode> FindPathSingle(MapNode startPoint, MapNode[] endPoints)
            {
                var pathFinders = new List<AStarEnumerator>();
                int attempt = 0;
                pathFinders.Add(new AStarEnumerator(this, startPoint, endPoints[0]));
                AStarNode result;

                while ((result = await FirstCompletedPathOrDefault(pathFinders)) == null && attempt < 40 && pathFinders.Count > 0)
                {
                    attempt++;
                    if (attempt == 5)
                    {
                        if (AreNodesClose(startPoint, endPoints[1]))
                            pathFinders.Add(new AStarEnumerator(this, startPoint, endPoints[1]));
                    }
                    if (attempt == 10)
                    {
                        if (AreNodesClose(startPoint, endPoints[2]))
                            pathFinders.Add(new AStarEnumerator(this, startPoint, endPoints[2]));
                    }
                    if (attempt == 15)
                    {
                        if (AreNodesClose(startPoint, endPoints[3]))
                            pathFinders.Add(new AStarEnumerator(this, startPoint, endPoints[3]));
                    }
                    if (attempt == 20)
                    {
                        if (AreNodesClose(startPoint, endPoints[4]))
                            pathFinders.Add(new AStarEnumerator(this, startPoint, endPoints[4]));
                    }
                }

                return result;
            }

            MapNode previousSelectedNode = null;

            foreach (var step in trip)
            {
                if (step.PreviousPoint == null)
                {
                    continue;
                }
                if (step.CurrentPoint.GetPoint().DistanceTo(step.PreviousPoint.GetPoint()) < MaxDist)
                {
                    step.Drop();
                    continue;
                }

                AStarNode path = null;

                double minLat;
                double minLon;
                double maxLat;
                double maxLon;

                GeoPoint boundingPoint0 = step.CurrentPoint.GetPoint();
                GeoPoint boundingPoint1;

                if (previousSelectedNode != null)
                    boundingPoint1 = previousSelectedNode.GetPoint();
                else if (step.PreviousPoint != null)
                    boundingPoint1 = step.PreviousPoint.GetPoint();
                else if (closestNodesPrevious != null)
                    boundingPoint1 = closestNodesPrevious[0].GetPoint();
                else
                    boundingPoint1 = boundingPoint0;

                minLat = Math.Min(boundingPoint0.Latitude, boundingPoint1.Latitude) - 2 * MaxDist;
                minLon = Math.Min(boundingPoint0.Longitude, boundingPoint1.Longitude) - 2 * MaxDist;
                maxLat = Math.Max(boundingPoint0.Latitude, boundingPoint1.Latitude) + 2 * MaxDist;
                maxLon = Math.Max(boundingPoint0.Longitude, boundingPoint1.Longitude) + 2 * MaxDist;

                await MemoryDatabase.SetFromRegion(Database, new GeoPoint(minLon, minLat), new GeoPoint(maxLon, maxLat));

                closestNodesCurrent = await MemoryDatabase.GetClosestNodes(step.CurrentPoint.GetPoint(), 5);
                closestNodesPrevious = null;

                if (previousSelectedNode != null)
                {
                    path = await FindPathSingle(previousSelectedNode, closestNodesCurrent);
                }
                if (path == null)
                {
                    if (closestNodesPrevious == null)
                        closestNodesPrevious = await MemoryDatabase.GetClosestNodes(step.PreviousPoint.GetPoint(), 5);
                    path = await FindPathMultiple(closestNodesPrevious, closestNodesCurrent);
                }

                if (path != null)
                {
                    if (previousSelectedNode == null)
                        pathResult.Add(path.GetFirstParent().CurrentNode);
                    pathResult.Add(path.CurrentNode);
                }

                previousSelectedNode = path?.CurrentNode;
            }

            return pathResult;
        }
    }
}
