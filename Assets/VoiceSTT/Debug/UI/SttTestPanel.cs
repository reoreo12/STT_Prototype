using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VoxelMagic.Voice.Debug
{
    /// <summary>
    /// 실시간 확인 UI(뷰). 하위 UI를 코드로 만들고, 세션·엔진 이벤트를 구독해 화면만 갱신한다.
    /// 사용자 조작은 SttTestSession의 공개 메서드로 전달한다. Canvas가 있는 오브젝트에 붙인다.
    /// </summary>
    public sealed class SttTestPanel : MonoBehaviour
    {
        private const int MaxRows = 200;
        private const string FreeInputOption = "(자유 입력)";

        private static readonly Color Background = new Color(0.10f, 0.11f, 0.13f, 0.96f);
        private static readonly Color ColumnBackground = new Color(0.16f, 0.17f, 0.20f, 1f);
        private static readonly Color TextColor = new Color(0.92f, 0.92f, 0.92f);
        private static readonly Color DimTextColor = new Color(0.65f, 0.67f, 0.70f);
        private static readonly Color NoticeColor = new Color(1f, 0.82f, 0.35f);
        private static readonly Color IdleColor = new Color(0.62f, 0.62f, 0.62f);
        private static readonly Color ListeningColor = new Color(0.95f, 0.30f, 0.30f);
        private static readonly Color TranscribingColor = new Color(1f, 0.80f, 0.20f);
        private static readonly Color CompletedColor = new Color(0.35f, 0.85f, 0.45f);
        private static readonly Color FailedColor = new Color(0.75f, 0.15f, 0.15f);
        private static readonly Color AccentColor = new Color(0.30f, 0.60f, 1f);

        private readonly List<SttEngineOption> engineOptions = new List<SttEngineOption>();
        private readonly List<string> micOptions = new List<string>();

        private SttTestSession session;
        private ISpeechToText engine;
        private IReadOnlyList<string> phrases = new string[0];
        private bool built;
        private bool suppressUiEvents;
        private string engineMessage = "";

        private Dropdown engineDropdown;
        private Dropdown micDropdown;
        private Dropdown targetDropdown;
        private InputField freeInput;
        private Slider thresholdSlider;
        private Text noticeText;
        private Text statusText;
        private Text levelText;
        private RectTransform levelFill;
        private Text hypothesisText;
        private Text rawText;
        private Text normalizedText;
        private Text timingText;
        private Text thresholdValueText;
        private Text judgeText;
        private Text summaryText;
        private RectTransform tableContent;
        private ScreenButtonPushToTalk pushButton;

        public IPushToTalkSource PushButton => pushButton;
        public bool IsTextInputFocused => freeInput != null && freeInput.isFocused;

        /// <summary>UI를 만든다. Bind보다 먼저 호출한다.</summary>
        public void Build(SttSettings settings)
        {
            if (built) return;
            built = true;
            phrases = settings.TargetPhrases;

            RectTransform root = SttPanelLayout.CreateRoot(transform, Background);
            BuildTopBar(root);
            noticeText = SttPanelLayout.CreateLabel(root, "", SttPanelLayout.BodyFontSize, NoticeColor);

            RectTransform body = SttPanelLayout.CreateGroup(root, "Body", false, 16, 0);
            body.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
            SttPanelLayout.SetSize(body, -1, -1, 1, 1);

            BuildCurrentAttemptColumn(body, settings);
            BuildHistoryColumn(body);
            RenderState(SttState.Idle);
        }

        /// <summary>세션에 연결하고 이벤트를 구독한다.</summary>
        public void Bind(SttTestSession testSession)
        {
            if (!built) throw new InvalidOperationException("Build를 먼저 호출해야 합니다.");
            Unbind();
            session = testSession ?? throw new ArgumentNullException(nameof(testSession));
            session.AttemptRecorded += OnAttemptRecorded;
            session.StatsChanged += OnStatsChanged;
            session.AttemptsCleared += OnAttemptsCleared;
            session.Voice.EngineChanged += AttachEngine;
            session.Voice.AvailabilityChanged += OnAvailabilityChanged;

            suppressUiEvents = true;
            engineOptions.Clear();
            engineOptions.AddRange(session.EngineFactory.Options);
            var engineLabels = new List<string>();
            foreach (SttEngineOption option in engineOptions) engineLabels.Add(option.DisplayName);
            engineDropdown.ClearOptions();
            engineDropdown.AddOptions(engineLabels);
            thresholdSlider.value = session.Threshold;
            int phraseIndex = IndexOf(phrases, session.TargetPhrase);
            targetDropdown.value = phraseIndex >= 0 ? phraseIndex : phrases.Count;
            suppressUiEvents = false;
            UpdateThresholdLabel();
            UpdateFreeInputState();

            AttachEngine(session.Voice.Engine);
            OnStatsChanged();
        }

        private void Update()
        {
            if (engine == null) return;

            if (engine.State == SttState.Listening)
            {
                statusText.text = $"● 녹음 중   {engine.ListeningElapsedMs:0} ms";
            }
            else if (engine.State == SttState.Transcribing)
            {
                statusText.text = $"● 변환 중   {engine.TranscribingElapsedMs:0} ms";
            }

            float level = engine.InputLevel;
            levelFill.anchorMax = new Vector2(Mathf.Clamp01(level), 1f);
            levelText.text = level < 0f
                ? "음량: 녹음 중에만 표시 (이 엔진·설정에서는 사용 불가할 수 있음)"
                : $"음량 {level * 100f:0}%";
        }

        private void OnDestroy()
        {
            Unbind();
        }

        // ───────────────────────── 화면 구성 ─────────────────────────

        private void BuildTopBar(Transform root)
        {
            RectTransform bar = SttPanelLayout.CreateGroup(root, "TopBar", false, 10, 0);
            SttPanelLayout.SetSize(bar, -1, 44);

            SttPanelLayout.SetSize(SttPanelLayout.CreateLabel(bar, "엔진", SttPanelLayout.BodyFontSize, TextColor), 50);
            // 항목은 Bind에서 세션의 엔진 팩토리 목록으로 채운다.
            engineDropdown = SttPanelLayout.CreateDropdown(bar, new[] { "-" }, 330, 40);
            engineDropdown.onValueChanged.AddListener(OnEngineSelected);

            SttPanelLayout.SetSize(SttPanelLayout.CreateLabel(bar, "마이크", SttPanelLayout.BodyFontSize, TextColor), 64);
            micOptions.Add("");
            var micLabels = new List<string> { "(기본 장치)" };
            foreach (string device in Microphone.devices)
            {
                micOptions.Add(device);
                micLabels.Add(device);
            }
            micDropdown = SttPanelLayout.CreateDropdown(bar, micLabels, 460, 40);
            micDropdown.onValueChanged.AddListener(OnMicSelected);

            SttPanelLayout.SetSize(SttPanelLayout.CreateLabel(bar, "", SttPanelLayout.BodyFontSize, TextColor), -1, -1, 1);
            SttPanelLayout.CreateButton(bar, "로그 폴더 열기", 180, 40, OpenLogFolder);
            SttPanelLayout.CreateButton(bar, "목록 지우기", 140, 40, () =>
            {
                session?.ClearAttempts();
                ClearSelection();
            });
        }

        private void BuildCurrentAttemptColumn(Transform body, SttSettings settings)
        {
            RectTransform column = SttPanelLayout.CreateGroup(body, "CurrentAttempt", true, 10, 16, ColumnBackground);
            SttPanelLayout.SetSize(column, -1, -1, 1.2f, 1);

            statusText = SttPanelLayout.CreateLabel(column, "", 34, IdleColor, FontStyle.Bold);
            levelFill = SttPanelLayout.CreateBar(column, 14, new Color(0.25f, 0.25f, 0.25f), CompletedColor);
            levelText = SttPanelLayout.CreateLabel(column, "", 16, DimTextColor);

            SttPanelLayout.CreateLabel(column, "중간 결과", 18, DimTextColor);
            hypothesisText = SttPanelLayout.CreateLabel(column, "-", 26, TextColor);

            SttPanelLayout.CreateLabel(column, "최종 결과", 18, DimTextColor);
            rawText = SttPanelLayout.CreateLabel(column, "원문: -", 26, TextColor, FontStyle.Bold);
            normalizedText = SttPanelLayout.CreateLabel(column, "정규화: -", SttPanelLayout.BodyFontSize, TextColor);
            timingText = SttPanelLayout.CreateLabel(column, "시간: -", SttPanelLayout.BodyFontSize, TextColor);

            RectTransform targetRow = SttPanelLayout.CreateGroup(column, "TargetRow", false, 10, 0);
            SttPanelLayout.SetSize(targetRow, -1, 40);
            SttPanelLayout.SetSize(SttPanelLayout.CreateLabel(targetRow, "목표 주문", SttPanelLayout.BodyFontSize, TextColor), 100);
            var targetLabels = new List<string>(phrases) { FreeInputOption };
            targetDropdown = SttPanelLayout.CreateDropdown(targetRow, targetLabels, 220, 38);
            targetDropdown.onValueChanged.AddListener(_ => OnTargetChanged());
            freeInput = SttPanelLayout.CreateInputField(targetRow, "자유 입력 (예: 얼어라)", 260, 38);
            freeInput.onValueChanged.AddListener(_ => OnTargetChanged());

            RectTransform thresholdRow = SttPanelLayout.CreateGroup(column, "ThresholdRow", false, 10, 0);
            SttPanelLayout.SetSize(thresholdRow, -1, 36);
            SttPanelLayout.SetSize(SttPanelLayout.CreateLabel(thresholdRow, "성공 기준", SttPanelLayout.BodyFontSize, TextColor), 100);
            thresholdSlider = SttPanelLayout.CreateSlider(thresholdRow, 320, 24, AccentColor);
            thresholdSlider.onValueChanged.AddListener(OnThresholdChanged);
            thresholdValueText = SttPanelLayout.CreateLabel(thresholdRow, "", SttPanelLayout.BodyFontSize, TextColor);
            SttPanelLayout.SetSize(thresholdValueText, 80);

            judgeText = SttPanelLayout.CreateLabel(column, "판정: -", 28, TextColor, FontStyle.Bold);

            SttPanelLayout.SetSize(SttPanelLayout.CreateLabel(column, "", 10, TextColor), -1, -1, -1, 1);
            Button button = SttPanelLayout.CreateButton(column, $"누르고 말하기  ({settings.PushToTalkKey})", -1, 90, null);
            button.transition = Selectable.Transition.ColorTint;
            button.GetComponentInChildren<Text>().fontSize = 30;
            pushButton = button.gameObject.AddComponent<ScreenButtonPushToTalk>();
        }

        private void BuildHistoryColumn(Transform body)
        {
            RectTransform column = SttPanelLayout.CreateGroup(body, "History", true, 8, 16, ColumnBackground);
            SttPanelLayout.SetSize(column, -1, -1, 1, 1);

            SttPanelLayout.CreateLabel(column, "세션 요약", 18, DimTextColor);
            summaryText = SttPanelLayout.CreateLabel(column, "-", 22, TextColor, FontStyle.Bold);

            SttPanelLayout.CreateLabel(column, "기록 (최신순)", 18, DimTextColor);
            AddRow(column, DimTextColor, "#", "목표", "인식 결과", "유사도", "변환 ms", "판정");
            tableContent = SttPanelLayout.CreateScrollList(column);
        }

        // ───────────────────────── 엔진·세션 이벤트 ─────────────────────────

        private void AttachEngine(ISpeechToText next)
        {
            DetachEngine();
            engine = next;
            if (engine == null) return;

            engine.StateChanged += RenderState;
            engine.HypothesisReceived += OnHypothesis;

            SelectCurrentEngineOption();

            engineMessage = "";
            RenderState(engine.State);
            UpdateNotice();
        }

        private void DetachEngine()
        {
            if (engine == null) return;
            engine.StateChanged -= RenderState;
            engine.HypothesisReceived -= OnHypothesis;
            engine = null;
        }

        private void Unbind()
        {
            DetachEngine();
            if (session == null) return;
            session.AttemptRecorded -= OnAttemptRecorded;
            session.StatsChanged -= OnStatsChanged;
            session.AttemptsCleared -= OnAttemptsCleared;
            session.Voice.EngineChanged -= AttachEngine;
            session.Voice.AvailabilityChanged -= OnAvailabilityChanged;
            session = null;
        }

        private void RenderState(SttState state)
        {
            switch (state)
            {
                case SttState.Idle:
                    SetStatus("● 대기", IdleColor);
                    break;
                case SttState.Listening:
                    SetStatus("● 녹음 중", ListeningColor);
                    hypothesisText.text = "…";
                    break;
                case SttState.Transcribing:
                    SetStatus("● 변환 중", TranscribingColor);
                    break;
                case SttState.Completed:
                    SetStatus("● 완료", CompletedColor);
                    break;
                case SttState.Failed:
                    SetStatus("● 실패", FailedColor);
                    break;
            }
        }

        private void OnHypothesis(string text)
        {
            hypothesisText.text = text;
        }

        private void OnAvailabilityChanged(bool available)
        {
            UpdateNotice();
        }

        private void OnAttemptRecorded(SttAttemptRecord record)
        {
            if (record.Succeeded)
            {
                rawText.text = "원문: " + record.RawText + (record.UsedHypothesisFallback ? "   (최종 결과 대신 중간 결과 사용)" : "");
                rawText.color = TextColor;
            }
            else
            {
                rawText.text = "오류: " + record.Error;
                rawText.color = FailedColor;
            }
            normalizedText.text = "정규화: " + (record.NormalizedText.Length > 0 ? record.NormalizedText : "-");
            if (record.Hypotheses.Count == 0) hypothesisText.text = "(이 시도에는 중간 결과 없음)";

            string engineStart = record.EngineStartMs >= 0 ? $"{record.EngineStartMs:0} ms" : "-";
            timingText.text =
                $"녹음 {record.RecordMs:0} ms · 변환 {record.TranscribeMs:0} ms · 전체 {record.TotalLatencyMs:0} ms\n" +
                $"음성 길이 {record.AudioSec:0.00} s · 실시간 배율 {record.RealtimeFactor:0.00} · 엔진 시작 {engineStart}" +
                (record.Compute.Length > 0 ? " · " + record.Compute : "");

            judgeText.text = $"판정: \"{record.TargetPhrase}\" 유사도 {record.Similarity:0.000} (기준 {record.Threshold:0.00}) → {(record.Passed ? "성공" : "실패")}";
            judgeText.color = record.Passed ? CompletedColor : FailedColor;

            Color rowColor = record.Passed ? CompletedColor : (record.Succeeded ? TextColor : FailedColor);
            RectTransform row = AddRow(tableContent, rowColor,
                record.Id.ToString(),
                record.TargetPhrase,
                record.Succeeded ? record.RawText : "(" + record.Error + ")",
                record.Similarity.ToString("0.000"),
                record.Succeeded ? record.TranscribeMs.ToString("0") : "-",
                record.Passed ? "성공" : "실패");
            row.SetAsFirstSibling();
            if (tableContent.childCount > MaxRows) Destroy(tableContent.GetChild(tableContent.childCount - 1).gameObject);
        }

        private void OnStatsChanged()
        {
            if (session == null) return;
            SttSessionStats stats = session.Stats;
            summaryText.text = stats.Count == 0
                ? "아직 시도가 없습니다."
                : $"시도 {stats.Count}회 · 평균 변환 {stats.AverageTranscribeMs:0} ms\n평균 유사도 {stats.AverageSimilarity:0.000} · 성공률 {stats.SuccessRate * 100:0}% ({stats.PassedCount}/{stats.Count})";
        }

        private void OnAttemptsCleared()
        {
            for (int i = tableContent.childCount - 1; i >= 0; i--) Destroy(tableContent.GetChild(i).gameObject);
        }

        // ───────────────────────── 사용자 조작 ─────────────────────────

        private void OnEngineSelected(int index)
        {
            if (suppressUiEvents || session == null) return;
            ClearSelection();
            if (session.TrySwitchEngine(engineOptions[index], out string message)) return;

            engineMessage = message;
            SelectCurrentEngineOption();
            UpdateNotice();
        }

        private void OnMicSelected(int index)
        {
            if (suppressUiEvents || session == null) return;
            ClearSelection();
            session.Voice.SetMicDevice(micOptions[index]);
            UpdateNotice();
        }

        private void OnTargetChanged()
        {
            UpdateFreeInputState();
            if (suppressUiEvents || session == null) return;
            bool isFree = targetDropdown.value >= phrases.Count;
            session.SetTargetPhrase(isFree ? freeInput.text.Trim() : phrases[targetDropdown.value]);
            if (!isFree) ClearSelection();
        }

        private void OnThresholdChanged(float value)
        {
            UpdateThresholdLabel();
            if (suppressUiEvents || session == null) return;
            session.SetThreshold(Mathf.Round(value * 100f) / 100f);
        }

        private void OpenLogFolder()
        {
            ClearSelection();
            if (session == null) return;
            Directory.CreateDirectory(session.LogFolderPath);
            Application.OpenURL("file:///" + session.LogFolderPath.Replace('\\', '/'));
        }

        // ───────────────────────── 도우미 ─────────────────────────

        private void UpdateNotice()
        {
            if (engine == null || session == null) return;
            var lines = new List<string>();
            if (!engine.IsAvailable) lines.Add($"음성 인식 사용 불가: {engine.UnavailableReason}  → 게임에서는 HUD 모드로 전환");
            if (!engine.SupportsDeviceSelection) lines.Add($"{engine.EngineName}는 Windows 기본 마이크를 씁니다. 마이크 선택은 음량 미터·WAV 녹음에만 적용됩니다.");
            if (!session.IsLoggingEnabled) lines.Add("기록이 꺼져 있습니다 (SttSettings).");
            if (engineMessage.Length > 0) lines.Add(engineMessage);
            noticeText.text = string.Join("\n", lines);
        }

        private void UpdateThresholdLabel()
        {
            thresholdValueText.text = thresholdSlider.value.ToString("0.00");
        }

        private void UpdateFreeInputState()
        {
            freeInput.interactable = targetDropdown.value >= phrases.Count;
        }

        private void SetStatus(string text, Color color)
        {
            statusText.text = text;
            statusText.color = color;
        }

        private static RectTransform AddRow(Transform parent, Color color, params string[] cells)
        {
            float[] widths = { 50, 110, 250, 90, 100, 70 };
            RectTransform row = SttPanelLayout.CreateGroup(parent, "Row", false, 6, 0);
            SttPanelLayout.SetSize(row, -1, 30);
            for (int i = 0; i < cells.Length; i++)
            {
                Text cell = SttPanelLayout.CreateLabel(row, cells[i], 18, color);
                cell.verticalOverflow = VerticalWrapMode.Truncate;
                cell.horizontalOverflow = HorizontalWrapMode.Wrap;
                SttPanelLayout.SetSize(cell, i < widths.Length ? widths[i] : 80, 28, i == 2 ? 1 : -1);
            }
            return row;
        }

        private static void ClearSelection()
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        private void SelectCurrentEngineOption()
        {
            if (session == null) return;
            int index = engineOptions.IndexOf(session.CurrentEngineOption);
            if (index < 0) return;
            suppressUiEvents = true;
            engineDropdown.value = index;
            suppressUiEvents = false;
        }

        private static int IndexOf(IReadOnlyList<string> list, string value)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == value) return i;
            }
            return -1;
        }
    }
}
