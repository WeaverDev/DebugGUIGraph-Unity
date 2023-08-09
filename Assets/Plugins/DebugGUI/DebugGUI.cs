using UnityEngine;
using System.IO;
using WeavUtils;
using System;

public partial class DebugGUI : MonoBehaviour
{
    static bool quitting;
    bool initialized;

    // Ensure a singleton is always present when used
    private static DebugGUI _instance;
    private static DebugGUI Instance
    {
        get
        {
            if (_instance == null && !quitting)
            {
                _instance = FindObjectOfType<DebugGUI>();

                if (_instance == null && Application.isPlaying)
                {
                    _instance = new GameObject("DebugGUI").AddComponent<DebugGUI>();
                }

                if (!_instance.initialized)
                {
                    _instance.Init();
                }
            }
            return _instance;
        }
    }

    public DebugGUISettings _settings;
    public static DebugGUISettings Settings => Instance._settings;

    #region Graph

    /// <summary>
    /// Set the properties of a graph.
    /// </summary>
    /// <param name="key">The graph's key</param>
    /// <param name="label">The graph's label</param>
    /// <param name="min">Value at the bottom of the graph box</param>
    /// <param name="max">Value at the top of the graph box</param>
    /// <param name="group">The graph's ordinal position on screen</param>
    /// <param name="color">The graph's color</param>
    public static void SetGraphProperties(object key, string label, float min, float max, int group, Color color, bool autoScale)
    {
        if (Settings.enableGraphs)
            Instance.graphWindow.SetGraphProperties(key, label, min, max, group, color, autoScale);
    }

    /// <summary>
    /// Add a data point to a graph.
    /// </summary>
    /// <param name="key">The graph's key</param>
    /// <param name="val">Value to be added</param>
    public static void Graph(object key, float val)
    {
        if (Settings.enableGraphs)
            Instance.graphWindow.Graph(key, val);
    }

    /// <summary>
    /// Remove an existing graph.
    /// </summary>
    /// <param name="key">The graph's key</param>
    public static void RemoveGraph(object key)
    {
        if (Settings.enableGraphs)
            Instance.graphWindow.RemoveGraph(key);
    }

    /// <summary>
    /// Resets a graph's data.
    /// </summary>
    /// <param name="key">The graph's key</param>
    public static void ClearGraph(object key)
    {
        if (Settings.enableGraphs)
            Instance.graphWindow.ClearGraph(key);
    }

    /// <summary>
    /// Export graphs to a json file. Returns file path.
    /// </summary>
    public static string ExportGraphs()
    {
        if (Instance == null || !Settings.enableGraphs)
            return null;

        string dateTimeStr = DateTime.Now.ToString("YYY-mm-ddTHH-mm-ss");
        string filename = $"debuggui_graph_export_{dateTimeStr}.json";

        string filePath = Path.Combine(Application.persistentDataPath, filename);

        try
        {
            using StreamWriter writer = new StreamWriter(filePath);
            writer.Write(Instance.graphWindow.ToJson());

            Debug.Log($"[DebugGUI] Wrote graph data to {filePath}");
            return filePath;
        }
        catch (Exception e)
        {
            Debug.LogError("[DebugGUI] Graph export failed");
            Debug.LogException(e);
            return null;
        }
    }

    #endregion

    #region Log

    /// <summary>
    /// Create or update an existing message with the same key.
    /// </summary>
    public static void LogPersistent(object key, string message)
    {
        if (Settings.enableLogs)
            Instance.logWindow.LogPersistent(key, message);
    }

    /// <summary>
    /// Remove an existing persistent message.
    /// </summary>
    public static void RemovePersistent(object key)
    {
        if (Settings.enableLogs)
            Instance.logWindow.RemovePersistent(key);
    }

    /// <summary>
    /// Clears all persistent logs.
    /// </summary>
    public static void ClearPersistent()
    {
        if (Settings.enableLogs)
            Instance.logWindow.ClearPersistent();
    }

    /// <summary>
    /// Print a temporary message.
    /// </summary>
    public static void Log(object message)
    {
        if (Settings.enableLogs)
            Instance.logWindow.Log(message.ToString());
    }

    #endregion

    /// <summary>
    /// Re-scans for DebugGUI attribute holders (i.e. [DebugGUIGraph] and [DebugGUIPrint])
    /// </summary>
    public static void ForceReinitializeAttributes()
    {
        if (Instance == null) return;

        Instance.graphWindow.ReinitializeAttributes();
        Instance.logWindow.ReinitializeAttributes();
    }

    GraphWindow graphWindow;
    LogWindow logWindow;

    void Awake()
    {
        if (!initialized)
            Init();
    }

    void Init()
    {
        Application.quitting += () => quitting = true;
        initialized = true;
        _settings = Resources.Load<DebugGUISettings>("DebugGUISettings");

        DontDestroyOnLoad(gameObject);
        if (Settings.enableGraphs)
        {
            graphWindow = new GameObject("Graph").AddComponent<GraphWindow>();
            graphWindow.Init();
            graphWindow.transform.parent = transform;
        }
        if (Settings.enableLogs)
        {
            logWindow = new GameObject("Log").AddComponent<LogWindow>();
            logWindow.Init();
            logWindow.transform.parent = transform;
        }
    }
}
