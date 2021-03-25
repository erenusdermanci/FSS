using System;
using UnityEngine;
using Utils;

namespace Player
{
    public class PlayerInput : MonoBehaviour
    {
        public float PlayerSpeed;

        public Camera PlayerCamera;
        public bool FloatingCamera;

        public float MinZoom;
        public float MaxZoom;
        public float ZoomAmount;
        public float ZoomSpeed;

        private float playerCurrentSpeed;
        private Transform playerTransform;
        private float horizontalMovement;
        private float verticalMovement;

        private Vector3 dragStart;
        private Vector3 dragDiff;
        private bool dragging;

        private float targetZoom;
        private bool zooming;

        // Start is called before the first frame update
        private void Start()
        {
            playerTransform = GetComponent<Transform>();
            playerCurrentSpeed = PlayerSpeed;

            dragStart = Vector3.zero;
            dragDiff = Vector3.zero;
            dragging = false;

            zooming = false;
            targetZoom = PlayerCamera.orthographicSize;
        }

        // Update is called once per frame
        private void Update()
        {
            if (Input.GetMouseButtonDown(2))
            {
                FloatingCamera = !FloatingCamera;
                dragging = false;
            }
            HandleMovementInput();
        }

        private void FixedUpdate()
        {
            if (!FloatingCamera)
                MovePlayer();
        }

        private void LateUpdate()
        {
            HandleFloatingCamera();
            HandleCameraZoom();
        }

        private void HandleMovementInput()
        {
            if (!FloatingCamera)
            {
                if (Input.GetButton("Sprint"))
                {
                    playerCurrentSpeed = PlayerSpeed * 2;
                }
                else
                {
                    playerCurrentSpeed = PlayerSpeed;
                }

                // Input management
                horizontalMovement = Input.GetAxisRaw("Horizontal");
                verticalMovement = Input.GetAxisRaw("Vertical");
            }
        }

        private void MovePlayer()
        {
            if (horizontalMovement != 0)
            {
                playerTransform.transform.position = new Vector3(
                    playerTransform.transform.position.x + horizontalMovement * playerCurrentSpeed * Time.deltaTime,
                    playerTransform.transform.position.y,
                    playerTransform.transform.position.z);
            }

            if (verticalMovement != 0)
            {
                playerTransform.transform.position = new Vector3(
                    playerTransform.transform.position.x,
                    playerTransform.transform.position.y + verticalMovement * playerCurrentSpeed * Time.deltaTime,
                    playerTransform.transform.position.z);
            }

            Camera.main.transform.position = new Vector3(playerTransform.position.x, playerTransform.position.y, -10);
        }

        private void HandleFloatingCamera()
        {
            if (FloatingCamera)
            {
                if (Input.GetMouseButtonDown(1))
                {
                    if (!dragging)
                    {
                        dragging = true;
                        dragStart = PlayerCamera.ScreenToWorldPoint(Input.mousePosition);
                    }
                }
                else if (Input.GetMouseButtonUp(1))
                {
                    if (dragging)
                    {
                        dragging = false;
                        dragStart = Vector3.zero;
                    }
                }
                else if (Input.GetMouseButton(1))
                {
                    if (dragging)
                    {
                        dragDiff = (PlayerCamera.ScreenToWorldPoint(Input.mousePosition)) - PlayerCamera.transform.position;
                        PlayerCamera.transform.position = dragStart - dragDiff;
                    }
                }
            }
        }

        private void HandleCameraZoom()
        {
            // Handle input
            var scrollDelta = Input.mouseScrollDelta.y;
            if (scrollDelta != 0)
            {
                zooming = true;
                targetZoom += scrollDelta < 0 ? ZoomAmount : -ZoomAmount;

                if (targetZoom > MaxZoom)
                    targetZoom = MaxZoom;
                else if (targetZoom < MinZoom)
                    targetZoom = MinZoom;
            }

            // Handle zoom execution
            if (zooming)
            {
                if (!Helpers.EqualsEpsilon(PlayerCamera.orthographicSize, targetZoom, 0.005f))
                {
                    PlayerCamera.orthographicSize = Mathf.Lerp(PlayerCamera.orthographicSize, targetZoom, Time.deltaTime * ZoomSpeed);
                }
                else
                {
                    zooming = false;
                }
            }
        }
    }
}
