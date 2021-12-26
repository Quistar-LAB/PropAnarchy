using ColossalFramework;
using ColossalFramework.Math;
using EManagersLib;
using System;
using UnityEngine;

namespace PropAnarchy.PLT {
    [Flags]
    internal enum CollisionType : byte {
        None = 0x00,
        Props = 0x01,
        Trees = 0x02,
        Blocked = 0x04,
        Water = 0x08,
        GameArea = 0x10
    }

    internal static class ItemCollisionTypeExtensions {
        internal static bool HasFlag(this CollisionType value, CollisionType comparison) => (value & comparison) == comparison;
    }

    internal static class PlacementError {
        internal delegate bool TreeOverlapQuadHandler(Quad2 quad, float minY, float maxY, ItemClass.CollisionType collisionType, int layer, uint ignoreTree);
        internal delegate bool NetOverlapQuadHandler(Quad2 quad, float minY, float maxY, ItemClass.CollisionType collisionType, ItemClass.Layer layers, ushort ignoreNode1, ushort ignoreNode2, ushort ignoreSegment);
        internal delegate bool BuildingOverlapQuadHandler(Quad2 quad, float minY, float maxY, ItemClass.CollisionType collisionType, ItemClass.Layer layers, ushort ignoreBuilding, ushort ignoreNode1, ushort ignoreNode2);
        internal delegate bool GameAreaQuadOutOfAreaHandler(Quad2 quad);
        internal delegate bool TerrainHasWaterHandler(Vector2 position);
        internal static TreeOverlapQuadHandler TreeOverlapQuad;
        internal static NetOverlapQuadHandler NetOverlapQuad;
        internal static BuildingOverlapQuadHandler BuildingOverlapQuad;
        internal static TerrainHasWaterHandler TerrainHasWater;
        internal static GameAreaQuadOutOfAreaHandler OutOfArea;

        internal static void Initialize() {
            TreeOverlapQuad = Singleton<TreeManager>.instance.OverlapQuad;
            NetOverlapQuad = Singleton<NetManager>.instance.OverlapQuad;
            BuildingOverlapQuad = Singleton<BuildingManager>.instance.OverlapQuad;
            TerrainHasWater = Singleton<TerrainManager>.instance.HasWater;
            OutOfArea = Singleton<GameAreaManager>.instance.QuadOutOfArea;
        }

        public static CollisionType CheckAllCollisionsProp(Vector3 worldPosition, PropInfo propInfo) {
            const float radius = 0.5f;
            Vector2 center = VectorUtils.XZ(worldPosition);
            Quad2 quad = default;
            quad.a = new Vector2(center.x - radius, center.y - radius);
            quad.b = new Vector2(center.x - radius, center.y + radius);
            quad.c = new Vector2(center.x + radius, center.y + radius);
            quad.d = new Vector2(center.x + radius, center.y - radius);
            float minY = worldPosition.y;
            float maxY = worldPosition.y + propInfo.m_generatedInfo.m_size.y * EMath.Max(propInfo.m_maxScale, propInfo.m_minScale);
            ItemClass.CollisionType collisionType = ItemClass.CollisionType.Terrain;
            if (EPropManager.OverlapQuad(quad, minY, maxY, collisionType, 0, 0)) {
                return CollisionType.Props;
            } else if (TreeOverlapQuad(quad, minY, maxY, collisionType, 0, 0)) {
                return CollisionType.Trees;
            } else if (NetOverlapQuad(quad, minY, maxY, collisionType, propInfo.m_class.m_layer, 0, 0, 0)) {
                return CollisionType.Blocked;
            } else if (BuildingOverlapQuad(quad, minY, maxY, collisionType, propInfo.m_class.m_layer, 0, 0, 0)) {
                return CollisionType.Blocked;
            } else if (TerrainHasWater(new Vector2(worldPosition.x, worldPosition.z))) {
                return CollisionType.Water;
            } else if (OutOfArea(quad)) {
                return CollisionType.GameArea;
            }
            return CollisionType.None;
        }

        public static CollisionType CheckAllCollisionsTree(Vector3 worldPosition, TreeInfo treeInfo) {
            const float radius = 0.5f;
            Vector2 center = VectorUtils.XZ(worldPosition);
            Quad2 quad = default;
            quad.a = new Vector2(center.x - radius, center.y - radius);
            quad.b = new Vector2(center.x - radius, center.y + radius);
            quad.c = new Vector2(center.x + radius, center.y + radius);
            quad.d = new Vector2(center.x + radius, center.y - radius);
            float minY = worldPosition.y;
            float maxY = worldPosition.y + treeInfo.m_generatedInfo.m_size.y * EMath.Max(treeInfo.m_maxScale, treeInfo.m_minScale);
            ItemClass.CollisionType collisionType = ItemClass.CollisionType.Terrain;
            if (EPropManager.OverlapQuad(quad, minY, maxY, collisionType, 0, 0)) {
                return CollisionType.Props;
            } else if (TreeOverlapQuad(quad, minY, maxY, collisionType, 0, 0)) {
                return CollisionType.Trees;
            } else if (NetOverlapQuad(quad, minY, maxY, collisionType, treeInfo.m_class.m_layer, 0, 0, 0)) {
                return CollisionType.Blocked;
            } else if (BuildingOverlapQuad(quad, minY, maxY, collisionType, treeInfo.m_class.m_layer, 0, 0, 0)) {
                return CollisionType.Blocked;
            } else if (TerrainHasWater(new Vector2(worldPosition.x, worldPosition.z))) {
                return CollisionType.Water;
            } else if (OutOfArea(quad)) {
                return CollisionType.GameArea;
            }
            return CollisionType.None;
        }
    }
}
