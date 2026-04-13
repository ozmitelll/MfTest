using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Game.Scripts.Core
{
    public static class InputBindingsPersistence
    {
        private const string PlayerPrefsKey = "modfall.input.binding-overrides";

        public static void ApplySavedOverrides(InputSystem_Actions actions)
        {
            if (actions?.asset == null || !PlayerPrefs.HasKey(PlayerPrefsKey))
                return;

            string overridesJson = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(overridesJson))
                return;

            actions.asset.LoadBindingOverridesFromJson(overridesJson);
        }

        public static void SaveOverrides(InputSystem_Actions actions)
        {
            if (actions?.asset == null)
                return;

            string overridesJson = actions.asset.SaveBindingOverridesAsJson();
            if (string.IsNullOrWhiteSpace(overridesJson) || string.Equals(overridesJson, "[]", StringComparison.Ordinal))
            {
                PlayerPrefs.DeleteKey(PlayerPrefsKey);
            }
            else
            {
                PlayerPrefs.SetString(PlayerPrefsKey, overridesJson);
            }

            PlayerPrefs.Save();
        }

        public static void ResetOverrides(InputSystem_Actions actions)
        {
            if (actions?.asset == null)
                return;

            actions.asset.RemoveAllBindingOverrides();
            PlayerPrefs.DeleteKey(PlayerPrefsKey);
            PlayerPrefs.Save();
        }

        public static string FormatBindingDisplay(InputAction action, int bindingIndex)
        {
            if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
                return "[N/A]";

            string display = action.GetBindingDisplayString(bindingIndex, InputBinding.DisplayStringOptions.DontIncludeInteractions);
            if (string.IsNullOrWhiteSpace(display))
                display = "UNBOUND";

            return $"[{NormalizeDisplay(display)}]";
        }

        private static string NormalizeDisplay(string display)
        {
            string normalized = display.Trim().ToUpperInvariant();

            return normalized switch
            {
                "LEFT BUTTON" => "LMB",
                "RIGHT BUTTON" => "RMB",
                "MIDDLE BUTTON" => "MMB",
                "PRESS" => "PRESS",
                _ => normalized
            };
        }
    }
}