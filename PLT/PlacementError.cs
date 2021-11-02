using ColossalFramework;
using ColossalFramework.Math;
using EManagersLib.API;
using System;
using UnityEngine;

namespace PropAnarchy.PLT {
    [Flags]
    public enum ItemCollisionType {
        None = 0,
        Props = 1,
        Trees = 2,
        Blocked = 4,
        Water = 8,
        GameArea = 16
    }

    public static class ItemCollisionTypeExtensions {
        public static bool HasFlag(this ItemCollisionType value, ItemCollisionType comparison) => (value & comparison) == comparison;
    }

    public static class PlacementError {
        // ==========================================================================  CHECK ALL COLLISIONS  ==========================================================================
        public static ItemCollisionType CheckAllCollisionsProp(Vector3 worldPosition, PropInfo propInfo) {
            const float radius = 0.5f;
            ItemCollisionType result = ItemCollisionType.None;
            Vector2 center = VectorUtils.XZ(worldPosition);
            Quad2 quad = default;
            quad.a = center + new Vector2(-radius, -radius);
            quad.b = center + new Vector2(-radius, radius);
            quad.c = center + new Vector2(radius, radius);
            quad.d = center + new Vector2(radius, -radius);
            float minY = worldPosition.y;
            float maxY = worldPosition.y + propInfo.m_generatedInfo.m_size.y * Mathf.Max(propInfo.m_maxScale, propInfo.m_minScale);
            ItemClass.CollisionType collisionType = ItemClass.CollisionType.Terrain;
            if (DoesPropCollideWithProps(quad, minY, maxY, collisionType)) {
                result |= ItemCollisionType.Props;
            }
            if (DoesPropCollideWithTrees(quad, minY, maxY, collisionType)) {
                result |= ItemCollisionType.Trees;
            }
            if (CheckPropBlocked(quad, minY, maxY, collisionType, propInfo)) {
                result |= ItemCollisionType.Blocked;
            }
            if (DoesPositionHaveWater(worldPosition)) {
                result |= ItemCollisionType.Water;
            }
            if (IsQuadOutOfGameArea(quad)) {
                result |= ItemCollisionType.GameArea;
            }
            return result;
        }
        public static ItemCollisionType CheckAllCollisionsTree(Vector3 worldPosition, TreeInfo treeInfo) {
            const float radius = 0.5f;
            ItemCollisionType result = ItemCollisionType.None;
            Vector2 center = VectorUtils.XZ(worldPosition);
            Quad2 quad = default;
            quad.a = center + new Vector2(-radius, -radius);
            quad.b = center + new Vector2(-radius, radius);
            quad.c = center + new Vector2(radius, radius);
            quad.d = center + new Vector2(radius, -radius);
            float minY = worldPosition.y;
            float maxY = worldPosition.y + treeInfo.m_generatedInfo.m_size.y * Mathf.Max(treeInfo.m_maxScale, treeInfo.m_minScale);
            ItemClass.CollisionType collisionType = ItemClass.CollisionType.Terrain;
            if (DoesTreeCollideWithProps(quad, minY, maxY, collisionType)) {
                result |= ItemCollisionType.Props;
            }
            if (DoesTreeCollideWithTrees(quad, minY, maxY, collisionType)) {
                result |= ItemCollisionType.Trees;
            }
            if (CheckTreeBlocked(quad, minY, maxY, collisionType, treeInfo)) {
                result |= ItemCollisionType.Blocked;
            }
            if (DoesPositionHaveWater(worldPosition)) {
                result |= ItemCollisionType.Water;
            }
            if (IsQuadOutOfGameArea(quad)) {
                result |= ItemCollisionType.GameArea;
            }
            return result;
        }

        // ==========================================================================  CHECK ALL COLLISIONS LITE  ==========================================================================
        public static bool CheckValidPlacementPropLite(Vector3 worldPosition, PropInfo propInfo) {
            const float radius = 0.5f;
            Vector2 center = VectorUtils.XZ(worldPosition);
            Quad2 quad = default;
            quad.a = center + new Vector2(-radius, -radius);
            quad.b = center + new Vector2(-radius, radius);
            quad.c = center + new Vector2(radius, radius);
            quad.d = center + new Vector2(radius, -radius);
            float minY = worldPosition.y;
            float maxY = worldPosition.y + propInfo.m_generatedInfo.m_size.y * Mathf.Max(propInfo.m_maxScale, propInfo.m_minScale);
            ItemClass.CollisionType collisionType = ItemClass.CollisionType.Terrain;
            if (IsQuadOutOfGameArea(quad)) {
                return false;
            } else if (CheckPropBlocked(quad, minY, maxY, collisionType, propInfo)) {
                return false;
            } else if (DoesPositionHaveWater(worldPosition)) {
                return false;
            } else if (DoesPropCollideWithTrees(quad, minY, maxY, collisionType)) {
                return false;
            } else if (DoesPropCollideWithProps(quad, minY, maxY, collisionType)) {
                return false;
            }
            return true;
        }
        public static bool CheckValidPlacementTreeLite(Vector3 worldPosition, TreeInfo treeInfo) {
            const float radius = 0.5f;
            Vector2 center = VectorUtils.XZ(worldPosition);
            Quad2 quad = default;
            quad.a = center + new Vector2(-radius, -radius);
            quad.b = center + new Vector2(-radius, radius);
            quad.c = center + new Vector2(radius, radius);
            quad.d = center + new Vector2(radius, -radius);
            float minY = worldPosition.y;
            float maxY = worldPosition.y + treeInfo.m_generatedInfo.m_size.y * Mathf.Max(treeInfo.m_maxScale, treeInfo.m_minScale);
            ItemClass.CollisionType _collisionType = ItemClass.CollisionType.Terrain;
            if (IsQuadOutOfGameArea(quad)) {
                return false;
            } else if (CheckTreeBlocked(quad, minY, maxY, _collisionType, treeInfo)) {
                return false;
            } else if (DoesPositionHaveWater(worldPosition)) {
                return false;
            } else if (DoesTreeCollideWithTrees(quad, minY, maxY, _collisionType)) {
                return false;
            } else if (DoesTreeCollideWithProps(quad, minY, maxY, _collisionType)) {
                return false;
            }
            return true;
        }

        // ==========================================================================  PROP COLLISION  ==========================================================================
        public static bool DoesPropCollideWithProps(Quad2 quad, float minY, float maxY, ItemClass.CollisionType collisionType) =>
            EPropManager.OverlapQuad(quad, minY, maxY, collisionType, 0, 0);
        public static bool DoesTreeCollideWithProps(Quad2 quad, float minY, float maxY, ItemClass.CollisionType collisionType) =>
            EPropManager.OverlapQuad(quad, minY, maxY, collisionType, 0, 0);

        // ==========================================================================  TREE COLLISION  ==========================================================================
        public static bool DoesPropCollideWithTrees(Quad2 quad, float minY, float maxY, ItemClass.CollisionType collisionType) =>
            Singleton<TreeManager>.instance.OverlapQuad(quad, minY, maxY, collisionType, 0, 0u);
        public static bool DoesTreeCollideWithTrees(Quad2 quad, float minY, float maxY, ItemClass.CollisionType collisionType) =>
            Singleton<TreeManager>.instance.OverlapQuad(quad, minY, maxY, collisionType, 0, 0u);

        // ==========================================================================  NET/BUILDING COLLISION  ==========================================================================
        public static bool CheckPropBlocked(Quad2 quad, float minY, float maxY, ItemClass.CollisionType collisionType, PropInfo propInfo) {
            if (!Singleton<NetManager>.instance.OverlapQuad(quad, minY, maxY, collisionType, propInfo.m_class.m_layer, 0, 0, 0)) {
                return false;
            }
            if (!Singleton<BuildingManager>.instance.OverlapQuad(quad, minY, maxY, collisionType, propInfo.m_class.m_layer, 0, 0, 0)) {
                return false;
            }
            return true;
        }
        public static bool CheckTreeBlocked(Quad2 quad, float minY, float maxY, ItemClass.CollisionType collisionType, TreeInfo treeInfo) {
            if (!Singleton<NetManager>.instance.OverlapQuad(quad, minY, maxY, collisionType, treeInfo.m_class.m_layer, 0, 0, 0)) {
                return false;
            }
            if (!Singleton<BuildingManager>.instance.OverlapQuad(quad, minY, maxY, collisionType, treeInfo.m_class.m_layer, 0, 0, 0)) {
                return false;
            }
            return true;
        }

        // ==========================================================================  TERRAIN WATER COLLISION  ==========================================================================
        public static bool DoesPositionHaveWater(Vector3 worldPosition) => Singleton<TerrainManager>.instance.HasWater(new Vector2(worldPosition.x, worldPosition.z));

        // ==========================================================================  GAME AREA COLLISION  ==========================================================================
        public static bool IsQuadOutOfGameArea(Quad2 quad) => Singleton<GameAreaManager>.instance.QuadOutOfArea(quad);
    }
}
