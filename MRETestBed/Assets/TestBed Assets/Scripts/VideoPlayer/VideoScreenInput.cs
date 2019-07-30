using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEditor;

[RequireComponent(typeof(EventTrigger))]
public class VideoScreenInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    private Mesh mesh;
    private IVideoScreenInputHandler inputHandler;
    private float xRatio = 0f;
    private float yRatio = 0f;

    void Start()
    {
        mesh = this.EnsureComponent<MeshFilter>().mesh;
        inputHandler = this.EnsureComponent<IVideoScreenInputHandler>();
    }

    // Returns true if interacting with screen, false if otherwise.
    private bool UpdateCursorPosOnScreen()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane thisPlane = new Plane(transform.forward, transform.position);
        float distance;
        // Get the 3D point of the intersection
        if (thisPlane.Raycast(ray, out distance))
        {
            var intersectionPoint = (Vector2)transform.InverseTransformPoint(ray.origin + ray.direction * distance);
            intersectionPoint -= (Vector2)mesh.bounds.min;

            xRatio = intersectionPoint.x / (mesh.bounds.extents.x * 2);
            yRatio = intersectionPoint.y / (mesh.bounds.extents.y * 2);
            // There is a subtraction on the y because the remote desktop screen has the origin in the top left corner

            return true;
        }
        return false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (UpdateCursorPosOnScreen())
        {
            inputHandler.HandleSelectDrag(xRatio, yRatio);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (UpdateCursorPosOnScreen())
        {
            inputHandler.HandleSelectDown(xRatio, yRatio);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (UpdateCursorPosOnScreen())
        {
            inputHandler.HandleSelectUp(xRatio, yRatio);
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(VideoScreenInput))]
    public class VideoScreenInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            VideoScreenInput script = (VideoScreenInput)target;
            if (script.GetComponent<EventTrigger>() == null)
            {
                EditorGUILayout.HelpBox("SharedScreenInput needs a component of type EventTrigger to work.", MessageType.Error);
            }
        }
    }
#endif
}
