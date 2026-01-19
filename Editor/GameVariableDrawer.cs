using System;
using UnityEditor;
using UnityEngine;
using Variable.Timer;


namespace AV.BoundedViz.Editor
{
    [HelpURL("https://github.com/IAFahim/AV.BoundedViz")]
    [CustomPropertyDrawer(typeof(Timer))]
    [CustomPropertyDrawer(typeof(Cooldown))]
    // Add other types here...
    public class GameVariableDrawer : PropertyDrawer
    {
        private SerializedProperty _currentProp;
        private SerializedProperty _maxProp;
        private SerializedProperty _minProp;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // If expanded, add standard height for children
            var extraHeight = property.isExpanded
                ? EditorGUI.GetPropertyHeight(property, true) + EditorGUIUtility.standardVerticalSpacing
                : 0f;

            return GameVariableConfig.Instance.Height + extraHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var config = GameVariableConfig.Instance;
            Initialize(property);

            // -- Layout Calculations --
            // Top Bar Area
            var headerRect = new Rect(position.x, position.y, position.width, config.Height);

            // Label Area (Left)
            var labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, config.Height);

            // Bar Area (Right)
            var barRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y,
                position.width - EditorGUIUtility.labelWidth, config.Height);

            // Padding for the bar visually
            var visualBarRect = new Rect(
                barRect.x + config.Padding,
                barRect.y + config.Padding,
                barRect.width - config.Padding,
                barRect.height - config.Padding * 2
            );

            // -- Data Retrieval --
            var current = GetValue(_currentProp);
            var max = GetValue(_maxProp);
            var min = _minProp != null ? GetValue(_minProp) : 0.0;
            var range = max - min;
            var ratio = range <= 0.00001 ? 0f : (float)Math.Clamp((current - min) / range, 0.0, 1.0);

            // -- Draw Logic --
            EditorGUI.BeginProperty(position, label, property);

            // 1. Foldout Label
            property.isExpanded = EditorGUI.Foldout(labelRect, property.isExpanded, label, true);

            // 2. Event Handling (Hover & Scrub)
            var isHovering = visualBarRect.Contains(Event.current.mousePosition);
            if (config.AllowScrubbing)
            {
                EditorGUIUtility.AddCursorRect(visualBarRect, MouseCursor.SlideArrow);
                HandleScrubbing(visualBarRect, min, max);
            }

            // 3. Draw The Bar (Repaint Only)
            if (Event.current.type == EventType.Repaint)
            {
                DrawBarVisuals(visualBarRect, ratio, config, property.name, property.type, isHovering);

                if (config.ShowText)
                    DrawBarText(visualBarRect, current, max, config);
            }

            // 4. Draw Expanded Properties
            if (property.isExpanded)
            {
                var childrenRect = new Rect(position.x,
                    position.y + config.Height + EditorGUIUtility.standardVerticalSpacing, position.width,
                    position.height - config.Height);
                DrawChildren(childrenRect, property);
            }

            EditorGUI.EndProperty();
        }

        private void DrawBarVisuals(Rect rect, float ratio, GameVariableConfig config, string propName, string typeName,
            bool isHovering)
        {
            // Background (Gutter)
            DrawBlock(rect, config.GutterColor, config.Rounding);

            // Fill
            if (ratio > 0)
            {
                var fillRect = new Rect(rect.x, rect.y, rect.width * ratio, rect.height);
                var gradient = config.GetGradient(propName, typeName);
                var fillColor = gradient.Evaluate(ratio);

                // If hovering, brighten the color slightly for feedback
                if (isHovering && config.AllowScrubbing)
                    fillColor = Color.Lerp(fillColor, Color.white, 0.15f);

                DrawBlock(fillRect, fillColor, config.Rounding);
            }

            // Border (Optional outline)
            if (config.BorderColor.a > 0)
            {
                // Simple border via GL or just another rect behind? 
                // Using lines looks cleaner:
                Handles.color = config.BorderColor;
                Handles.DrawWireCube(rect.center, rect.size);
            }
        }

        private void DrawBarText(Rect rect, double current, double max, GameVariableConfig config)
        {
            var text = $"{current:0.##} / {max:0.##}";

            // Create a custom style for clean text
            var style = new GUIStyle(EditorStyles.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = config.TextColor;
            style.fontSize = 10;
            style.fontStyle = FontStyle.Bold;

            // Draw Shadow manually for better control than DropShadowLabel
            var shadowRect = new Rect(rect);
            shadowRect.x += 1f;
            shadowRect.y += 1f;
            style.normal.textColor = new Color(0, 0, 0, 0.7f);
            GUI.Label(shadowRect, text, style);

            // Draw Foreground
            style.normal.textColor = config.TextColor;
            GUI.Label(rect, text, style);
        }

        // Helper to draw a colored quad with a 1x1 texture
        private void DrawBlock(Rect rect, Color color, float rounding)
        {
            // Using a white texture allows GUI.color to tint it correctly
            var tex = Texture2D.whiteTexture;
            GUI.color = color;
            // DrawTexture supports basic rendering. For rounded corners in UIElements it's easier,
            // but in IMGUI standard DrawTexture is square. 
            // We use GUI.Box with a custom style for rounding if needed, or just DrawTexture for speed.
            GUI.DrawTexture(rect, tex);
            GUI.color = Color.white;
        }

        private void HandleScrubbing(Rect rect, double min, double max)
        {
            var controlID = GUIUtility.GetControlID(FocusType.Passive);
            var evt = Event.current;

            switch (evt.type)
            {
                case EventType.MouseDown:
                    if (rect.Contains(evt.mousePosition) && evt.button == 0)
                    {
                        GUIUtility.hotControl = controlID;
                        ApplyScrub(evt.mousePosition.x, rect, min, max);
                        evt.Use();
                    }

                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID)
                    {
                        ApplyScrub(evt.mousePosition.x, rect, min, max);
                        evt.Use();
                    }

                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID)
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();
                    }

                    break;
            }
        }

        private void ApplyScrub(float mouseX, Rect barRect, double min, double max)
        {
            var localX = mouseX - barRect.x;
            var pct = Mathf.Clamp01(localX / barRect.width);
            var range = max - min;
            var newValue = min + range * pct;

            if (_currentProp.propertyType == SerializedPropertyType.Float)
                _currentProp.floatValue = (float)newValue;
            else if (_currentProp.propertyType == SerializedPropertyType.Integer)
                _currentProp.intValue = (int)Math.Round(newValue);
            else if (_currentProp.propertyType == SerializedPropertyType.Integer && _currentProp.type == "long")
                _currentProp.longValue = (long)Math.Round(newValue);

            _currentProp.serializedObject.ApplyModifiedProperties();
        }

        private void DrawChildren(Rect rect, SerializedProperty property)
        {
            EditorGUI.indentLevel++;
            var endProp = property.GetEndProperty();
            var iterator = property.Copy();
            var y = rect.y;

            if (iterator.NextVisible(true))
                while (!SerializedProperty.EqualContents(iterator, endProp))
                {
                    var h = EditorGUI.GetPropertyHeight(iterator);
                    EditorGUI.PropertyField(new Rect(rect.x, y, rect.width, h), iterator, true);
                    y += h + EditorGUIUtility.standardVerticalSpacing;
                    if (!iterator.NextVisible(false)) break;
                }

            EditorGUI.indentLevel--;
        }

        private void Initialize(SerializedProperty root)
        {
            // Optimization: Only look up properties if we haven't yet, or if they are invalid
            if (_currentProp != null && _currentProp.serializedObject.targetObject != null) return;

            // Strategy: Check common naming conventions
            _currentProp = root.FindPropertyRelative("Current");
            _maxProp = root.FindPropertyRelative("Max") ?? root.FindPropertyRelative("Duration");
            _minProp = root.FindPropertyRelative("Min");

            // Strategy: Nested Structs (e.g. RegenFloat.Value.Current)
            if (_currentProp == null)
            {
                var valProp = root.FindPropertyRelative("Value"); // RegenFloat
                var volProp = root.FindPropertyRelative("Volume"); // Reservoir

                var wrapper = valProp ?? volProp;

                if (wrapper != null)
                {
                    _currentProp = wrapper.FindPropertyRelative("Current");
                    _maxProp = wrapper.FindPropertyRelative("Max");
                    _minProp = wrapper.FindPropertyRelative("Min");
                }
            }
        }

        private double GetValue(SerializedProperty p)
        {
            if (p == null) return 0;
            if (p.propertyType == SerializedPropertyType.Float) return p.floatValue;
            if (p.propertyType == SerializedPropertyType.Integer)
                // Handle Long vs Int
                return p.type == "long" ? p.longValue : p.intValue;
            return 0;
        }
    }
}