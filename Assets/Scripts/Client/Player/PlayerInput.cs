using DebugTools;
using UnityEngine;

namespace Client.Player
{
    public class PlayerInput : MonoBehaviour
    {
        public float playerSpeed;

        public LayerMask groundLayer;

        private Rigidbody2D _rigidbody;
        private CapsuleCollider2D _capsuleCollider;
        public float jumpVelocity;

        public Animator animator;

        private float _playerCurrentSpeed;
        private SpriteRenderer _playerSprite;
        private Transform _playerTransform;
        private Vector3 _movement;

        private static readonly int Move = Animator.StringToHash("Move");
        private static readonly int Run = Animator.StringToHash("Run");
        private static readonly int Jump = Animator.StringToHash("Jump");

        private bool _grounded;
        private static readonly int Landing = Animator.StringToHash("Landing");
        private static readonly int Falling = Animator.StringToHash("Falling");

        private void Start()
        {
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

        private void FixedUpdate()
        {
            _playerTransform.transform.position += _movement * (_playerCurrentSpeed * Time.deltaTime);
        }

        private void HandleMovementInput()
        {
            var sprint = Input.GetButton("Sprint");
            var jump = Input.GetButtonDown("Jump");
            var horizontal = Input.GetAxisRaw("Horizontal");
            animator.SetBool(Move, horizontal != 0.0f);
            animator.SetBool(Run, sprint);
            _playerCurrentSpeed = sprint ? playerSpeed * 2 : playerSpeed;

            animator.SetBool(Falling, IsFalling());
            if (IsGrounded())
            {
                if (jump)
                {
                    animator.SetBool(Jump, true);
                    animator.SetLayerWeight(1, 1.0f);
                    _rigidbody.velocity = Vector2.up * jumpVelocity;
                }
                else if (!IsJumping())
                {
                    animator.SetLayerWeight(1, 0.0f);
                }

                if (!_grounded)
                {
                    animator.SetLayerWeight(1, 0.0f);
                    animator.SetBool(Landing, false);
                }

                _grounded = true;
            }
            else
            {
                if (IsFalling())
                {
                    animator.SetBool(Jump, false);
                    animator.SetLayerWeight(1, 1.0f);

                    if (IsCloseToGround())
                    {
                        animator.SetBool(Landing, true);
                    }
                }
                else
                    animator.SetLayerWeight(1, 0.0f);

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
            var hit = Physics2D.BoxCast(bounds.center, new Vector2(bounds.size.x * 0.99f, bounds.size.y), 0.0f, Vector2.down, 0.05f, groundLayer);
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
