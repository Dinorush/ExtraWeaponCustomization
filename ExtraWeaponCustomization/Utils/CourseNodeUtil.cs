using AIGraph;
using EWC.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EWC.Utils
{
    public static class CourseNodeUtil
    {
        class DimensionMap
        {
            private const float IndexSize = 4f;

            public List<AIG_CourseNode>?[,] BuildNodeMap;
            public AIG_CourseNode[,][] NodeMap;
            public (int x, int z) MinCellBound;
            public (int x, int z) MaxCellBound;
            public (int x, int z) MapSize;

            internal DimensionMap()
            {
                BuildNodeMap = null!;
                NodeMap = null!;
                MapSize = (0, 0);
                MinCellBound = (int.MaxValue, int.MaxValue);
                MaxCellBound = (int.MinValue, int.MinValue);
            }

            // Expands the bounds to encapsulate the position
            internal void UpdateBounds(Vector3 position)
            {
                int x = (int)(position.x / IndexSize);
                int z = (int)(position.z / IndexSize);
                MinCellBound = (Math.Min(x, MinCellBound.x), Math.Min(z, MinCellBound.z));
                MaxCellBound = (Math.Max(x, MaxCellBound.x), Math.Max(z, MaxCellBound.z));
            }

            internal void CreateNodeMap()
            {
                MapSize = (MaxCellBound.x - MinCellBound.x + 1, MaxCellBound.z - MinCellBound.z + 1);
                BuildNodeMap = new List<AIG_CourseNode>[MapSize.x, MapSize.z];
                NodeMap = new AIG_CourseNode[MapSize.x, MapSize.z][];
            }

            internal (int x, int z) GetMapPos(Vector3 position)
            {
                return (
                    Math.Clamp((int)(position.x / IndexSize) - MinCellBound.x, 0, MapSize.x - 1),
                    Math.Clamp((int)(position.z / IndexSize) - MinCellBound.z, 0, MapSize.z - 1)
                    );
            }

            internal List<AIG_CourseNode> GetBuildNodeList(Vector3 position)
            {
                var (x, z) = GetMapPos(position);
                var list = BuildNodeMap[x, z];
                if (list == null)
                    list = BuildNodeMap[x, z] = new();
                return list;
            }

            internal void FinishBuild()
            {
                for (int x = 0; x < MapSize.x; x++)
                {
                    for (int z = 0; z < MapSize.z; z++)
                    {
                        NodeMap[x, z] = BuildNodeMap[x, z]?.ToArray()!;
                    }
                }
                BuildNodeMap = null!;
            }

            public AIG_CourseNode[] GetNodes(Vector3 position)
            {
                var (x, z) = GetMapPos(position);
                if (NodeMap[x, z] != null)
                    return NodeMap[x, z];

                float maxRadius = Math.Max(MapSize.x, MapSize.z);
                // Check increasing rings to find a nearby node
                for (int radius = 1; radius < maxRadius; radius++)
                {
                    int left = Math.Max(x - radius, 0);
                    int right = Math.Min(x + radius, MapSize.x - 1);
                    int top = Math.Max(z - radius, 0);
                    int bottom = Math.Min(z + radius, MapSize.z - 1);

                    for (int xPos = left; xPos <= right; xPos++)
                    {
                        if (NodeMap[xPos, top] != null)
                            return NodeMap[xPos, top];
                        if (NodeMap[xPos, bottom] != null)
                            return NodeMap[xPos, bottom];
                    }

                    for (int zPos = top + 1; zPos <= bottom - 1; zPos++)
                    {
                        if (NodeMap[left, zPos] != null)
                            return NodeMap[left, zPos];
                        if (NodeMap[right, zPos] != null)
                            return NodeMap[right, zPos];
                    }
                }

                EWCLogger.Error($"Unable to get any node for ({position})! How are you even playing the game?!");
                return null!;
            }
        }

        private readonly static Dictionary<eDimensionIndex, DimensionMap> _maps = new();

        [InvokeOnBuildDone]
        private static void OnBuildDone()
        {
            _maps.Clear();

            foreach (var cluster in AIG_NodeCluster.AllNodeClusters)
            {
                if (!_maps.TryGetValue(cluster.m_courseNode.m_dimension.DimensionIndex, out var map))
                    _maps.Add(cluster.m_courseNode.m_dimension.DimensionIndex, map = new());

                foreach (var node in cluster.m_nodes)
                    map.UpdateBounds(node.Position);
            }

            foreach (var map in _maps.Values)
                map.CreateNodeMap();

            foreach (var cluster in AIG_NodeCluster.AllNodeClusters)
            {
                var map = _maps[cluster.m_courseNode.m_dimension.DimensionIndex];
                var id = cluster.CourseNode.NodeID;
                foreach (var node in cluster.m_nodes)
                {
                    var list = map.GetBuildNodeList(node.Position);
                    if (!list.Any(node => id == node.NodeID))
                        list.Add(cluster.CourseNode);
                }
            }

            foreach (var map in _maps.Values)
                map.FinishBuild();
        }

        public static AIG_CourseNode ResolveCourseNode(AIG_CourseNode[] list, Vector3 position)
        {
            if (list == null)
                return null!;

            if (list.Length == 1)
                return list[0];

            // Retrieve the closest nodes in each course node
            (AIG_CourseNode, AIG_INode)[] bestNodes = new (AIG_CourseNode, AIG_INode)[list.Length];
            int index = 0;
            foreach (var courseNode in list)
            {
                courseNode.m_nodeCluster.TryGetClosestNodeInCluster(position, out var bestNodeInNode);
                bestNodes[index++] = (courseNode, bestNodeInNode);
            }

            // Determine the closest of the closest nodes
            // Prioritize below the position so hitting a roof uses the node with the roof
            bool bestIsValidHeight = false;
            float bestDist = float.MaxValue;
            AIG_CourseNode bestNode = null!;
            foreach ((var courseNode, var node) in bestNodes)
            {
                if (node == null) continue;

                bool validHeight = node.Position.y - 0.25f <= position.y;
                float sqrDist = (position - node.Position).sqrMagnitude;
                if ((!bestIsValidHeight && validHeight) || (bestIsValidHeight == validHeight && sqrDist < bestDist))
                {
                    bestIsValidHeight = validHeight;
                    bestDist = sqrDist;
                    bestNode = courseNode;
                }
            }

            return bestNode;
        }

        public static AIG_CourseNode GetCourseNode(Vector3 position, eDimensionIndex dimensionIndex)
        {
            if (!_maps.TryGetValue(dimensionIndex, out var map))
            {
                EWCLogger.Error($"No Position-To-Node map for dimension {dimensionIndex}!");
                return null!;
            }

            return ResolveCourseNode(map.GetNodes(position), position);
        }

        public static AIG_CourseNode[] GetCourseNodes(Vector3 position, eDimensionIndex dimensionIndex)
        {
            if (!_maps.TryGetValue(dimensionIndex, out var map))
            {
                EWCLogger.Error($"No Position-To-Node map for dimension {dimensionIndex}!");
                return null!;
            }

            return map.GetNodes(position);
        }
    }
}
