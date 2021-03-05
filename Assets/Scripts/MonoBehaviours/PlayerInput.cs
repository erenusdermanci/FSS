using UnityEngine;

namespace Assets.Scripts.MonoBehaviours
{
    public class PlayerInput : MonoBehaviour
    {
        public float PlayerSpeed;
        public Camera PlayerCamera;
        public float MinZoom;
        public float MaxZoom;
        public float ZoomAmount;
        public float ZoomSpeed;

        private float playerCurrentSpeed;
        private Transform playerTransform;
        private float targetZoom;
        private bool zooming;

        // Start is called before the first frame update
        private void Start()
        {
            playerTransform = GetComponent<Transform>();
            playerCurrentSpeed = PlayerSpeed;

            zooming = false;
        }

        // Update is called once per frame
        private void Update()
        {
            HandleMovement();
            HandleCameraZoom();
        }

        private void HandleMovement()
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
            var horizontalMov = Input.GetAxisRaw("Horizontal");
            if (horizontalMov != 0)
            {
                playerTransform.transform.position = new Vector3(
                    playerTransform.transform.position.x + horizontalMov * playerCurrentSpeed * Time.deltaTime,
                    playerTransform.transform.position.y,
                    playerTransform.transform.position.z);
            }

            var verticalMov = Input.GetAxisRaw("Vertical");
            if (verticalMov != 0)
            {
                playerTransform.transform.position = new Vector3(
                playerTransform.transform.position.x,
                playerTransform.transform.position.y + verticalMov * playerCurrentSpeed * Time.deltaTime,
                playerTransform.transform.position.z);
            }

            Camera.main.transform.position = new Vector3(playerTransform.position.x, playerTransform.position.y, -10);
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
                if (!Helpers.EqualsEpsilon(PlayerCamera.orthographicSize, targetZoom))
                {
                    PlayerCamera.orthographicSize = Mathf.Lerp(PlayerCamera.orthographicSize, targetZoom, Time.deltaTime * ZoomSpeed);
                    if (Helpers.EqualsEpsilon(PlayerCamera.orthographicSize, targetZoom))
                    {
                        PlayerCamera.orthographicSize = targetZoom;
                    }
                }
                else
                {
                    zooming = false;
                }
            }
        }
    }
}