#if UNITY_EDITOR
using System.Collections;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;
using System.Linq;

[CustomPropertyDrawer(typeof(PowerGradient))]
public class PowerGradientDrawer : PropertyDrawer
{
    public const string NAME_COPY = "C";
    public const string NAME_PASTE = "P";

    static PowerGradient copyTarget;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        Event guiEvent = Event.current;

        EditorGUI.BeginProperty(position, label, property);
        {
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            PowerGradient gradient = GetDataObject(property) as PowerGradient;

            if (gradient == null)
            {
                return;
            }
            var textureRect = new Rect(position.x, position.y, position.width - 40, position.height);
            var copyButtonRect = new Rect(textureRect.xMax + 1, textureRect.y, 20, textureRect.height);
            var pasteButtonRect = new Rect(copyButtonRect.xMax, textureRect.y, 20, textureRect.height);

            if (guiEvent.type == EventType.Repaint)
            {
                GUIStyle gradientStyle = new GUIStyle();
                gradientStyle.normal.background = gradient.GetTexture((int)position.width);

                GUI.Label(textureRect, GUIContent.none, gradientStyle);
                GUI.Button(copyButtonRect, NAME_COPY);
                GUI.Button(pasteButtonRect, NAME_PASTE);
            }
            else
            {
                HandleAction(guiEvent, gradient,property, ref textureRect, ref copyButtonRect, ref pasteButtonRect);

            }
        }
        EditorGUI.EndProperty();
    }

    private void HandleAction(Event guiEvent, PowerGradient gradient,SerializedProperty sp, ref Rect textureRect, ref Rect copyButtonRect, ref Rect pasteButtonRect)
    {
        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0)
        {
            if (textureRect.Contains(guiEvent.mousePosition))
            {
                PowerGradientWindow window = EditorWindow.GetWindow<PowerGradientWindow>();
                window.SetGradient(gradient,sp);
            }
            else if (copyButtonRect.Contains(guiEvent.mousePosition))
            {
                Copy(gradient);
            }
            else if (pasteButtonRect.Contains(guiEvent.mousePosition))
            {
                Paste(copyTarget, gradient);
            }
        }
    }

    public object GetDataObject(SerializedProperty prop)
    {
        var path = prop.propertyPath.Replace(".Array.data[", "[");
        object obj = prop.serializedObject.targetObject;

        var elements = path.Split('.');
        foreach (var element in elements)
        {
            if (element.Contains("["))
            {
                var elementName = element.Substring(0, element.IndexOf("["));
                var index = Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                obj = GetValue(obj, elementName, index);

            }
            else
            {
                obj = GetValue(obj, element);
            }
        }
        return obj;
    }

    public object GetValue(object source, string name)
    {
        if (source == null)
            return null;
        var type = source.GetType();
        var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (f == null)
        {
            var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (p == null)
                return null;
            return p.GetValue(source, null);
        }
        return f.GetValue(source);
    }

    public object GetValue(object source, string name, int index)
    {
        var enumerable = GetValue(source, name) as IEnumerable;
        var enm = enumerable.GetEnumerator();
        try
        {
            while (index-- >= 0)
                enm.MoveNext();
            return enm.Current;
        }
        catch
        {
            return null;
        }
    }

    void Copy(PowerGradient g)
    {
        copyTarget = g;
    }

    void Paste(PowerGradient from,PowerGradient to)
    {
        if (from != null && to != null)
        {
            to.CopyFrom(from);
        }
    }
}
#endif