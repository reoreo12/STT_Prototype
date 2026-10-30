// STT 엔진 교체의 기준이 되는 인터페이스를 정의한다. 게임과 UI는 이것만 참조해 엔진 A/B를 바꿔 끼운다.

using System;
using System.Threading.Tasks;

namespace VoxelMagic.Voice
{
    /// <summary>
    /// STT 엔진의 공통 계약. UI와 게임 코드는 이 인터페이스만 알고, 어떤 엔진인지는 모른다.
    /// 모든 이벤트는 메인 스레드에서 발생한다.
    /// </summary>
    public interface ISpeechToText : IDisposable
    {
        string EngineName { get; }
        string ModelName { get; }

        /// <summary>연산 장치 설정 ("GPU", "CPU"). 해당 없는 엔진은 빈 문자열. 실제 장치가 아니라 요청한 값이다.</summary>
        string ComputeBackend { get; }

        /// <summary>마이크·OS 지원 여부. false면 게임은 HUD 모드로 넘어가면 된다.</summary>
        bool IsAvailable { get; }
        string UnavailableReason { get; }

        /// <summary>마이크 장치를 직접 고를 수 있는 엔진인지. (엔진 A는 Windows 기본 마이크만 사용)</summary>
        bool SupportsDeviceSelection { get; }

        SttState State { get; }
        SttResult LastResult { get; }

        /// <summary>녹음 중일 때 누른 뒤 지난 시간. 그 외에는 0.</summary>
        double ListeningElapsedMs { get; }

        /// <summary>변환 중일 때 뗀 뒤 지난 시간. 그 외에는 0.</summary>
        double TranscribingElapsedMs { get; }

        /// <summary>입력 음량 0~1. 지원하지 않으면 -1.</summary>
        float InputLevel { get; }

        event Action<SttState> StateChanged;
        event Action<string> HypothesisReceived;
        event Action<SttResult> Completed;

        /// <summary>앞으로 쓸 마이크 장치를 알린다. 마이크를 미리 열어 두는 엔진은 이때 연다.</summary>
        void SetPreferredDevice(string deviceName);

        void StartListening(string deviceName);
        Task<SttResult> StopAndTranscribeAsync();
        void Cancel();

        /// <summary>매 프레임 호출. 시간 제한, 음량 갱신 등을 처리한다.</summary>
        void Tick();

        void RefreshAvailability();
    }
}
