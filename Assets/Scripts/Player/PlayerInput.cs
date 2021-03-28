using DebugTools;
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
            _playerCurrentSpeed = PlayerSpeed;

            _dragStart = Vector3.zero;
            _dragDiff = Vector3.zero;
            _dragging = false;

            _zooming = false;
            _targetZoom = PlayerCamera.orthographicSize;
        }

        // Update is called once per frame
        private void Update()
        {
            GlobalDebugConfig.StaticGlobalConfig.disableDrawingTool = !FloatingCamera;
            if (Input.GetMouseButtonDown(2))
            {
                FloatingCamera = !FloatingCamera;
                _dragging = false;
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
                var sprint = Input.GetButton("Sprint");
                var horizontal = Input.GetAxisRaw("Horizontal");
                animator.SetBool(Move, horizontal != 0.0f);
                animator.SetBool(Run, sprint);
                _playerCurrentSpeed = sprint ? PlayerSpeed * 2 : PlayerSpeed;
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
            if (FloatingCamera)
            {
                if (Input.GetMouseButtonDown(1))
                {
                    if (!_dragging)
                    {
                        _dragging = true;
                        _dragStart = PlayerCamera.ScreenToWorldPoint(Input.mousePosition);
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
                        _dragDiff = (PlayerCamera.ScreenToWorldPoint(Input.mousePosition)) - PlayerCamera.transform.position;
                        PlayerCamera.transform.position = _dragStart - _dragDiff;
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
                _targetZoom += scrollDelta < 0 ? ZoomAmount : -ZoomAmount;

                if (_targetZoom > MaxZoom)
                    _targetZoom = MaxZoom;
                else if (_targetZoom < MinZoom)
                    _targetZoom = MinZoom;
            }

            // Handle zoom execution
            if (_zooming)
            {
                if (!Helpers.EqualsEpsilon(PlayerCamera.orthographicSize, _targetZoom, 0.005f))
                {
                    PlayerCamera.orthographicSize = Mathf.Lerp(PlayerCamera.orthographicSize, _targetZoom, Time.deltaTime * ZoomSpeed);
                }
                else
                {
                    _zooming = false;
                }
            }
        }
    }
}
