using UnityEngine;

namespace SnowRunner
{
    /// <summary>Procedural animation of a primitive-built character (limb swing, jump tuck, slide lean).</summary>
    public class PlayerAnimation : MonoBehaviour
    {
        public enum State { Run, Jump, Slide, Dead }

        Transform visual, armL, armR, legL, legR, head;
        State state = State.Run;
        float runPhase;

        public static GameObject BuildCharacter(Transform parent)
        {
            var root = new GameObject("Visual");
            root.transform.SetParent(parent, false);

            Mats.Prim(PrimitiveType.Capsule, root.transform, new Vector3(0f, 1.0f, 0f), new Vector3(0.65f, 0.45f, 0.5f), Mats.Coat);   // torso
            Mats.Prim(PrimitiveType.Sphere, root.transform, new Vector3(0f, 1.6f, 0f), new Vector3(0.42f, 0.42f, 0.42f), Mats.Skin).name = "Head";
            Mats.Prim(PrimitiveType.Sphere, root.transform, new Vector3(0f, 1.72f, 0f), new Vector3(0.46f, 0.3f, 0.46f), Mats.Get("Hat", new Color(0.2f, 0.45f, 0.9f))); // beanie

            // Limbs are pivoted at joints via empty parents.
            Transform Limb(string n, Vector3 pivot, Material m, Vector3 scale)
            {
                var piv = new GameObject(n).transform;
                piv.SetParent(root.transform, false);
                piv.localPosition = pivot;
                Mats.Prim(PrimitiveType.Capsule, piv, new Vector3(0f, -0.3f, 0f), scale, m);
                return piv;
            }
            Limb("ArmL", new Vector3(-0.38f, 1.3f, 0f), Mats.Coat, new Vector3(0.18f, 0.3f, 0.18f));
            Limb("ArmR", new Vector3(0.38f, 1.3f, 0f), Mats.Coat, new Vector3(0.18f, 0.3f, 0.18f));
            Limb("LegL", new Vector3(-0.15f, 0.7f, 0f), Mats.Pants, new Vector3(0.2f, 0.36f, 0.2f));
            Limb("LegR", new Vector3(0.15f, 0.7f, 0f), Mats.Pants, new Vector3(0.2f, 0.36f, 0.2f));
            return root;
        }

        void Awake()
        {
            visual = transform.Find("Visual");
            if (visual == null) return;
            armL = visual.Find("ArmL"); armR = visual.Find("ArmR");
            legL = visual.Find("LegL"); legR = visual.Find("LegR");
            head = visual.Find("Head");
        }

        public void SetState(State s) { state = s; }

        void Update()
        {
            if (visual == null) return;
            float dt = Time.deltaTime;

            switch (state)
            {
                case State.Run:
                    runPhase += dt * 12f;
                    float sw = Mathf.Sin(runPhase) * 55f;
                    Set(armL, sw); Set(armR, -sw); Set(legL, -sw); Set(legR, sw);
                    visual.localRotation = Quaternion.Slerp(visual.localRotation, Quaternion.identity, 12f * dt);
                    visual.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(runPhase)) * 0.06f, 0f);
                    break;
                case State.Jump:
                    Set(armL, -150f); Set(armR, -150f); Set(legL, 30f); Set(legR, -20f);
                    visual.localPosition = Vector3.zero;
                    break;
                case State.Slide:
                    Set(armL, -80f); Set(armR, -80f); Set(legL, -80f); Set(legR, -80f);
                    visual.localRotation = Quaternion.Slerp(visual.localRotation, Quaternion.Euler(-65f, 0f, 0f), 18f * dt);
                    visual.localPosition = new Vector3(0f, -0.35f, 0f);
                    break;
                case State.Dead:
                    visual.localRotation = Quaternion.Slerp(visual.localRotation, Quaternion.Euler(-85f, 0f, 0f), 8f * dt);
                    visual.localPosition = new Vector3(0f, -0.5f, 0f);
                    break;
            }
        }

        static void Set(Transform t, float xAngle)
        {
            if (t != null) t.localRotation = Quaternion.Euler(xAngle, 0f, 0f);
        }
    }
}
