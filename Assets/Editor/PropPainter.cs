using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class PropPainter : EditorWindow
{
    [MenuItem("Tools/Prop Painter")]
    public static void OpenPropPainter() => GetWindow<PropPainter>();

    public float radius = 2f;
    public int spawnCount = 8;

    private SerializedObject so;
    private SerializedProperty propRadius;
    private SerializedProperty propSpawnCount;

    private Vector2[] randomPoints;
    
    
    
    private void OnEnable()
    {
        so = new SerializedObject(this);
        propRadius = so.FindProperty("radius");
        propSpawnCount = so.FindProperty("spawnCount");
        GenerateRandomPoints();
        SceneView.duringSceneGui += DuringSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DuringSceneGUI;
    }

    void GenerateRandomPoints()
    {
        randomPoints = new Vector2[spawnCount];
        for (int i = 0; i < spawnCount; i++)
        {
            // get a random point inside the disc
            randomPoints[i] = Random.insideUnitCircle;
        }
    }
    
    void OnGUI()
    {
        so.Update();
        EditorGUILayout.PropertyField(propRadius);
        EditorGUILayout.PropertyField(propSpawnCount);
        if (so.ApplyModifiedProperties())
        {
            SceneView.RepaintAll();
        }
    }

    void DrawSphere(Vector3 pos)
    {
        Handles.SphereHandleCap(-1, pos,Quaternion.identity, 0.1f, EventType.Repaint);
    }

    void DuringSceneGUI(SceneView sceneView)
    {
        Transform camTransform = sceneView.camera.transform;
        
        
        Ray ray = new Ray(camTransform.position, camTransform.forward);

        // cast a ray from camera to surface and project a normal from that hit point
        if(Physics.Raycast(ray, out RaycastHit hit))
        {
            // coordinate systems for each point in the disc
            Vector3 hitNormal = hit.normal;
            Vector3 hitTangent = Vector3.Cross(hitNormal, camTransform.up).normalized;
            Vector3 hitBiTangent = Vector3.Cross(hitNormal, hitTangent);
            
            // draw points
            foreach (Vector2 p in randomPoints)
            {
                Vector3 worldPosition = hit.point + hitTangent * p.x + hitBiTangent * p.y;
                DrawSphere(worldPosition);
            }
            
            // draw the coordinate system
            Handles.color = Color.blue;
            Handles.DrawAAPolyLine(4, hit.point, hit.point + hitNormal);
            Handles.color = Color.green;
            Handles.DrawAAPolyLine(4, hit.point, hit.point + hitBiTangent);
            Handles.color = Color.red;
            Handles.DrawAAPolyLine(4, hit.point, hit.point + hitTangent);
            Handles.color = Color.white;
            Handles.DrawWireDisc(hit.point, hit.normal, radius);
        }

        
    }
}
