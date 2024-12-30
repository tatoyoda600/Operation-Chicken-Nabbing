using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Reflection;

[CustomEditor(typeof(PathWeb))]
public class PathWebEditor : Editor
{
    Color handleColor = Color.cyan;
    Color enabledColor = Color.green;
    Color disabledColor = Color.gray;
    Color selectedHandleColor = Color.yellow;

    const float size = 0.3f;
    PathWeb.WebNode selectedNode;
    const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    static GUIStyle style = null;
    float clickCooldownTime = 0.1f;
    float clickCooldown = 0f;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        foreach (Object target in targets)
        {
            foreach (MethodInfo method in target.GetType().GetMethods(flags))
            {
                InspectorButtonAttribute btn = method.GetCustomAttribute<InspectorButtonAttribute>();
                if (btn != null)
                {
                    if (method.GetParameters().Length > 0)
                    {
                        throw new TargetParameterCountException("InspectorButton attribute can not be used on methods with parameters.");
                    }
                    else if (GUILayout.Button(btn.ButtonText))
                    {
                        method.Invoke(target, null);
                    }
                }
            }
        }
    }

    private void OnSceneGUI()
    {
        PathWeb component = (PathWeb)target;
        HashSet<int> checkedNodes = new HashSet<int>();
        bool selectedHandle = false;
        Handles.color = handleColor;

        for (int i = 0; i < component.nodes.Count; i++)
        {
            PathWeb.WebNode node1 = component.nodes[i];
            Handles.color = node1.Equals(selectedNode) ? selectedHandleColor : handleColor;

            for (int j = 0; j < node1.connections.Count; j++)
            {
                PathWeb.WebNode node2 = component.GetWebNode(node1.connections[j]);
                if (node2 != null)
                {
                    if (!checkedNodes.Contains(node2.id) || !node2.connections.Contains(node1.id))
                    {
                        if (node2.Equals(selectedNode))
                        {
                            Handles.color = selectedHandleColor;
                            Handles.DrawDottedLine(node1.position, node2.position, 4);
                            Handles.color = handleColor;
                        }
                        else
                        {
                            Handles.DrawDottedLine(node1.position, node2.position, 4);
                        }
                    }
                    Vector3 arrowDirection = (node2.position - node1.position).normalized;
                    Handles.ConeHandleCap(GUIUtility.GetControlID(FocusType.Passive), node2.position - arrowDirection * size * 0.5f, Quaternion.LookRotation(arrowDirection), size, EventType.Repaint);
                }
                else
                {
                    node1.connections.RemoveAt(j);
                    j--;
                }
            }

            checkedNodes.Add(node1.id);
            Handles.color = handleColor;
        }

        for (int i = 0; i < component.nodes.Count; i++)
        {
            PathWeb.WebNode node1 = component.nodes[i];
            Handles.color = node1.Equals(selectedNode) ? selectedHandleColor : (node1.active ? enabledColor : disabledColor);

            Handles.PositionHandleIds id = Handles.PositionHandleIds.@default;
            node1.position = Handles.PositionHandle(id, node1.position, Quaternion.identity);
            Handles.CircleHandleCap(GUIUtility.GetControlID(FocusType.Passive), node1.position, Quaternion.identity, 0.5f, EventType.Repaint);
            DrawText(node1.name, node1.position, new Vector2(50, 100), Handles.color == disabledColor ? Color.white : Handles.color);

            Handles.color = node1.Equals(selectedNode) ? selectedHandleColor : handleColor;

            List<int> ids = new List<int> { id.x, id.xy, id.xyz, id.xz, id.y, id.yz, id.z };
            if (clickCooldown <= Time.realtimeSinceStartup && ids.Contains(GUIUtility.hotControl))
            {
                selectedHandle = true;
                if (selectedNode != null)
                {
                    if (Event.current.shift)
                    {
                        PathWeb.WebNode.ConnectNodes(ref selectedNode, ref node1, component.bidirectional);
                        GUI.changed = true;
                    }
                    else if (Event.current.control)
                    {
                        PathWeb.WebNode.DisconnectNodes(ref selectedNode, ref node1, true);
                        GUI.changed = true;
                    }
                    else
                    {
                        selectedNode = node1;
                    }
                }
                else
                {
                    selectedNode = node1;
                }
                clickCooldown = Time.realtimeSinceStartup + clickCooldownTime;
                break;
            }
        }

        if (selectedNode != null)
        {
            /*
            if (Event.current.type == EventType.KeyUp && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
            {
                selectedNode.active = !selectedNode.active;
                Event.current.Use();
            }
            else
            */
            if (Event.current.type == EventType.KeyUp && (Event.current.keyCode == KeyCode.Delete || Event.current.keyCode == KeyCode.Backspace))
            {
                for (int i = 0; i < component.nodes.Count; i++)
                {
                    PathWeb.WebNode node = component.nodes[i];
                    PathWeb.WebNode.DisconnectNodes(ref node, ref selectedNode, false);
                }
                component.nodes.Remove(selectedNode);
                GUI.changed = true;
                selectedNode = null;
                Event.current.Use();
            }
            else if (!selectedHandle && clickCooldown <= Time.realtimeSinceStartup && !Event.current.shift && !Event.current.control && GUIUtility.hotControl != 0)
            {
                selectedNode = null;
            }
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(component);
            EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
        }
    }

    static public void DrawText(string text, Vector3 worldPos, Vector2 screenOffset = default, Color? color = default, int alignment = 0)
    {
        UnityEditor.Handles.BeginGUI();

        var restoreColor = GUI.color;
        if (color.HasValue)
        {
            GUI.color = color.Value;
        }

        var view = UnityEditor.SceneView.currentDrawingSceneView;
        var screenPos = view.camera.WorldToScreenPoint(worldPos);
        screenPos += new Vector3(screenOffset.x, screenOffset.y, 0f);

        if (screenPos.y >= 0f && screenPos.y <= Screen.height && screenPos.x >= 0f && screenPos.x <= Screen.width && screenPos.z >= 0f)
        {
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label);
                style.fontSize = 30;
            }
            if (color.HasValue)
            {
                style.normal.textColor = color.Value;
            }
            var size = style.CalcSize(new GUIContent(text));

            if (alignment == 0)
            {
                screenPos.x -= (size.x / 2f) + 1f;
            }
            else if (alignment < 0)
            {
                screenPos.x -= size.x - 2f;
            }
            else
            {
                screenPos.x -= 4f;
            }

            GUI.Label(new Rect(screenPos.x, -screenPos.y + view.position.height + 4f, size.x, size.y), text, style);
        }

        GUI.color = restoreColor;
        if (style != null)
        {
            style.normal.textColor = restoreColor;
        }
        UnityEditor.Handles.EndGUI();
    }
}
