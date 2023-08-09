using UnityEngine;

namespace WeavUtils
{
    // Draggable window clamped to the corners
    public class DebugGUIWindow : MonoBehaviour
    {
        protected const int outOfScreenClampPadding = 30;
        protected readonly Vector2 Padding = new Vector2(5, 5);

        static bool dragInProgress;
        bool dragged;

        protected Rect rect;

        Vector3 lastMousePos;

        static Material drawMat;
        Material CreateMaterial()
        {
            // Unity has a built-in shader that is useful for drawing
            // simple colored things.
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            Material mat = new Material(shader);
            mat.hideFlags = HideFlags.HideAndDontSave;
            // Turn on alpha blending
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            // Turn backface culling off
            mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            // Turn off depth writes
            mat.SetInt("_ZWrite", 0);

            return mat;
        }

        public virtual Rect GetDraggableRect()
        {
            return new Rect(rect.position, rect.size + Padding * 2);
        }

        public virtual void Init()
        {
            if (drawMat == null)
            {
                drawMat = CreateMaterial();
            }
        }

        void Update()
        {
            // Flip mouse Y
            var mousePos = Input.mousePosition;
            mousePos.y = Screen.height - mousePos.y;

            if (Input.GetMouseButtonDown(2))
            {
                if (!dragInProgress)
                {
                    var rect = GetDraggableRect();
                    if (rect.Contains(mousePos))
                    {
                        dragged = true;
                        dragInProgress = true;
                    }
                }
            }
            else if (Input.GetMouseButtonUp(2))
            {
                if (dragged) dragInProgress = false;
                dragged = false;
            }

            if (dragged)
            {
                var mouseDelta = mousePos - lastMousePos;
                Move(mouseDelta);
            }
            lastMousePos = mousePos;
        }

        protected void Move(Vector2 delta = default)
        {
            rect.position += delta;

            var viewportRect = new Rect(Vector2.zero, new Vector2(Screen.width, Screen.height));

            var min = -GetDraggableRect().size + Vector2.one * outOfScreenClampPadding;
            var max = viewportRect.size - Vector2.one * outOfScreenClampPadding;

            // Limit graph window offset so we can't get lost off screen
            rect.position = new Vector2(
                Mathf.Clamp(rect.position.x, min.x, max.x),
                Mathf.Clamp(rect.position.y, min.y, max.y)
            );
        }

        protected virtual void OnGUI()
        {
            // Only draw once per frame
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            drawMat.SetPass(0);
        }

        GUIContent tmpGuiContent = new();
        protected Vector2 GetMultilineStringSize(GUIStyle style, in string str)
        {
            tmpGuiContent.text = str;
            style.CalcMinMaxWidth(
                tmpGuiContent, out _, out float width
            );
            var height = style.CalcHeight(tmpGuiContent, width);

            return new Vector2(
                width,
                height
            );
        }

        protected void DrawRect(Rect rect, Color color, Vector2 padding = default)
        {
            rect.position += this.rect.position;
            rect.size += padding * 2;

            GL.Begin(GL.QUADS);
            {
                GL.Color(color);

                GL.Vertex3(rect.x, rect.y, 0.0f);
                GL.Vertex3(rect.x, rect.y + rect.height, 0.0f);
                GL.Vertex3(rect.x + rect.width, rect.y + rect.height, 0.0f);
                GL.Vertex3(rect.x + rect.width, rect.y, 0.0f);
            }
            GL.End();
        }

        protected void DrawLine(Vector2 start, Vector2 end, Color color)
        {
            start += rect.position;
            end += rect.position;

            GL.Begin(GL.LINES);
            {
                GL.Color(color);

                GL.Vertex(start);
                GL.Vertex(end);
            }
            GL.End();
        }

        protected void DrawLabel(Vector2 pos, string label, Vector2 padding = default, GUIStyle style = null)
        {
            DrawLabel(new Rect(pos, GetMultilineStringSize(GUIStyle.none, in label)), label, padding, style);
        }
        protected void DrawLabel(Rect rect, string label, Vector2 padding = default, GUIStyle style = null)
        {
            rect.position += this.rect.position;
            tmpGuiContent.text = label;
            GUI.Label(new Rect(rect.position + padding, rect.size + padding), tmpGuiContent, style ?? GUIStyle.none);
        }
    }
}