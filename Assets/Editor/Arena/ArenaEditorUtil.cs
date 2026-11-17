using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Shared helpers for the Tools > Arena menu: folders, materials, tags and layers, and
/// small factory methods for legacy UI elements (Text, Image, Button, InputField) that
/// work without importing TextMesh Pro essentials.
/// </summary>
public static class ArenaEditorUtil
{
    public const string MaterialsFolder = "Assets/Materials";
    public const string ResourcesFolder = "Assets/Resources";
    public const string ScenesFolder = "Assets/Scenes";

    public static Font DefaultFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    /// <summary>Creates every folder in a path like "Assets/A/B" if it does not exist.</summary>
    public static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }

    /// <summary>Creates or updates a material asset with a flat colour, URP Lit when available.</summary>
    public static Material CreateMaterial(string name, Color color)
    {
        EnsureFolder(MaterialsFolder);
        string path = MaterialsFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    /// <summary>Adds the tags and layers from GameTags to the project if they are missing.</summary>
    public static void EnsureTagsAndLayers()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0)
        {
            Debug.LogError("Could not open TagManager.asset");
            return;
        }
        SerializedObject tagManager = new SerializedObject(assets[0]);

        SerializedProperty tags = tagManager.FindProperty("tags");
        foreach (string tag in new[] { GameTags.PlayerTag, GameTags.ArenaTag, GameTags.SpawnPointTag, GameTags.ProjectileTag })
        {
            if (!ContainsString(tags, tag))
            {
                tags.InsertArrayElementAtIndex(tags.arraySize);
                tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            }
        }

        SerializedProperty layers = tagManager.FindProperty("layers");
        foreach (string layer in new[] { GameTags.PlayerLayer, GameTags.ProjectileLayer, GameTags.ArenaLayer })
        {
            if (ContainsString(layers, layer))
            {
                continue;
            }
            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty slot = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(slot.stringValue))
                {
                    slot.stringValue = layer;
                    break;
                }
            }
        }

        tagManager.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
    }

    private static bool ContainsString(SerializedProperty array, string value)
    {
        for (int i = 0; i < array.arraySize; i++)
        {
            if (array.GetArrayElementAtIndex(i).stringValue == value)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Sets a private [SerializeField] on a component without making the field public.</summary>
    public static void SetReference(Component component, string fieldName, Object value)
    {
        SerializedObject serialized = new SerializedObject(component);
        SerializedProperty property = serialized.FindProperty(fieldName);
        if (property == null)
        {
            Debug.LogError(component.GetType().Name + " has no field named " + fieldName);
            return;
        }
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>Sets a private [SerializeField] array of object references.</summary>
    public static void SetReferenceArray(Component component, string fieldName, IList<Object> values)
    {
        SerializedObject serialized = new SerializedObject(component);
        SerializedProperty property = serialized.FindProperty(fieldName);
        if (property == null)
        {
            Debug.LogError(component.GetType().Name + " has no field named " + fieldName);
            return;
        }
        property.arraySize = values.Count;
        for (int i = 0; i < values.Count; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------------- UI factories ----------------

    /// <summary>Creates a screen-space canvas scaled for 1920x1080 plus an EventSystem if the scene has none.</summary>
    public static Canvas CreateCanvas(string name)
    {
        GameObject go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
        return canvas;
    }

    public static RectTransform CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return rect;
    }

    public static Image CreateImage(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        RectTransform rect = CreateRect(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    public static Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        RectTransform rect = CreateRect(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = DefaultFont;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.text = content;
        text.supportRichText = true;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    public static Button CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        Image background = CreateImage(parent, name, new Color(0.2f, 0.5f, 0.9f), anchorMin, anchorMax, offsetMin, offsetMax);
        Button button = background.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        ColorBlock colors = button.colors;
        colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
        button.colors = colors;
        CreateText(background.transform, "Label", label, 32, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return button;
    }

    public static InputField CreateInputField(Transform parent, string name, string placeholder, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        Image background = CreateImage(parent, name, new Color(0.95f, 0.95f, 0.95f), anchorMin, anchorMax, offsetMin, offsetMax);
        InputField field = background.gameObject.AddComponent<InputField>();
        field.targetGraphic = background;

        Text placeholderText = CreateText(background.transform, "Placeholder", placeholder, 28, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(16f, 0f), new Vector2(-16f, 0f));
        placeholderText.color = new Color(0.5f, 0.5f, 0.5f);
        placeholderText.fontStyle = FontStyle.Italic;

        Text valueText = CreateText(background.transform, "Text", string.Empty, 28, TextAnchor.MiddleLeft, Vector2.zero, Vector2.one, new Vector2(16f, 0f), new Vector2(-16f, 0f));
        valueText.color = Color.black;
        valueText.supportRichText = false;

        field.placeholder = placeholderText;
        field.textComponent = valueText;
        field.characterLimit = 16;
        return field;
    }
}
