using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;

namespace Unity.HLODSystem
{
    [CustomEditor(typeof(MaterialMapping))]
    public class MaterialMappingEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var materialMapping = target as MaterialMapping;

            var materialMappingEditor = new MaterialMappingElement();
            materialMappingEditor.Bind(null, materialMapping);

            return materialMappingEditor;
        }
    }

    public class ShaderDropdownField : VisualElement
    {
        public DropdownField Dropdown;

        public string InvalidName = "None";

        public ShaderDropdownField(string label, Action<Shader> onValueChanged)
        {
            var shaders = ShaderUtil.GetAllShaderInfo();
            var shaderNames = new List<string>(shaders.Length + 1);


            for (var s = 0; s < shaders.Length; ++s)
            {
                var shaderName = shaders[s].name;

                shaderNames.Add(shaderName);
            }
            shaderNames.Add(InvalidName);

            Dropdown = new DropdownField() { label = label, choices = shaderNames};
            Add(Dropdown);

            Dropdown.RegisterValueChangedCallback((e) =>
            {
                Shader shader = null;
                if(e.newValue != InvalidName)
                {
                    shader = Shader.Find(e.newValue);
                }
                onValueChanged(shader);
            });
        }

        public Shader value { get { return Shader.Find(Dropdown.value); } }
        public void SetValueWithoutNotify(Shader shader)
        {
            if (shader != null)
            {
                Dropdown.SetValueWithoutNotify(shader.name);
            }
            else
            {
                Dropdown.SetValueWithoutNotify(InvalidName);
            }
        }
    }

    public class MaterialMappingElement : VisualElement
    {
        MaterialMapping Mapping;
        HLODBase Hlod;

        private List<string> inputTexturePropertyNames = null;
        private List<string> inputColorPropertyNames = null;
        private List<string> outputTexturePropertyNames = null;
        private List<string> outputColorPropertyNames = null;
        private List<string> defaultColorNames = null;

        ShaderDropdownField ShaderDropdown;
        Toggle EnableTintColor;
        DropdownField TintColorInputDropdown;
        DropdownField TintColorOutputDropdown;
        DropdownField TintColorOutputTextureDropdown;
        ListView TextureSlots;


        public MaterialMapping value
        {
            get { return Mapping; }
        }

        public HLODBase HLOD
        {
            get { return Hlod; }
        }

        public void RefreshShaderProperties()
        {
            var resolvedShader = Mapping.Shader != null ? Mapping.Shader : Utils.GraphicsUtils.GetDefaultShader();

            outputTexturePropertyNames = GetTexturePropertyNames(resolvedShader);
            outputColorPropertyNames = GetColorPropertyNames(resolvedShader);

            if (Hlod != null)
            {
                inputTexturePropertyNames = GetAllMaterialTexturePropertyNames(Hlod.gameObject);
                inputColorPropertyNames = GetAllMaterialColorPropertyNames(Hlod.gameObject);
            }
            else
            {
                inputTexturePropertyNames = outputTexturePropertyNames;
                inputColorPropertyNames = outputColorPropertyNames;
            }
        }

        class TextureSlotField : VisualElement
        {
            ListView SlotListView;
            ListView InputTextureList;
            ListView InputColorList;
            DropdownField OutputDropdown;
            DropdownField DefaultColorDropdown;

            TextureInfo TextureInfo;

            public TextureSlotField(List<string> inputTexturePropertyNames, List<string> inputColorPropertyNames, List<string> outputTexturePropertyNames, List<string> defaultColorNames)
            {
                style.height = StyleKeyword.Auto;

                var foldout = new Foldout();
                Add(foldout);
                foldout.contentContainer.style.flexDirection = FlexDirection.Row;

                {
                    InputTextureList = new ListView();
                    foldout.Add(InputTextureList);

                    InputTextureList.makeItem = () =>
                    {
                        var inputSlotTemplate = new DropdownField();
                        inputSlotTemplate.choices = inputTexturePropertyNames;
                        inputSlotTemplate.RegisterValueChangedCallback((e) =>
                        {
                            TextureInfo.InputTexturePropertyNames[(int)inputSlotTemplate.userData] = e.newValue;
                        });
                        return inputSlotTemplate;
                    };
                    InputTextureList.bindItem = (e, idx) =>
                    {
                        e.userData = idx;
                        var inputDropdown = e as DropdownField;
                        inputDropdown.SetValueWithoutNotify(TextureInfo.InputTexturePropertyNames[idx]);
                    };
                    InputTextureList.showAddRemoveFooter = true;
                    InputTextureList.style.width = new Length(25.0f, LengthUnit.Percent);
                }

                {
                    InputColorList = new ListView();
                    foldout.Add(InputColorList);

                    InputColorList.makeItem = () =>
                    {
                        var inputSlotTemplate = new DropdownField();
                        inputSlotTemplate.choices = inputColorPropertyNames;
                        inputSlotTemplate.RegisterValueChangedCallback((e) =>
                        {
                            TextureInfo.InputColorPropertyNames[(int)inputSlotTemplate.userData] = e.newValue;
                        });
                        return inputSlotTemplate;
                    };
                    InputColorList.bindItem = (e, idx) =>
                    {
                        e.userData = idx;
                        var inputDropdown = e as DropdownField;
                        inputDropdown.SetValueWithoutNotify(TextureInfo.InputColorPropertyNames[idx]);
                    };
                    InputColorList.showAddRemoveFooter = true;
                    InputColorList.style.width = new Length(25.0f, LengthUnit.Percent);
                }

                var lineHeight = 22;

                OutputDropdown = new DropdownField();
                OutputDropdown.choices = outputTexturePropertyNames;
                OutputDropdown.RegisterValueChangedCallback((e) =>
                {
                    TextureInfo.OutputName = e.newValue;
                });
                foldout.Add(OutputDropdown);
                OutputDropdown.style.height = lineHeight;
                OutputDropdown.style.width = new Length(25.0f, LengthUnit.Percent);

                DefaultColorDropdown = new DropdownField();
                DefaultColorDropdown.choices = defaultColorNames;
                DefaultColorDropdown.RegisterValueChangedCallback((e) =>
                {
                    if (Enum.TryParse(e.newValue, out PackingType defaultColor))
                    {
                        TextureInfo.Type = defaultColor;
                    }
                });
                foldout.Add(DefaultColorDropdown);
                DefaultColorDropdown.style.height = lineHeight;
                DefaultColorDropdown.style.width = new Length(25.0f, LengthUnit.Percent);

                //var deleteButton = new Button();
                //deleteButton.clicked += () => SlotListView.itemsSource.Remove(TextureInfo);
                //Add(deleteButton);
            }

            public void Bind(ListView slotListView, TextureInfo slot)
            {
                SlotListView = slotListView;
                TextureInfo = slot;

                InputTextureList.itemsSource = slot.InputTexturePropertyNames;
                InputTextureList.RefreshItems();
                InputColorList.itemsSource = slot.InputColorPropertyNames;
                InputColorList.RefreshItems();
                OutputDropdown.value = slot.OutputName;
                DefaultColorDropdown.value = slot.Type.ToString();
            }
        }

        public MaterialMappingElement()
        {
            var packingTypes = Enum.GetValues(typeof(PackingType));
            defaultColorNames = new List<string>();
            foreach (var packingType in packingTypes)
            {
                defaultColorNames.Add(packingType.ToString());
            }

            ShaderDropdown = new ShaderDropdownField("Shader", (s) => {
                Mapping.Shader = s;
                var resolvedShader = s != null ? s : Utils.GraphicsUtils.GetDefaultShader();

                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(resolvedShader, out string shaderGUID,
                    out long localShaderID))
                {
                    Mapping.ShaderGUID = shaderGUID;
                }

                RefreshShaderProperties();
            });
            ShaderDropdown.tooltip = $"A value of {ShaderDropdown.InvalidName} equals the value of Preferences/HLOD/Default Shader.";
            Add(ShaderDropdown);

            var textureSlotFoldout = new Foldout() { text = "Textures" };           
            {
                var header = new VisualElement();
                header.style.flexDirection = FlexDirection.Row;
                textureSlotFoldout.Add(header);

                var headerStub = new VisualElement();
                headerStub.style.width = 35.0f;
                header.Add(headerStub);

                var headerLabels = new VisualElement();
                headerLabels.style.flexDirection = FlexDirection.Row;
                headerLabels.style.flexGrow = 1;
                header.Add(headerLabels);

                var inputTexturesLabel = new Label() { text = "Input Textures" };
                inputTexturesLabel.style.width = new Length(25.0f, LengthUnit.Percent);
                headerLabels.Add(inputTexturesLabel);
                var inputColorsLabel = new Label() { text = "Input Colors" };
                inputColorsLabel.style.width = new Length(25.0f, LengthUnit.Percent);
                headerLabels.Add(inputColorsLabel);
                var outputTextureLabel = new Label() { text = "Output Texture" };
                outputTextureLabel.style.width = new Length(25.0f, LengthUnit.Percent);
                headerLabels.Add(outputTextureLabel);
                var defaultColor = new Label() { text = "Default Color" };
                defaultColor.style.width = new Length(25.0f, LengthUnit.Percent);
                headerLabels.Add(defaultColor);

                TextureSlots = new ListView();
                textureSlotFoldout.Add(TextureSlots);
                TextureSlots.makeItem = () =>
                {
                    return new TextureSlotField(inputTexturePropertyNames, inputColorPropertyNames, outputTexturePropertyNames, defaultColorNames);
                };
                TextureSlots.bindItem = (element, idx) =>
                {
                    var slot = Mapping.TextureInfoList[idx];
                    if(slot == null)
                    {
                        slot = new TextureInfo();
                        Mapping.TextureInfoList[idx] = slot;
                    }
                    (element as TextureSlotField).Bind(TextureSlots, slot);
                };
                TextureSlots.showAddRemoveFooter = true;
                TextureSlots.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;

                //var updateTexturePropertiesButton = new Button() { text = "Add new texture property" };
                //Add(updateTexturePropertiesButton);
                //updateTexturePropertiesButton.clicked += () =>
                //{
                //    Mapping.TextureInfoList.Add(new TextureInfo());
                //};

                var refreshTexturePropertiesButton = new Button() { text = "Refresh Shader Properties" };
                Add(refreshTexturePropertiesButton);
                refreshTexturePropertiesButton.clicked += () =>
                {
                    //TODO: Need to update automatically if shader changes
                    RefreshShaderProperties();
                };
            }
            Add(textureSlotFoldout);
        }

        static List<string> GetTexturePropertyNames(Shader shader)
        {
            var mat = new Material(shader);
            return new List<string>(mat.GetTexturePropertyNames());
        }        
        
        static List<string> GetColorPropertyNames(Shader shader)
        {
            List<string> colorPropertyNames = new List<string>();
            int propertyCount = ShaderUtil.GetPropertyCount(shader);
            for (int i = 0; i < propertyCount; ++i)
            {
                string name = ShaderUtil.GetPropertyName(shader, i);
                if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.Color)
                {
                    colorPropertyNames.Add(name);
                }
            }
            return colorPropertyNames;
        }

        static List<string> GetAllMaterialTexturePropertyNames(GameObject root)
        {
            var meshRenderers = root.GetComponentsInChildren<MeshRenderer>();
            HashSet<string> uniquePropertyNames = new HashSet<string>();
            for (int m = 0; m < meshRenderers.Length; ++m)
            {
                var mesh = meshRenderers[m];
                foreach (Material material in mesh.sharedMaterials)
                {
                    if (material == null)
                        continue;

                    var names = material.GetTexturePropertyNames();
                    for (int n = 0; n < names.Length; ++n)
                    {
                        uniquePropertyNames.Add(names[n]);
                    }
                }

            }

            return new List<string>(uniquePropertyNames);
        }

        static List<string> GetAllMaterialColorPropertyNames(GameObject root)
        {
            var meshRenderers = root.GetComponentsInChildren<MeshRenderer>();
            var uniqueShaders = new HashSet<Shader>();
            for (int m = 0; m < meshRenderers.Length; ++m)
            {
                var mesh = meshRenderers[m];
                foreach (Material material in mesh.sharedMaterials)
                {
                    if (material == null)
                        continue;

                    uniqueShaders.Add(material.shader);
                }
            }

            HashSet<string> uniquePropertyNames = new HashSet<string>();
            foreach (var shader in uniqueShaders)
            {
                var names = GetColorPropertyNames(shader);
                for (int n = 0; n < names.Count; ++n)
                {
                    uniquePropertyNames.Add(names[n]);
                }
            }

            return new List<string>(uniquePropertyNames);
        }

        public void Bind(HLODBase hlod, MaterialMapping mapping)
        {
            Hlod = hlod;
            Mapping = mapping;

            if (mapping != null)
            {
                ShaderDropdown.SetValueWithoutNotify(mapping.Shader);
                RefreshShaderProperties();

                TextureSlots.itemsSource = mapping.TextureInfoList;
                TextureSlots.RefreshItems();
            }
        }
    }
}