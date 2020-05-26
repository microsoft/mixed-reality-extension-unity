using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

[RequireComponent(typeof(CharacterController))]
public class MRTKCharacterController : MonoBehaviour
{
	[SerializeField] private bool m_IsWalking;
	[SerializeField] private float m_WalkSpeed;
	[SerializeField] private float m_RunSpeed;
	[SerializeField] private float m_StickToGroundForce;
	[SerializeField] private float m_GravityMultiplier;

	private Camera m_Camera;
	private float m_YRotation;
	private Vector3 m_Input;
	private Vector3 m_MoveDir = Vector3.zero;
	private CharacterController m_CharacterController;
	private CollisionFlags m_CollisionFlags;

	// Use this for initialization
	private void Start()
	{
		m_CharacterController = GetComponent<CharacterController>();
		m_Camera = Camera.main;
	}


	// Update is called once per frame
	private void Update()
	{
		RotateView();
	}


	private void FixedUpdate()
	{
		float speed;
		GetInput(out speed);
		// always move along the camera forward as it is the direction that it being aimed at
		Vector3 desiredMove = m_Camera.transform.forward * m_Input.y
							+ m_Camera.transform.right * m_Input.x
							+ m_Camera.transform.up * m_Input.z;

		m_MoveDir = desiredMove * speed;

		if (!m_CharacterController.isGrounded)
		{
			m_MoveDir += Physics.gravity * m_GravityMultiplier * Time.fixedDeltaTime;
		}
		m_CollisionFlags = m_CharacterController.Move(m_MoveDir * Time.fixedDeltaTime);
	}

	private void GetInput(out float speed)
	{
		// Read input
		float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
		float vertical = CrossPlatformInputManager.GetAxis("Vertical");
		float upordown = 0.0f;

		bool waswalking = m_IsWalking;

#if !MOBILE_INPUT
		// On standalone builds, walk/run speed is modified by a key press.
		// keep track of whether or not the character is walking or running
		m_IsWalking = !Input.GetKey(KeyCode.LeftShift);

		// add also additional possibility for up-down with keys
		upordown = (Input.GetKey(KeyCode.Q)) ? m_WalkSpeed : ((Input.GetKey(KeyCode.Z)) ? -m_WalkSpeed : 0.0f);
#endif

		// set the desired speed to be walking or running
		speed = m_IsWalking ? m_WalkSpeed : m_RunSpeed;
		m_Input = new Vector3(horizontal, vertical, upordown);

		// normalize input if it exceeds 1 in combined length:
		if (m_Input.sqrMagnitude > 1)
		{
			m_Input.Normalize();
		}
	}


	private void RotateView()
	{
		
	}


	private void OnControllerColliderHit(ControllerColliderHit hit)
	{
		Rigidbody body = hit.collider.attachedRigidbody;
		//dont move the rigidbody if the character is on top of it
		if (m_CollisionFlags == CollisionFlags.Below)
		{
			return;
		}

		if (body == null || body.isKinematic)
		{
			return;
		}
		body.AddForceAtPosition(m_CharacterController.velocity * 0.1f, hit.point, ForceMode.Impulse);
	}
}
