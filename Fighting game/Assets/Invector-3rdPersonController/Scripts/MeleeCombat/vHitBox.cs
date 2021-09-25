using UnityEngine;
using System.Collections;

namespace Invector
{
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(Rigidbody))]

    public class vHitBox : MonoBehaviour
    {
        public HitBarPoints hitBarPoint;
        [HideInInspector]
        public vMeleeWeapon hitControl;
        private BoxCollider box;
        private Rigidbody rgd;

        void Start()
        {
            rgd = GetComponent<Rigidbody>();
            box = GetComponent<BoxCollider>();
            rgd.isKinematic = true;
            rgd.useGravity = false;
            rgd.constraints = RigidbodyConstraints.FreezeAll;
            box.isTrigger = true;
            this.gameObject.SetActive(false);
        }

        void OnTriggerEnter(Collider other)
        {            
            if (hitControl != null && hitControl.isActive)
            {
                CheckHitProperties(other);
            }
            else this.gameObject.SetActive(false);
        }

        //void OnTriggerStay(Collider other)
        //{
        //    if (hitControl != null && hitControl.isActive)
        //    {
        //        CheckHitProperties(other);
        //    }
        //    else this.enabled = false;
        //}

        protected void CheckHitProperties(Collider other)
        {
            var inDamage = false;
            var inRecoil = false;

            if (hitControl.hitProperties.hitRecoilLayer == (hitControl.hitProperties.hitRecoilLayer | (1 << other.gameObject.layer)))
                inRecoil = true;

            if (hitControl.hitProperties.hitDamageTags == null || hitControl.hitProperties.hitDamageTags.Count == 0)
                inDamage = true;
            else if (hitControl.hitProperties.hitDamageTags.Contains(other.tag))
                inDamage = true;

            if (inDamage == true)
            {
                SendMessageUpwards("OnDamageHit", new HitInfo(other, transform.position, hitBarPoint), SendMessageOptions.DontRequireReceiver);
            }

            if (inRecoil == true)
            {
                if (hitBarPoint == HitBarPoints.Bottom || hitBarPoint == HitBarPoints.Center)
                {
                    SendMessageUpwards("OnRecoilHit", new HitInfo(other, transform.position, hitBarPoint), SendMessageOptions.DontRequireReceiver);
                }
            }
        }

        [System.Serializable]
        public class HitInfo
        {
            public HitBarPoints hitBarPoint;
            public Vector3 hitPoint;
            public Collider hitCollider;
            public HitInfo(Collider hitCollider, Vector3 hitPoint, HitBarPoints hitBarPoint)
            {
                this.hitCollider = hitCollider;
                this.hitPoint = hitPoint;
                this.hitBarPoint = hitBarPoint;
            }
        }
    }
}