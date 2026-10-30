using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VoxelMagic.Voice
{
    /// <summary>
    /// 설정값으로 엔진 선택 항목(Options)을 만들고, 선택한 항목의 엔진을 생성한다.
    /// 엔진을 추가할 때 고치는 곳은 여기와 SttEngineType뿐이다.
    /// </summary>
    public sealed class SttEngineFactory
    {
        private readonly SttSettings settings;
        private readonly ITimeSource time;
        private readonly List<SttEngineOption> options = new List<SttEngineOption>();

        public SttEngineFactory(SttSettings settings, ITimeSource time)
        {
            this.settings = settings ? settings : throw new ArgumentNullException(nameof(settings));
            this.time = time ?? throw new ArgumentNullException(nameof(time));

            options.Add(new SttEngineOption(SttEngineType.WindowsDictation, "", "엔진 A: Windows 받아쓰기", true));
#if VOICESTT_WHISPER
            foreach (string model in settings.WhisperModels)
            {
                if (string.IsNullOrWhiteSpace(model)) continue;
                options.Add(new SttEngineOption(SttEngineType.Whisper, model,
                    "엔진 B: Whisper " + ShortModelName(model) + (settings.WhisperUseGpu ? " · GPU" : " · CPU"), true));
            }
#else
            options.Add(new SttEngineOption(SttEngineType.Whisper, "", "엔진 B: Whisper (패키지 없음)", false));
#endif
        }

        public IReadOnlyList<SttEngineOption> Options => options;

        /// <summary>설정의 engineType·whisperModel에 맞는 항목. 없거나 만들 수 없으면 첫 번째 항목.</summary>
        public SttEngineOption DefaultOption
        {
            get
            {
                string model = settings.EngineType == SttEngineType.Whisper ? settings.WhisperModel : "";
                foreach (SttEngineOption option in options)
                {
                    if (option.IsImplemented && option.Type == settings.EngineType && option.Model == model) return option;
                }
                return options[0];
            }
        }

        public ISpeechToText Create(SttEngineOption option)
        {
            if (!option.IsImplemented) throw new NotSupportedException($"{option.DisplayName}은(는) 이 프로젝트에서 사용할 수 없습니다.");

            switch (option.Type)
            {
                case SttEngineType.WindowsDictation:
                    return new WindowsDictationStt(
                        time,
                        settings.MaxRecordSeconds,
                        settings.TranscribeTimeoutSeconds,
                        settings.DictationFinalResultWaitMs,
                        settings.DictationUseUnityMicrophone ? new MicrophoneRecorder(settings.MaxRecordSeconds) : null);

#if VOICESTT_WHISPER
                case SttEngineType.Whisper:
                    return new WhisperStt(
                        time,
                        settings.MaxRecordSeconds,
                        settings.TranscribeTimeoutSeconds,
                        ModelPath(option.Model),
                        option.Model,
                        settings.Language,
                        settings.WhisperUseGpu,
                        settings.WhisperUseInitialPrompt ? string.Join(", ", settings.TargetPhrases) : "",
                        settings.MinSpeechLevel,
                        settings.MinAudioSeconds,
                        settings.KeepMicrophoneOpen,
                        settings.PreRollSeconds,
                        settings.PostRollSeconds,
                        new MicrophoneRecorder(settings.MaxRecordSeconds + settings.PreRollSeconds + settings.PostRollSeconds));
#endif

                default:
                    throw new NotSupportedException($"{option.Type} 엔진은 아직 구현되지 않았습니다.");
            }
        }

        /// <summary>StreamingAssets 아래 모델 파일의 절대 경로.</summary>
        public string ModelPath(string model)
        {
            return Path.Combine(Application.streamingAssetsPath, settings.WhisperModelFolder, model + ".bin");
        }

        private static string ShortModelName(string model)
        {
            return model.StartsWith("ggml-", StringComparison.Ordinal) ? model.Substring(5) : model;
        }
    }
}
