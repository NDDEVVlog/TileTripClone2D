using UnityEditor;
using UnityEngine;
using System.Linq;

public class LevelEditorWindow : EditorWindow
{
    private LevelData _currentLevel;
    private int _currentLayer = 0;
    private float _snapStep = 0.5f; 
    private readonly float _tileSize = 60f;
    private Vector2 _scrollPosition;

    [MenuItem("TileTrip/Level Editor")]
    public static void ShowWindow()
    {
        GetWindow<LevelEditorWindow>("Level Editor");
    }

    public void LoadLevelData(LevelData data)
    {
        _currentLevel = data;
        Repaint();
    }

    private void OnGUI()
    {
        DrawToolbar();

        if (_currentLevel == null)
        {
            EditorGUILayout.HelpBox("Assign a LevelData to begin.", MessageType.Warning);
            return;
        }

        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
        DrawWorkspace();
        GUILayout.EndScrollView();
    }

    private void DrawToolbar()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        _currentLevel = (LevelData)EditorGUILayout.ObjectField(_currentLevel, typeof(LevelData), false, GUILayout.Width(200));
        
        GUILayout.Space(10);
        GUILayout.Label("Edit Layer:", GUILayout.Width(65));
        _currentLayer = EditorGUILayout.IntSlider(_currentLayer, 0, 20, GUILayout.Width(150));
        
        GUILayout.Space(10);
        GUILayout.Label("Snap Size:", GUILayout.Width(65));
        
        if (GUILayout.Button("1.0 (Full)", EditorStyles.toolbarButton)) _snapStep = 1f;
        if (GUILayout.Button("0.5 (Half)", EditorStyles.toolbarButton)) _snapStep = 0.5f;
        if (GUILayout.Button("0.25 (Quarter)", EditorStyles.toolbarButton)) _snapStep = 0.25f;

        GUILayout.FlexibleSpace();
        
        if (_currentLevel != null)
        {
            GUILayout.Label($"Total Tiles: {_currentLevel.LayoutCoordinates.Count}");
            if (GUILayout.Button("Clear All", EditorStyles.toolbarButton))
            {
                _currentLevel.LayoutCoordinates.Clear();
                EditorUtility.SetDirty(_currentLevel);
            }
        }
        
        GUILayout.EndHorizontal();
    }

    private void DrawWorkspace()
    {
        Rect workspaceRect = GUILayoutUtility.GetRect(2000, 2000);
        EditorGUI.DrawRect(workspaceRect, new Color(0.15f, 0.15f, 0.15f));

        Vector2 centerOffset = workspaceRect.center;

        DrawGrid(workspaceRect, centerOffset);
        DrawTiles(workspaceRect, centerOffset);
        HandleMouseEvents(workspaceRect, centerOffset);
    }

    private void DrawGrid(Rect workspaceRect, Vector2 center)
    {
        // 1. Vẽ lưới mờ (Grid)
        Handles.color = new Color(1, 1, 1, 0.05f);
        float stepX = _tileSize * _snapStep;
        
        for (float i = center.x % stepX; i < workspaceRect.width; i += stepX)
            Handles.DrawLine(new Vector2(i, 0), new Vector2(i, workspaceRect.height));
            
        for (float j = center.y % stepX; j < workspaceRect.height; j += stepX)
            Handles.DrawLine(new Vector2(0, j), new Vector2(workspaceRect.width, j));

        // 2. Vẽ 2 trục toạ độ chính đi qua tâm (0,0)
        
        // Trục X (Ngang) - Màu Đỏ
        Handles.color = new Color(1f, 0.3f, 0.3f, 0.6f);
        Handles.DrawAAPolyLine(3f, new Vector2(0, center.y), new Vector2(workspaceRect.width, center.y));

        // Trục Y (Dọc) - Màu Xanh Lá
        Handles.color = new Color(0.3f, 1f, 0.3f, 0.6f);
        Handles.DrawAAPolyLine(3f, new Vector2(center.x, 0), new Vector2(center.x, workspaceRect.height));

        // 3. Vẽ tâm điểm và Text (0,0)
        Handles.color = Color.yellow;
        Handles.DrawSolidDisc(center, Vector3.forward, 4f); // Chấm tròn ngay tâm

        GUIStyle originStyle = new GUIStyle(EditorStyles.boldLabel);
        originStyle.normal.textColor = Color.yellow;
        GUI.Label(new Rect(center.x + 5, center.y + 5, 50, 20), "(0, 0)", originStyle);
    }

    private void DrawTiles(Rect workspaceRect, Vector2 centerOffset)
    {
        var sortedTiles = _currentLevel.LayoutCoordinates.OrderBy(t => t.Layer).ToList();

        foreach (var coord in sortedTiles)
        {
            Vector2 drawPos = centerOffset + new Vector2(coord.Position.x * _tileSize, -coord.Position.y * _tileSize);
            Rect tileRect = new Rect(drawPos.x - _tileSize / 2, drawPos.y - _tileSize / 2, _tileSize, _tileSize);

            bool isCurrentLayer = coord.Layer == _currentLayer;
            float brightness = 0.4f + (coord.Layer * 0.05f);
            
            Color tileColor = isCurrentLayer 
                ? new Color(0.3f, 0.8f, 0.3f, 1f) 
                : new Color(brightness, brightness, brightness, 1f);

            EditorGUI.DrawRect(new Rect(tileRect.x + 4, tileRect.y + 4, tileRect.width, tileRect.height), new Color(0, 0, 0, 0.4f));

            EditorGUI.DrawRect(tileRect, tileColor);

            Handles.color = isCurrentLayer ? Color.white : Color.black;
            Handles.DrawWireCube(tileRect.center, tileRect.size);

            Handles.color = isCurrentLayer ? Color.red : new Color(0, 0, 0, 0.5f);
            Handles.DrawLine(tileRect.center + Vector2.up * 4, tileRect.center + Vector2.down * 4);
            Handles.DrawLine(tileRect.center + Vector2.left * 4, tileRect.center + Vector2.right * 4);

            GUIStyle style = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.UpperLeft };
            style.normal.textColor = isCurrentLayer ? Color.white : Color.black;
            GUI.Label(new Rect(tileRect.x + 2, tileRect.y + 2, 20, 20), $"{coord.Layer}", style);
        }
    }

    private void HandleMouseEvents(Rect workspaceRect, Vector2 centerOffset)
    {
        Event e = Event.current;

        if (!workspaceRect.Contains(e.mousePosition) || (e.type != EventType.MouseDown && e.type != EventType.MouseDrag)) 
            return;

        Vector2 clickPos = e.mousePosition - centerOffset;
        Vector2 exactWorldClick = new Vector2(clickPos.x / _tileSize, -clickPos.y / _tileSize);
        
        Vector2 snappedWorldPos = new Vector2(
            Mathf.Round(exactWorldClick.x / _snapStep) * _snapStep,
            Mathf.Round(exactWorldClick.y / _snapStep) * _snapStep
        );

        if (e.button == 0)
        {
            if (!IsOverlappingInSameLayer(snappedWorldPos, _currentLayer))
            {
                _currentLevel.LayoutCoordinates.Add(new TileCoordinate { Position = snappedWorldPos, Layer = _currentLayer });
                EditorUtility.SetDirty(_currentLevel);
                GUI.changed = true;
            }
            e.Use();
        }
        else if (e.button == 1)
        {
            int indexToRemove = _currentLevel.LayoutCoordinates.FindLastIndex(c => 
                c.Layer == _currentLayer &&
                exactWorldClick.x >= c.Position.x - 0.5f && exactWorldClick.x <= c.Position.x + 0.5f &&
                exactWorldClick.y >= c.Position.y - 0.5f && exactWorldClick.y <= c.Position.y + 0.5f);

            if (indexToRemove != -1)
            {
                _currentLevel.LayoutCoordinates.RemoveAt(indexToRemove);
                EditorUtility.SetDirty(_currentLevel);
                GUI.changed = true;
            }
            e.Use();
        }
    }

    private bool IsOverlappingInSameLayer(Vector2 newPos, int layer)
    {
        foreach (var tile in _currentLevel.LayoutCoordinates)
        {
            if (tile.Layer == layer)
            {
                bool intersectX = Mathf.Abs(tile.Position.x - newPos.x) < 0.99f;
                bool intersectY = Mathf.Abs(tile.Position.y - newPos.y) < 0.99f;

                if (intersectX && intersectY) return true;
            }
        }
        return false;
    }
}