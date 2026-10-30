// 누르고 말하기 입력과 STT 엔진을 이어 음성 주문 입력을 만든다. 게임 이식 시 주문 시스템이 구독하는 진입점이다.

using System;
using System.Collections.Generic;

namespace VoxelMagic.Voice
{
    /// <summary>
    /// 음성 주문 입력. 누르고 말하기 입력과 STT 엔진을 연결한다.
    /// 엔진의 소유권을 가지며, 엔진을 바꾸면 이전 엔진을 Dispose한다.
    /// </summary>
    public sealed class VoiceSpellInputSource : ISpellInputSource, IDisposable
    {
        private readonly List<IPushToTalkSource> triggers; // 구독 중인 누르고 말하기 입력들. 모두 떼야 변환을 시작한다
        private ISpeechToText engine; // 현재 소유한 STT 엔진
        private string micDevice = ""; // 엔진에 넘길 마이크 장치 이름 (빈 문자열이면 기본 장치)
        private bool lastAvailability; // 마지막으로 알린 사용 가능 여부 (변화 감지용)
        private bool disposed; // Dispose 호출 여부

        public VoiceSpellInputSource(ISpeechToText engine, IEnumerable<IPushToTalkSource> triggers)
        {
            this.triggers = new List<IPushToTalkSource>();
            if (triggers != null)
            {
                foreach (IPushToTalkSource trigger in triggers)
                {
                    if (trigger == null) continue;
                    trigger.Pressed += OnPressed;
                    trigger.Released += OnReleased;
                    this.triggers.Add(trigger);
                }
            }
            Attach(engine ?? throw new ArgumentNullException(nameof(engine)));
        }

        public event Action<SpellInput> SpellInputReceived;
        public event Action<bool> AvailabilityChanged;

        /// <summary>성공·실패와 관계없이 모든 변환 결과. (기록·디버그용)</summary>
        public event Action<SttResult> TranscriptionCompleted;

        /// <summary>엔진이 바뀌면 새 엔진을 알린다. UI는 여기서 다시 구독한다.</summary>
        public event Action<ISpeechToText> EngineChanged;

        public SpellInputKind Kind => SpellInputKind.Voice;
        public ISpeechToText Engine => engine;
        public string MicDevice => micDevice;
        public bool IsAvailable => engine.IsAvailable;
        public bool IsBusy => engine.State == SttState.Listening || engine.State == SttState.Transcribing;

        public void SetMicDevice(string deviceName)
        {
            micDevice = deviceName ?? "";
            engine.SetPreferredDevice(micDevice);
        }

        /// <summary>엔진을 바꾼다. 녹음·변환 중이면 false.</summary>
        public bool SetEngine(ISpeechToText newEngine)
        {
            if (newEngine == null) throw new ArgumentNullException(nameof(newEngine));
            if (IsBusy) return false;

            ISpeechToText old = engine;
            Detach(old);
            old.Dispose();
            Attach(newEngine);
            EngineChanged?.Invoke(newEngine);
            return true;
        }

        public void RefreshAvailability()
        {
            engine.RefreshAvailability();
            NotifyAvailabilityIfChanged();
        }

        public void Tick()
        {
            if (disposed) return;
            engine.Tick();
            NotifyAvailabilityIfChanged();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            foreach (IPushToTalkSource trigger in triggers)
            {
                trigger.Pressed -= OnPressed;
                trigger.Released -= OnReleased;
            }
            triggers.Clear();
            Detach(engine);
            engine.Dispose();
        }

        private void Attach(ISpeechToText target)
        {
            engine = target;
            engine.Completed += OnEngineCompleted;
            engine.SetPreferredDevice(micDevice);
            lastAvailability = engine.IsAvailable;
            AvailabilityChanged?.Invoke(lastAvailability);
        }

        private void Detach(ISpeechToText target)
        {
            target.Completed -= OnEngineCompleted;
        }

        private void OnPressed()
        {
            if (disposed || IsBusy) return;
            engine.StartListening(micDevice);
            NotifyAvailabilityIfChanged();
        }

        private void OnReleased()
        {
            if (disposed || engine.State != SttState.Listening) return;
            foreach (IPushToTalkSource trigger in triggers)
            {
                if (trigger.IsHeld) return;
            }
            _ = engine.StopAndTranscribeAsync();
        }

        private void OnEngineCompleted(SttResult result)
        {
            TranscriptionCompleted?.Invoke(result);
            if (result.Succeeded)
            {
                SpellInputReceived?.Invoke(new SpellInput(result.RawText, SpellInputKind.Voice, result, DateTime.Now));
            }
        }

        private void NotifyAvailabilityIfChanged()
        {
            bool now = engine.IsAvailable;
            if (now == lastAvailability) return;
            lastAvailability = now;
            AvailabilityChanged?.Invoke(now);
        }
    }
}
