namespace VoxelMagic.Voice
{
    /// <summary>STT 엔진의 진행 단계. 대기 → 녹음 중 → 변환 중 → 완료 / 실패.</summary>
    public enum SttState
    {
        Idle = 0,
        Listening = 1,
        Transcribing = 2,
        Completed = 3,
        Failed = 4,
    }
}
