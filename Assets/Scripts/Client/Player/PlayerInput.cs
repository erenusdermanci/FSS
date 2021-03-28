using DebugTools;
using UnityEngine;
using UnityEngine.Serialization;
using Utils;

namespace Client.Player
{
    public class PlayerInput : MonoBehaviour
    {
        [FormerlySerializedAs("PlayerSpeed")] public float playerSpeed;

        [FormerlySerializedAs("PlayerCamera")] public Camera playerCamera;
        [FormerlySerializedAs("FloatingCamera")] public bool floatingCamera;

        [FormerlySerializedAs("MinZoom")] public float minZoom;
        [FormerlySerializedAs("MaxZoom")] public float maxZoom;
        [FormerlySerializedAs("ZoomAmount")] public float zoomAmount;
        [FormerlySerializedAs("ZoomSpeed")] public float zoomSpeed;

        public Animator animator;

        private float _playerCurrentSpeed;
        private SpriteRenderer _playerSprite;
        private Transform _playerTransform;
        private Vector3 _movement;

        private Vector3 _dragStart;
        private Vector3 _dragDiff;
        private bool _dragging;

        private float _targetZoom;
        private bool _zooming;
        private static readonly int Move = Animator.StringToHash("Move");
        private static readonly int Run = Animator.StringToHash("Run");

        // Start is called before the first frame update
        private void Start()
        {
            _playerSprite = GetComponent<SpriteRenderer>();
            _playerTransform = GetComponent<Transform>();
            _playerCurrentSpeed = playerSpeed;

            _dragStart = Vector3.zero;
            _dragDiff = Vector3.zero;
            _dragging = false;

            _zooming = false;
            _targetZoom = playerCamera.orthographicSize;
        }

        // Update is called once per frame
        private void Update()
        {
            GlobalDebugConfig.StaticGlobalConfig.disableDrawingTool = !floatingCamera;
            if (Input.GetMouseButtonDown(2))
            {
                floatingCamera = !floatingCamera;
                _dragging = false;
            }
            HandleMovementInput();
        }

        private void FixedUpdate()
        {
            if (!floatingCamera)
                MovePlayer();
        }

        private void LateUpdate()
        {
            HandleFloatingCamera();
            HandleCameraZoom();
        }

        private void HandleMovementInput()
        {
            if (!floatingCamera)
            {
                var sprint = Input.GetButton("Sprint");
                var horizontal = Input.GetAxisRaw("Horizontal");
                animator.SetBool(Move, horizontal != 0.0f);
                animator.SetBool(Run, sprint);
                _playerCurrentSpeed = sprint ? playerSpeed * 2 : playerSpeed;
                _movement = new Vector3(horizontal,  0.0f, 0.0f);

                if (horizontal != 0.0f)
                {
                    _playerSprite.flipX = horizontal < 0.0f;
                }
            }
        }

        private void MovePlayer()
        {
            var playerTransform = _playerTransform.transform;
            playerTransform.position += _movement * (_playerCurrentSpeed * Time.deltaTime);

            Camera.main.transform.position = new Vector3(_playerTransform.position.x, _playerTransform.position.y, -10);
        }

        private void HandleFloatingCamera()
        {
            if (floatingCamera)
            {
                if (Input.GetMouseButtonDown(1))
                {
                    if (!_dragging)
                    {
                        _dragging = true;
                        _dragStart = playerCamera.ScreenToWorldPoint(Input.mousePosition);
                    }
                }
                else if (Input.GetMouseButtonUp(1))
                {
                    if (_dragging)
                    {
                        _dragging = false;
                        _dragStart = Vector3.zero;
                    }
                }
                else if (Input.GetMouseButton(1))
                {
                    if (_dragging)
                    {
                        _dragDiff = (playerCamera.ScreenToWorldPoint(Input.mousePosition)) - playerCamera.transform.position;
                        playerCamera.transform.position = _dragStart - _dragDiff;
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
                _zooming = true;
                _targetZoom += scrollDelta < 0 ? zoomAmount : -zoomAmount;

                if (_targetZoom > maxZoom)
                    _targetZoom = maxZoom;
                else if (_targetZoom < minZoom)
                    _targetZoom = minZoom;
            }

            // Handle zoom execution
            if (_zooming)
            {
                if (!Helpers.EqualsEpsilon(playerCamera.orthographicSize, _targetZoom, 0.005f))
                {
                    playerCamera.orthographicSize = Mathf.Lerp(playerCamera.orthographicSize, _targetZoom, Time.deltaTime * zoomSpeed);
                }
                else
                {
                    _zooming = false;
                }
            }
        }
    }
}
