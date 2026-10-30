// 설정과 팩토리에서 고르는 엔진 종류 목록을 정의한다.

namespace VoxelMagic.Voice
{
    /// <summary>
    /// 사용할 STT 엔진 종류. 새 엔진을 추가하면 여기와 SttEngineFactory에 한 줄씩 추가한다.
    /// </summary>
    public enum SttEngineType
    {
        /// <summary>엔진 A: Windows 내장 받아쓰기 (DictationRecognizer).</summary>
        WindowsDictation = 0,

        /// <summary>엔진 B: Whisper 로컬 실행 (승인 후 구현).</summary>
        Whisper = 1,
    }
}
