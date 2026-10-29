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
