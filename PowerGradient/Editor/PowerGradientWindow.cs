#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class PowerGradientWindow : EditorWindow
{
    SerializedProperty property;
    PowerGradient gradient;
    const int borderSize = 10;
    const float keyWidth = 10;
    const float keyHeight = 20;

    Rect gradientPreviewRect;
    Rect[] keyRects;
    bool mouseIsDownOverKey;
    int selectedKeyIndex;

    Rect keyInteractiveRect;

    private void OnGUI()
    {

        Draw();

        if (HandleInput())
        {
            Repaint();
        }
    }

    void Draw()
    {
        EditorGUIUtility.labelWidth = 100;

        gradientPreviewRect = new Rect(borderSize, borderSize, position.width - borderSize * 2, 25);
        keyInteractiveRect = new Rect(borderSize,gradientPreviewRect.yMax + borderSize,gradientPreviewRect.width, borderSize + keyHeight);

        GUI.DrawTexture(gradientPreviewRect, gradient.GetTexture((int)gradientPreviewRect.width));
        var keyRectHeight = DrawInteractiveKeys();

        Rect settingsRect = new Rect(borderSize, keyRectHeight + borderSize, position.width - borderSize * 2, position.height + 100);
        DrawSettings(settingsRect);
    }

    private void DrawSettings(Rect settingsRect)
    {
        GUILayout.BeginArea(settingsRect);
        {
            DrawColorAndProgress();

            gradient.blendMode = (PowerGradient.BlendMode)EditorGUILayout.EnumPopup("Blend mode", gradient.blendMode);
            gradient.randomizeColour = EditorGUILayout.Toggle("Randomize colour", gradient.randomizeColour);
            gradient.range = EditorGUILayout.Vector2Field("Range", gradient.range);
        }
        GUILayout.EndArea();
    }

    private void DrawColorAndProgress()
    {
        GUILayout.BeginHorizontal("box");
        {
            EditorGUI.BeginChangeCheck();
            var key = gradient.GetKey(selectedKeyIndex);

            Color newColour = EditorGUILayout.ColorField("Color", key.Colour);
            //progress
            var time = key.Time;
            time = EditorGUILayout.FloatField("Location", time * gradient.range.y, GUILayout.Width(200));
            time = Mathf.Clamp(time, gradient.range.x, gradient.range.y);
            if (EditorGUI.EndChangeCheck())
            {
                gradient.UpdateKeyColour(selectedKeyIndex, newColour);
                selectedKeyIndex = gradient.UpdateKeyTime(selectedKeyIndex, time / gradient.range.y);
            }
        }
        GUILayout.EndHorizontal();
    }

    float DrawInteractiveKeys()
    {
        keyRects = new Rect[gradient.NumKeys];
        for (int i = 0; i < gradient.NumKeys; i++)
        {
            PowerGradient.ColourKey key = gradient.GetKey(i);
            Rect keyRect = new Rect(gradientPreviewRect.x + gradientPreviewRect.width * key.Time - keyWidth / 2f, gradientPreviewRect.yMax + borderSize, keyWidth, keyHeight);
            if (i == selectedKeyIndex)
            {
                EditorGUI.DrawRect(new Rect(keyRect.x - 2, keyRect.y - 2, keyRect.width + 4, keyRect.height + 4), Color.black);
            }
            //vertical line
            DrawVerticalLine(ref key, ref keyRect);

            EditorGUI.DrawRect(keyRect, key.Colour);
            keyRects[i] = keyRect;
        }
        return keyRects[0].yMax;
    }

    private void DrawVerticalLine(ref PowerGradient.ColourKey key, ref Rect keyRect)
    {
        var lineSize = new Vector3(1, -keyRect.height);
        var linePos = new Vector2(keyRect.center.x, keyRect.position.y);
        EditorGUI.DrawRect(new Rect(linePos, lineSize), key.Colour);
    }

    void ProgressBar(float value,string text)
    {
        var r = EditorGUILayout.BeginVertical(GUILayout.Width(100));
        EditorGUI.ProgressBar(r,value,text);
        GUILayout.Space(18);
        EditorGUILayout.EndHorizontal();
    }

    bool HandleInput()
    {
        Event guiEvent = Event.current;
        return HandleMouse(guiEvent) || HandleKeyboard(guiEvent);
    }

    private bool HandleKeyboard(Event guiEvent)
    {
        if (guiEvent.keyCode == KeyCode.Delete && guiEvent.type == EventType.KeyDown)
        {
            gradient.RemoveKey(selectedKeyIndex);
            if (selectedKeyIndex >= gradient.NumKeys)
            {
                selectedKeyIndex--;
            }
            return true;
        }
        return false;
    }

    private bool HandleMouse(Event guiEvent)
    {
        var needsRepaint = false;
        if (guiEvent.button != 0)
            return false;

        if (guiEvent.type == EventType.MouseDown)
        {
            for (int i = 0; i < keyRects.Length; i++)
            {
                if (keyRects[i].Contains(guiEvent.mousePosition))
                {
                    selectedKeyIndex = i;
                    mouseIsDownOverKey = true;
                    needsRepaint = true;
                    break;
                }
            }

            if (keyInteractiveRect.Contains(guiEvent.mousePosition) && !mouseIsDownOverKey)
            {

                float keyTime = Mathf.InverseLerp(gradientPreviewRect.x, gradientPreviewRect.xMax, guiEvent.mousePosition.x);
                Color interpolatedColour = gradient.Evaluate(keyTime);
                Color randomColour = new Color(Random.value, Random.value, Random.value);

                selectedKeyIndex = gradient.AddKey((gradient.randomizeColour) ? randomColour : interpolatedColour, keyTime);
                mouseIsDownOverKey = true;
                needsRepaint = true;
            }
        }

        if (guiEvent.type == EventType.MouseUp)
        {
            mouseIsDownOverKey = false;
        }

        if (mouseIsDownOverKey && guiEvent.type == EventType.MouseDrag)
        {
            float keyTime = Mathf.InverseLerp(gradientPreviewRect.x, gradientPreviewRect.xMax, guiEvent.mousePosition.x);
            selectedKeyIndex = gradient.UpdateKeyTime(selectedKeyIndex, keyTime);
            needsRepaint = true;
        }
        return needsRepaint;
    }

    public void SetGradient(PowerGradient gradient,SerializedProperty sp)
    {
        this.gradient = gradient;
        this.property = sp;
    }

    private void OnEnable()
    {
        titleContent.text = "Gradient Editor";
        minSize = new Vector2(400, 200);
        maxSize = new Vector2(1920, 200);
        position.Set(position.x, position.y, minSize.x, minSize.y);
    }

    private void OnDisable()
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}
#endif