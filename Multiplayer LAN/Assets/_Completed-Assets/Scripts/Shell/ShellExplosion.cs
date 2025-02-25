using UnityEngine;
using Mirror;
using System.Collections;


namespace Complete
{
    public class ShellExplosion : NetworkBehaviour
    {
        public LayerMask m_TankMask;                        // Used to filter what the explosion affects, this should be set to "Players"
        public ParticleSystem m_ExplosionParticles;         // Reference to the particles that will play on explosion
        public AudioSource m_ExplosionAudio;                // Reference to the audio that will play on explosion
        public float m_MaxDamage = 100f;                    // The amount of damage done if the explosion is centred on a tank
        public float m_ExplosionForce = 1000f;              // The amount of force added to a tank at the centre of the explosion
        public float m_MaxLifeTime = 2f;                    // The time in seconds before the shell is removed
        public float m_ExplosionRadius = 5f;                // The maximum distance away from the explosion tanks can be and are still affected

        public bool IsBomb = false;                         //Indica si es o no una bomba
        public float TimeForDetonate = 4f;                  //Tiempo para detonar la bomba

        private Color bombBodyMaterialColor;
        private Color bombFuseMaterialColor;


        private void Start ()
        {
            //GetComponent<Rigidbody>().velocity = transform.forward * ShellVelocity;
            // If it isn't destroyed by then, destroy the shell after it's lifetime
            if (!IsBomb)
            {
                Destroy(gameObject, m_MaxLifeTime);
            }
            else
            {
                //Lanza el m�todo correspondiente para que la bomba se detone en el tiempo deseado
                Invoke("Explosion", TimeForDetonate);
                //ejecuta la corutina de parpadeo de la bomba
                StartCoroutine("BlinkBomb");
                //se guardan los colores originales del material
                bombBodyMaterialColor = GetComponent<MeshRenderer>().materials[0].color;
                bombFuseMaterialColor = GetComponent<MeshRenderer>().materials[1].color;
             }
        }

        private void OnTriggerEnter (Collider other)
        {
            if (!IsBomb)
                Explosion();
        }

        IEnumerator BlinkBomb()
        {
           //un segundo antes de detonar ejecuta los cambios de color
           yield return new WaitForSeconds(TimeForDetonate - 1f);
            while (gameObject.activeSelf)
            {
                GetComponent<MeshRenderer>().materials[0].color = Color.red;
                GetComponent<MeshRenderer>().materials[1].color = Color.red;
                yield return new WaitForSeconds(0.0625f);
                GetComponent<MeshRenderer>().materials[0].color = bombBodyMaterialColor;
                GetComponent<MeshRenderer>().materials[1].color = bombFuseMaterialColor;
                yield return new WaitForSeconds(0.0625f);
            }
        }



        private void Explosion()
        {
            // Collect all the colliders in a sphere from the shell's current position to a radius of the explosion radius
            Collider[] colliders = Physics.OverlapSphere(transform.position, m_ExplosionRadius, m_TankMask);

            // Go through all the colliders...
            for (int i = 0; i < colliders.Length; i++)
            {
                // ... and find their rigidbody
                Rigidbody targetRigidbody = colliders[i].GetComponent<Rigidbody>();

                // If they don't have a rigidbody, go on to the next collider
                if (!targetRigidbody)
                {
                    continue;
                }

                // Add an explosion force
                targetRigidbody.AddExplosionForce(m_ExplosionForce, transform.position, m_ExplosionRadius);

                // Find the TankHealth script associated with the rigidbody
                TankHealth targetHealth = targetRigidbody.GetComponent<TankHealth>();

                // If there is no TankHealth script attached to the gameobject, go on to the next collider
                if (!targetHealth)
                {
                    continue;
                }

                // If has a team tag (Blue/Red) and both the shell and the TankHealth have the same tag
                // then is fire friendly, so no damage.
                if (GameManager.IsTeamsGame && GameManager.IsFriendlyFire) {
                    if (tag.Equals("Blue") || tag.Equals("Red"))
                        if (targetHealth.CompareTag(tag))
                            continue;
                }

                // Calculate the amount of damage the target should take based on it's distance from the shell
                float damage = CalculateDamage(targetRigidbody.position);

                // Deal this damage to the tank
                targetHealth.TakeDamage(damage);
            }

            if (isServer)
            {
                RpcDestroyShell();
            }

        }


        private float CalculateDamage (Vector3 targetPosition)
        {
            // Create a vector from the shell to the target
            Vector3 explosionToTarget = targetPosition - transform.position;

            // Calculate the distance from the shell to the target
            float explosionDistance = explosionToTarget.magnitude;

            // Calculate the proportion of the maximum distance (the explosionRadius) the target is away
            float relativeDistance = (m_ExplosionRadius - explosionDistance) / m_ExplosionRadius;

            // Calculate damage as this proportion of the maximum possible damage
            float damage = relativeDistance * m_MaxDamage;

            // Make sure that the minimum damage is always 0
            damage = Mathf.Max (0f, damage);

            return damage;
        }

        [ClientRpc]
        public void RpcDestroyShell()
        {
            // Unparent the particles from the shell
            m_ExplosionParticles.transform.parent = null;

            // Play the particle system
            m_ExplosionParticles.Play();
            //Debug.Log("Part�cula funciona");

            // Play the explosion sound effect
            m_ExplosionAudio.Play();

            // Once the particles have finished, destroy the gameobject they are on
            ParticleSystem.MainModule mainModule = m_ExplosionParticles.main;
            Destroy(m_ExplosionParticles.gameObject, mainModule.duration);

            // Destroy the shell
            Destroy(gameObject);
        }
    }
}