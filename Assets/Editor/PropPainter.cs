using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

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
        propRadius.floatValue = propRadius.floatValue.AtLeast(1);
        EditorGUILayout.PropertyField(propSpawnCount);
        propSpawnCount.intValue = propSpawnCount.intValue.AtLeast(1);
        
        if (so.ApplyModifiedProperties())
        {
            GenerateRandomPoints();
            SceneView.RepaintAll();
        }
        
        // detects when the left mouse button is clicked in the editor window
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            GUI.FocusControl(null);
            Repaint();
        }
    }

    void DrawSphere(Vector3 pos)
    {
        Handles.SphereHandleCap(-1, pos,Quaternion.identity, 0.1f, EventType.Repaint);
    }

    void DuringSceneGUI(SceneView sceneView)
    {
        Handles.zTest = CompareFunction.LessEqual;
        
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
                // create a ray to get the point on the disc
                Vector3 rayOrigin = hit.point + (hitTangent * p.x + hitBiTangent * p.y) * radius;
                // offset the points 
                rayOrigin += hitNormal * 2;
                Vector3 rayDirection = -hitNormal;

                // finding a point on the surface
                Ray pointRay = new Ray(rayOrigin, rayDirection);
                if (Physics.Raycast(pointRay, out RaycastHit pointHit))
                {
                    // draw at the hit point on the surface
                    DrawSphere(pointHit.point);
                    Handles.DrawAAPolyLine(pointHit.point, pointHit.point+ pointHit.normal);
                }
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
