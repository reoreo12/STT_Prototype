// 엔진 드롭다운 항목 하나(엔진 종류 + 모델)를 나타내는 값 타입을 담는다.

using System;

namespace VoxelMagic.Voice
{
    /// <summary>엔진 선택 항목 하나: 엔진 종류 + 모델 이름. 같은 종류라도 모델이 다르면 다른 항목이다.</summary>
    public readonly struct SttEngineOption : IEquatable<SttEngineOption>
    {
        public SttEngineOption(SttEngineType type, string model, string displayName, bool isImplemented)
        {
            Type = type;
            Model = model ?? "";
            DisplayName = displayName ?? type.ToString();
            IsImplemented = isImplemented;
        }

        public SttEngineType Type { get; }
        public string Model { get; }
        public string DisplayName { get; }

        /// <summary>이 프로젝트에서 실제로 만들 수 있는지 (패키지 설치 여부 등).</summary>
        public bool IsImplemented { get; }

        public bool Equals(SttEngineOption other)
        {
            return Type == other.Type && Model == other.Model;
        }

        public override bool Equals(object obj)
        {
            return obj is SttEngineOption other && Equals(other);
        }

        public override int GetHashCode()
        {
            return ((int)Type * 397) ^ Model.GetHashCode();
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
