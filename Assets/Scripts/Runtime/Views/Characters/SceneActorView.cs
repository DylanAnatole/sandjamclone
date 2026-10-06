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
        public float DisplayedFill { get; private set; } = 1;
        public Vector3 AimPoint { get { return Visual ? Visual.TransformPoint(new Vector3(0,.81f,0)) : transform.position + Vector3.up * .70f; } }
        Vector3 from, destination, baseScale;
        Quaternion baseRotation;
        float travel, duration, exitDelay;

        public void Bind(Shooter shooter, Vector3 position)
        {
            Shooter = shooter;
            gameObject.SetActive(true);
            if (baseScale == Vector3.zero) { baseScale = Visual.localScale;baseRotation=Visual.localRotation; }
            Visual.localScale = baseScale;
            Visual.localRotation = baseRotation;
            transform.position = destination = from = position;
            Departing = false; travel = duration = 0;
            exitDelay=0; DisplayedFill=1; AmmoLabel.gameObject.SetActive(true);
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
            float side=transform.position.x<0?-1:1;
            MoveTo(new Vector3(side*6,transform.position.y+.12f,transform.position.z),1.25f);
            Departing = true;
            exitDelay=.24f;
            if(Motion)Motion.SetWalking(false);
            AmmoLabel.gameObject.SetActive(false);
            ClickCollider.enabled = false;
        }

        public void Advance(float delta, bool firing)
        {
            if (!gameObject.activeSelf || Shooter == null) return;
            float targetFill=(float)Shooter.Ammo/Mathf.Max(1,Shooter.InitialAmmo);
            DisplayedFill=Mathf.Lerp(DisplayedFill,targetFill,1-Mathf.Exp(-18*delta));
            if(Mathf.Abs(DisplayedFill-targetFill)<.003f)DisplayedFill=targetFill;
            if(Departing && exitDelay>0)
            {
                exitDelay=Mathf.Max(0,exitDelay-delta);
                if(exitDelay>0)return;
                if(Motion)Motion.SetWalking(true);
            }
            if (!AtRest)
            {
                travel = Mathf.Min(duration, travel + delta);
                float t = duration > 0 ? travel / duration : 1;
                float ease = t*t*t*(t*(t*6-15)+10);
                transform.position = Vector3.Lerp(from, destination, ease) + Vector3.up * Mathf.Sin(t * Mathf.PI) * (Departing?.035f:.075f);
                if(!Motion)Visual.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * Mathf.PI * 4) * 7);
                if(Motion)Motion.SetWalking(!AtRest);
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

