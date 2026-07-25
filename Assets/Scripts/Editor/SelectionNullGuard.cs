using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[InitializeOnLoad]
public static class SelectionNullGuard
{
    private static double nextRepairTime;

    static SelectionNullGuard()
    {
        EditorApplication.delayCall += RemoveNullSelectionTargets;
        EditorApplication.delayCall += UnlockStaleInspectors;
        EditorApplication.update += RepairEditorSelection;
        Selection.selectionChanged += RemoveNullSelectionTargets;
        EditorApplication.hierarchyChanged += RemoveNullSelectionTargets;
    }

    [MenuItem("TinyMonsterKeeper/Tools/Repair Stale Inspector Selection")]
    public static void RepairNow()
    {
        Selection.objects = new Object[0];
        UnlockStaleInspectors();
        InternalEditorUtility.RepaintAllViews();
    }

    private static void RepairEditorSelection()
    {
        if (EditorApplication.timeSinceStartup < nextRepairTime)
            return;

        nextRepairTime = EditorApplication.timeSinceStartup + 0.5d;
        RemoveNullSelectionTargets();
    }

    private static void RemoveNullSelectionTargets()
    {
        Object[] selectedObjects = Selection.objects;
        if (selectedObjects == null || selectedObjects.Length == 0)
            return;

        Object[] validObjects = selectedObjects.Where(selectedObject => selectedObject != null).ToArray();
        if (validObjects.Length != selectedObjects.Length)
            Selection.objects = validObjects;
    }

    private static void UnlockStaleInspectors()
    {
        System.Type inspectorType = typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");
        if (inspectorType == null)
            return;

        Object[] inspectors = Resources.FindObjectsOfTypeAll(inspectorType);
        PropertyInfo isLockedProperty = inspectorType.GetProperty("isLocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        MethodInfo repaintMethod = inspectorType.GetMethod("Repaint", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        for (int i = 0; i < inspectors.Length; i++)
        {
            Object inspector = inspectors[i];
            if (inspector == null)
                continue;

            if (isLockedProperty != null && isLockedProperty.CanWrite)
                isLockedProperty.SetValue(inspector, false);

            repaintMethod?.Invoke(inspector, null);
        }
    }
}
