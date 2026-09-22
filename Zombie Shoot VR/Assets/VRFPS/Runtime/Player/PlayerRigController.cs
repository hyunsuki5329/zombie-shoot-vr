using UnityEngine;
using UnityEngine.InputSystem;

namespace VRFPS.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerRigController : MonoBehaviour
    {
        [SerializeField] Transform head;
        [SerializeField] InputActionReference moveAction;
        [SerializeField] InputActionReference turnAction;
        [SerializeField, Range(1f, 2f)] float moveSpeed = 1.6f;
        [SerializeField, Range(30f, 90f)] float turnSpeed = 60f;
        CharacterController capsule;
        bool locomotionEnabled = true;
        float verticalSpeed;

        public Transform Head { get => head; set => head = value; }

        void Awake() => capsule = GetComponent<CharacterController>();

        // InputActionManager owns action lifetime; tracking actions remain enabled during pauses.
        void Update() => Tick(moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero,
            turnAction != null ? turnAction.action.ReadValue<Vector2>() : Vector2.zero, Time.deltaTime);

        public static Vector3 GetPlanarMove(Vector3 headForward, Vector2 input)
        {
            var forward = Vector3.ProjectOnPlane(headForward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f) return Vector3.zero;
            forward.Normalize();
            return Vector3.ClampMagnitude(forward * input.y + Vector3.Cross(Vector3.up, forward) * input.x, 1f);
        }

        public void SetLocomotionEnabled(bool value)
        {
            locomotionEnabled = value;
            if (!value) verticalSpeed = 0;
        }

        public void Tick(Vector2 move, Vector2 turn, float deltaTime)
        {
            if (head == null) return;
            if (capsule == null) capsule = GetComponent<CharacterController>();
            var headLocal = transform.InverseTransformPoint(head.position);
            capsule.height = Mathf.Clamp(headLocal.y, capsule.radius * 2, 2.5f);
            capsule.center = new Vector3(headLocal.x, capsule.height * 0.5f, headLocal.z);
            if (!locomotionEnabled || deltaTime <= 0 || !capsule.enabled) return;

            // Avoid a large catch-up jump after a stalled frame. Never alter the tracked local pose.
            deltaTime = Mathf.Min(deltaTime, 0.1f);
            var pivot = new Vector3(head.position.x, transform.position.y, head.position.z);
            transform.RotateAround(pivot, Vector3.up, Mathf.Clamp(turn.x, -1, 1) * turnSpeed * deltaTime);
            if (capsule.isGrounded && verticalSpeed < 0) verticalSpeed = -1f;
            verticalSpeed = Mathf.Max(verticalSpeed + Physics.gravity.y * deltaTime, -20f);
            var velocity = GetPlanarMove(head.forward, move) * moveSpeed + Vector3.up * verticalSpeed;
            var collisions = capsule.Move(velocity * deltaTime);
            if ((collisions & CollisionFlags.Below) != 0) verticalSpeed = -1f;
            if ((collisions & CollisionFlags.Above) != 0 && verticalSpeed > 0) verticalSpeed = 0;
        }
    }
}
