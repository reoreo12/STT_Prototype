// 마우스·터치용 누르고 말하기 입력을 구현한다. 화면 버튼을 누르는 동안 녹음하도록 신호를 보낸다.

using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VoxelMagic.Voice
{
    /// <summary>화면 버튼을 누르고 있는 동안 녹음. UI 오브젝트(Graphic이 있는 곳)에 붙인다.</summary>
    public sealed class ScreenButtonPushToTalk : MonoBehaviour, IPushToTalkSource, IPointerDownHandler, IPointerUpHandler
    {
        public event Action Pressed;
        public event Action Released;

        public bool IsHeld { get; private set; }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsHeld || eventData.button != PointerEventData.InputButton.Left) return;
            IsHeld = true;
            Pressed?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            ReleaseIfHeld();
        }

        private void OnDisable()
        {
            ReleaseIfHeld();
        }

        private void ReleaseIfHeld()
        {
            if (!IsHeld) return;
            IsHeld = false;
            Released?.Invoke();
        }
    }
}
