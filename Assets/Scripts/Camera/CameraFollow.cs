using UnityEngine;

namespace SnowRunner
{
    /// <summary>Smooth third-person chase camera behind the player, looking slightly downward.</summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform Target;
        public Vector3 Offset = new Vector3(0f, 4.2f, -7.5f);
        public float LookAhead = 6f;
        public float FollowSpeed = 6f;
        public float LateralFactor = 0.6f;   // camera follows lanes only partially for smoothness

        Vector3 vel;

        public void SnapToTarget()
        {
            if (Target == null) return;
            transform.position = Desired();
            transform.LookAt(LookPoint());
        }

        Vector3 Desired()
        {
            return new Vector3(Target.position.x * LateralFactor + Offset.x,
                               Offset.y + Target.position.y * 0.35f,
                               Target.position.z + Offset.z);
        }

        Vector3 LookPoint()
        {
            return new Vector3(Target.position.x * LateralFactor, 1.2f, Target.position.z + LookAhead);
        }

        void LateUpdate()
        {
            if (Target == null) return;
            transform.position = Vector3.SmoothDamp(transform.position, Desired(), ref vel, 1f / FollowSpeed);
            var rot = Quaternion.LookRotation(LookPoint() - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, 8f * Time.deltaTime);
        }
    }
}
