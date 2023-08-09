using UnityEngine;

// [CreateAssetMenu(fileName = "DebugGUISettings", menuName = "DebugGUI/Settings", order = 1)]
public class DebugGUISettings : ScriptableObject
{
    [SerializeField] public bool enableGraphs = true;
    [SerializeField] public bool enableLogs = true;

    [SerializeField] public Color backgroundColor = new Color(0f, 0f, 0f, 0.7f);
    [SerializeField] public Color scrubberColor = new Color(1f, 1f, 0f, 0.7f);
    [SerializeField] public int graphWidth = 300;
    [SerializeField] public int graphHeight = 100;
    [SerializeField] public float temporaryLogLifetime = 5;
}
