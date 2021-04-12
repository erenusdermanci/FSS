using Entities;
using UnityEngine;

namespace Client.Player
{
    public class PlayerInput : Collidable
    {
        public float playerSpeed;
        public LayerMask groundLayer;
        public float jumpVelocity;

        private Animator _animator;
        private Rigidbody2D _rigidbody;
        private CapsuleCollider2D _capsuleCollider;
        private float _playerCurrentSpeed;
        private SpriteRenderer _playerSprite;
        private Transform _playerTransform;
        private Vector3 _movement;
        private bool _grounded;

        private static readonly int Landing = Animator.StringToHash("Landing");
        private static readonly int Falling = Animator.StringToHash("Falling");
        private static readonly int Move = Animator.StringToHash("Move");
        private static readonly int Run = Animator.StringToHash("Run");
        private static readonly int Jump = Animator.StringToHash("Jump");

        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            _animator = GetComponent<Animator>();
            _playerSprite = GetComponent<SpriteRenderer>();
            _playerTransform = GetComponent<Transform>();
            _rigidbody = GetComponent<Rigidbody2D>();
            _capsuleCollider = GetComponent<CapsuleCollider2D>();
            _playerCurrentSpeed = playerSpeed;
        }

        private void Update()
        {
            HandleMovementInput();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            _playerTransform.transform.position += _movement * (_playerCurrentSpeed * Time.deltaTime);
        }

        private void HandleMovementInput()
        {
            var sprint = Input.GetButton("Sprint");
            var jump = Input.GetButtonDown("Jump");
            var horizontal = Input.GetAxisRaw("Horizontal");
            _animator.SetBool(Move, horizontal != 0.0f);
            _animator.SetBool(Run, sprint);
            _playerCurrentSpeed = sprint ? playerSpeed * 2 : playerSpeed;

            _animator.SetBool(Falling, IsFalling());
            if (IsGrounded())
            {
                if (jump)
                {
                    _animator.SetBool(Jump, true);
                    _animator.SetLayerWeight(1, 1.0f);
                    _rigidbody.velocity = Vector2.up * jumpVelocity;
                }
                else if (!IsJumping())
                {
                    _animator.SetLayerWeight(1, 0.0f);
                }

                if (!_grounded)
                {
                    _animator.SetLayerWeight(1, 0.0f);
                    _animator.SetBool(Landing, false);
                }

                _grounded = true;
            }
            else
            {
                if (IsFalling())
                {
                    _animator.SetBool(Jump, false);
                    _animator.SetLayerWeight(1, 1.0f);

                    if (IsCloseToGround())
                    {
                        _animator.SetBool(Landing, true);
                    }
                }
                else
                    _animator.SetLayerWeight(1, 0.0f);

                _grounded = false;
            }

            _movement = new Vector3(horizontal, 0.0f, 0.0f);

            if (horizontal != 0.0f)
            {
                _playerSprite.flipX = horizontal < 0.0f;
            }
        }

        private bool IsGrounded()
        {
            var bounds = _capsuleCollider.bounds;
            var hit = Physics2D.BoxCast(bounds.center, new Vector2(bounds.size.x * 0.99f, bounds.size.y), 0.0f,
                Vector2.down, 0.05f, groundLayer);
            return hit.collider != null;
        }

        private bool IsCloseToGround()
        {
            var bounds = _capsuleCollider.bounds;
            var hit = Physics2D.BoxCast(bounds.center, bounds.size, 0.0f, Vector2.down, 0.5f, groundLayer);
            return hit.collider != null;
        }

        private bool IsJumping()
        {
            return _rigidbody.velocity.y > 0.0f;
        }

        private bool IsFalling()
        {
            return _rigidbody.velocity.y < 0.0f;
        }
    }
}