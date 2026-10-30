using System.Collections.Generic;

namespace VoxelMagic.Voice
{
    /// <summary>
    /// 한 번의 음성 변환 결과. 만든 뒤에는 바뀌지 않는다.
    /// 시간 값은 모두 ms이며, 측정할 수 없는 값은 -1이다.
    /// </summary>
    public sealed class SttResult
    {
        private readonly string[] hypotheses;

        public SttResult(
            string engineName,
            string modelName,
            string computeBackend,
            string micDevice,
            string rawText,
            string normalizedText,
            IReadOnlyList<string> hypotheses,
            string error,
            double recordMs,
            double transcribeMs,
            double totalLatencyMs,
            double audioSec,
            double realtimeFactor,
            double engineStartMs,
            bool usedHypothesisFallback,
            AudioSamples audio)
        {
            EngineName = engineName ?? "";
            ModelName = modelName ?? "";
            ComputeBackend = computeBackend ?? "";
            MicDevice = micDevice ?? "";
            RawText = rawText ?? "";
            NormalizedText = normalizedText ?? "";
            this.hypotheses = hypotheses == null ? new string[0] : new List<string>(hypotheses).ToArray();
            Error = error ?? "";
            RecordMs = recordMs;
            TranscribeMs = transcribeMs;
            TotalLatencyMs = totalLatencyMs;
            AudioSec = audioSec;
            RealtimeFactor = realtimeFactor;
            EngineStartMs = engineStartMs;
            UsedHypothesisFallback = usedHypothesisFallback;
            Audio = audio;
        }

        public string EngineName { get; }
        public string ModelName { get; }

        /// <summary>"GPU", "CPU" 또는 빈 문자열 (엔진의 ComputeBackend).</summary>
        public string ComputeBackend { get; }

        public string MicDevice { get; }

        /// <summary>엔진이 돌려준 원문.</summary>
        public string RawText { get; }

        /// <summary>공백·문장부호를 제거한 텍스트. 판정에 쓴다.</summary>
        public string NormalizedText { get; }

        /// <summary>변환 중에 받은 중간 결과 목록 (엔진이 제공하는 경우).</summary>
        public IReadOnlyList<string> Hypotheses => hypotheses;

        /// <summary>실패 이유. 성공이면 빈 문자열.</summary>
        public string Error { get; }

        public bool Succeeded => Error.Length == 0;

        /// <summary>누른 순간 → 뗀 순간.</summary>
        public double RecordMs { get; }

        /// <summary>뗀 순간 → 최종 결과.</summary>
        public double TranscribeMs { get; }

        /// <summary>누른 순간 → 최종 결과.</summary>
        public double TotalLatencyMs { get; }

        /// <summary>음성 길이(초). 녹음 샘플이 없으면 녹음 시간으로 대신한다.</summary>
        public double AudioSec { get; }

        /// <summary>변환 시간 ÷ 음성 길이.</summary>
        public double RealtimeFactor { get; }

        /// <summary>StartListening 호출 → 엔진이 실제로 듣기 시작한 시점. 모르면 -1.</summary>
        public double EngineStartMs { get; }

        /// <summary>최종 결과 대신 마지막 중간 결과를 썼는지.</summary>
        public bool UsedHypothesisFallback { get; }

        /// <summary>녹음 샘플. 엔진이 제공하지 않으면 null.</summary>
        public AudioSamples Audio { get; }
    }
}
