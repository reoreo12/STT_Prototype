// 누르고 말하기 입력의 공통 계약을 정의한다. 키보드·화면 버튼 등 입력 장치를 바꿔도 음성 입력 흐름은 그대로 둔다.

using System;

namespace VoxelMagic.Voice
{
    /// <summary>누르고 말하기 입력. 누르면 Pressed, 떼면 Released.</summary>
    public interface IPushToTalkSource
    {
        bool IsHeld { get; }

        event Action Pressed;
        event Action Released;
    }
}
