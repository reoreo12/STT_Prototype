// PC용 누르고 말하기 입력을 구현한다. 지정 키를 누르는 동안 녹음하도록 신호를 보낸다.

using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VoxelMagic.Voice
{
    /// <summary>키보드 키(기본 Space)를 누르는 동안 녹음. 새 Input System을 쓴다.</summary>
    public sealed class KeyboardPushToTalk : MonoBehaviour, IPushToTalkSource
    {
        [SerializeField] private Key key = Key.Space; // 누르고 말하기 키 (None이면 입력 무시)

        private Func<bool> blocker; // true를 돌려주면 새 누름을 무시하는 조건 (null이면 항상 허용)

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
