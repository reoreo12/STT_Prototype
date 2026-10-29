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
