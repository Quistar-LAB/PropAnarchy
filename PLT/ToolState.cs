namespace PropAnarchy.PLT {
    public class ToolState {
        public enum PLTActiveState : int {
            Undefined = 0,
            CreatePointFirst = 1,
            CreatePointSecond = 2,
            CreatePointThird = 3,
            LockIdle = 10,
            MovePointFirst = 11,
            MovePointSecond = 12,
            MovePointThird = 13,
            MoveSegment = 14,
            ChangeSpacing = 15,
            ChangeAngle = 16,
            ItemwiseLock = 30,
            MoveItemwiseItem = 31,
            MaxFillContinue = 40
        }

        private PLTActiveState m_currentState;

        public ToolState() {
            m_currentState = PLTActiveState.CreatePointFirst;
        }

        public void StartPlacement() {


            m_currentState = PLTActiveState.CreatePointSecond;
        }
    }
}
