using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[HelpURL("https://github.com/IAFahim/AV.BoundedViz")]

namespace AV.BoundedViz.Editor
{
    // usage: Create one instance of this in your Resources folder named "GameVariableConfig"
    [CreateAssetMenu(fileName = "GameVariableConfig", menuName = "Variable/Editor Config")]
    public class GameVariableConfig : ScriptableObject
    {
        private static GameVariableConfig _instance;

        [Header("Layout")] public float Height = 18f;

        public float Padding = 2f;
        public float Rounding = 3f;

        [Header("Behavior")] public bool AllowScrubbing = true;

        public bool ShowText = true;

        [Header("Colors")] public Color TextColor = Color.white;

        public Color GutterColor = new(0.1f, 0.1f, 0.1f, 1f);
        public Color BorderColor = new(0, 0, 0, 0.5f);
        public Gradient DefaultGradient;

        [Header("Overrides")] [Tooltip("Map field names (e.g. 'Health', 'Mana') to specific gradients")]
        public List<GradientOverride> Overrides = new();

        public static GameVariableConfig Instance
        {
            get
            {
                if (_instance != null) return _instance;
                // Try to load from Resources
                _instance = Resources.Load<GameVariableConfig>("GameVariableConfig");
                // Fallback if missing
                if (_instance == null) _instance = CreateInstance<GameVariableConfig>();
                return _instance;
            }
        }

        private void OnEnable()
        {
            if (DefaultGradient == null || DefaultGradient.colorKeys.Length == 0)
            {
                DefaultGradient = new Gradient();
                DefaultGradient.SetKeys(
                    new[]
                    {
                        new GradientColorKey(new Color(0.2f, 0.6f, 1f), 0f),
                        new GradientColorKey(new Color(0.1f, 0.3f, 0.8f), 1f)
                    },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
                );
            }
        }

        public Gradient GetGradient(string propertyName, string typeName)
        {
            // Try to match by property name (e.g., "Health")
            var match = Overrides.FirstOrDefault(x => propertyName.Contains(x.Key));
            if (match != null && match.Gradient != null) return match.Gradient;

            return DefaultGradient;
        }

        [Serializable]
        public class GradientOverride
        {
            public string Key; // Case sensitive substring match
            public Gradient Gradient;
        }
    }
}