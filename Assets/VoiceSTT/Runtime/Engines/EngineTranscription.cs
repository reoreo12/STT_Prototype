namespace VoxelMagic.Voice
{
    /// <summary>
    /// 엔진 구현이 기반 클래스에 돌려주는 변환 결과.
    /// 시간 계산과 정규화는 기반 클래스가 맡으므로 엔진은 텍스트와 오류만 채운다.
    /// </summary>
    public readonly struct EngineTranscription
    {
        private EngineTranscription(string text, string error, bool usedHypothesisFallback, AudioSamples audio)
        {
            Text = text ?? "";
            Error = error ?? "";
            UsedHypothesisFallback = usedHypothesisFallback;
            Audio = audio;
        }

        public string Text { get; }
        public string Error { get; }
        public bool UsedHypothesisFallback { get; }
        public AudioSamples Audio { get; }

        public static EngineTranscription Success(string text, bool usedHypothesisFallback, AudioSamples audio)
        {
            return new EngineTranscription(text, "", usedHypothesisFallback, audio);
        }

        public static EngineTranscription Failure(string error, AudioSamples audio)
        {
            return new EngineTranscription("", string.IsNullOrEmpty(error) ? "알 수 없는 오류" : error, false, audio);
        }
    }
}
