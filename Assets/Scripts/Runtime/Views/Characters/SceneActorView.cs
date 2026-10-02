using UnityEngine;

namespace SandJamTest.Scene3D
{
    public sealed class SceneActorView : MonoBehaviour
    {
        public int SourceLane;
        public int SourceOrder;
        public int ColorId; public int Divider = 40;
        public Transform Visual;
        public CharacterMotion Motion;
        public TextMesh AmmoLabel;
        public BoxCollider ClickCollider;
        public Shooter Shooter { get; private set; }
        public bool AtRest { get { return travel >= duration; } }
        public bool Departing { get; private set; }
        public Vector3 AimPoint { get { return transform.position + Vector3.up * .70f + Vector3.back * .18f; } }
        Vector3 from, destination, baseScale;
        Quaternion baseRotation;
        float travel, duration;

        public void Bind(Shooter shooter, Vector3 position)
        {
            Shooter = shooter;
            gameObject.SetActive(true);
            if (baseScale == Vector3.zero) { baseScale = Visual.localScale;baseRotation=Visual.localRotation; }
            Visual.localScale = baseScale;
            Visual.localRotation = baseRotation;
            transform.position = destination = from = position;
            Departing = false; travel = duration = 0;
            if(Motion)Motion.ResetPose();
            ClickCollider.enabled = true;
            AmmoLabel.text = DisplayAmount.Units(shooter.Ammo, Divider).ToString();
        }

        public void MoveTo(Vector3 position, float seconds = .32f)
        {
            if (Departing || (destination - position).sqrMagnitude < .00001f) return;
            from = transform.position; destination = position;
            if(Motion && Mathf.Approximately(seconds,.32f))seconds=.6f;
            travel = 0; duration = seconds;
            if(Motion)Motion.SetWalking(true);
        }

        public void Leave()
        {
            if (Departing || !gameObject.activeSelf) return;
            MoveTo(transform.position + new Vector3(transform.position.x < 0 ? -.5f : .5f, .1f, 0), .28f);
            Departing = true;
            ClickCollider.enabled = false;
        }

        public void Advance(float delta, bool firing)
        {
            if (!gameObject.activeSelf || Shooter == null) return;
            if (!AtRest)
            {
                travel = Mathf.Min(duration, travel + delta);
                float t = duration > 0 ? travel / duration : 1;
                float ease = t * t * (3 - 2 * t);
                transform.position = Vector3.Lerp(from, destination, ease) + Vector3.up * Mathf.Sin(t * Mathf.PI) * .12f;
                if(!Motion)Visual.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * Mathf.PI * 4) * 7);
                if(Motion)Motion.SetWalking(!AtRest);
                if (Departing) Visual.localScale = baseScale * (1 - t);
            }
            else
            {
                transform.position = destination;
                if(!Motion)Visual.localRotation = Quaternion.Euler(firing ? Mathf.Sin(Time.time * 25) * 3 : 0, 0, 0);
                if(Motion)Motion.SetWalking(false);
                if (Departing) { gameObject.SetActive(false); return; }
            }
            string ammo = DisplayAmount.Units(Shooter.Ammo, Divider).ToString();
            if (AmmoLabel.text != ammo) AmmoLabel.text = ammo;
        }
    }
}

