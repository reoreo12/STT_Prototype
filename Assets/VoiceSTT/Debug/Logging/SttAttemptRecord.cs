// STT 시도 한 번의 결과·판정·시간 측정을 CSV와 JSONL이 함께 쓰는 한 줄 데이터로 묶는다.

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace VoxelMagic.Voice.Debug
{
    /// <summary>
    /// 기록 파일의 한 줄(시도 한 번). JsonUtility로 직렬화하기 위해 [SerializeField] private 필드를 쓴다.
    /// 만든 뒤에는 바뀌지 않는다.
    /// </summary>
    [Serializable]
    public sealed class SttAttemptRecord
    {
        // CSV 첫 줄의 열 이름. ToCsvFields가 돌려주는 값과 순서가 같아야 한다.
        public static readonly string[] CsvHeader =
        {
            "id", "timestamp", "engine", "model", "compute", "micDevice", "unityVersion", "buildType",
            "targetPhrase", "rawText", "normalizedText", "similarity", "passed", "threshold",
            "recordMs", "transcribeMs", "totalLatencyMs", "audioSec", "realtimeFactor",
            "engineStartMs", "usedHypothesisFallback", "hypotheses", "wavFile", "error",
        };

        [SerializeField] private int id; // 세션 안 시도 번호 (1부터)
        [SerializeField] private string timestamp; // 기록 시각 (ISO 8601, 밀리초·시간대 포함)
        [SerializeField] private string engine; // 엔진 이름
        [SerializeField] private string model; // 사용한 모델 이름
        [SerializeField] private string compute; // 연산 장치 "GPU"/"CPU", 해당 없으면 빈 문자열
        [SerializeField] private string micDevice; // 사용한 마이크 장치 이름
        [SerializeField] private string unityVersion; // 실행한 Unity 버전
        [SerializeField] private string buildType; // Editor / DevelopmentBuild / ReleaseBuild
        [SerializeField] private string targetPhrase; // 판정 기준이 된 목표 주문
        [SerializeField] private string rawText; // 엔진이 돌려준 원문
        [SerializeField] private string normalizedText; // 공백·문장부호를 뺀 판정용 텍스트
        [SerializeField] private float similarity; // 목표 주문과의 유사도 (0~1, 소수 4자리 반올림)
        [SerializeField] private bool passed; // 판정기가 성공으로 판정했는지
        [SerializeField] private float threshold; // 판정 당시 성공 기준 (0~1)
        [SerializeField] private double recordMs; // 누른 순간 → 뗀 순간 (ms, 소수 1자리)
        [SerializeField] private double transcribeMs; // 뗀 순간 → 최종 결과 (ms, 소수 1자리)
        [SerializeField] private double totalLatencyMs; // 누른 순간 → 최종 결과 (ms, 소수 1자리)
        [SerializeField] private double audioSec; // 음성 길이 (초, 소수 3자리)
        [SerializeField] private double realtimeFactor; // 변환 시간 ÷ 음성 길이 (소수 3자리)
        [SerializeField] private double engineStartMs; // StartListening → 실제 듣기 시작 (ms, 모르면 -1)
        [SerializeField] private bool usedHypothesisFallback; // 최종 결과 대신 마지막 중간 결과를 썼는지
        [SerializeField] private List<string> hypotheses; // 시도 중 받은 중간 결과 목록
        [SerializeField] private string wavFile; // 로그 폴더 기준 WAV 상대 경로. 저장 안 했으면 빈 문자열
        [SerializeField] private string error; // 오류 메시지. 비어 있으면 성공한 시도

        private SttAttemptRecord() { }

        public int Id => id;
        public string Timestamp => timestamp;
        public string Engine => engine;
        public string Model => model;
        public string Compute => compute;
        public string MicDevice => micDevice;
        public string UnityVersion => unityVersion;
        public string BuildType => buildType;
        public string TargetPhrase => targetPhrase;
        public string RawText => rawText;
        public string NormalizedText => normalizedText;
        public float Similarity => similarity;
        public bool Passed => passed;
        public float Threshold => threshold;
        public double RecordMs => recordMs;
        public double TranscribeMs => transcribeMs;
        public double TotalLatencyMs => totalLatencyMs;
        public double AudioSec => audioSec;
        public double RealtimeFactor => realtimeFactor;
        public double EngineStartMs => engineStartMs;
        public bool UsedHypothesisFallback => usedHypothesisFallback;
        public IReadOnlyList<string> Hypotheses => hypotheses;
        public string WavFile => wavFile;
        public string Error => error;
        public bool Succeeded => string.IsNullOrEmpty(error);

        public static SttAttemptRecord Create(int id, DateTime timestamp, SttResult result, string targetPhrase,
            SpellMatchResult match, string wavFile, string unityVersion, string buildType)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            return new SttAttemptRecord
            {
                id = id,
                timestamp = timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz", CultureInfo.InvariantCulture),
                engine = result.EngineName,
                model = result.ModelName,
                compute = result.ComputeBackend,
                micDevice = result.MicDevice,
                unityVersion = unityVersion ?? "",
                buildType = buildType ?? "",
                targetPhrase = targetPhrase ?? "",
                rawText = result.RawText,
                normalizedText = result.NormalizedText,
                similarity = (float)Math.Round(match.Similarity, 4),
                passed = match.Passed,
                threshold = match.Threshold,
                recordMs = Math.Round(result.RecordMs, 1),
                transcribeMs = Math.Round(result.TranscribeMs, 1),
                totalLatencyMs = Math.Round(result.TotalLatencyMs, 1),
                audioSec = Math.Round(result.AudioSec, 3),
                realtimeFactor = Math.Round(result.RealtimeFactor, 3),
                engineStartMs = Math.Round(result.EngineStartMs, 1),
                usedHypothesisFallback = result.UsedHypothesisFallback,
                hypotheses = new List<string>(result.Hypotheses),
                wavFile = wavFile ?? "",
                error = result.Error,
            };
        }

        /// <summary>CsvHeader와 같은 순서의 값 목록 (이스케이프 전).</summary>
        public string[] ToCsvFields()
        {
            CultureInfo inv = CultureInfo.InvariantCulture;
            return new[]
            {
                id.ToString(inv), timestamp, engine, model, compute, micDevice, unityVersion, buildType,
                targetPhrase, rawText, normalizedText,
                similarity.ToString("0.####", inv), passed ? "true" : "false", threshold.ToString("0.##", inv),
                recordMs.ToString("0.#", inv), transcribeMs.ToString("0.#", inv), totalLatencyMs.ToString("0.#", inv),
                audioSec.ToString("0.###", inv), realtimeFactor.ToString("0.###", inv),
                engineStartMs.ToString("0.#", inv), usedHypothesisFallback ? "true" : "false",
                string.Join(" | ", hypotheses), wavFile, error,
            };
        }
    }
}
