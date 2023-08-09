using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace WeavUtils
{
    public partial class LogWindow : DebugGUIWindow
    {
        List<TransientLog> transientLogs = new();
        List<MonoBehaviour> attributeContainers = new();
        Dictionary<MonoBehaviour, Type> typeCache = new();
        Dictionary<Type, int> typeInstanceCounts = new();
        Dictionary<object, string> persistentLogs = new();
        Dictionary<MonoBehaviour, List<PersistentLogAttributeKey>> attributeKeys = new();
        Dictionary<Type, HashSet<FieldInfo>> debugGUIPrintFields = new();
        Dictionary<Type, HashSet<PropertyInfo>> debugGUIPrintProperties = new();

        StringBuilder persistentLogStringBuilder = new();

        GUIStyle textStyle;

        public override void Init()
        {
            base.Init();
            RegisterAttributes();
            textStyle = new GUIStyle();
            textStyle.normal.textColor = Color.white;
        }

        void LateUpdate()
        {
            // Clean up expired logs
            int expiredCt = 0;
            for (int i = 0; i < transientLogs.Count; i++)
            {
                if (transientLogs[i].expiryTime <= Time.time)
                {
                    expiredCt++;
                }
            }
            transientLogs.RemoveRange(0, expiredCt);

            CleanUpDeletedAttributes();
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            // Only draw once per frame
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            if (persistentLogs.Count + transientLogs.Count == 0)
            {
                return;
            }

            GUI.color = Color.white;
            GUI.backgroundColor = DebugGUI.Settings.backgroundColor;

            var lineHeight = GetMultilineStringSize(textStyle, in string.Empty).y;
            
            persistentLogStringBuilder.Clear();


            foreach (var mb in attributeContainers)
            {
                Type type = typeCache[mb];
                if (debugGUIPrintFields.ContainsKey(type))
                {
                    foreach (var field in debugGUIPrintFields[type])
                    {
                        persistentLogStringBuilder.AppendLine($"{mb.name} {field.Name}: {field.GetValue(mb)}");
                    }
                }
                if (debugGUIPrintProperties.ContainsKey(type))
                {
                    foreach (var property in debugGUIPrintProperties[type])
                    {
                        persistentLogStringBuilder.AppendLine($"{mb.name} {property.Name}: {property.GetValue(mb, null)}");
                    }
                }
            }

            foreach (var log in persistentLogs.Values)
            {
                persistentLogStringBuilder.AppendLine(log);
            }

            var persistentLogStr = persistentLogStringBuilder.ToString();
            var textSize = GetMultilineStringSize(textStyle, in persistentLogStr);
            // Min size in case of no entries
            textSize.x = Mathf.Max(100, textSize.x);
            rect.size = textSize;

            textSize.y += lineHeight;
            float transientLogY = textSize.y;

            // Calculate size
            for (int i = 0; i < transientLogs.Count; i++)
            {
                var log = transientLogs[i];
                var size = GetMultilineStringSize(textStyle, in log.text);
                textSize = new Vector2(
                    Mathf.Max(size.x, textSize.x),
                    textSize.y + size.y
                );
            }

            var backgroundRect = new Rect(default, new Vector2(textSize.x, textSize.y));
            DrawRect(backgroundRect, DebugGUI.Settings.backgroundColor, Padding);
            // Draw a little bit extra for the draggable area
            DrawRect(new Rect(0, 0, rect.width, rect.height), new Color(1, 1, 1, 0.05f), Padding);

            // Draw persistent logs
            DrawLabel(new Rect(default, textSize), persistentLogStr, Padding, textStyle);

            // Draw transient logs
            for (int i = transientLogs.Count - 1; i >= 0; i--)
            {
                // Clear up transient logs going off screen
                if (transientLogY > Screen.height)
                {
                    transientLogs.RemoveRange(0, i + 1);
                    break;
                }

                var log = transientLogs[i];
                DrawLabel(Vector2.up * transientLogY, log.text, Padding, textStyle);
                transientLogY += lineHeight;
            }
        }

        public void Log(string str)
        {
            transientLogs.Add(new TransientLog(str, Time.time + DebugGUI.Settings.temporaryLogLifetime));
        }

        public void LogPersistent(object key, string message)
        {
            if (persistentLogs.ContainsKey(key))
                persistentLogs[key] = message;
            else
                persistentLogs.Add(key, message);
        }

        public void RemovePersistent(object key)
        {
            if (persistentLogs.ContainsKey(key))
            {
                persistentLogs.Remove(key);
            }
        }

        public void ClearPersistent()
        {
            persistentLogs.Clear();
        }

        public void ReinitializeAttributes()
        {
            // Clean up graphs
            List<object> toRemove = new List<object>();
            foreach (var key in persistentLogs.Keys)
            {
                if (key is PersistentLogAttributeKey)
                    toRemove.Add(key);
            }
            foreach (var key in toRemove)
            {
                persistentLogs.Remove(key);
            }

            attributeContainers = new();
            debugGUIPrintFields = new();
            debugGUIPrintProperties = new();
            typeInstanceCounts = new();
            attributeKeys = new();
        }

        private void RegisterAttributes()
        {
            foreach (var mb in FindObjectsOfType<MonoBehaviour>())
            {
                Type mbType = mb.GetType();

                HashSet<MonoBehaviour> uniqueAttributeContainers = new();

                // Fields
                {
                    // Retreive the fields from the mono instance
                    FieldInfo[] objectFields = mbType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    // search all fields/properties for the [DebugGUIVar] attribute
                    for (int i = 0; i < objectFields.Length; i++)
                    {
                        DebugGUIPrintAttribute printAttribute = Attribute.GetCustomAttribute(objectFields[i], typeof(DebugGUIPrintAttribute)) as DebugGUIPrintAttribute;

                        if (printAttribute != null)
                        {
                            uniqueAttributeContainers.Add(mb);
                            typeCache[mb] = mb.GetType();
                            if (!debugGUIPrintFields.ContainsKey(mbType))
                            {
                                debugGUIPrintFields.Add(mbType, new HashSet<FieldInfo>());
                            }

                            debugGUIPrintFields[mbType].Add(objectFields[i]);
                        }
                    }
                }

                // Properties
                {
                    PropertyInfo[] objectProperties = mbType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    for (int i = 0; i < objectProperties.Length; i++)
                    {
                        if (Attribute.GetCustomAttribute(objectProperties[i], typeof(DebugGUIPrintAttribute)) is DebugGUIPrintAttribute)
                        {
                            uniqueAttributeContainers.Add(mb);
                            typeCache[mb] = mb.GetType();

                            if (!debugGUIPrintProperties.ContainsKey(mbType))
                            {
                                debugGUIPrintProperties.Add(mbType, new HashSet<PropertyInfo>());
                            }
                            debugGUIPrintProperties[mbType].Add(objectProperties[i]);
                        }
                    }
                }

                foreach (var attributeContainer in uniqueAttributeContainers)
                {
                    attributeContainers.Add(attributeContainer);
                    Type type = attributeContainer.GetType();
                    if (!typeInstanceCounts.ContainsKey(type))
                        typeInstanceCounts.Add(type, 0);
                    typeInstanceCounts[type]++;
                }
            }
        }

        private void CleanUpDeletedAttributes()
        {
            for (int i = 0; i < attributeContainers.Count; i++)
            {
                var mb = attributeContainers[i];
                if (attributeContainers[i] == null)
                {
                    attributeKeys.Remove(mb);
                    typeCache.Remove(mb);

                    Type type = mb.GetType();
                    typeInstanceCounts[type]--;
                    if (typeInstanceCounts[type] == 0)
                    {
                        if (debugGUIPrintFields.ContainsKey(type))
                            debugGUIPrintFields.Remove(type);
                        if (debugGUIPrintProperties.ContainsKey(type))
                            debugGUIPrintProperties.Remove(type);
                    }
                    attributeContainers.RemoveAt(i);

                    i--;
                }
            }
        }

        private struct TransientLog
        {
            public string text;
            public float expiryTime;

            public TransientLog(string text, float expiryTime)
            {
                this.text = text;
                this.expiryTime = expiryTime;
            }
        }

        // Wrapper to differentiate attributes from
        // manually created logs
        public class PersistentLogAttributeKey
        {
            public MemberInfo memberInfo;
            public PersistentLogAttributeKey(MemberInfo memberInfo)
            {
                this.memberInfo = memberInfo;
            }
        }
    }
}