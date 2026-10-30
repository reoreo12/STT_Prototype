// 주문 시스템에 전달되는 입력 한 건과 그 출처 종류를 정의한다.

using System;

namespace VoxelMagic.Voice
{
    /// <summary>주문 입력 출처. 이번에 구현하는 것은 Voice뿐이다.</summary>
    public enum SpellInputKind
    {
        Voice = 0,
        Hud = 1,
        MagicCircle = 2,
    }

    /// <summary>출처와 무관한 주문 입력 한 건. 만든 뒤에는 바뀌지 않는다.</summary>
    public sealed class SpellInput
    {
        public SpellInput(string text, SpellInputKind kind, SttResult sttResult, DateTime timestamp)
        {
            Text = text ?? "";
            Kind = kind;
            SttResult = sttResult;
            Timestamp = timestamp;
        }

        public string Text { get; }
        public SpellInputKind Kind { get; }

        /// <summary>음성 입력일 때의 변환 결과. 다른 출처면 null.</summary>
        public SttResult SttResult { get; }

        public DateTime Timestamp { get; }
    }
}
