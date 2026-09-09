using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Geodashy.Core
{
    /// <summary>
    /// The one button, on every device: a rebindable keyboard key (plus Space / Up / W), any gamepad face
    /// button or trigger, mouse and touch (handled by the callers). Pause is Escape, the gamepad Start button
    /// and the Android back button.
    /// </summary>
    public static class InputConfig
    {
        const string JumpPref = "geodashy.key.jump";

        public static Key JumpKey
        {
            get => Enum.TryParse(PlayerPrefs.GetString(JumpPref, Key.Space.ToString()), out Key k) ? k : Key.Space;
            set
            {
                PlayerPrefs.SetString(JumpPref, value.ToString());
                PlayerPrefs.Save();
            }
        }

        public static string JumpKeyName => JumpKey.ToString();

        static bool KeyHeld(Keyboard kb, Key k) => k != Key.None && kb[k].isPressed;
        static bool KeyPressed(Keyboard kb, Key k) => k != Key.None && kb[k].wasPressedThisFrame;

        public static bool JumpHeld()
        {
            var kb = Keyboard.current;
            if (kb != null && (KeyHeld(kb, JumpKey) || kb.spaceKey.isPressed || kb.upArrowKey.isPressed || kb.wKey.isPressed)) return true;
            var gp = Gamepad.current;
            if (gp != null && (gp.buttonSouth.isPressed || gp.buttonEast.isPressed || gp.buttonWest.isPressed || gp.buttonNorth.isPressed || gp.rightTrigger.isPressed || gp.leftTrigger.isPressed || gp.rightShoulder.isPressed || gp.leftShoulder.isPressed)) return true;
            return false;
        }

        public static bool JumpPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && (KeyPressed(kb, JumpKey) || kb.spaceKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)) return true;
            var gp = Gamepad.current;
            if (gp != null && (gp.buttonSouth.wasPressedThisFrame || gp.buttonEast.wasPressedThisFrame || gp.buttonWest.wasPressedThisFrame || gp.buttonNorth.wasPressedThisFrame || gp.rightTrigger.wasPressedThisFrame || gp.leftTrigger.wasPressedThisFrame || gp.rightShoulder.wasPressedThisFrame || gp.leftShoulder.wasPressedThisFrame)) return true;
            return false;
        }

        public static bool PausePressed()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) return true;
            var gp = Gamepad.current;
            return gp != null && gp.startButton.wasPressedThisFrame;
        }

        public static bool ConfirmPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.enterKey.wasPressedThisFrame) return true;
            var gp = Gamepad.current;
            return gp != null && gp.startButton.wasPressedThisFrame;
        }

        /// <summary>Polls for the next keyboard key press (call every frame while remapping). Returns Key.None until one arrives.</summary>
        public static Key PollNewKey()
        {
            var kb = Keyboard.current;
            if (kb == null) return Key.None;
            foreach (var control in kb.allKeys)
            {
                if (control.wasPressedThisFrame && control.keyCode != Key.Escape) return control.keyCode;
            }
            return Key.None;
        }
    }
}
