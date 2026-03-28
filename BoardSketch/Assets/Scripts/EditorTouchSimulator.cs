#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using Board.Input;
using Board.Input.Simulation;

namespace BoardSketch
{
    /// <summary>
    /// Editor-only: enables touch simulation for testing in the Unity Editor.
    ///
    /// Features:
    /// 1. Auto-enables the Board SDK's mouse-as-finger simulation on Play mode entry.
    ///    Just enter Play mode and click/drag in the Game view to draw.
    /// 2. Provides programmatic touch injection via menu: BoardSketch > Run Touch Test.
    ///    Uses EditorApplication.update to spread events across frames (MCP-compatible).
    /// </summary>
    public static class EditorTouchSimulator
    {
        // Reflection handles for the internal BoardContactEvent struct
        private static MethodInfo s_QueueMethod;
        private static Type s_EventType;
        private static FieldInfo s_fContactId;
        private static FieldInfo s_fPosition;
        private static FieldInfo s_fOrientation;
        private static FieldInfo s_fTypeId;
        private static FieldInfo s_fPhaseId;
        private static FieldInfo s_fGlyphId;
        private static FieldInfo s_fCenter;
        private static FieldInfo s_fExtents;
        private static FieldInfo s_fIsTouched;
        private static bool s_ReflectionCached;

        // Menu-driven test state
        private static int s_TestContactId = 9000;
        private static int s_TestFrame = -1;
        private static Vector2[] s_TestPath;

        /// <summary>
        /// Auto-enables mouse-as-finger simulation when entering Play mode.
        /// </summary>
        // Auto-enable removed — it was enabling mouse-as-finger which conflicts
        // with glyph placement in the Board Simulator.
        // Use Board > Input > Simulator manually to test both fingers and pieces.

        /// <summary>
        /// Menu item to run a programmatic touch test during Play mode.
        /// Injects a stroke along a default path, one point per frame.
        /// </summary>
        [MenuItem("BoardSketch/Run Touch Test")]
        private static void MenuRunTouchTest()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[EditorTouchSimulator] Enter Play mode first.");
                return;
            }

            CacheReflection();
            if (s_QueueMethod == null)
            {
                Debug.LogError("[EditorTouchSimulator] Reflection not available, cannot inject touches.");
                return;
            }

            s_TestContactId++;
            s_TestFrame = 0;
            s_TestPath = new Vector2[]
            {
                new Vector2(400, 540),
                new Vector2(500, 440),
                new Vector2(650, 350),
                new Vector2(800, 300),
                new Vector2(950, 350),
                new Vector2(1100, 440),
                new Vector2(1200, 540),
                new Vector2(1300, 640),
                new Vector2(1400, 700),
                new Vector2(1500, 640),
            };

            Debug.Log("[EditorTouchSimulator] Starting programmatic touch test: " + s_TestPath.Length + " points, contactId=" + s_TestContactId);
            EditorApplication.update += TestUpdate;
        }

        /// <summary>
        /// Called each editor frame to inject one touch event into the pipeline.
        /// </summary>
        private static void TestUpdate()
        {
            if (s_TestFrame < 0 || s_TestPath == null || !EditorApplication.isPlaying)
            {
                EditorApplication.update -= TestUpdate;
                s_TestFrame = -1;
                return;
            }

            if (s_TestFrame == 0)
            {
                InjectTouch(s_TestContactId, s_TestPath[0], BoardContactPhase.Began);
            }
            else if (s_TestFrame < s_TestPath.Length)
            {
                InjectTouch(s_TestContactId, s_TestPath[s_TestFrame], BoardContactPhase.Moved);
            }
            else
            {
                InjectTouch(s_TestContactId, s_TestPath[s_TestPath.Length - 1], BoardContactPhase.Ended);
                EditorApplication.update -= TestUpdate;
                s_TestFrame = -1;
                Debug.Log("[EditorTouchSimulator] Programmatic touch test complete");
                return;
            }
            s_TestFrame++;
        }

        /// <summary>
        /// Injects a single touch event into the Board SDK input pipeline via reflection.
        /// BoardContactEvent is internal to the SDK, so we construct it entirely through reflection.
        /// </summary>
        public static void InjectTouch(int contactId, Vector2 screenPosition, BoardContactPhase phase)
        {
            CacheReflection();
            if (s_QueueMethod == null || s_EventType == null) return;

            // Create the internal BoardContactEvent struct via reflection (boxed)
            object evt = Activator.CreateInstance(s_EventType);
            s_fContactId.SetValue(evt, contactId);
            s_fPosition.SetValue(evt, screenPosition);
            s_fOrientation.SetValue(evt, 0f);
            s_fTypeId.SetValue(evt, (byte)BoardContactType.Finger);
            s_fPhaseId.SetValue(evt, (byte)phase);
            s_fGlyphId.SetValue(evt, -1);
            s_fCenter.SetValue(evt, screenPosition);
            s_fExtents.SetValue(evt, new Vector2(10f, 10f));
            s_fIsTouched.SetValue(evt, phase != BoardContactPhase.Ended && phase != BoardContactPhase.Canceled);

            s_QueueMethod.Invoke(null, new object[] { evt });
        }

        private static void CacheReflection()
        {
            if (s_ReflectionCached) return;
            s_ReflectionCached = true;

            var asm = typeof(BoardInput).Assembly;

            // Get the internal BoardContactEvent type
            s_EventType = asm.GetType("Board.Input.BoardContactEvent");
            if (s_EventType == null)
            {
                Debug.LogError("[EditorTouchSimulator] Could not find Board.Input.BoardContactEvent type");
                return;
            }

            // Cache field accessors (fields are public even though the struct is internal)
            var pubInst = BindingFlags.Public | BindingFlags.Instance;
            s_fContactId = s_EventType.GetField("contactId", pubInst);
            s_fPosition = s_EventType.GetField("position", pubInst);
            s_fOrientation = s_EventType.GetField("orientation", pubInst);
            s_fTypeId = s_EventType.GetField("typeId", pubInst);
            s_fPhaseId = s_EventType.GetField("phaseId", pubInst);
            s_fGlyphId = s_EventType.GetField("glyphId", pubInst);
            s_fCenter = s_EventType.GetField("center", pubInst);
            s_fExtents = s_EventType.GetField("extents", pubInst);
            s_fIsTouched = s_EventType.GetField("isTouched", pubInst);

            // Get the internal QueueStateEvent method
            s_QueueMethod = typeof(BoardInput).GetMethod(
                "QueueStateEvent",
                BindingFlags.Static | BindingFlags.NonPublic);

            if (s_QueueMethod == null)
                Debug.LogError("[EditorTouchSimulator] Could not find BoardInput.QueueStateEvent");
            else
                Debug.Log("[EditorTouchSimulator] Reflection setup complete");
        }
    }
}
#endif
