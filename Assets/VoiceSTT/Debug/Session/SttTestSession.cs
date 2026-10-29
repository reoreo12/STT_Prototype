using System;
using System.Collections.Generic;

namespace VoxelMagic.Voice.Debug
{
    /// <summary>
    /// 테스트 세션의 핵심 로직: 변환 결과 → 판정 → 기록 데이터 → 기록기 → 이벤트.
    /// UI는 이 클래스의 이벤트를 구독하고 공개 메서드만 호출한다.
    /// </summary>
    public sealed class SttTestSession : IDisposable
    {
        private readonly VoiceSpellInputSource voice;
        private readonly ISpellMatcher matcher;
        private readonly SttLogger logger;
        private readonly SttEngineFactory factory;
        private readonly string unityVersion;
        private readonly string buildType;
        private readonly List<SttAttemptRecord> attempts = new List<SttAttemptRecord>();
        private readonly SttSessionStats stats = new SttSessionStats();
        private int nextId = 1;

        public SttTestSession(VoiceSpellInputSource voice, ISpellMatcher matcher, SttLogger logger,
            SttEngineFactory factory, SttEngineOption initialEngine, string initialTarget, string unityVersion, string buildType)
        {
            CurrentEngineOption = initialEngine;
            this.voice = voice ?? throw new ArgumentNullException(nameof(voice));
            this.matcher = matcher ?? throw new ArgumentNullException(nameof(matcher));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            this.unityVersion = unityVersion ?? "";
            this.buildType = buildType ?? "";
            TargetPhrase = initialTarget ?? "";
            voice.TranscriptionCompleted += OnTranscriptionCompleted;
        }

        public event Action<SttAttemptRecord> AttemptRecorded;
        public event Action StatsChanged;
        public event Action AttemptsCleared;

        public string TargetPhrase { get; private set; }
        public SttEngineOption CurrentEngineOption { get; private set; }
        public float Threshold => matcher.Threshold;
        public IReadOnlyList<SttAttemptRecord> Attempts => attempts;
        public SttSessionStats Stats => stats;
        public bool IsLoggingEnabled => logger.IsEnabled;
        public string LogFolderPath => logger.FolderPath;
        public VoiceSpellInputSource Voice => voice;
        public SttEngineFactory EngineFactory => factory;

        public void SetTargetPhrase(string phrase)
        {
            TargetPhrase = phrase ?? "";
        }

        /// <summary>다음 시도부터 적용된다. 설정 에셋은 바꾸지 않는다.</summary>
        public void SetThreshold(float threshold)
        {
            matcher.SetThreshold(threshold);
        }

        /// <summary>엔진을 바꾼다. 미구현이거나 녹음·변환 중이면 false와 이유를 돌려준다.</summary>
        public bool TrySwitchEngine(SttEngineOption option, out string message)
        {
            if (option.Equals(CurrentEngineOption))
            {
                message = "";
                return true;
            }
            if (!option.IsImplemented)
            {
                message = $"{option.DisplayName}은(는) 이 프로젝트에서 사용할 수 없습니다.";
                return false;
            }
            if (voice.IsBusy)
            {
                message = "녹음·변환 중에는 엔진을 바꿀 수 없습니다.";
                return false;
            }

            // EngineChanged를 받는 쪽이 새 항목을 읽을 수 있도록 먼저 바꾼다.
            SttEngineOption previous = CurrentEngineOption;
            CurrentEngineOption = option;
            ISpeechToText engine = factory.Create(option);
            if (!voice.SetEngine(engine))
            {
                CurrentEngineOption = previous;
                engine.Dispose();
                message = "엔진 전환 실패";
                return false;
            }
            message = "";
            return true;
        }

        /// <summary>화면의 목록과 요약만 지운다. 기록 파일은 그대로 둔다.</summary>
        public void ClearAttempts()
        {
            attempts.Clear();
            stats.Clear();
            AttemptsCleared?.Invoke();
            StatsChanged?.Invoke();
        }

        public void Dispose()
        {
            voice.TranscriptionCompleted -= OnTranscriptionCompleted;
        }

        private void OnTranscriptionCompleted(SttResult result)
        {
            int id = nextId++;
            SpellMatchResult match = matcher.Match(result.NormalizedText, TargetPhrase);
            string wavFile = logger.SaveWav(id, result.Audio);
            SttAttemptRecord record = SttAttemptRecord.Create(id, DateTime.Now, result, TargetPhrase, match, wavFile, unityVersion, buildType);

            attempts.Add(record);
            stats.Add(record);
            logger.Write(record);

            AttemptRecorded?.Invoke(record);
            StatsChanged?.Invoke();
        }
    }
}
