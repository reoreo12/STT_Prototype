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
        public static readonly string[] CsvHeader =
        {
            "id", "timestamp", "engine", "model", "compute", "micDevice", "unityVersion", "buildType",
            "targetPhrase", "rawText", "normalizedText", "similarity", "passed", "threshold",
            "recordMs", "transcribeMs", "totalLatencyMs", "audioSec", "realtimeFactor",
            "engineStartMs", "usedHypothesisFallback", "hypotheses", "wavFile", "error",
        };

        [SerializeField] private int id;
        [SerializeField] private string timestamp;
        [SerializeField] private string engine;
        [SerializeField] private string model;
        [SerializeField] private string compute;
        [SerializeField] private string micDevice;
        [SerializeField] private string unityVersion;
        [SerializeField] private string buildType;
        [SerializeField] private string targetPhrase;
        [SerializeField] private string rawText;
        [SerializeField] private string normalizedText;
        [SerializeField] private float similarity;
        [SerializeField] private bool passed;
        [SerializeField] private float threshold;
        [SerializeField] private double recordMs;
        [SerializeField] private double transcribeMs;
        [SerializeField] private double totalLatencyMs;
        [SerializeField] private double audioSec;
        [SerializeField] private double realtimeFactor;
        [SerializeField] private double engineStartMs;
        [SerializeField] private bool usedHypothesisFallback;
        [SerializeField] private List<string> hypotheses;
        [SerializeField] private string wavFile;
        [SerializeField] private string error;

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
