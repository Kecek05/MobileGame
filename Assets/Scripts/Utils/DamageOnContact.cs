using Unity.Netcode;
using UnityEngine;

public class DamageOnContact : MonoBehaviour
{
    [SerializeField] private float damage;
    public float Damage => damage;
    private void OnCollisionEnter(Collision collision)
    {

        if (collision.rigidbody != null)
        {
            if(collision.rigidbody.TryGetComponent(out IDamageable damageableObject))
            {
                if(NetworkManager.Singleton.IsServer)
                {
                    damageableObject.TakeDamage(damage);
                    Debug.Log("Dealt " + damage + " damage to " + collision.gameObject.name);
                }
                Destroy(gameObject);
            }
        }
    }

}
