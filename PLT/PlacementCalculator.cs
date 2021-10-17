using ColossalFramework;
using ColossalFramework.Math;
using EManagersLib.API;
using UnityEngine;
using static PropAnarchy.PLT.PropLineTool;

namespace PropAnarchy.PLT {
    public static class PlacementCalculator {
        private const int SEED_INT = 8675309;
        private static readonly Vector3 VECTOR3DOWN = Vector3.down;
        public static PLTItemPlacementInfo[] m_placementInfos;
        public static Vector3[] m_fenceEndPoints;
        public static int m_itemCount;
        public static float m_tolerance = 0.001f;
        public static float m_spacingSingle = 0.1f;
        public static PLTAngleMode m_angleMode = PLTAngleMode.Dynamic;
        public static float m_angleSingle = 0f;     //absolute angle, in radians
        public static float m_angleOffset = 0f;   //absolute angle, in radians
        public static float m_assetLength = 2f;
        public static float m_assetWidth = 2f;
        public static float GetDefaultSpacing() {
            float result;
            if (m_fenceMode) {
                float scaleFactor = 2f;
                switch (m_objectMode) {
                case PLTObjectMode.Props:
                    if (m_assetLength < 4f) scaleFactor = 2.2f;
                    break;
                case PLTObjectMode.Trees:
                    if (m_assetLength > 7f) scaleFactor = 1.1f;
                    else scaleFactor = 2f;
                    break;
                }
                result = Mathf.Clamp(m_assetLength * scaleFactor, 0f, SPACING_MAX);
            } else {
                result = Mathf.Clamp(Mathf.Round(m_assetLength), 0f, SPACING_MAX);
                result = m_objectMode == PLTObjectMode.Props && result < 2f ? 2f : result;
            }
            return result != 0f ? result : 8f;
        }
        public static Vector3 InitialItemPosition => m_fenceMode ? m_fenceEndPoints[0] : m_placementInfos[0].m_position;
        public static Vector3 FinalItemPosition => m_fenceMode ? m_fenceEndPoints[m_itemCount] : m_placementInfos[m_itemCount - 1].m_position;
        public static bool IsIndexWithinBounds(int index, bool isFenceEndPoint) => index >= 0 && index < (MAX_ITEM_ARRAY_LENGTH + (isFenceEndPoint ? 1 : 0));
        private static Vector3 GetFenceEndpoint(int index) => IsIndexWithinBounds(index, true) ? m_fenceEndPoints[index] : VECTOR3DOWN;
        public static bool GetFenceEndpoint(int index, out Vector3 fenceEndpoint) {
            if (IsIndexWithinBounds(index, true)) {
                fenceEndpoint = m_fenceEndPoints[index];
                return true;
            }
            fenceEndpoint = Vector3.down;
            return false;
        }
        public static bool GetItemT(int index, out float itemT) {
            if (IsIndexWithinBounds(index, false)) {
                itemT = m_placementInfos[index].m_t;
                return true;
            }
            itemT = 0f;
            return false;
        }
        public static float GetItemT(int index) => IsIndexWithinBounds(index, false) ? m_placementInfos[index].m_t : 0f;
        public static bool GetItemPosition(int index, out Vector3 itemPosition) {
            if (IsIndexWithinBounds(index, false)) {
                itemPosition = m_placementInfos[index].m_position;
                return true;
            }
            itemPosition = Vector3.zero;
            return false;
        }
        public static Vector3 GetItemPosition(int index) => IsIndexWithinBounds(index, false) ? m_placementInfos[index].m_position : VECTOR3DOWN;
        public static void RevertLastContinueParameters(float lastFinalOffset, Vector3 lastFenceEndpoint) => m_segmentState.RevertLastContinueParameters(lastFinalOffset, lastFenceEndpoint);
        public static void ResetLastContinueParameters() {
            m_segmentState.m_lastFenceEndpoint = Vector3.down;
            m_segmentState.m_lastFinalOffset = 0f;
            UpdateItemPlacementInfo(m_isCopyPlacing, m_segmentState, m_segmentState.IsContinueDrawing, m_segmentState.m_keepLastOffsets);
        }
        public static bool UpdateItemPlacementInfo(bool forceContinueDrawing) => UpdateItemPlacementInfo(m_isCopyPlacing, m_segmentState, forceContinueDrawing, m_segmentState.m_keepLastOffsets);
        public static bool UpdateItemPlacementInfo(bool isCopyPlacing, SegmentState segmentState, bool forceContinueDrawing, bool forceKeepLastOffsets) {
            bool result;
            if (isCopyPlacing) {
                segmentState.m_keepLastOffsets = true;
                result = CalculateAll(segmentState, true);
            } else {
                segmentState.m_keepLastOffsets = forceKeepLastOffsets;
                result = CalculateAll(segmentState, forceContinueDrawing || segmentState.m_isContinueDrawing);
            }
            segmentState.m_isContinueDrawing = forceContinueDrawing;
            return result;
        }

        private static bool CalculateAll(SegmentState segmentState, bool continueDrawing) {
            float initialOffset = 0f;
            Vector3 lastFenceEndPoint;
            m_itemCount = MAX_ITEM_ARRAY_LENGTH;   //not sure about setting m_itemCount here, before CalculateAllPositions
            if (continueDrawing) {
                initialOffset = segmentState.m_lastFinalOffset;
                lastFenceEndPoint = segmentState.m_lastFenceEndpoint;
            } else {
                lastFenceEndPoint = m_mainSegment.b;
            }
            switch (m_controlMode) {
            case PLTControlMode.Itemwise:
                if (CalculateItemwisePosition(m_spacingSingle, initialOffset, lastFenceEndPoint)) {
                    if (CalculateAllDirections()) {
                        CalculateAllAnglesBase();
                        SetAllItemPrefabInfos();
                        UpdatePlacementErrors();
                        return true;
                    }
                }
                break;
            case PLTControlMode.Spacing:
                if (CalculateAllPositionsBySpacing(m_spacingSingle, initialOffset, lastFenceEndPoint)) {
                    if (CalculateAllDirections()) {
                        CalculateAllAnglesBase();
                        SetAllItemPrefabInfos();
                        UpdatePlacementErrors();
                        return true;
                    }
                }
                break;
            }
            segmentState.m_maxItemCountExceeded = false;
            return false;
        }

        public static void SetAllItemPrefabInfos() {
            //make sure to use the same randomizer as item placement (PropLineTool.FinalizePlacement)
            switch (m_objectMode) {
            case PLTObjectMode.Props:
                Randomizer randomizer = new Randomizer(EPropManager.m_props.NextFreeItem());
                int itemCount = m_itemCount;
                PropInfo propInfo = m_propInfo;
                if (!(propInfo is null)) {
                    if (propInfo.m_variations.Length > 0) {
                        for (int i = 0; i < itemCount; i++) {
                            PropInfo propVariation = propInfo.GetVariation(ref randomizer);
                            Randomizer randomizer2 = new Randomizer(EPropManager.m_props.NextFreeItem(ref randomizer));
                            m_placementInfos[i].PropPrefab = propVariation;
                            m_placementInfos[i].m_scale = propVariation.m_minScale + randomizer2.Int32(10000u) * (propVariation.m_maxScale - propVariation.m_minScale) * 0x0001f;
                            m_placementInfos[i].m_color = propVariation.GetColor(ref randomizer2);
                        }
                    } else {
                        if (m_controlMode == PLTControlMode.Itemwise && itemCount == 1) {
                            Randomizer randomizer2 = new Randomizer(EPropManager.m_props.NextFreeItem(ref randomizer));
                            m_placementInfos[ITEMWISE_INDEX].PropPrefab = propInfo;
                            m_placementInfos[ITEMWISE_INDEX].m_scale = propInfo.m_minScale + randomizer2.Int32(10000u) * (propInfo.m_maxScale - propInfo.m_minScale) * 0.0001f;
                            m_placementInfos[ITEMWISE_INDEX].m_color = propInfo.GetColor(ref randomizer2);
                        } else {
                            for (int i = 0; i < itemCount; i++) {
                                m_placementInfos[i].PropPrefab = propInfo;
                                m_placementInfos[i].m_scale = propInfo.m_minScale + randomizer.Int32(10000u) * (propInfo.m_maxScale - propInfo.m_minScale) * 0.0001f;
                                m_placementInfos[i].m_color = propInfo.GetColor(ref randomizer);
                            }
                        }
                    }
                    return;
                }
                break;
            case PLTObjectMode.Trees:
                randomizer = new Randomizer(Singleton<TreeManager>.instance.m_trees.NextFreeItem());
                itemCount = m_itemCount;
                TreeInfo treeInfo = m_treeInfo;
                if (!(treeInfo is null)) {
                    if (treeInfo.m_variations.Length > 0) {
                        for (int i = 0; i < m_itemCount; i++) {
                            TreeInfo treeVariation = treeInfo.GetVariation(ref randomizer);
                            m_placementInfos[i].TreePrefab = treeVariation;
                            Randomizer randomizer2 = new Randomizer(Singleton<TreeManager>.instance.m_trees.NextFreeItem(ref randomizer));
                            m_placementInfos[i].m_scale = treeVariation.m_minScale + randomizer2.Int32(10000u) * (treeVariation.m_maxScale - treeVariation.m_minScale) * 0.0001f;
                            m_placementInfos[i].m_brightness = treeVariation.m_minBrightness + randomizer2.Int32(10000u) * (treeVariation.m_maxBrightness - treeVariation.m_minBrightness) * 0.0001f;
                        }
                    } else {
                        if (m_controlMode == PLTControlMode.Itemwise && itemCount == 1) {
                            m_placementInfos[ITEMWISE_INDEX].TreePrefab = treeInfo;
                            Randomizer randomizer2 = new Randomizer(Singleton<TreeManager>.instance.m_trees.NextFreeItem(ref randomizer));
                            m_placementInfos[ITEMWISE_INDEX].m_scale = treeInfo.m_minScale + randomizer2.Int32(10000u) * (treeInfo.m_maxScale - treeInfo.m_minScale) * 0.0001f;
                            m_placementInfos[ITEMWISE_INDEX].m_brightness = treeInfo.m_minBrightness + randomizer2.Int32(10000u) * (treeInfo.m_maxBrightness - treeInfo.m_minBrightness) * 0.0001f;
                        } else {
                            for (int i = 0; i < m_itemCount; i++) {
                                m_placementInfos[i].TreePrefab = treeInfo;
                                m_placementInfos[i].m_scale = treeInfo.m_minScale + randomizer.Int32(10000u) * (treeInfo.m_maxScale - treeInfo.m_minScale) * 0.0001f;
                                m_placementInfos[i].m_brightness = treeInfo.m_minBrightness + randomizer.Int32(10000u) * (treeInfo.m_maxBrightness - treeInfo.m_minBrightness) * 0.0001f;
                            }
                        }
                    }
                    return;
                }
                break;
            }
            m_itemCount = 0;
        }

        public static void UpdatePlacementErrors() {
            int itemCount = m_itemCount;
            if (!PLTSettings.ErrorChecking) {
                for (int i = 0; i < itemCount; i++) {
                    m_placementInfos[i].m_collisionFlags = ItemCollisionType.None;
                    m_placementInfos[i].m_isValidPlacement = true;
                }
                m_segmentState.m_allItemsValid = true;
                return;
            }
            bool itemsValid = true;
            switch (m_objectMode) {
            case PLTObjectMode.Props:
                PropInfo propInfo = m_propInfo;
                if (!(propInfo is null)) {
                    for (int i = 0; i < itemCount; i++) {
                        Vector3 position = m_placementInfos[i].m_position;
                        propInfo = m_placementInfos[i].PropPrefab;
                        ItemCollisionType collisionFlags = PlacementError.CheckAllCollisionsProp(position, propInfo);
                        m_placementInfos[i].m_collisionFlags = collisionFlags;
                        if (collisionFlags == ItemCollisionType.None || PLTSettings.AnarchyPLT || (PLTSettings.PlaceBlockedItems && collisionFlags == ItemCollisionType.Blocked)) {
                            m_placementInfos[i].m_isValidPlacement = true;
                        } else {
                            m_placementInfos[i].m_isValidPlacement = false;
                            itemsValid = false;
                        }
                    }
                }
                break;
            case PLTObjectMode.Trees:
                TreeInfo treeInfo = m_treeInfo;
                if (!(treeInfo is null)) {
                    for (int i = 0; i < itemCount; i++) {
                        Vector3 position = m_placementInfos[i].m_position;
                        TreeInfo tree = m_placementInfos[i].TreePrefab;
                        ItemCollisionType collisionFlags = PlacementError.CheckAllCollisionsTree(position, tree);
                        m_placementInfos[i].m_collisionFlags = collisionFlags;
                        if (collisionFlags == ItemCollisionType.None || PLTSettings.AnarchyPLT || (PLTSettings.PlaceBlockedItems && collisionFlags == ItemCollisionType.Blocked)) {
                            m_placementInfos[i].m_isValidPlacement = true;
                        } else {
                            m_placementInfos[i].m_isValidPlacement = false;
                            itemsValid = false;
                        }
                    }
                }
                break;
            default:
                m_segmentState.m_allItemsValid = true;
                break;
            }
            m_segmentState.m_allItemsValid = itemsValid;
        }

        private static void CalculateAllAnglesBase() {
            int itemCount = m_itemCount;
            if (m_fenceMode) {
                Vector3 xAxis = Vector3.right;
                Vector3 yAxis = Vector3.up;
                float offsetAngle = Mathf.Deg2Rad * ((TotalPropertyAngleOffset + m_angleOffset) * Mathf.Rad2Deg % 360f);
                for (int i = 0; i < itemCount; i++) {
                    m_placementInfos[i].m_angle = PLTMath.AngleSigned(m_placementInfos[i].m_itemDirection, xAxis, yAxis) + Mathf.PI + offsetAngle;
                }
            } else {
                switch (m_angleMode) {
                case PLTAngleMode.Dynamic:
                    Vector3 xAxis = Vector3.right;
                    Vector3 yAxis = Vector3.up;
                    float offsetAngle = Mathf.Deg2Rad * ((TotalPropertyAngleOffset + m_angleOffset) * Mathf.Rad2Deg % 360f);
                    for (int i = 0; i < itemCount; i++) {
                        m_placementInfos[i].m_angle = PLTMath.AngleSigned(m_placementInfos[i].m_itemDirection, xAxis, yAxis) + Mathf.PI + offsetAngle;
                    }
                    break;
                case PLTAngleMode.Single:
                    float singleAngle = Mathf.Deg2Rad * ((TotalPropertyAngleOffset + m_angleSingle) * Mathf.Rad2Deg % 360f);
                    for (int i = 0; i < itemCount; i++) {
                        m_placementInfos[i].m_angle = singleAngle;
                    }
                    break;
                }
            }
        }

        private static bool CalculateAllDirections() {
            int itemCount = m_itemCount;
            switch (m_drawMode) {
            case PLTDrawMode.Straight:
                //calculate from segment
                Vector3 itemDir = m_mainSegment.b - m_mainSegment.a;
                //this function takes care of the normalization for you
                for (int i = 0; i < itemCount; i++) {
                    m_placementInfos[i].SetDirectionsXZ(itemDir);
                }
                break;
            case PLTDrawMode.Curved:
            case PLTDrawMode.Freeform:
                if (m_fenceMode) {
                    //calculate fenceEndpoint to fenceEndpoint
                    for (int i = 0; i < itemCount; i++) {
                        m_placementInfos[i].SetDirectionsXZ(m_fenceEndPoints[i + 1] - m_fenceEndPoints[i]);
                    }
                } else {
                    //calculate from curve tangent
                    for (int i = 0; i < itemCount; i++) {
                        m_placementInfos[i].SetDirectionsXZ(m_mainBezier.Tangent(m_placementInfos[i].m_t));
                    }
                }
                break;
            case PLTDrawMode.Circle:
                if (m_fenceMode) {
                    //calculate fenceEndpoint to fenceEndpoint
                    for (int i = 0; i < itemCount; i++) {
                        m_placementInfos[i].SetDirectionsXZ(m_fenceEndPoints[i + 1] - m_fenceEndPoints[i]);
                    }
                } else {
                    //calculate from curve tangent
                    for (int i = 0; i < itemCount; i++) {
                        m_placementInfos[i].SetDirectionsXZ(m_mainCircle.Tangent(m_placementInfos[i].m_t));
                    }
                }
                break;
            default:
                return false;
            }
            return true;
        }

        public delegate float SampleDetailHeight(Vector3 position);
        private static bool CalculateItemwisePosition(float fencePieceLength, float initialOffset, Vector3 lastFenceEndpoint) {
            m_itemCount = 1;
            if (m_fenceMode) { //FenceMode = ON
                switch (m_drawMode) {
                case PLTDrawMode.Straight:
                    float mouseT = m_hoverItemwiseT;
                    float deltaT = fencePieceLength / PLTMath.LinearSpeedXZ(m_mainSegment);
                    float itemTStart = mouseT;
                    float sumT = mouseT + deltaT;
                    if (sumT > 1f && m_mainSegment.LengthXZ() >= fencePieceLength) {
                        itemTStart += (1f - sumT);
                    }
                    Vector3 positionStart = PLTMath.LinePosition(m_mainSegment, itemTStart);
                    Vector3 positionEnd = PLTMath.LinePosition(m_mainSegment, itemTStart + deltaT);
                    m_fenceEndPoints[ITEMWISE_FENCE_INDEX_START] = positionStart;
                    m_fenceEndPoints[ITEMWISE_FENCE_INDEX_END] = positionEnd;
                    m_placementInfos[ITEMWISE_INDEX].m_position = Vector3.Lerp(positionStart, positionEnd, 0.50f); // Set Midpoint
                    break;
                case PLTDrawMode.Curved:
                case PLTDrawMode.Freeform:
                    if (fencePieceLength > PLTMath.CubicBezierArcLengthXZGauss12(m_mainBezier, 0f, 1f)) {
                        m_itemCount = 0;
                        return false;
                    }
                    mouseT = m_hoverItemwiseT;
                    itemTStart = mouseT;
                    _ = PLTMath.CircleCurveFenceIntersectXZ(m_mainBezier, itemTStart, fencePieceLength, m_tolerance, out float itemTEnd, false);
                    if (itemTEnd > 1f) {
                        //out of bounds? -> attempt to snap to d-end of curve
                        //invert the curve to go "backwards"
                        itemTEnd = 0f;
                        Bezier3 inverseBezier = m_mainBezier.Invert();
                        if (!PLTMath.CircleCurveFenceIntersectXZ(inverseBezier, itemTEnd, fencePieceLength, m_tolerance, out itemTStart, false)) {
                            //failed to snap to d-end of curve
                            m_itemCount = 0;
                            return false;
                        } else {
                            itemTStart = 1f - itemTStart;
                            itemTEnd = 1f - itemTEnd;
                        }
                    }
                    positionStart = m_mainBezier.Position(itemTStart);
                    positionEnd = m_mainBezier.Position(itemTEnd);
                    m_fenceEndPoints[ITEMWISE_FENCE_INDEX_START] = positionStart;
                    m_fenceEndPoints[ITEMWISE_FENCE_INDEX_END] = positionEnd;
                    m_placementInfos[ITEMWISE_INDEX].m_position = Vector3.Lerp(positionStart, positionEnd, 0.50f); // Set Midpoint
                    break;
                case PLTDrawMode.Circle:
                    if (m_mainCircle.m_radius == 0f || fencePieceLength > m_mainCircle.Diameter) {
                        m_itemCount = 0;
                        return false;
                    }
                    mouseT = m_hoverItemwiseT;
                    itemTStart = mouseT;
                    deltaT = m_mainCircle.ChordDeltaT(fencePieceLength);
                    if (deltaT <= 0f || deltaT >= 1f) {
                        m_itemCount = 0;
                        return false;
                    }
                    itemTEnd = itemTStart + deltaT;
                    positionStart = m_mainCircle.Position(itemTStart);
                    positionEnd = m_mainCircle.Position(itemTEnd);
                    m_fenceEndPoints[ITEMWISE_FENCE_INDEX_START] = positionStart;
                    m_fenceEndPoints[ITEMWISE_FENCE_INDEX_END] = positionEnd;
                    m_placementInfos[ITEMWISE_INDEX].m_position = Vector3.Lerp(positionStart, positionEnd, 0.50f);
                    break;
                default:
                    m_itemCount = 0;
                    return false;
                }
            } else { //Non-fence mode
                switch (m_drawMode) {
                case PLTDrawMode.Straight:
                    Vector3 position = PLTMath.LinePosition(m_mainSegment, m_hoverItemwiseT);
                    position.y = Singleton<TerrainManager>.instance.SampleDetailHeight(position);
                    m_placementInfos[ITEMWISE_INDEX].m_t = m_hoverItemwiseT;
                    m_placementInfos[ITEMWISE_INDEX].m_position = position;
                    break;
                case PLTDrawMode.Curved:
                case PLTDrawMode.Freeform:
                    position = m_mainBezier.Position(m_hoverItemwiseT);
                    position.y = Singleton<TerrainManager>.instance.SampleDetailHeight(position);
                    m_placementInfos[ITEMWISE_INDEX].m_t = m_hoverItemwiseT;
                    m_placementInfos[ITEMWISE_INDEX].m_position = position;
                    break;
                case PLTDrawMode.Circle:
                    position = m_mainCircle.Position(m_hoverItemwiseT);
                    position.y = Singleton<TerrainManager>.instance.SampleDetailHeight(position);
                    m_placementInfos[ITEMWISE_INDEX].m_t = m_hoverItemwiseT;
                    m_placementInfos[ITEMWISE_INDEX].m_position = position;
                    break;
                default:
                    m_itemCount = 0;
                    return false;
                }
            }
            return true;
        }

        public static bool IsVectorXZPositionChanging(Vector3 oldPosition, Vector3 newPosition, float tolerance) {
            float sqrMagnitudeThreshold = tolerance * tolerance;
            oldPosition.y = 0f;
            newPosition.y = 0f;
            if (Vector3.SqrMagnitude(newPosition - oldPosition) > sqrMagnitudeThreshold) {
                return true;
            }
            return false;
        }

        public static bool IsCurveLengthLongEnoughXZ() {
            switch (m_drawMode) {
            case PLTDrawMode.Straight:
                if (m_fenceMode) return m_mainSegment.LengthXZ() >= 0.75f * m_spacingSingle;
                return m_mainSegment.LengthXZ() >= m_spacingSingle;
            case PLTDrawMode.Curved:
            case PLTDrawMode.Freeform:
                if (m_fenceMode) return (m_mainBezier.d - m_mainBezier.a).magnitude >= m_spacingSingle;
                return PLTMath.CubicBezierArcLengthXZGauss12(m_mainBezier, 0f, 1f) >= m_spacingSingle;
            case PLTDrawMode.Circle:
                if (m_fenceMode) return m_mainCircle.Diameter >= m_spacingSingle;
                return m_mainCircle.Circumference >= m_spacingSingle;
            }
            return false;
        }

        private static bool CalculateAllPositionsBySpacing(float spacing, float initialOffset, Vector3 lastFenceEndpoint) {
            int numItems;
            int numItemsRaw = 0;
            float initialT;
            float finalT;
            float deltaT;
            initialOffset = Mathf.Abs(initialOffset);
            if (spacing == 0 || IsCurveLengthLongEnoughXZ()) {
                m_itemCount = 0;
                return false;
            }
            if (m_fenceMode) {   //FenceMode = ON
                switch (m_drawMode) {
                // ====== STRAIGHT FENCE ======
                case PLTDrawMode.Straight:
                    float lengthFull = m_mainSegment.LengthXZ();
                    float speed = PLTMath.LinearSpeedXZ(m_mainSegment);
                    float lengthAfterFirst = m_segmentState.IsMaxFillContinue ? lengthFull - initialOffset : lengthFull;
                    float numItemsFloat = Mathf.Abs(lengthAfterFirst / spacing);
                    numItemsRaw = Mathf.FloorToInt(numItemsFloat);
                    numItems = Mathf.Min(m_itemCount, Mathf.Clamp(numItemsRaw, 0, MAX_ITEM_ARRAY_LENGTH));
                    //add an extra item at the end if within 75% of spacing
                    bool extraItem = false;
                    float remainder = lengthAfterFirst % spacing;
                    float remFraction = remainder / spacing;
                    if (remFraction >= 0.75f && numItems < MAX_ITEM_ARRAY_LENGTH) {
                        numItems += 1;
                        extraItem = true;
                    }
                    //If not MaxFillContinue:
                    //In straight fence mode, no segment-linking occurs
                    //   so we don't use initialOffset here
                    //the continuous draw resets the first control point to the last fence endpoint
                    deltaT = spacing / speed;
                    float t = 0f;
                    Vector3 position;
                    //Max Fill Continue
                    if (m_segmentState.IsMaxFillContinue && initialOffset > 0f) {
                        t = initialOffset / lengthFull;
                    }
                    //calculate endpoints TODO: Optimize this
                    for (int i = 0; i < numItems + 1; i++) {
                        position = PLTMath.LinePosition(m_mainSegment, t);
                        m_fenceEndPoints[i] = position;
                        t += deltaT;
                    }
                    for (int i = 0; i < numItems; i++) {
                        m_placementInfos[i].m_position = Vector3.Lerp(m_fenceEndPoints[i], m_fenceEndPoints[i + 1], 0.50f);
                    }
                    //linear fence fill
                    bool realizedLinearFenceFill = false;
                    if (PLTSettings.LinearFenceFill) {
                        //check conditions first
                        if (numItems > 0 && numItems < MAX_ITEM_ARRAY_LENGTH) {
                            if (numItems == 1 && lengthFull > spacing) {
                                realizedLinearFenceFill = true;
                            } else {
                                realizedLinearFenceFill = true;
                            }
                        }
                        //if conditions for linear fence fill are met
                        if (realizedLinearFenceFill) {
                            //account for extra item
                            if (!extraItem) {
                                numItems++;
                            }
                            Vector3 p0 = m_mainSegment.a;
                            Vector3 p1 = m_mainSegment.b;
                            p0.y = 0f;
                            p1.y = 0f;
                            m_fenceEndPoints[numItems] = p1;
                            Vector3 localX = (p1 - p0).normalized;
                            Vector3 localZ = new Vector3(localX.z, 0f, -1f * localX.x);
                            Vector3 finalFenceMidpoint = p1 + (0.5f * spacing) * ((p0 - p1).normalized);
                            finalFenceMidpoint += (0.00390625f * localX) + (0.00390625f * localZ);    //correct for z-fighting
                            m_placementInfos[numItems - 1].m_position = finalFenceMidpoint;
                        }
                    }
                    finalT = t - deltaT;
                    Vector3 finalPos = PLTMath.LinePosition(m_mainSegment, finalT);
                    if (m_segmentState.IsReadyForMaxContinue) {
                        m_segmentState.m_newFinalOffset = Vector3.Distance(m_mainSegment.a, finalPos);
                    } else {
                        m_segmentState.m_newFinalOffset = Vector3.Distance(finalPos, m_mainSegment.b);
                    }
                    break;
                case PLTDrawMode.Curved:
                case PLTDrawMode.Freeform:
                    //check if curve is unwieldly
                    //check if curve is too tight for convergence
                    if (m_mainElbowAngle * Mathf.Rad2Deg < 5f) {    //if elbow angle is less than 5 degrees
                        m_itemCount = 0;
                        return false;
                    }
                    lengthFull = PLTMath.CubicBezierArcLengthXZGauss12(m_mainBezier, 0f, 1f);
                    lengthAfterFirst = m_segmentState.IsMaxFillContinue ? lengthFull - initialOffset : lengthFull;
                    numItemsRaw = Mathf.CeilToInt(lengthAfterFirst / spacing);
                    numItems = Mathf.Min(m_itemCount, Mathf.Clamp(numItemsRaw, 0, MAX_ITEM_ARRAY_LENGTH));
                    if (spacing > lengthFull) {
                        m_itemCount = 0;
                        return false;
                    }
                    t = 0f;
                    float penultimateT = 0f;
                    int forLoopStart = 0;
                    position = lastFenceEndpoint;
                    //max fill continue
                    if (m_segmentState.IsMaxFillContinue && initialOffset > 0f) {
                        forLoopStart = 0;
                        PLTMath.StepDistanceCurve(m_mainBezier, 0f, initialOffset, m_tolerance, out t);
                        goto endpointsForLoop;
                    } else if (initialOffset > 0f && lastFenceEndpoint != Vector3.down) { //link curves in continuous draw
                        m_fenceEndPoints[0] = lastFenceEndpoint;
                        if (!PLTMath.LinkCircleCurveFenceIntersectXZ(m_mainBezier, lastFenceEndpoint, spacing, m_tolerance, out t, false)) {
                            //could not link segments, so start at t = 0 instead
                            forLoopStart = 0;
                            t = 0f;
                            goto endpointsForLoop;
                        }
                        m_fenceEndPoints[1] = m_mainBezier.Position(t);
                        //float tFirstFencepoint = t;
                        //fourth continueDrawing if (4/4)
                        if (!PLTMath.CircleCurveFenceIntersectXZ(m_mainBezier, t, spacing, m_tolerance, out t, false)) {
                            //failed to converge
                            numItems = 1;
                            goto endpointsFinish;
                        }
                        forLoopStart = 2;
                    }
endpointsForLoop:
                    for (int i = forLoopStart; i < numItems + 1; i++) {
                        //this should be the first if (1/3)
                        //this is necessary for bendy fence mode since we didn't estimate count
                        if (t > 1f) {
                            numItems = i - 1;
                            goto endpointsFinish;
                        }
                        //second if (2/3)
                        m_fenceEndPoints[i] = m_mainBezier.Position(t);
                        penultimateT = t;
                        //third if (3/3)
                        if (!PLTMath.CircleCurveFenceIntersectXZ(m_mainBezier, t, spacing, m_tolerance, out t, false)) {
                            //failed to converge
                            numItems = i - 1;
                            goto endpointsFinish;
                        }
                    }
endpointsFinish:
                    numItems = Mathf.Clamp(numItems, 0, MAX_ITEM_ARRAY_LENGTH);
                    finalT = t;
                    for (int i = 0; i < numItems; i++) {
                        m_placementInfos[i].m_position = Vector3.Lerp(GetFenceEndpoint(i), GetFenceEndpoint(i + 1), 0.50f); // Set midpoint
                    }
                    //prep for MaxFillContinue
                    if (m_segmentState.IsReadyForMaxContinue) {
                        m_segmentState.m_newFinalOffset = PLTMath.CubicBezierArcLengthXZGauss12(m_mainBezier, 0f, penultimateT);
                    } else {
                        m_segmentState.m_newFinalOffset = PLTMath.CubicBezierArcLengthXZGauss04(m_mainBezier, finalT, 1f);
                    }
                    break;
                case PLTDrawMode.Circle:
                    Circle3XZ mainCircle = m_mainCircle;
                    float chordAngle = mainCircle.ChordAngle(spacing);
                    if (chordAngle <= 0f || chordAngle > Mathf.PI || mainCircle.m_radius <= 0f) {
                        numItems = 0;
                        break;
                    }
                    float angleFull = 2f * Mathf.PI;
                    float initialAngle = Mathf.Abs(initialOffset) / mainCircle.m_radius;
                    float angleAfterFirst = m_segmentState.IsMaxFillContinue ? angleFull - initialAngle : angleFull;
                    if (PLTSettings.PerfectCircles) {
                        numItemsRaw = Mathf.RoundToInt(angleAfterFirst / chordAngle);
                        numItems = Mathf.Min(m_itemCount, Mathf.Clamp(numItemsRaw, 0, MAX_ITEM_ARRAY_LENGTH));
                    } else {
                        numItemsRaw = Mathf.FloorToInt(angleAfterFirst / chordAngle);
                        numItems = Mathf.Min(m_itemCount, Mathf.Clamp(numItemsRaw, 0, MAX_ITEM_ARRAY_LENGTH));
                    }
                    deltaT = m_mainCircle.ChordDeltaT(spacing);
                    //If No MaxFillContinue:
                    //In circle fence mode, no segment-linking occurs
                    //   so we don't use initialOffset here
                    t = 0f;
                    penultimateT = 0f;
                    //Max Fill Continue
                    if (m_segmentState.IsMaxFillContinue && initialOffset > 0f) {
                        t = m_mainCircle.DeltaT(initialOffset);
                        penultimateT = t;
                    }
                    position = mainCircle.Position(t);
                    Vector3 center = mainCircle.m_center;
                    Vector3 radiusVector = position - center;
                    Quaternion rotation = Quaternion.AngleAxis(-1f * chordAngle * Mathf.Rad2Deg, Vector3.up);
                    for (int i = 0; i < numItems + 1; i++) {
                        penultimateT = t;
                        m_fenceEndPoints[i] = position;
                        radiusVector = rotation * radiusVector;
                        position = center + radiusVector;
                        t += deltaT;
                    }
                    for (int i = 0; i < numItems; i++) {
                        m_placementInfos[i].m_position = Vector3.Lerp(m_fenceEndPoints[i], m_fenceEndPoints[i + 1], 0.50f); // set midpoints
                    }
                    if (m_segmentState.IsReadyForMaxContinue) {
                        m_segmentState.m_newFinalOffset = m_mainCircle.ArclengthBetween(0f, penultimateT);
                    } else {
                        m_segmentState.m_newFinalOffset = m_mainCircle.ArclengthBetween(t, 1f);
                    }
                    break;
                default:
                    numItems = 0;
                    break;
                }
                if (numItems > 0 && numItems <= MAX_ITEM_ARRAY_LENGTH) {
                    m_segmentState.m_newFenceEndpoint = m_fenceEndPoints[numItems];
                } else {
                    m_itemCount = 0;
                    return false;
                }
            } else {  //Non-fence mode
                switch (m_drawMode) {
                case PLTDrawMode.Straight:
                    Vector3 position;
                    float lengthFull = m_mainSegment.LengthXZ();
                    float lengthAfterFirst = lengthFull - initialOffset;
                    float speed = PLTMath.LinearSpeedXZ(m_mainSegment);
                    //use ceiling for non-fence, because the point at the beginning is an extra point
                    numItemsRaw = Mathf.CeilToInt(lengthAfterFirst / spacing);
                    numItems = Mathf.Min(m_itemCount, Mathf.Clamp(numItemsRaw, 0, MAX_ITEM_ARRAY_LENGTH));
                    if (speed == 0) {
                        return false;
                    }
                    deltaT = spacing / speed;
                    float t = 0f;
                    if (initialOffset > 0f) {
                        initialT = initialOffset / speed;
                        t = initialT;
                    }
                    for (int i = 0; i < numItems; i++) {
                        position = PLTMath.LinePosition(m_mainSegment, t);
                        position.y = Singleton<TerrainManager>.instance.SampleDetailHeight(position);
                        m_placementInfos[i].m_t = t;
                        m_placementInfos[i].m_position = position;
                        t += deltaT;
                    }
                    if (!GetItemPosition(numItems - 1, out Vector3 finalPos)) {
                        ResetLastContinueParameters();
                    } else {
                        if (m_segmentState.IsReadyForMaxContinue) {
                            m_segmentState.m_newFinalOffset = spacing + Vector3.Distance(m_mainSegment.a, finalPos);
                        } else {
                            m_segmentState.m_newFinalOffset = spacing - Vector3.Distance(finalPos, m_mainSegment.b);
                        }
                    }
                    break;
                case PLTDrawMode.Curved:
                case PLTDrawMode.Freeform:
                    if (m_mainArm1.Length() + m_mainArm2.Length() <= 0.01f) {
                        return false;
                    }
                    lengthFull = PLTMath.CubicBezierArcLengthXZGauss12(m_mainBezier, 0f, 1f);
                    lengthAfterFirst = lengthFull - initialOffset;
                    //use ceiling for non-fence, because the point at the beginning is an extra point
                    numItemsRaw = Mathf.CeilToInt(lengthAfterFirst / spacing);
                    numItems = Mathf.Min(m_itemCount, Mathf.Clamp(numItemsRaw, 0, MAX_ITEM_ARRAY_LENGTH));
                    t = 0f;
                    if (initialOffset > 0f) {
                        PLTMath.StepDistanceCurve(m_mainBezier, 0f, initialOffset, m_tolerance, out t);
                    }
                    for (int i = 0; i < numItems; i++) {
                        position = m_mainBezier.Position(t);
                        position.y = Singleton<TerrainManager>.instance.SampleDetailHeight(position);
                        m_placementInfos[i].m_t = t;
                        m_placementInfos[i].m_position = position;
                        PLTMath.StepDistanceCurve(m_mainBezier, t, spacing, m_tolerance, out t);
                    }
                    if (!GetItemT(numItems - 1, out finalT)) {
                        ResetLastContinueParameters();
                    } else {
                        if (m_segmentState.IsReadyForMaxContinue) {
                            m_segmentState.m_newFinalOffset = spacing + PLTMath.CubicBezierArcLengthXZGauss12(m_mainBezier, 0f, finalT);
                        } else {
                            m_segmentState.m_newFinalOffset = spacing - PLTMath.CubicBezierArcLengthXZGauss04(m_mainBezier, finalT, 1f);
                        }
                    }
                    break;
                case PLTDrawMode.Circle:
                    Circle3XZ mainCircle = m_mainCircle;
                    deltaT = mainCircle.DeltaT(spacing);
                    if (deltaT <= 0f || deltaT > 1f || mainCircle.m_radius <= 0f) {
                        numItems = 0;
                        break;
                    }
                    t = 0f;
                    float remainingSpace = mainCircle.Circumference;
                    if (m_segmentState.IsMaxFillContinue) {
                        if (remainingSpace > 0f) {
                            t = initialOffset / remainingSpace;
                            remainingSpace -= initialOffset;
                        } else {
                            numItems = 0;
                            break;
                        }
                    }
                    //use ceiling for non-fence, because the point at the beginning is an extra point
                    numItemsRaw = Mathf.CeilToInt(remainingSpace / spacing);
                    numItems = Mathf.Min(m_itemCount, Mathf.Clamp(numItemsRaw, 0, MAX_ITEM_ARRAY_LENGTH));
                    position = mainCircle.Position(t);
                    Vector3 center = mainCircle.m_center;
                    Vector3 radiusVector = position - center;
                    float deltaAngle = m_mainCircle.DeltaAngle(spacing);
                    Quaternion rotation = Quaternion.AngleAxis(-1f * deltaAngle * Mathf.Rad2Deg, Vector3.up);
                    SampleDetailHeight CalcHeight = Singleton<TerrainManager>.instance.SampleDetailHeight;
                    for (int i = 0; i < numItems; i++) {
                        position.y = CalcHeight(position);
                        m_placementInfos[i].m_t = t;
                        m_placementInfos[i].m_position = position;
                        radiusVector = rotation * radiusVector;
                        position = center + radiusVector;
                        t += deltaT;
                    }
                    if (!GetItemT(numItems - 1, out finalT)) {
                        ResetLastContinueParameters();
                    } else {
                        if (m_segmentState.IsReadyForMaxContinue) {
                            m_segmentState.m_newFinalOffset = spacing + m_mainCircle.ArclengthBetween(0f, finalT);
                        } else {
                            m_segmentState.m_newFinalOffset = m_mainCircle.ArclengthBetween(t, 1f);
                        }
                    }
                    break;
                default:
                    numItems = 0;
                    break;
                }
            }
            //re-set item count
            m_itemCount = numItems;
            //flag if not enough item slots
            if (Mathf.FloorToInt(numItemsRaw) > MAX_ITEM_ARRAY_LENGTH) {
                m_segmentState.m_maxItemCountExceeded = true;
            } else {
                m_segmentState.m_maxItemCountExceeded = false;
            }
            return true;
        }

    }
}
