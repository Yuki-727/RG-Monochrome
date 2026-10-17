using RhythmCore.Data;
using RhythmCore.Logic;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class ChartEditorWindow : EditorWindow
{
    
    private ChartData currentChart;
    private AudioSource previewSource;
    private AudioSource editorAudioSource;
    private AudioClip currentClip;
    private Object currentBackground;
    private Texture2D currentCover;

    private float topSplitRatio = 0.5f;
    private bool isResizingSplit = false;
    private const float MIN_PANE_HEIGHT = 150f;

    private Vector2 scrollPos;

    private double startBeatMarker = 0;

    private float pixelsPerBeat = 100f;
    private const float MIN_ZOOM = 20f;
    private const float MAX_ZOOM = 600f;
    private const float LEFT_PANEL_WIDTH = 250f;
    private const float LANE_HEIGHT = 60f;
    private const float EVENT_ROW_HEIGHT = 30f; 
    private const float HEADER_HEIGHT = 30f;    

    private Vector2 editorPanOffset = Vector2.zero;
    private float editorZoom = 1.0f;
    private int editorSelectedLaneID = -1;
    private Vector2 referenceSize = new Vector2(16, 9);
    private bool isDraggingLaneInEditor = false;
    private LaneEvent activeEditingEvent = null;

    private NoteData selectedNote = null;
    private object selectedEvent = null; 
    private double selectedEventBeat = -1;
    private double selectedBeatIndex = -1; 
    private int snapDivisor = 4;
    private NoteType selectedTool = NoteType.Tap;

    private float[] playbackSpeeds = { 0.25f, 0.5f, 0.75f, 1.0f };
    private int selectedSpeedIndex = 3;

    private Vector2 inspectorScrollPos; 

    private bool isDraggingNote = false;
    private bool isResizingHold = false;
    private double dragStartBeatOffset = 0;

    private enum EditMode { None, Dragging, Resizing }
    private EditMode currentMode = EditMode.None;
    private double dragStartBeat;
    private double originalNoteBeat;
    private double originalDuration;

    public enum EditorTool { None, Hand, Pen }
    private EditorTool currentEditorTool = EditorTool.Pen;
    private bool isPanning = false;

    private bool isRecordingTaps = false; 
    private List<double> guideBeats = new List<double>(); 

    private struct LaneState { public float x, y, rot, alpha; public bool isAutoplay; }

    [MenuItem("Rhythm Game/Chart Editor Pro")]
    public static void ShowWindow() { GetWindow<ChartEditorWindow>("Chart Editor Pro"); }

    private void EnsureEditorAudioSource()
    {
        
        var go = GameObject.Find("EditorAudioSource_Tool");

        if (go == null)
        {
            go = new GameObject("EditorAudioSource_Tool");
            go.hideFlags = HideFlags.HideAndDontSave; 
        }

        editorAudioSource = go.GetComponent<AudioSource>();
        if (editorAudioSource == null)
        {
            editorAudioSource = go.AddComponent<AudioSource>();
        }

        editorAudioSource.playOnAwake = false; 
        previewSource = editorAudioSource;
    }

    private void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
        EnsureEditorAudioSource();
        if (currentChart == null) CreateNewChart();
        ReloadAssets(); 
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        if (previewSource != null) previewSource.Stop();
    }

    private void OnEditorUpdate()
    {
        if (previewSource != null && previewSource.isPlaying)
        {
            Repaint(); 
        }
    }

    void ReloadAssets()
    {
        if (currentChart == null || currentChart.metadata == null) return;

        if (!string.IsNullOrEmpty(currentChart.metadata.audioPath))
        {
            var loadedClip = AssetDatabase.LoadAssetAtPath<AudioClip>(currentChart.metadata.audioPath);
            if (loadedClip != null)
            {
                currentClip = loadedClip;
                if (previewSource != null) previewSource.clip = currentClip;
            }
        }

        if (!string.IsNullOrEmpty(currentChart.metadata.backgroundPath))
            currentBackground = AssetDatabase.LoadAssetAtPath<Object>(currentChart.metadata.backgroundPath);

        if (!string.IsNullOrEmpty(currentChart.metadata.ArtCoverPath))
            currentCover = AssetDatabase.LoadAssetAtPath<Texture2D>(currentChart.metadata.ArtCoverPath);
    }

    void OnGUI()
    {
        HandleGlobalInput();

        if (currentChart == null)
        {
            DrawToolbar();
            GUILayout.Label("No chart loaded. Create or Load one.", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        DrawToolbar();

        GUILayout.BeginHorizontal();

        GUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(LEFT_PANEL_WIDTH), GUILayout.ExpandHeight(true));
        DrawSidebarContent();
        GUILayout.EndVertical();

        GUILayout.BeginVertical();

        float availableHeight = position.height - 20; 
        float timelineHeight = availableHeight * topSplitRatio;
        float previewHeight = availableHeight - timelineHeight - 5; 

        Rect timelineRect = GUILayoutUtility.GetRect(0, timelineHeight, GUILayout.ExpandWidth(true));
        
        GUI.Box(timelineRect, GUIContent.none);

        GUI.BeginGroup(timelineRect);
        
        Rect localRect = new Rect(0, 0, timelineRect.width, timelineRect.height);
        DrawTimeline(localRect);
        GUI.EndGroup();
        HandleTimelineInput(timelineRect); 

        Rect splitRect = GUILayoutUtility.GetRect(0, 5, GUILayout.ExpandWidth(true));
        GUI.Box(splitRect, GUIContent.none, EditorStyles.centeredGreyMiniLabel);
        EditorGUIUtility.AddCursorRect(splitRect, MouseCursor.ResizeVertical);

        if (Event.current.type == EventType.MouseDown && splitRect.Contains(Event.current.mousePosition))
            isResizingSplit = true;

        if (isResizingSplit)
        {
            topSplitRatio = Event.current.mousePosition.y / availableHeight;
            topSplitRatio = Mathf.Clamp(topSplitRatio, 0.2f, 0.8f);
            Repaint();
            if (Event.current.type == EventType.MouseUp) isResizingSplit = false;
        }

        Rect previewRect = GUILayoutUtility.GetRect(0, previewHeight, GUILayout.ExpandWidth(true));
        GUI.Box(previewRect, GUIContent.none); 
        DrawLaneEditor(previewRect);

        GUILayout.EndVertical(); 
        GUILayout.EndHorizontal(); 

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Space)
        {
            if (previewSource.isPlaying) previewSource.Pause();
            else previewSource.Play();
            Event.current.Use();
        }

        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            GUI.FocusControl(null);
            Repaint();
        }
    }

    void DrawTimeline(Rect visibleRect)
    {

        float contentWidth = Mathf.Max(visibleRect.width, 3000 * pixelsPerBeat);
        
        float contentHeight = (currentChart.laneDefinitions.Count * LANE_HEIGHT) + HEADER_HEIGHT;

        Rect contentRect = new Rect(0, 0, contentWidth, contentHeight);

        scrollPos = GUI.BeginScrollView(visibleRect, scrollPos, contentRect);

        DrawGrid(contentRect);   

        Color guideColor = new Color(1f, 0.92f, 0.016f, 0.5f); 

        foreach (var beat in guideBeats)
        {
            float x = (float)beat * pixelsPerBeat;

            if (x > scrollPos.x - 50 && x < scrollPos.x + visibleRect.width + 50)
            {

                EditorGUI.DrawRect(new Rect(x, 0, 2, contentRect.height), guideColor);

                EditorGUI.DrawRect(new Rect(x - 3, 0, 8, 8), guideColor);
            }
        }

        DrawEvents(contentRect); 
        DrawNotes(contentRect);  

        if (previewSource != null)
        {
            
            double currentBeat = GetBeatFromTime(previewSource.time);
            float playHeadX = (float)currentBeat * pixelsPerBeat;

            EditorGUI.DrawRect(new Rect(playHeadX, 0, 2, contentHeight), new Color(1f, 0.2f, 0.2f, 0.8f));

            GUI.Box(new Rect(playHeadX - 5, 0, 10, 10), "", EditorStyles.miniButton);
        }

        GUI.EndScrollView();
    }

    void DrawGrid(Rect rect)
    {
        
        EditorGUI.DrawRect(new Rect(0, 0, rect.width, rect.height), new Color(0.12f, 0.12f, 0.12f));

        float startVisBeat = scrollPos.x / pixelsPerBeat;
        float endVisBeat = (scrollPos.x + position.width) / pixelsPerBeat;

        float step = (snapDivisor > 0) ? 1.0f / snapDivisor : 1.0f;

        float currentDrawBeat = Mathf.Floor(startVisBeat / step) * step;

        while (currentDrawBeat <= endVisBeat + step)
        {
            float x = currentDrawBeat * pixelsPerBeat;

            bool isMeasure = Mathf.Abs(currentDrawBeat % 4) < 0.001f; 
            bool isBeat = Mathf.Abs(currentDrawBeat % 1) < 0.001f;    

            float thickness = 1f;
            Color col;

            if (isMeasure)
            {
                col = new Color(0.6f, 0.6f, 0.6f, 0.5f); 
                thickness = 2f;
            }
            else if (isBeat)
            {
                col = new Color(0.4f, 0.4f, 0.4f, 0.3f); 
            }
            else
            {
                
                col = new Color(0.3f, 0.3f, 0.3f, 0.1f); 
            }

            EditorGUI.DrawRect(new Rect(x, EVENT_ROW_HEIGHT, thickness, rect.height - EVENT_ROW_HEIGHT), col);

            if (isBeat)
            {
                GUI.Label(new Rect(x + 4, EVENT_ROW_HEIGHT, 50, 20), Mathf.RoundToInt(currentDrawBeat).ToString(), EditorStyles.miniLabel);
            }

            currentDrawBeat += step;
        }

        for (int i = 0; i < currentChart.laneDefinitions.Count; i++)
        {
            float y = EVENT_ROW_HEIGHT + i * LANE_HEIGHT;
            LaneData lane = currentChart.laneDefinitions[i];

            EditorGUI.DrawRect(new Rect(scrollPos.x, y, rect.width + scrollPos.x, 1), new Color(0.3f, 0.3f, 0.3f));
            if (i % 2 == 0)
                EditorGUI.DrawRect(new Rect(scrollPos.x, y, rect.width + scrollPos.x, LANE_HEIGHT), new Color(1f, 1f, 1f, 0.02f));

            Rect labelRect = new Rect(scrollPos.x, y, 60, 20);
            GUI.Box(labelRect, $"ID: {lane.lane_id}", EditorStyles.miniButton);
        }
    }

    void DrawEvents(Rect rect)
    {
        
        float startBeat = scrollPos.x / pixelsPerBeat;
        float endBeat = (scrollPos.x + rect.width) / pixelsPerBeat;

        foreach (var sv in currentChart.sv_events)
        {
            
            if (sv.beat < startBeat - 1 || sv.beat > endBeat + 1) continue;

            float x = (float)sv.beat * pixelsPerBeat;
            Rect r = new Rect(x - 4, 2, 8, 12); 

            bool isSelected = Mathf.Abs((float)(sv.beat - selectedEventBeat)) < 0.001f;
            EditorGUI.DrawRect(r, isSelected ? Color.white : new Color(1f, 0.8f, 0f)); 

            if (pixelsPerBeat > 30) 
                GUI.Label(new Rect(x + 5, 0, 40, 15), $"{sv.target_speed:0.##}x", EditorStyles.miniLabel);
        }

        foreach (var lev in currentChart.lane_events)
        {
            if (lev.beat < startBeat - 1 || lev.beat > endBeat + 1) continue;

            float x = (float)lev.beat * pixelsPerBeat;
            Rect r = new Rect(x - 4, 16, 8, 12);

            bool isSelected = Mathf.Abs((float)(lev.beat - selectedEventBeat)) < 0.001f;
            EditorGUI.DrawRect(r, isSelected ? Color.white : Color.cyan);

            if (pixelsPerBeat > 30)
                GUI.Label(new Rect(x + 5, 15, 40, 15), $"L{lev.lane_id}", EditorStyles.miniLabel);
        }
    }

    void DrawLaneEditorArea(Rect rect)
    {
        EditorGUI.DrawRect(rect, Color.black);
        GUI.BeginGroup(rect);
        DrawLaneEditor(new Rect(0, 0, rect.width, rect.height));
        GUI.EndGroup();
    }

    void HandleSplitterResize(Rect splitterRect)
    {
        Event e = Event.current;
        if (e.type == EventType.MouseDown && splitterRect.Contains(e.mousePosition))
            isResizingSplit = true;

        if (isResizingSplit)
        {
            topSplitRatio = e.mousePosition.y / position.height;
            topSplitRatio = Mathf.Clamp(topSplitRatio, 0.2f, 0.8f);
            Repaint();
        }
        if (e.type == EventType.MouseUp) isResizingSplit = false;
    }

    void DrawLaneEditor(Rect rect)
    {

        GUI.BeginGroup(rect);

        float width = rect.width;
        float height = rect.height;

        Vector2 centerScreen = new Vector2(width / 2, height / 2);
        Vector2 originScreen = centerScreen + editorPanOffset;
        float pixelsPerUnit = 100f * editorZoom;

        DrawEditorGrid(new Rect(0, 0, width, height), originScreen, pixelsPerUnit);

        float viewW = referenceSize.x * pixelsPerUnit;
        float viewH = referenceSize.y * pixelsPerUnit;
        Rect gameViewRect = new Rect(originScreen.x - viewW / 2, originScreen.y - viewH / 2, viewW, viewH);
        Handles.color = Color.yellow;
        Handles.DrawWireCube(gameViewRect.center, gameViewRect.size);
        GUI.Label(new Rect(gameViewRect.x, gameViewRect.y - 20, 200, 20), "Game View (16:9)", EditorStyles.whiteLabel);

        double currentBeat = GetBeatFromTime(previewSource ? previewSource.time : 0);
        DrawLanesInEditor(originScreen, pixelsPerUnit, currentBeat);

        GUI.EndGroup();

        HandleLaneEditorInput(rect, originScreen, pixelsPerUnit, currentBeat);
    }

    void DrawLanesInEditor(Vector2 origin, float scale, double beat)
    {
        if (currentChart == null) return;
        foreach (var laneDef in currentChart.laneDefinitions)
        {
            LaneState state = CalculateLaneState(laneDef.lane_id, beat);
            Vector2 pos = LogicToScreen(new Vector2(state.x, state.y), origin, scale);

            Matrix4x4 oldMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(- state.rot, pos);

            Color c = (laneDef.lane_id == editorSelectedLaneID) ? Color.green : Color.white;
            c.a = state.alpha;

            float boxSize = 20f; 
            Rect boxRect = new Rect(pos.x - boxSize / 2, pos.y - boxSize / 2, boxSize, boxSize);

            EditorGUI.DrawRect(boxRect, c); 
            Handles.color = Color.black;
            Handles.DrawWireCube(pos, new Vector3(boxSize, boxSize, 0)); 

            GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = Color.black;
            style.alignment = TextAnchor.MiddleCenter;
            GUI.Label(boxRect, laneDef.lane_id.ToString(), style);

            Handles.color = c;
            float arrowLen = 40f;
            Vector2 arrowEnd = pos + new Vector2(arrowLen, 0);
            Handles.DrawLine(pos + new Vector2(boxSize / 2, 0), arrowEnd); 
            
            Handles.DrawLine(arrowEnd, arrowEnd + new Vector2(-5, -5));
            Handles.DrawLine(arrowEnd, arrowEnd + new Vector2(-5, 5));

            GUI.matrix = oldMatrix;
        }
    }

    void HandleGlobalInput()
    {
        Event e = Event.current;

        bool isTyping = EditorGUIUtility.editingTextField;

        if (e.type == EventType.KeyDown && !isTyping)
        {
            if (isRecordingTaps && previewSource != null && previewSource.isPlaying)
            {
                
                if (e.keyCode == KeyCode.D || e.keyCode == KeyCode.F ||
                    e.keyCode == KeyCode.J || e.keyCode == KeyCode.K)
                {
                    
                    double currentBeat = GetBeatFromTime(previewSource.time);

                    guideBeats.Add(currentBeat);

                    guideBeats.Sort();

                    e.Use(); 
                    return;
                }
            }

            switch (e.keyCode)
            {
                case KeyCode.Space: TogglePlayback(); e.Use(); break;
                case KeyCode.H: currentEditorTool = EditorTool.Hand; Repaint(); e.Use(); break;
                case KeyCode.B: currentEditorTool = EditorTool.Pen; Repaint(); e.Use(); break;
                case KeyCode.Alpha1: selectedTool = NoteType.Tap; Repaint(); e.Use(); break;
                case KeyCode.Alpha2: selectedTool = NoteType.Hold; Repaint(); e.Use(); break;
                case KeyCode.Alpha3: selectedTool = NoteType.Drag; Repaint(); e.Use(); break;
                case KeyCode.Alpha4: selectedTool = NoteType.Blast; Repaint(); e.Use(); break;
                case KeyCode.Delete:
                    if (selectedNote != null) { currentChart.notes.Remove(selectedNote); selectedNote = null; Repaint(); }
                    else if (selectedEventBeat != -1) {  }
                    e.Use();
                    break;
            }
        }
    }

    void HandleTimelineInput(Rect timelineRect)
    {
        Event e = Event.current;

        if (e.type == EventType.MouseUp)
        {
            isDraggingNote = false;
            isResizingHold = false;
        }

        if (!timelineRect.Contains(e.mousePosition) && !isDraggingNote && !isResizingHold) return;

        Vector2 localMousePos = e.mousePosition - timelineRect.position;
        localMousePos.x += scrollPos.x;
        localMousePos.y += scrollPos.y;

        bool isHeader = localMousePos.y < HEADER_HEIGHT;
        double rawBeat = localMousePos.x / pixelsPerBeat;
        double snappedBeat = GetSnappedBeat(rawBeat);

        if (previewSource != null && !previewSource.isPlaying)
        {
            SetAudioTime(snappedBeat);
            Repaint(); 
        }

        if (e.type == EventType.ScrollWheel && e.control)
        {
            float zoomDelta = -e.delta.y * 2f;
            double mouseBeat = rawBeat; 
            pixelsPerBeat = Mathf.Clamp(pixelsPerBeat + zoomDelta, 10f, 600f); 

            float newScrollX = (float)(mouseBeat * pixelsPerBeat) - (e.mousePosition.x - timelineRect.x);
            scrollPos.x = Mathf.Max(0, newScrollX);
            e.Use(); Repaint(); return;
        }

        if (e.type == EventType.MouseDrag && (currentEditorTool == EditorTool.Hand || e.button == 2))
        {
            scrollPos -= e.delta;
            e.Use(); Repaint(); return;
        }

        if (e.type == EventType.MouseDrag && e.button == 0 && selectedNote != null)
        {
            
            if (isResizingHold && selectedNote.type == NoteType.Hold)
            {
                double newDuration = snappedBeat - selectedNote.beat;
                selectedNote.duration = System.Math.Max(0.125, newDuration); 
                e.Use(); Repaint(); return;
            }

            if (isDraggingNote)
            {
                selectedNote.beat = snappedBeat; 

                if (!isHeader)
                {
                    float yInGrid = localMousePos.y - HEADER_HEIGHT;
                    int laneIndex = Mathf.FloorToInt(yInGrid / LANE_HEIGHT);
                    if (laneIndex >= 0 && laneIndex < currentChart.laneDefinitions.Count)
                    {
                        selectedNote.lane = currentChart.laneDefinitions[laneIndex].lane_id;
                    }
                }
                e.Use(); Repaint(); return;
            }
        }

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            GUI.FocusControl(null); 

            if (isHeader)
            {
                
                double clickedTime = rawBeat;
                double bestBeat = -1;
                double minDst = 0.5f;

                foreach (var sv in currentChart.sv_events)
                {
                    double dst = System.Math.Abs(sv.beat - clickedTime);
                    if (dst < minDst) { minDst = dst; bestBeat = sv.beat; }
                }
                foreach (var le in currentChart.lane_events)
                {
                    double dst = System.Math.Abs(le.beat - clickedTime);
                    if (dst < minDst) { minDst = dst; bestBeat = le.beat; }
                }

                if (bestBeat != -1)
                {
                    selectedEventBeat = bestBeat;
                    
                }
                else
                {
                    selectedEventBeat = -1;
                    if (previewSource != null) SetAudioTime(snappedBeat); 
                }
                selectedNote = null;
            }
            
            else
            {
                
                if (e.shift)
                {
                    startBeatMarker = snappedBeat;
                    SetAudioTime(startBeatMarker);
                    e.Use(); Repaint(); return;
                }

                NoteData clickedNote = null;
                bool clickedOnResizeHandle = false;

                for (int i = currentChart.notes.Count - 1; i >= 0; i--)
                {
                    var n = currentChart.notes[i];
                    float noteX = (float)n.beat * pixelsPerBeat;
                    float noteY = HEADER_HEIGHT + (GetLaneIndex(n.lane) * LANE_HEIGHT);
                    float noteW = (n.type == NoteType.Hold) ? Mathf.Max(20, (float)n.duration * pixelsPerBeat) : 20;

                    Rect noteRect = new Rect(noteX, noteY, noteW, LANE_HEIGHT - 2);

                    if (noteRect.Contains(localMousePos))
                    {
                        clickedNote = n;
                        
                        if (n.type == NoteType.Hold && localMousePos.x > (noteRect.x + noteRect.width - 15))
                        {
                            clickedOnResizeHandle = true;
                        }
                        break;
                    }
                }

                if (clickedNote != null)
                {
                    selectedNote = clickedNote;

                    if (clickedOnResizeHandle)
                    {
                        isResizingHold = true;
                        isDraggingNote = false;
                    }
                    else
                    {
                        isDraggingNote = true;
                        isResizingHold = false;
                    }
                }
                else if (currentEditorTool == EditorTool.Pen)
                {
                    
                    float yInGrid = localMousePos.y - HEADER_HEIGHT;
                    int laneIndex = Mathf.FloorToInt(yInGrid / LANE_HEIGHT);
                    if (laneIndex >= 0 && laneIndex < currentChart.laneDefinitions.Count)
                    {
                        NoteData newNote = new NoteData
                        {
                            beat = snappedBeat,
                            lane = currentChart.laneDefinitions[laneIndex].lane_id,
                            type = selectedTool
                        };
                        if (newNote.type == NoteType.Hold) newNote.duration = 1.0;

                        currentChart.notes.Add(newNote);
                        selectedNote = newNote;
                        isDraggingNote = true; 
                    }
                }
                else
                {
                    selectedNote = null; 
                }
            }
            e.Use(); Repaint();
        }

        if (e.type == EventType.MouseDown && e.button == 1)
        {
            bool clickedOnNote = false;
            
            if (!isHeader || true)
            {
                NoteData noteToDelete = null;
                for (int i = currentChart.notes.Count - 1; i >= 0; i--)
                {
                    var n = currentChart.notes[i];
                    float noteX = (float)n.beat * pixelsPerBeat;
                    float noteY = HEADER_HEIGHT + (GetLaneIndex(n.lane) * LANE_HEIGHT);
                    float noteW = (n.type == NoteType.Hold) ? Mathf.Max(20, (float)n.duration * pixelsPerBeat) : 20;

                    Rect noteRect = new Rect(noteX, noteY, noteW, LANE_HEIGHT - 2);
                    if (noteRect.Contains(localMousePos)) { noteToDelete = n; break; }
                }

                if (noteToDelete != null)
                {
                    currentChart.notes.Remove(noteToDelete);
                    selectedNote = null;
                    clickedOnNote = true;
                    Repaint();
                }
            }

            if (!clickedOnNote && isHeader)
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Add SV Event"), false, () => {
                    currentChart.sv_events.Add(new SVEvent { beat = snappedBeat, target_speed = 1.0f, duration = 0, ease = EasingType.Linear });
                });
                menu.AddItem(new GUIContent("Add Lane Keyframe"), false, () => {
                    int targetLane = (editorSelectedLaneID != -1) ? editorSelectedLaneID : 0;
                    currentChart.lane_events.Add(new LaneEvent
                    {
                        beat = snappedBeat,
                        lane_id = targetLane,
                        pos_x = 0,
                        pos_y = 0,
                        rotation = 0,
                        alpha = 1,
                        ease = EasingType.EaseOutQuad
                    });
                    selectedEventBeat = snappedBeat;
                });
                menu.ShowAsContext();
            }
            e.Use();
        }
    }

    int GetLaneIndex(int laneId)
    {
        return currentChart.laneDefinitions.FindIndex(x => x.lane_id == laneId);
    }

    double GetSnappedBeat(double rawBeat)
    {
        double bestDist = double.MaxValue;
        double bestGuide = -1;
        double snapRange = 0.05f; 

        foreach (var guide in guideBeats)
        {
            double dist = System.Math.Abs(guide - rawBeat);
            if (dist < snapRange && dist < bestDist)
            {
                bestDist = dist;
                bestGuide = guide;
            }
        }

        if (bestGuide != -1) return bestGuide;

        if (snapDivisor <= 0) return System.Math.Round(rawBeat);

        double step = 1.0 / (double)snapDivisor;

        double snapped = System.Math.Round(rawBeat / step) * step;

        return snapped < 0 ? 0 : snapped;
    }

    void DrawSidebarContent()
    {
        inspectorScrollPos = GUILayout.BeginScrollView(inspectorScrollPos);

        GUILayout.Label("CHART INFO", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical();
        GUILayout.Label("Song ID", EditorStyles.miniLabel);
        currentChart.metadata.id = EditorGUILayout.TextField(currentChart.metadata.id);
        GUILayout.EndVertical();
        GUILayout.BeginVertical(GUILayout.Width(50));
        GUILayout.Label("Level", EditorStyles.miniLabel);
        currentChart.metadata.level = EditorGUILayout.IntField(currentChart.metadata.level);
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        GUILayout.Label("Difficulty  (Logic)", EditorStyles.miniLabel);
        currentChart.metadata.difficultyType = EditorGUILayout.TextField(currentChart.metadata.difficultyType);
        GUILayout.Label("Difficulty Name (Visual)", EditorStyles.miniLabel);
        currentChart.metadata.difficultyName = EditorGUILayout.TextField(currentChart.metadata.difficultyName);

        GUILayout.Space(5);

        GUILayout.Label("METADATA", EditorStyles.boldLabel);

        EditorGUIUtility.labelWidth = 60; 
        currentChart.metadata.title = EditorGUILayout.TextField("Title", currentChart.metadata.title);
        currentChart.metadata.artist = EditorGUILayout.TextField("Artist", currentChart.metadata.artist);
        currentChart.metadata.charter = EditorGUILayout.TextField("Charter", currentChart.metadata.charter);
        EditorGUIUtility.labelWidth = 0; 

        GUILayout.Space(10);

        GUILayout.Label("ASSETS", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        GUILayout.BeginHorizontal();

        currentClip = (AudioClip)DrawAssetSlot("Audio", currentClip, typeof(AudioClip));
        currentBackground = DrawAssetSlot("BG", currentBackground, typeof(Object));
        currentCover = (Texture2D)DrawAssetSlot("Cover", currentCover, typeof(Texture2D));

        GUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
        {
            if (currentClip != null) currentChart.metadata.audioPath = AssetDatabase.GetAssetPath(currentClip);
            if (currentBackground != null) currentChart.metadata.backgroundPath = AssetDatabase.GetAssetPath(currentBackground);
            if (currentCover != null) currentChart.metadata.ArtCoverPath = AssetDatabase.GetAssetPath(currentCover);
            if (editorAudioSource != null) editorAudioSource.clip = currentClip;
        }

        GUILayout.Space(10);

        GUILayout.Label("TIMING", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        EditorGUIUtility.labelWidth = 40;
        currentChart.metadata.bpm = EditorGUILayout.DoubleField("BPM", currentChart.metadata.bpm);
        currentChart.metadata.offset_ms = EditorGUILayout.DoubleField("Ofs", currentChart.metadata.offset_ms);
        EditorGUIUtility.labelWidth = 0;
        GUILayout.EndHorizontal();

        GUILayout.Label("Preview Section", EditorStyles.miniBoldLabel);
        GUILayout.BeginHorizontal();
        EditorGUIUtility.labelWidth = 40;
        currentChart.metadata.previewStartBeat = EditorGUILayout.DoubleField("Start", currentChart.metadata.previewStartBeat);
        currentChart.metadata.previewDurationBeats = EditorGUILayout.DoubleField("Dur", currentChart.metadata.previewDurationBeats);
        EditorGUIUtility.labelWidth = 0;
        GUILayout.EndHorizontal();

        DrawHorizontalLine();

        GUILayout.Label("LANE SETUP", EditorStyles.boldLabel);

        for (int i = 0; i < currentChart.laneDefinitions.Count; i++)
        {
            var lane = currentChart.laneDefinitions[i];
            GUILayout.BeginHorizontal();

            bool isSelected = (editorSelectedLaneID == lane.lane_id && selectedEventBeat == -1);
            if (GUILayout.Button($"L{lane.lane_id}", isSelected ? EditorStyles.miniButtonMid : EditorStyles.miniButtonLeft))
            {
                editorSelectedLaneID = lane.lane_id;
                selectedNote = null;
                selectedEventBeat = -1;
                activeEditingEvent = null;
                GUI.FocusControl(null);
            }

            GUI.backgroundColor = new Color(1, 0.6f, 0.6f);
            if (GUILayout.Button("X", EditorStyles.miniButtonRight, GUILayout.Width(20)))
            {
                int lid = lane.lane_id;
                currentChart.laneDefinitions.RemoveAt(i);
                currentChart.notes.RemoveAll(n => n.lane == lid);
                currentChart.lane_events.RemoveAll(e => e.lane_id == lid);
                if (editorSelectedLaneID == lid) editorSelectedLaneID = -1;
                return;
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
        }

        if (GUILayout.Button("+ Add Lane"))
        {
            int newId = currentChart.laneDefinitions.Count > 0 ? currentChart.laneDefinitions.Max(l => l.lane_id) + 1 : 0;
            AddLaneDefinition(newId);
        }

        GUILayout.Space(10);
        DrawHorizontalLine();

        DrawInspector();

        GUILayout.EndScrollView();
    }

    void DrawHorizontalLine()
    {
        Rect rect = GUILayoutUtility.GetRect(1, 1);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), new Color(0.5f, 0.5f, 0.5f, 0.5f));
    }

    void DrawInspector()
    {

        if (editorSelectedLaneID != -1 && selectedNote == null && selectedEventBeat == -1)
        {
            GUILayout.Label($"LANE DEFINITION (ID: {editorSelectedLaneID})", EditorStyles.boldLabel);
            var def = currentChart.laneDefinitions.Find(l => l.lane_id == editorSelectedLaneID);

            if (def != null)
            {
                GUILayout.Space(5);
                GUILayout.Label("Base Transform (Start)", EditorStyles.miniBoldLabel);

                def.pos_x = EditorGUILayout.FloatField("Base X", def.pos_x);
                def.pos_y = EditorGUILayout.FloatField("Base Y", def.pos_y);
                def.rotation = EditorGUILayout.FloatField("Base Rotation", def.rotation);
                def.alpha = EditorGUILayout.Slider("Base Alpha", def.alpha, 0f, 1f);

                GUILayout.Space(5);
                def.autoplay = EditorGUILayout.Toggle("Autoplay", def.autoplay);

                GUILayout.Space(10);
                GUILayout.Label("ℹ Click Timeline Header or Drag in Editor\nto edit Keyframes/Events.", EditorStyles.centeredGreyMiniLabel);
            }
            return;
        }

        if (selectedNote != null)
        {
            GUILayout.Label("NOTE INSPECTOR", EditorStyles.boldLabel);
            selectedNote.type = (NoteType)EditorGUILayout.EnumPopup("Type", selectedNote.type);
            selectedNote.beat = EditorGUILayout.DoubleField("Beat", selectedNote.beat);
            selectedNote.lane = EditorGUILayout.IntField("Lane ID", selectedNote.lane);
            if (selectedNote.type == NoteType.Hold) selectedNote.duration = EditorGUILayout.DoubleField("Duration", selectedNote.duration);
            return;
        }

        if (selectedEventBeat != -1)
        {
            GUILayout.Label($"TIMELINE EVENTS @ {selectedEventBeat:0.##}", EditorStyles.boldLabel);

            var svAtBeat = currentChart.sv_events.Where(e => Mathf.Abs((float)(e.beat - selectedEventBeat)) < 0.001f).ToList();
            SVEvent svToRemove = null;

            if (svAtBeat.Count > 0)
            {
                GUILayout.Label("SV Events:", EditorStyles.miniBoldLabel);
                foreach (var sv in svAtBeat)
                {
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Speed Config");
                    if (GUILayout.Button("X", GUILayout.Width(20))) svToRemove = sv;
                    GUILayout.EndHorizontal();

                    sv.target_speed = EditorGUILayout.FloatField("Target Spd", sv.target_speed);
                    sv.duration = EditorGUILayout.DoubleField("Duration", sv.duration);
                    sv.ease = (EasingType)EditorGUILayout.EnumPopup("Ease", sv.ease);
                    GUILayout.EndVertical();
                }
            }
            if (svToRemove != null) currentChart.sv_events.Remove(svToRemove);

            var lanesAtBeat = currentChart.lane_events.Where(e => Mathf.Abs((float)(e.beat - selectedEventBeat)) < 0.001f).ToList();
            LaneEvent laneEvtToRemove = null;

            if (lanesAtBeat.Count > 0)
            {
                GUILayout.Space(5);
                GUILayout.Label($"Lane Keyframes ({lanesAtBeat.Count}):", EditorStyles.miniBoldLabel);
                foreach (var lev in lanesAtBeat)
                {
                    GUILayout.BeginVertical(EditorStyles.helpBox);

                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Lane {lev.lane_id}");
                    if (GUILayout.Button("X", GUILayout.Width(20))) laneEvtToRemove = lev;
                    GUILayout.EndHorizontal();

                    lev.lane_id = EditorGUILayout.IntField("Target Lane", lev.lane_id);
                    lev.pos_x = EditorGUILayout.FloatField("X", lev.pos_x);
                    lev.pos_y = EditorGUILayout.FloatField("Y", lev.pos_y);
                    lev.rotation = EditorGUILayout.FloatField("Rotation", lev.rotation);
                    lev.autoplay = EditorGUILayout.Toggle("Autoplay", lev.autoplay);
                    lev.alpha = EditorGUILayout.Slider("Alpha (Opacity)", lev.alpha, 0f, 1f);
                    lev.ease = (EasingType)EditorGUILayout.EnumPopup("Ease", lev.ease);

                    GUILayout.EndVertical();
                }
            }
            if (laneEvtToRemove != null) currentChart.lane_events.Remove(laneEvtToRemove);

            if (svAtBeat.Count == 0 && lanesAtBeat.Count == 0)
            {
                GUILayout.Label("No events here.", EditorStyles.centeredGreyMiniLabel);
            }
        }
    }

    void TogglePlayback()
    {
        
        EnsureEditorAudioSource();

        if (previewSource.clip == null) ReloadAssets();

        if (previewSource.clip == null)
        {
            Debug.LogWarning("Chưa có nhạc! Hãy kéo file nhạc vào ô Audio ở Metadata.");
            return;
        }

        if (previewSource.isPlaying)
        {
            previewSource.Pause();
            startBeatMarker = GetBeatFromTime(previewSource.time);

            Repaint();
        }
        else
        {
            SetAudioTime(startBeatMarker);

            double currentBeat = scrollPos.x / pixelsPerBeat;

            previewSource.pitch = playbackSpeeds[selectedSpeedIndex];
            previewSource.Play();
        }
    }

    void SetAudioTime(double beat)
    {
        if (previewSource == null || currentChart == null) return;
        float time = (float)(beat * 60.0 / currentChart.metadata.bpm) + (float)(currentChart.metadata.offset_ms / 1000.0);
        previewSource.time = Mathf.Max(0, time);
    }

    void CreateNewChart()
    {
        currentChart = new ChartData { metadata = new ChartMetadata { title = "New Song", bpm = 120 }, laneDefinitions = new List<LaneData>(), notes = new List<NoteData>(), sv_events = new List<SVEvent>(), lane_events = new List<LaneEvent>() };
        for (int i = 0; i < 2; i++) AddLaneDefinition(i); 
    }
    void AddLaneDefinition(int id) { currentChart.laneDefinitions.Add(new LaneData { lane_id = id, pos_x = -5, pos_y = (id == 0 ? 2 : -2), alpha = 1f, autoplay = false, rotation = 0 }); }
    void DrawToolbar()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(40))) SaveChart();
        if (GUILayout.Button("Load", EditorStyles.toolbarButton, GUILayout.Width(40))) LoadChart();

        GUILayout.Space(10);
        GUILayout.Label("Speed:", GUILayout.Width(40));
        string[] speedLabels = playbackSpeeds.Select(s => s + "x").ToArray();
        int newSpeed = EditorGUILayout.Popup(selectedSpeedIndex, speedLabels, EditorStyles.toolbarPopup, GUILayout.Width(50));
        if (newSpeed != selectedSpeedIndex)
        {
            selectedSpeedIndex = newSpeed;
            if (previewSource != null) previewSource.pitch = playbackSpeeds[selectedSpeedIndex];
        }

        bool isPlaying = previewSource != null && previewSource.isPlaying;
        if (GUILayout.Button(isPlaying ? "Stop [Spc]" : "Play [Spc]", EditorStyles.toolbarButton, GUILayout.Width(80))) TogglePlayback();

        GUILayout.Label($"Beat: {GetBeatFromTime((previewSource ? previewSource.time : 0)):F1}");
        GUILayout.Space(10);
        GUILayout.Label("Jump to Beat:", EditorStyles.miniLabel);

        double currentBeatDisplay = (previewSource != null) ? GetBeatFromTime(previewSource.time) : 0;
        double newBeat = EditorGUILayout.DoubleField(currentBeatDisplay, EditorStyles.toolbarTextField, GUILayout.Width(50));

        if (Mathf.Abs((float)(newBeat - currentBeatDisplay)) > 0.01f)
        {
            startBeatMarker = newBeat; 
            SetAudioTime(newBeat);     

            scrollPos.x = (float)(newBeat * pixelsPerBeat) - 100; 
        }

        GUILayout.Space(20);

        Color oldColor = GUI.backgroundColor;
        if (isRecordingTaps) GUI.backgroundColor = Color.red;

        if (GUILayout.Button(isRecordingTaps ? "● REC Taps ON" : "○ REC Taps OFF", EditorStyles.toolbarButton, GUILayout.Width(100)))
        {
            isRecordingTaps = !isRecordingTaps;
            
            if (isRecordingTaps)
            {
                currentEditorTool = EditorTool.None;
                GUI.FocusControl(null); 
            }
        }
        GUI.backgroundColor = oldColor;

        if (GUILayout.Button("Clear Guides", EditorStyles.toolbarButton))
        {
            guideBeats.Clear();
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button($"Hand (H)", currentEditorTool == EditorTool.Hand ? EditorStyles.miniButtonMid : EditorStyles.toolbarButton)) currentEditorTool = EditorTool.Hand;
        if (GUILayout.Button($"Pen (B)", currentEditorTool == EditorTool.Pen ? EditorStyles.miniButtonMid : EditorStyles.toolbarButton)) currentEditorTool = EditorTool.Pen;

        GUILayout.Label("Note:");
        selectedTool = (NoteType)EditorGUILayout.EnumPopup(selectedTool, EditorStyles.toolbarPopup, GUILayout.Width(50));

        GUILayout.Label("Snap:");
        snapDivisor = EditorGUILayout.IntField(snapDivisor, EditorStyles.toolbarTextField, GUILayout.Width(30));

        GUILayout.Space(10);
        GUILayout.Label("Zoom:");
        pixelsPerBeat = GUILayout.HorizontalSlider(pixelsPerBeat, MIN_ZOOM, MAX_ZOOM, GUILayout.Width(100));

        GUILayout.EndHorizontal();
    }
    void SaveChart() { string json = JsonUtility.ToJson(currentChart, true); string path = EditorUtility.SaveFilePanel("Save", "", currentChart.metadata.title, "json"); if (!string.IsNullOrEmpty(path)) System.IO.File.WriteAllText(path, json); }
    void LoadChart() { string path = EditorUtility.OpenFilePanel("Load", "", "json"); if (!string.IsNullOrEmpty(path)) { currentChart = JsonUtility.FromJson<ChartData>(System.IO.File.ReadAllText(path)); ReloadAssets(); } }
    double GetBeatFromTime(float time) { if (currentChart == null || currentChart.metadata.bpm <= 0) return 0; return (time - (currentChart.metadata.offset_ms / 1000.0)) * currentChart.metadata.bpm / 60.0; }
    void DrawEditorGrid(Rect r, Vector2 o, float s) { Handles.color = new Color(1, 1, 1, 0.1f); for (int i = -20; i <= 20; i++) { float p = i * s; Handles.DrawLine(new Vector2(o.x + p, 0), new Vector2(o.x + p, r.height)); Handles.DrawLine(new Vector2(0, o.y + p), new Vector2(r.width, o.y + p)); } Handles.color = Color.green; Handles.DrawLine(new Vector2(o.x, 0), new Vector2(o.x, r.height)); Handles.DrawLine(new Vector2(0, o.y), new Vector2(r.width, o.y)); }
    int GetNoteAtMouse(float mx, int li, out NoteData rn) { rn = null; double b = mx / pixelsPerBeat; foreach (var n in currentChart.notes) { if (currentChart.laneDefinitions.FindIndex(l => l.lane_id == n.lane) != li) continue; if (Mathf.Abs((float)(n.beat - b)) < 0.15f) { rn = n; return 1; } if (n.type == NoteType.Hold && b > n.beat && b < n.beat + n.duration + 0.15f) { rn = n; return Mathf.Abs((float)(n.beat + n.duration - b)) < 0.15f ? 2 : 1; } } return 0; }
    LaneState CalculateLaneState(int laneID, double currentBeat)
    {
        var def = currentChart.laneDefinitions.Find(l => l.lane_id == laneID); if (def == null) return new LaneState();
        float x = def.pos_x, y = def.pos_y, rot = def.rotation, alpha = def.alpha;
        var prev = currentChart.lane_events.Where(e => e.lane_id == laneID && e.beat <= currentBeat).OrderByDescending(e => e.beat).FirstOrDefault();
        var next = currentChart.lane_events.Where(e => e.lane_id == laneID && e.beat > currentBeat).OrderBy(e => e.beat).FirstOrDefault();
        if (prev != null && next != null) { float t = (float)((currentBeat - prev.beat) / (next.beat - prev.beat)); x = EasingHelper.Interpolate(prev.pos_x, next.pos_x, t, prev.ease); y = EasingHelper.Interpolate(prev.pos_y, next.pos_y, t, prev.ease); rot = EasingHelper.Interpolate(prev.rotation, next.rotation, t, prev.ease); }
        else if (prev != null) { x = prev.pos_x; y = prev.pos_y; rot = prev.rotation; };
        return new LaneState { x = x, y = y, rot = rot, alpha = alpha };
    }
    Vector2 LogicToScreen(Vector2 l, Vector2 o, float s) { return new Vector2(o.x + l.x * s, o.y - l.y * s); }
    Vector2 ScreenToLogic(Vector2 screenPos, Vector2 origin, float scale) 
    {
        return new Vector2((screenPos.x - origin.x) / scale, (origin.y - screenPos.y) / scale);
    }
    void HandleLaneEditorInput(Rect globalRect, Vector2 origin, float scale, double currentBeat)
    {
        Event e = Event.current;

        if (!globalRect.Contains(e.mousePosition)) return;

        Vector2 localMousePos = e.mousePosition - globalRect.position;

        if (e.type == EventType.ScrollWheel)
        {
            float zoomSpeed = 0.05f;
            editorZoom -= e.delta.y * zoomSpeed;
            editorZoom = Mathf.Clamp(editorZoom, 0.1f, 5.0f);
            e.Use(); Repaint();
        }

        if (e.type == EventType.MouseDrag && (e.button == 1 || e.button == 2))
        {
            editorPanOffset += e.delta;
            e.Use();
        }

        if (e.button == 0)
        {
            if (e.type == EventType.MouseDown)
            {
                
                int clickedLane = GetLaneAtScreenPos(localMousePos, origin, scale, currentBeat);

                if (clickedLane != -1)
                {
                    editorSelectedLaneID = clickedLane;
                    isDraggingLaneInEditor = true;

                    activeEditingEvent = currentChart.lane_events
                        .FirstOrDefault(ev => ev.lane_id == editorSelectedLaneID && Mathf.Abs((float)(ev.beat - currentBeat)) < 0.1f);

                    if (activeEditingEvent == null)
                    {
                        LaneState currentState = CalculateLaneState(editorSelectedLaneID, currentBeat);
                        LaneEvent newEvt = new LaneEvent
                        {
                            beat = currentBeat,
                            lane_id = editorSelectedLaneID,
                            pos_x = currentState.x,
                            pos_y = currentState.y,
                            rotation = currentState.rot,
                            alpha = currentState.alpha,
                            ease = EasingType.Linear
                        };
                        currentChart.lane_events.Add(newEvt);
                        activeEditingEvent = newEvt; 

                        currentChart.lane_events = currentChart.lane_events.OrderBy(x => x.beat).ToList();
                    }
                }
                else
                {
                    editorSelectedLaneID = -1;
                    activeEditingEvent = null;
                }
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && isDraggingLaneInEditor && activeEditingEvent != null)
            {
                
                float deltaX = e.delta.x / scale;
                float deltaY = -e.delta.y / scale;

                if (e.shift) { deltaX *= 0.1f; deltaY *= 0.1f; } 

                activeEditingEvent.pos_x += deltaX;
                activeEditingEvent.pos_y += deltaY;

                Repaint();
                e.Use();
            }
            else if (e.type == EventType.MouseUp)
            {
                isDraggingLaneInEditor = false;
            }
        }
    }

    void DrawNotes(Rect rect)
    {
        float startBeat = scrollPos.x / pixelsPerBeat - 4; float endBeat = (scrollPos.x + rect.width) / pixelsPerBeat + 4;
        for (int i = 0; i < currentChart.notes.Count; i++)
        {
            var n = currentChart.notes[i]; double nEnd = n.type == NoteType.Hold ? n.beat + n.duration : n.beat;
            if (nEnd < startBeat || n.beat > endBeat) continue;
            int lIdx = currentChart.laneDefinitions.FindIndex(l => l.lane_id == n.lane); if (lIdx == -1) continue;
            float x = (float)n.beat * pixelsPerBeat, y = HEADER_HEIGHT + lIdx * LANE_HEIGHT + 10, w = (n.type == NoteType.Hold ? (float)n.duration * pixelsPerBeat : 20), h = LANE_HEIGHT - 20;
            if (w < 5) w = 5; Color c = Color.green;
            if (n.type == NoteType.Hold) { c = new Color(1f, 0.92f, 0.016f); EditorGUI.DrawRect(new Rect(x, y, w, h), new Color(c.r, c.g, c.b, 0.3f)); EditorGUI.DrawRect(new Rect(x, y, 10, h), c); EditorGUI.DrawRect(new Rect(x + w - 5, y, 5, h), c); }
            else { if (n.type == NoteType.Drag) c = Color.magenta; if (n.type == NoteType.Blast) c = Color.red; if (selectedNote == n) c = Color.white; EditorGUI.DrawRect(new Rect(x, y, w, h), c); }
        }
    }

    Object DrawAssetSlot(string label, Object obj, System.Type type)
    {
        GUILayout.BeginVertical(GUILayout.Width(70)); 
        GUILayout.Label(label, EditorStyles.centeredGreyMiniLabel);

        Object result = EditorGUILayout.ObjectField(obj, type, false, GUILayout.Width(64), GUILayout.Height(64));
        
        GUILayout.EndVertical();
        return result;
    }

    int GetLaneAtScreenPos(Vector2 mousePos, Vector2 origin, float scale, double beat)
    {
        float bestDist = 30f; 
        int bestId = -1;

        foreach (var lane in currentChart.laneDefinitions)
        {
            
            LaneState state = CalculateLaneState(lane.lane_id, beat);

            Vector2 screenPos = LogicToScreen(new Vector2(state.x, state.y), origin, scale);

            float dist = Vector2.Distance(mousePos, screenPos);

            if (dist < bestDist)
            {
                bestDist = dist;
                bestId = lane.lane_id;
            }
        }
        return bestId;
    }
}