using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MreInputModule : PointerInputModule
{
	private const float MAX_CLICK_INTERVAL = 0.15f;

	private List<RaycastResult> raycastResults = new List<RaycastResult>(5);
	private HashSet<GameObject> lastHits = new HashSet<GameObject>();
	private HashSet<GameObject> receivedMouseDown = new HashSet<GameObject>();
	private bool mouseDownOnLastCheck = false;
	private float mouseDownTime = -1;

	public override void Process()
	{
		var mouseDown = Input.GetMouseButtonDown(0);

		var pointerEvent = new PointerEventData(eventSystem)
		{
			position = Input.mousePosition
		};
		if (mouseDown)
		{
			pointerEvent.button = PointerEventData.InputButton.Left;
			mouseDownTime = Time.realtimeSinceStartup;
		}

		eventSystem.RaycastAll(pointerEvent, raycastResults);
		var hits = new HashSet<GameObject>(raycastResults.Select(rc => rc.gameObject));

		// new hits
		foreach (var hit in hits)
		{
			if (!lastHits.Contains(hit))
			{
				hit.SendMessage("OnPointerEnter", pointerEvent, SendMessageOptions.DontRequireReceiver);
			}

			if (mouseDown && !mouseDownOnLastCheck)
			{
				hit.SendMessage("OnPointerDown", pointerEvent, SendMessageOptions.DontRequireReceiver);
				receivedMouseDown.Add(hit);
			}
			else if (mouseDownOnLastCheck && !mouseDown)
			{
				hit.SendMessage("OnPointerUp", pointerEvent, SendMessageOptions.DontRequireReceiver);

				if (receivedMouseDown.Contains(hit) && Time.realtimeSinceStartup - mouseDownTime < MAX_CLICK_INTERVAL)
				{
					hit.SendMessage("OnPointerClick", pointerEvent, SendMessageOptions.DontRequireReceiver);
				}
			}
		}

		// absent hits
		foreach (var oldHit in lastHits)
		{
			if (!hits.Contains(oldHit))
			{
				oldHit.SendMessage("OnPointerExit", pointerEvent, SendMessageOptions.DontRequireReceiver);
			}
		}

		// clean up state
		if (!mouseDown)
		{
			receivedMouseDown.Clear();
			mouseDownTime = -1;
		}
		lastHits = hits;
		mouseDownOnLastCheck = mouseDown;
		raycastResults.Clear();
	}
}
