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
    public GameObject spawnPrefab = null;
    
    private SerializedObject so;
    private SerializedProperty propRadius;
    private SerializedProperty propSpawnCount;
    private SerializedProperty propSpawnPrefab;

    private Vector2[] randomPoints;
    
    
    
    private void OnEnable()
    {
        so = new SerializedObject(this);
        propRadius = so.FindProperty("radius");
        propSpawnCount = so.FindProperty("spawnCount");
        propSpawnPrefab = so.FindProperty("spawnPrefab");
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
    

    void TrySpawnObjects(List<RaycastHit> hitPoints)
    {
        if (spawnPrefab == null)
            return;

        foreach (RaycastHit hit in hitPoints)
        {
            GameObject spawnObject = (GameObject) PrefabUtility.InstantiatePrefab(spawnPrefab);
            Undo.RegisterCreatedObjectUndo(spawnObject, "Spawn Objects");
            spawnObject.transform.position = hit.point;

            float randomAngleDegree = Random.value * 360;
            Quaternion randomRotation = Quaternion.Euler(0f, 0f, randomAngleDegree);
            
            // rotate the prefab on the X Axis
            Quaternion rot = Quaternion.LookRotation(hit.normal) * (randomRotation * Quaternion.Euler(90f, 0f, 0f)) ;
            spawnObject.transform.rotation = rot;

        }
         
        
    }
    
    void OnGUI()
    {
        so.Update();
        EditorGUILayout.PropertyField(propRadius);
        propRadius.floatValue = propRadius.floatValue.AtLeast(1);
        EditorGUILayout.PropertyField(propSpawnCount);
        propSpawnCount.intValue = propSpawnCount.intValue.AtLeast(1);
        EditorGUILayout.PropertyField(propSpawnPrefab);
        
        if (so.ApplyModifiedProperties())
        {
            GenerateRandomPoints();
            SceneView.RepaintAll();
        }

        // detects when  the left mouse button is clicked in the editor window
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

        //Ray ray = new Ray(camTransform.position, camTransform.forward);
        
        // repaint the scene view whenever the mouse moves
        if (Event.current.type == EventType.MouseMove)
        {
            sceneView.Repaint();
        }
        
        // is the alt key being held down
        bool holdingAlt = (Event.current.modifiers & EventModifiers.Alt) != 0;
        
        // only change the radius if the event type is scroll wheel, but
        // not holding down alt key
        // detect if the scroll wheel changed and change the radius
        if (Event.current.type == EventType.ScrollWheel && !holdingAlt)
        {
            // I don't really care of the value but more the direction. Strange that this does 
            // return an int??? but a float since it is either -1 or 1
            float scrollDirection = Mathf.Sign(Event.current.delta.y);
            
            so.Update();
            propRadius.floatValue *= 1f + scrollDirection * 0.05f;
            so.ApplyModifiedProperties();
            Repaint();
            Event.current.Use();

        }
        
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

        // cast a ray from camera to surface and project a normal from that hit point
        if(Physics.Raycast(ray, out RaycastHit hit))
        {
            // coordinate systems for each point in the disc
            Vector3 hitNormal = hit.normal;
            Vector3 hitTangent = Vector3.Cross(hitNormal, camTransform.up).normalized;
            Vector3 hitBiTangent = Vector3.Cross(hitNormal, hitTangent);

            Ray GetTangentRay(Vector2 tangentSpacePosition)
            {
                Vector3 rayOrigin = hit.point + (hitTangent * tangentSpacePosition.x + hitBiTangent * tangentSpacePosition.y) * radius;
                // offset the points 
                rayOrigin += hitNormal * 2;
                Vector3 rayDirection = -hitNormal;
                
                return new Ray(rayOrigin, rayDirection);
            }

            List<RaycastHit> hitPoints = new List<RaycastHit>();
            
            // draw points
            foreach (Vector2 p in randomPoints)
            {
                // finding a point on the surface
                Ray pointRay = GetTangentRay(p);
                if (Physics.Raycast(pointRay, out RaycastHit pointHit))
                {
                    hitPoints.Add(pointHit);
                    // draw at the hit point on the surface
                    DrawSphere(pointHit.point);
                    Handles.DrawAAPolyLine(pointHit.point, pointHit.point+ pointHit.normal);
                }
            }
            
            // check for a spacebar press to spawn objects
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Space)
            {
                TrySpawnObjects(hitPoints);
            }
            
            // draw the coordinate system
            Handles.color = Color.blue;
            Handles.DrawAAPolyLine(4, hit.point, hit.point + hitNormal);
            Handles.color = Color.green;
            Handles.DrawAAPolyLine(4, hit.point, hit.point + hitBiTangent);
            Handles.color = Color.red;
            Handles.DrawAAPolyLine(4, hit.point, hit.point + hitTangent);
            Handles.color = Color.white;
            
            // overlay the disc on to the surface
            // number of points on the circle
            const int circleDetail = 128;
            
            Vector3[] ringPoints = new Vector3[circleDetail];
            // get points around the circle (with an extra to draw the last one to the first
            for (int i = 0; i < circleDetail; i++)
            {
                const float TAU = 6.28318530718f;
                float t = i / (float)circleDetail;
                float angleRadian = t * TAU;
                Vector2 direction = new Vector2(Mathf.Cos(angleRadian), Mathf.Sin(angleRadian));
                Ray r =  GetTangentRay(direction);

                if (Physics.Raycast(r, out RaycastHit circleHit))
                {
                    ringPoints[i] = circleHit.point + circleHit.normal * 0.02f;
                }
                else
                {
                    ringPoints[i] = r.origin;
                }
            }
            
            //draw circle
            Handles.DrawAAPolyLine(ringPoints);
            
        }

        

    }
}
