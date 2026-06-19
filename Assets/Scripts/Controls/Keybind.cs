using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace IdleOff.Controls
{
    [Serializable]
    public sealed class Keybind
    {
        public string actionName;
        public List<string> keys = new();

        public bool IsPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || keys == null)
            {
                return false;
            }

            foreach (var keyName in keys)
            {
                if (TryGetKeyControl(keyboard, keyName, out var keyControl) && keyControl.isPressed)
                {
                    return true;
                }
            }

            return false;
        }

        public bool WasPressedThisFrame()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || keys == null)
            {
                return false;
            }

            foreach (var keyName in keys)
            {
                if (TryGetKeyControl(keyboard, keyName, out var keyControl) && keyControl.wasPressedThisFrame)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetKeyControl(Keyboard keyboard, string keyName, out KeyControl keyControl)
        {
            keyControl = null;
            if (keyboard == null || string.IsNullOrWhiteSpace(keyName) || !Enum.TryParse(keyName, true, out Key key))
            {
                return false;
            }

            keyControl = keyboard[key];
            return keyControl != null;
        }
    }
}
