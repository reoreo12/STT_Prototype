using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VoxelMagic.Voice
{
    /// <summary>키보드 키(기본 Space)를 누르는 동안 녹음. 새 Input System을 쓴다.</summary>
    public sealed class KeyboardPushToTalk : MonoBehaviour, IPushToTalkSource
    {
        [SerializeField] private Key key = Key.Space;

        private Func<bool> blocker;

        public event Action Pressed;
        public event Action Released;

        public bool IsHeld { get; private set; }
        public Key Key => key;

        public void SetKey(Key newKey)
        {
            ReleaseIfHeld();
            key = newKey;
        }

        /// <summary>true를 돌려주는 동안 새 누름을 무시한다. (예: 텍스트 입력칸에 입력 중)</summary>
        public void SetBlocker(Func<bool> isBlocked)
        {
            blocker = isBlocked;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || key == Key.None) return;

            var control = keyboard[key];
            if (!IsHeld && control.wasPressedThisFrame)
            {
                if (blocker != null && blocker()) return;
                IsHeld = true;
                Pressed?.Invoke();
            }
            else if (IsHeld && !control.isPressed)
            {
                IsHeld = false;
                Released?.Invoke();
            }
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
