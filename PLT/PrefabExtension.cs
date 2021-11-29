using UnityEngine;

namespace PropAnarchy.PLT {
    public static class PrefabExtensions {
        private const float CENTER_AREA_FRACTION = 0.00390625f;
        private static bool IsCenterAreaSignificant(Mesh mesh, out Vector3 centerCorrectionOrtho) {
            if (!(mesh is null)) {
                VectorXZ center = mesh.bounds.center;
                VectorXZ size = mesh.bounds.size;
                if (center.sqrMagnitude >= CENTER_AREA_FRACTION * size.sqrMagnitude) {
                    centerCorrectionOrtho = -center;    //negate center vector
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
