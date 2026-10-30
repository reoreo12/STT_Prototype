// 음성 외 입력(HUD·마법진)도 같은 방식으로 주문 시스템에 연결하기 위한 입력 출처 계약을 정의한다.

using System;

namespace VoxelMagic.Voice
{
    /// <summary>
    /// 주문 입력 출처(음성 / HUD / 마법진)를 같은 방식으로 다루기 위한 계약.
    /// 게임의 주문 시스템은 이 인터페이스만 구독하고 ISpellMatcher로 판정한다.
    /// </summary>
    public interface ISpellInputSource
    {
        SpellInputKind Kind { get; }

        /// <summary>지금 이 출처를 쓸 수 있는지. 음성은 마이크·OS 지원 여부.</summary>
        bool IsAvailable { get; }

        event Action<SpellInput> SpellInputReceived;
        event Action<bool> AvailabilityChanged;
    }
}
