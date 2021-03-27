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

        private float _playerCurrentSpeed;
        private Transform _playerTransform;
        private float _horizontalMovement;
        private float _verticalMovement;

        private Vector3 _dragStart;
        private Vector3 _dragDiff;
        private bool _dragging;

        private float _targetZoom;
        private bool _zooming;

        // Start is called before the first frame update
        private void Start()
        {
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
                if (Input.GetButton("Sprint"))
                {
                    _playerCurrentSpeed = PlayerSpeed * 2;
                }
                else
                {
                    _playerCurrentSpeed = PlayerSpeed;
                }

                // Input management
                _horizontalMovement = Input.GetAxisRaw("Horizontal");
                _verticalMovement = Input.GetAxisRaw("Vertical");
            }
        }

        private void MovePlayer()
        {
            if (_horizontalMovement != 0)
            {
                _playerTransform.transform.position = new Vector3(
                    _playerTransform.transform.position.x + _horizontalMovement * _playerCurrentSpeed * Time.deltaTime,
                    _playerTransform.transform.position.y,
                    _playerTransform.transform.position.z);
            }

            if (_verticalMovement != 0)
            {
                _playerTransform.transform.position = new Vector3(
                    _playerTransform.transform.position.x,
                    _playerTransform.transform.position.y + _verticalMovement * _playerCurrentSpeed * Time.deltaTime,
                    _playerTransform.transform.position.z);
            }

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
