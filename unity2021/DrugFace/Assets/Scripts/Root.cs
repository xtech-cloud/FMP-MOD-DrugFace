
using UnityEngine;

/// <summary>
/// 根程序类
/// </summary>
/// <remarks>
/// 不参与模块编译，仅用于在编辑器中开发调试
/// </remarks>
public class Root : RootBase
{
    private void Awake()
    {
        doAwake();
    }

    private void Start()
    {
        entry_.__DebugPreload(exportRoot);
    }

    private void OnDestroy()
    {
        doDestroy();
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(0, 0, 120, 60), "Create"))
        {
            entry_.__DebugCreate("test", "default");
        }

        if (GUI.Button(new Rect(0, 60, 120, 60), "Open"))
        {
            entry_.__DebugOpen("test", "file", "", 0.5f);
        }

        if (GUI.Button(new Rect(0, 120, 120, 60), "Close"))
        {
            entry_.__DebugClose("test", 0.5f);
        }

        if (GUI.Button(new Rect(0, 180, 120, 60), "Delete"))
        {
            entry_.__DebugDelete("test");
        }
    }
}

