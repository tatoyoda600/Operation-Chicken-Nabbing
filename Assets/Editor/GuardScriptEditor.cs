using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Reflection;

[CustomEditor(typeof(GuardScript))]
public class GuardScriptEditor : Editor
{
    Color handleColor = Color.cyan;
    Color defaultColor = Color.grey;
    Color selectedHandleColor = Color.yellow;
    Color pathColor = Color.magenta;

    const float size = 0.3f;
    PathWeb.WebNode selectedNode;
    const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    static GUIStyle style = null;

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

    int IndexOfConnection(GuardScript component, string node1, string node2)
    {
        if (component.path.Contains(node1) && component.path.Contains(node2))
        {
            // Find the last instance of selectedNode followed by node1, and delete all from there to the end (if found)
            for (int p = component.path.Count - 2; p >= 0; p--)
            {
                if (component.path[p].Equals(node1) && component.path[p + 1].Equals(node2))
                {
                    return p;
                }
            }
        }

        return -1;
    }

    private void OnSceneGUI()
    {
        GuardScript component = (GuardScript)target;
        PathWeb pathWeb = component.pathWeb;

        if (pathWeb)
        {
            HashSet<int> checkedNodes = new HashSet<int>();
            bool selectedHandle = false;
            Handles.color = handleColor;

            // For each node
            for (int i = 0; i < component.pathWeb.nodes.Count; i++)
            {
                PathWeb.WebNode node1 = pathWeb.nodes[i];
                Handles.color = node1.Equals(selectedNode) ? selectedHandleColor : handleColor;

                // For each of the node's connections
                for (int j = 0; j < node1.connections.Count; j++)
                {
                    PathWeb.WebNode node2 = pathWeb.GetWebNode(node1.connections[j]);
                    if (node2 != null)
                    {
                        Handles.color = IndexOfConnection(component, node1.name, node2.name) >= 0 ? pathColor : Handles.color;
                        // If the connection hasn't been checked, or it has been checked but it doesn't contain a link back to the node
                        //  (aka. no line has been drawn between them)
                        if (!checkedNodes.Contains(node2.id) || !node2.connections.Contains(node1.id))
                        {
                            if (Handles.color == pathColor)
                            {
                                // If the connection is used
                                Handles.DrawDottedLine(node1.position, node2.position, 4);
                            }
                            else if (IndexOfConnection(component, node2.name, node1.name) >= 0)
                            {
                                // If the inverse connection is used
                                Color temp = Handles.color;
                                Handles.color = pathColor;
                                Handles.DrawDottedLine(node1.position, node2.position, 4);
                                Handles.color = temp;
                            }
                            else if (node2.Equals(selectedNode))
                            {
                                // If the connection is the selected node
                                Handles.color = selectedHandleColor;
                                Handles.DrawDottedLine(node1.position, node2.position, 4);
                            }
                            else
                            {
                                // If the connection is unselected and unused
                                Handles.DrawDottedLine(node1.position, node2.position, 4);
                            }
                        }
                        Vector3 arrowDirection = (node2.position - node1.position).normalized;
                        Handles.ConeHandleCap(GUIUtility.GetControlID(FocusType.Passive), node2.position - arrowDirection * size * 0.5f, Quaternion.LookRotation(arrowDirection), size, EventType.Repaint);
                        Handles.color = node1.Equals(selectedNode) ? selectedHandleColor : handleColor;
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

            // For each node
            for (int i = 0; i < pathWeb.nodes.Count; i++)
            {
                PathWeb.WebNode node1 = pathWeb.nodes[i];
                Handles.color = node1.Equals(selectedNode) ? selectedHandleColor : defaultColor;

                // Draw the node handle
                Handles.PositionHandleIds id = Handles.PositionHandleIds.@default;
                Handles.PositionHandle(id, node1.position, Quaternion.identity);
                Handles.CircleHandleCap(GUIUtility.GetControlID(FocusType.Passive), node1.position, Quaternion.identity, 0.5f, EventType.Repaint);
                DrawText(node1.name, node1.position, new Vector2(50, 100), Handles.color == defaultColor ? Color.white : Handles.color);

                Handles.color = node1.Equals(selectedNode) ? selectedHandleColor : handleColor;

                // If the node handle was selected
                List<int> ids = new List<int> { id.x, id.xy, id.xyz, id.xz, id.y, id.yz, id.z };
                if (ids.Contains(GUIUtility.hotControl))
                {
                    selectedHandle = true;
                    if (selectedNode != null)
                    {
                        if (Event.current.shift)
                        {
                            if (selectedNode.connections.Contains(node1.id))
                            {
                                // If the selected node is not in the path
                                if (!component.path.Contains(selectedNode.name))
                                {
                                    // If it's the 1st node for the path
                                    if (component.path.Count <= 0)
                                    {
                                        component.path.Add(selectedNode.name);
                                        component.path.Add(node1.name);
                                        selectedNode = node1;
                                    }
                                    else
                                    {
                                        Debug.LogWarning("Can't include non-connected nodes in guard path");
                                    }
                                }
                                else
                                {
                                    // If the path contains selectedNode
                                    // Ensure that selectedNode is the last one in the path (Otherwise clear nodes until it is)
                                    for (int p = component.path.Count - 1; p >= 0; p--)
                                    {
                                        if (component.path[p].Equals(selectedNode.name))
                                        {
                                            int nextP = p + 1;
                                            if (nextP < component.path.Count)
                                            {
                                                component.path.RemoveRange(nextP, component.path.Count - nextP);
                                            }
                                            break;
                                        }
                                    }

                                    // Add the new node to the path, then change the selection to node1
                                    component.path.Add(node1.name);
                                    selectedNode = node1;
                                }
                            }
                        }
                        else if (Event.current.control)
                        {
                            // If selectedNode and node1 are in the path
                            if (selectedNode.connections.Contains(node1.id))
                            {
                                // Find the last instance of selectedNode followed by node1, and delete all from there to the end (if found)
                                int deletionIndex = IndexOfConnection(component, selectedNode.name, node1.name) + 1;
                                if (deletionIndex >= 0)
                                {
                                    component.path.RemoveRange(deletionIndex, component.path.Count - deletionIndex);
                                }
                            }
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
                }
            }

            if (selectedNode != null && !selectedHandle && !Event.current.shift && !Event.current.control && GUIUtility.hotControl != 0)
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
