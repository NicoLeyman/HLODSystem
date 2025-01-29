using System.Collections.Generic;
using Unity.HLODSystem;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.HLODSystem
{
    [CustomEditor(typeof(RootData))]
    public class HLODDataEditor : Editor
    {
        UVLayoutElement UVLayout;

        VisualElement Root;

        public override bool HasPreviewGUI()
        {
            return true;
        }

        public override VisualElement CreatePreview(VisualElement preview)
        {
            Root = preview; //new VisualElement();

            var meshesFoldout = new Foldout();
            meshesFoldout.text = "Meshes";
            Root.Add(meshesFoldout);

            var meshList = new ListView();
            meshesFoldout.Add(meshList);
            var serializedGameObjects = (target as RootData).SerializedGameObjects;
            meshList.itemsSource = serializedGameObjects;
            meshList.makeItem = () => new Label();
            meshList.bindItem = (e, i) =>
            {
                var rootData = target as RootData;
                var item = (e as Label);
                var gameOb = rootData.SerializedGameObjects[i];
                var meshFilter = gameOb.GetComponent<MeshFilter>();
                // The gameObject name should be the same, but still...
                item.text = meshFilter.sharedMesh.name;
            };
            meshList.selectionType = SelectionType.Single;
            meshList.selectionChanged += SelectMesh;

            Root.Bind(new SerializedObject(target));

            var atlasPreviewFoldout = new Foldout();
            atlasPreviewFoldout.text = "Atlas Texture";
            Root.Add(atlasPreviewFoldout);

            UVLayout = new UVLayoutElement();
            atlasPreviewFoldout.Add(UVLayout);

            if (serializedGameObjects.Count > 0)
            {
                meshList.selectedIndex = 0;
            }

            return Root;
        }

        void SelectMesh(IEnumerable<object> selection)
        {
            foreach(GameObject gameOb in selection)
            {
                var meshFilter = gameOb.GetComponent<MeshFilter>();
                var meshRenderer = gameOb.GetComponent<MeshRenderer>();

                if (meshFilter != null && meshRenderer != null)
                {

                    UVLayout.SetSelectedObject(meshFilter.sharedMesh, meshRenderer.sharedMaterial);

                    break;
                }
            }
        }

        class UVGrid : VisualElement
        {
            public Mesh Mesh;

            public float LineWidth = 0.5f;
            public Color LineColor = new Color(1.0f, 1.0f, 0.0f, 0.3f);

            public float BorderWidth = 1.0f;
            public Color BorderColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

            int UVChannelIdx = 0;

            public UVGrid()
            {
                generateVisualContent += OnGenerateVisualContent;

                var borderWidth = 1;
                var borderColor = Color.white;
                style.borderBottomWidth = borderWidth;
                style.borderTopWidth = borderWidth;
                style.borderLeftWidth = borderWidth;
                style.borderRightWidth = borderWidth;

                style.borderBottomColor = borderColor;
                style.borderTopColor = borderColor;
                style.borderLeftColor = borderColor;
                style.borderRightColor = borderColor;
            }

            public void SetChannelIdx(int channelIdx)
            {
                UVChannelIdx = channelIdx;
                MarkDirtyRepaint();
            }

            void OnGenerateVisualContent(MeshGenerationContext mgc)
            {
                if (Mesh != null)
                {
                    var paint2D = mgc.painter2D;

                    var offset = new Vector2();

                    var controlWidth = resolvedStyle.width;

                    var triangles = Mesh.GetIndices(0);
                    var texCoords = new List<Vector2>();
                    Mesh.GetUVs(UVChannelIdx, texCoords);

                    paint2D.strokeColor = LineColor;
                    paint2D.lineWidth = LineWidth;

                    if (texCoords.Count > 0)
                    {
                        for (var t = 0; t < triangles.Length; t += 3)
                        {
                            var vi0 = triangles[t + 0];
                            var vi1 = triangles[t + 1];
                            var vi2 = triangles[t + 2];

                            var tc0 = texCoords[vi0] * controlWidth + offset;
                            var tc1 = texCoords[vi1] * controlWidth + offset;
                            var tc2 = texCoords[vi2] * controlWidth + offset;

                            paint2D.BeginPath();
                            paint2D.MoveTo(tc0);
                            paint2D.LineTo(tc1);
                            paint2D.LineTo(tc2);
                            paint2D.LineTo(tc0);
                            paint2D.ClosePath();
                            paint2D.Stroke();
                        }
                    }
                }
            }
        }

        class UVLayoutElement : VisualElement
        {
            public Image TextureImage;
            public UVGrid UVGrid;

            public SliderInt UVChannelSlider;

            public UVLayoutElement()
            {
                var header = new VisualElement();
                Add(header);
                header.style.flexDirection = FlexDirection.Row;

                {
                    UVChannelSlider = new SliderInt();
                    UVChannelSlider.RegisterValueChangedCallback((e) => UVGrid.SetChannelIdx(e.newValue));
                    UVChannelSlider.lowValue = 0;
                    UVChannelSlider.highValue = 7;
                    UVChannelSlider.label = "TexCoord";
                    UVChannelSlider.style.width = new StyleLength(new Length(100.0f, LengthUnit.Percent));
                    UVChannelSlider.showInputField = true;
                    header.Add(UVChannelSlider);
                }

                var scrollView = new ScrollView();
                scrollView.mode = ScrollViewMode.VerticalAndHorizontal;
                Add(scrollView);

                TextureImage = new Image();
                scrollView.Add(TextureImage);
                TextureImage.scaleMode = ScaleMode.ScaleAndCrop;

                UVGrid = new UVGrid();
                TextureImage.Add(UVGrid);
            }

            public void SetSelectedObject(Mesh mesh, Material mat)
            {
                UVGrid.Mesh = mesh;

                var texture = mat.mainTexture;
                TextureImage.image = texture;
                UVGrid.style.width = texture.width;
                UVGrid.style.height = texture.height;
                TextureImage.style.width = texture.width;
                TextureImage.style.height = texture.height;

                UVGrid.MarkDirtyRepaint();
            }
        }
    }
}
