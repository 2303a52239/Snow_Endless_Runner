using UnityEngine;

namespace SnowRunner
{
    /// <summary>
    /// Three-lane runner. Moves forward automatically, handles lane switching, jump and slide.
    /// Collision is detected through trigger colliders on obstacles/pickups.
    /// </summary>
    [RequireComponent(typeof(CapsuleCollider), typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        public const float LaneWidth = 2.5f;
        const float StandHeight = 1.8f;
        const float SlideHeight = 0.8f;
        const float JumpVelocity = 9.5f;
        const float Gravity = 28f;
        const float SlideDuration = 0.7f;
        const float LaneLerpSpeed = 14f;

        public GameManager Game;

        CapsuleCollider capsule;
        PlayerAnimation anim;
        InputHandler input;

        int lane = 1;                 // 0 left, 1 center, 2 right
        float y, vy;
        float slideT;
        bool grounded = true;

        public bool IsSliding => slideT > 0f;
        public bool IsGrounded => grounded;
        public float Speed { get; set; }

        void Awake()
        {
            capsule = GetComponent<CapsuleCollider>();
            var rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            capsule.direction = 1;
            capsule.radius = 0.4f;
            capsule.isTrigger = false;
            ApplyCapsule(StandHeight);
            anim = GetComponent<PlayerAnimation>();
            input = new InputHandler();
        }

        public void ResetPlayer()
        {
            lane = 1;
            y = 0f; vy = 0f; slideT = 0f; grounded = true;
            transform.position = Vector3.zero;
            ApplyCapsule(StandHeight);
            if (anim != null) anim.SetState(PlayerAnimation.State.Run);
        }

        /// <summary>Used after revive: keeps distance travelled, resets pose/lane at the same z.</summary>
        public void ResetPlayerAt(float z)
        {
            lane = 1;
            y = 0f; vy = 0f; slideT = 0f; grounded = true;
            transform.position = new Vector3(0f, 0f, z);
            ApplyCapsule(StandHeight);
            if (anim != null) anim.SetState(PlayerAnimation.State.Run);
        }

        void ApplyCapsule(float h)
        {
            capsule.height = h;
            capsule.center = new Vector3(0f, h * 0.5f, 0f);
        }

        void Update()
        {
            if (Game == null || !Game.IsPlaying) return;
            float dt = Time.deltaTime;

            HandleInput(input.Poll());

            // Forward motion
            Vector3 p = transform.position;
            p.z += Speed * dt;

            // Lane lerp
            float targetX = (lane - 1) * LaneWidth;
            p.x = Mathf.Lerp(p.x, targetX, 1f - Mathf.Exp(-LaneLerpSpeed * dt));

            // Vertical
            if (!grounded)
            {
                vy -= Gravity * dt;
                y += vy * dt;
                if (y <= 0f) { y = 0f; vy = 0f; grounded = true; if (!IsSliding) anim?.SetState(PlayerAnimation.State.Run); }
            }
            p.y = y;
            transform.position = p;

            // Slide timer
            if (slideT > 0f)
            {
                slideT -= dt;
                if (slideT <= 0f)
                {
                    slideT = 0f;
                    ApplyCapsule(StandHeight);
                    if (grounded) anim?.SetState(PlayerAnimation.State.Run);
                }
            }
        }

        void HandleInput(InputCommand cmd)
        {
            switch (cmd)
            {
                case InputCommand.Left: lane = Mathf.Max(0, lane - 1); break;
                case InputCommand.Right: lane = Mathf.Min(2, lane + 1); break;
                case InputCommand.Jump:
                    if (grounded)
                    {
                        if (IsSliding) { slideT = 0f; ApplyCapsule(StandHeight); }
                        vy = JumpVelocity; grounded = false;
                        anim?.SetState(PlayerAnimation.State.Jump);
                        AudioManager.Instance.PlayJump();
                    }
                    break;
                case InputCommand.Slide:
                    if (!grounded) { vy = -JumpVelocity * 1.2f; } // fast-fall
                    if (!IsSliding)
                    {
                        slideT = SlideDuration;
                        ApplyCapsule(SlideHeight);
                        anim?.SetState(PlayerAnimation.State.Slide);
                        AudioManager.Instance.PlaySlide();
                    }
                    break;
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (Game == null || !Game.IsPlaying) return;

            var pickup = other.GetComponent<Pickup>();
            if (pickup != null) { Game.OnPickup(pickup); return; }

            var obstacle = other.GetComponent<Obstacle>();
            if (obstacle != null) Game.OnObstacleHit(obstacle);
        }
    }
}
