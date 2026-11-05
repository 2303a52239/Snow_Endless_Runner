using UnityEngine;

namespace SnowRunner
{
    public enum InputCommand { None, Left, Right, Jump, Slide }

    /// <summary>Keyboard (Windows) + swipe (Android touch, and mouse drag for editor testing).</summary>
    public class InputHandler
    {
        const float SwipeThresholdInches = 0.25f;

        bool tracking;
        Vector2 startPos;

        static float ThresholdPixels
        {
            get
            {
                float dpi = Screen.dpi > 0 ? Screen.dpi : 160f;
                return Mathf.Max(40f, dpi * SwipeThresholdInches * 0.5f);
            }
        }

        public InputCommand Poll()
        {
            // Keyboard
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) return InputCommand.Left;
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) return InputCommand.Right;
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space)) return InputCommand.Jump;
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) return InputCommand.Slide;

            // Touch swipes
            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Began) { tracking = true; startPos = t.position; }
                else if (tracking && (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Ended))
                {
                    var cmd = Evaluate(t.position - startPos);
                    if (cmd != InputCommand.None) { tracking = false; return cmd; }
                    if (t.phase == TouchPhase.Ended) tracking = false;
                }
                else if (t.phase == TouchPhase.Canceled) tracking = false;
                return InputCommand.None;
            }

            // Mouse drag fallback (desktop testing of swipes)
            if (Input.GetMouseButtonDown(0)) { tracking = true; startPos = Input.mousePosition; }
            else if (tracking && Input.GetMouseButton(0))
            {
                var cmd = Evaluate((Vector2)Input.mousePosition - startPos);
                if (cmd != InputCommand.None) { tracking = false; return cmd; }
            }
            else if (Input.GetMouseButtonUp(0)) tracking = false;

            return InputCommand.None;
        }

        InputCommand Evaluate(Vector2 delta)
        {
            if (delta.magnitude < ThresholdPixels) return InputCommand.None;
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                return delta.x > 0 ? InputCommand.Right : InputCommand.Left;
            return delta.y > 0 ? InputCommand.Jump : InputCommand.Slide;
        }
    }
}
