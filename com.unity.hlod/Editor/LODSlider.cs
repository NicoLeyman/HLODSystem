using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.HLODSystem
{
    public class LODSlider : VisualElement
    {
        public readonly Color[] kLODColors =
        {
            new Color(0.4831376f, 0.6211768f, 0.0219608f, 1.0f),
            new Color(0.2792160f, 0.4078432f, 0.5835296f, 1.0f),
            new Color(0.2070592f, 0.5333336f, 0.6556864f, 1.0f),
            new Color(0.5333336f, 0.1600000f, 0.0282352f, 1.0f),
            new Color(0.3827448f, 0.2886272f, 0.5239216f, 1.0f),
            new Color(0.8000000f, 0.4423528f, 0.0000000f, 1.0f),
            new Color(0.4486272f, 0.4078432f, 0.0501960f, 1.0f),
            new Color(0.7749016f, 0.6368624f, 0.0250984f, 1.0f)
        };

        public static readonly Color kDefaultLODColor = new Color(.4f, 0f, 0f, 1f);
        public const int k_SliderBarHeight = 30;

        private int m_SelectedIndex = -1;
        private LODSliderRange m_DefaultRange = null;

        private List<LODSliderRange> m_RangeList = new List<LODSliderRange>();

        public LODSlider(bool useDefault = false, string name = "")
        {
            if (useDefault)
            {
                var defaultRange = new LODSliderRange();
                defaultRange.Name = name;
                m_DefaultRange = defaultRange;
            }

            generateVisualContent += OnGenerateVisualContent;

            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseUpEvent>(OnMouseUp);

            style.width = new StyleLength(new Length(100.0f, LengthUnit.Percent));
            style.height = new StyleLength(new Length(30.0f, LengthUnit.Pixel));

            style.color = Color.white;
            focusable = true;
        }

        public void InsertRange(string name, SerializedProperty property)
        {
            var range = new LODSliderRange();
            range.Name = name;
            range.Property = property;

            int insertPosition = 0;

            for (; insertPosition < m_RangeList.Count; ++insertPosition)
            {
                if (m_RangeList[insertPosition].EndPosition < range.EndPosition)
                {
                    break;
                }
            }

            m_RangeList.Insert(insertPosition, range);
        }

        public int GetRangeCount()
        {
            return m_RangeList.Count;
        }

        //public void Draw()
        //{
        //    var sliderBarPosition = GUILayoutUtility.GetRect(0, k_SliderBarHeight, GUILayout.ExpandWidth(true));
        //    sliderBarPosition.width -= 5;   //< for margin
        //    Draw(sliderBarPosition);
        //}

        internal static void DrawRect(Painter2D painter, Rect rect, Color fillColor)
        {
            painter.BeginPath();

            painter.MoveTo(new Vector2(rect.xMin, rect.yMax));
            painter.LineTo(new Vector2(rect.xMax, rect.yMax));
            painter.LineTo(new Vector2(rect.xMax, rect.yMin));
            painter.LineTo(new Vector2(rect.xMin, rect.yMin));

            painter.ClosePath();
            painter.fillColor = fillColor;
            painter.Fill();
        }

        void OnMouseDown(MouseDownEvent e)
        {
            int count = m_RangeList.Count;
            if (m_DefaultRange == null)
                count -= 1;

            var sliderBarPosition = SliderBarPosition;
            var relativeMousePosition = e.localMousePosition + new Vector2(SliderBarPosition.xMin, SliderBarPosition.yMin);

            for (int i = 0; i < count; ++i)
            {
                Rect resizeArea = m_RangeList[i].GetResizeArea(sliderBarPosition);

                if (resizeArea.Contains(relativeMousePosition) == true)
                {
                    e.StopPropagation();
                    m_SelectedIndex = i;
                    break;
                }
            }
        }

        void OnMouseMove(MouseMoveEvent e)
        {
            MarkDirtyRepaint();

            // Drag?
            if (m_SelectedIndex >= 0)
            {
                var sliderBarPosition = SliderBarPosition;
                var relativeMousePosition = e.localMousePosition + new Vector2(SliderBarPosition.xMin, SliderBarPosition.yMin);

                e.StopPropagation();

                var percentage =
                    1.0f - Mathf.Clamp((relativeMousePosition.x - sliderBarPosition.x) / sliderBarPosition.width,
                        0.01f, 1.0f);
                percentage = (percentage * percentage);

                if (m_RangeList[m_SelectedIndex].Property != null)
                {
                    var property = m_RangeList[m_SelectedIndex].Property;
                    property.floatValue = percentage;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
        }

        void OnMouseUp(MouseUpEvent e)
        {
            e.StopPropagation();
            m_SelectedIndex = -1;
        }

        Rect SliderBarPosition => new Rect(resolvedStyle.left, resolvedStyle.top, resolvedStyle.width, resolvedStyle.height);

        //public void Draw(Rect sliderBarPosition)
        void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            var sliderBarPosition = SliderBarPosition;
            var localSliderBarPosition = new Rect(0, 0, sliderBarPosition.width, sliderBarPosition.height);

            var painter = mgc.painter2D;

            DrawRect(painter, localSliderBarPosition, kDefaultLODColor);
            //GUIStyle.Draw(sliderBarPosition, GUIContent.none, false, false, false, false);

            float startPosition = 1.0f;
            for (int i = 0; i < m_RangeList.Count; ++i)
            {
                m_RangeList[i].Draw(mgc, localSliderBarPosition, kLODColors[i], startPosition, resolvedStyle.fontSize, resolvedStyle.color);
                //if default range has not existed then last range should not be drawn.

                if (enabledSelf)
                {
                    if (i != m_RangeList.Count - 1 || m_DefaultRange != null)
                        m_RangeList[i].DrawCursor(sliderBarPosition);
                }

                startPosition = m_RangeList[i].EndPosition;
            }

            if (m_DefaultRange != null)
            {
                m_DefaultRange.Draw(mgc, localSliderBarPosition, kDefaultLODColor, startPosition, resolvedStyle.fontSize, resolvedStyle.color);
            }
        }

        class LODSliderRange
        {
            public string Name { set; get; }
            public SerializedProperty Property { set; get; }

            public float EndPosition
            {
                get
                {
                    if (Property == null)
                        return 0.0f;
                    return Property.floatValue;
                }
            }

            public Rect GetResizeArea(Rect sliderArea)
            {

                float pos = sliderArea.width * (1.0f - Mathf.Sqrt(EndPosition));
                return new Rect(sliderArea.x + pos - 5.0f, sliderArea.y, 10.0f, sliderArea.height);
            }
            public void Draw(MeshGenerationContext mgc, Rect sliderArea, Color backgroundColor, float startPosition, float textSize, Color textColor)
            {
                var startPercentageString = string.Format("{0}\n{1:0}%", Name, startPosition * 100.0f);

                var startX = Mathf.Round(sliderArea.width * (1.0f - Mathf.Sqrt(startPosition)));
                var endX = Mathf.Round(sliderArea.width * (1.0f - Mathf.Sqrt(EndPosition)));

                var rect = new Rect(sliderArea.x + startX, sliderArea.y, endX - startX, sliderArea.height);

                //Styles.LODSliderRange.Draw(rect, GUIContent.none, false, false, false, false);
                //Styles.LODSliderText.Draw(rect, startPercentageString, false, false, false, false);

                DrawRect(mgc.painter2D, rect, backgroundColor);
                mgc.DrawText(startPercentageString, new Vector2(rect.xMin, rect.yMin), textSize, textColor);
            }

            public void DrawCursor(Rect sliderArea)
            {
                EditorGUIUtility.AddCursorRect(GetResizeArea(sliderArea), MouseCursor.ResizeHorizontal);
            }
        }
    }
}