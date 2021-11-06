using UnityEngine;

namespace PropAnarchy.PLT {
    public static class PrefabExtensions {
        private const float CENTER_AREA_FRACTION = 0.00390625f;
        private static bool IsCenterAreaSignificant(Mesh mesh, out Vector3 centerCorrectionOrtho) {
            if (!(mesh is null)) {
                Vector3 center = mesh.bounds.center;
                Vector3 size = mesh.bounds.size;
                if (center.SqrMagnitudeXZ() >= CENTER_AREA_FRACTION * size.SqrMagnitudeXZ()) {
                    //negate center vector
                    centerCorrectionOrtho = -center;
                    centerCorrectionOrtho.y = 0f;
                    return true;
                }
            }
            centerCorrectionOrtho = default;
            return false;
        }

        public static bool IsMeshCenterOffset(this PropInfo prop, out Vector3 centerCorrectionOrthogonal) => IsCenterAreaSignificant(prop.m_mesh, out centerCorrectionOrthogonal);

        public static bool IsMeshCenterOffset(this TreeInfo tree, out Vector3 centerCorrectionOrthogonal) => IsCenterAreaSignificant(tree.m_mesh, out centerCorrectionOrthogonal);
    }
}
