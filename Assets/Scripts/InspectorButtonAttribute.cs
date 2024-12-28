using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
#endif

[System.AttributeUsage(System.AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public class InspectorButtonAttribute : System.Attribute
{
    public string ButtonText { get; private set; }

    public InspectorButtonAttribute(string text)
    {
        ButtonText = text;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(Object), true)]
[CanEditMultipleObjects]
public class InspectorButtonEditor : Editor
{
    const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

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
                    else
                    {
                        if (GUILayout.Button(btn.ButtonText))
                        {
                            method.Invoke(target, null);
                        }
                    }
                }
            }
        }
    }
}
#endif