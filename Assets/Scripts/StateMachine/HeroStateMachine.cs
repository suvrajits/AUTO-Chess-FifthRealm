using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class HeroStateMachine : NetworkBehaviour
{
    private HeroUnit hero;
    private HeroAnimatorHandler animHandler;

    [SerializeField] private float corpseSinkY = 0.5f;
    [SerializeField] private float vanishDelay = 2f;

    private void Awake()
    {
        hero = GetComponent<HeroUnit>();
        animHandler = GetComponent<HeroAnimatorHandler>();
    }

    public void Die()
    {
        Debug.Log("hero died from hero state machine");
        
        
        animHandler.SetRunning(false);
        animHandler.TriggerDeath();

        StopAllCoroutines();

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.constraints = RigidbodyConstraints.None;
            rb.useGravity = true;
            rb.isKinematic = false;
        }

        StartCoroutine(FreezeCorpseAfterFall());
    }


    private IEnumerator FreezeCorpseAfterFall()
    {
        yield return new WaitForSeconds(1f);
        transform.position -= new Vector3(0, corpseSinkY, 0);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.constraints = RigidbodyConstraints.FreezeAll;
            rb.isKinematic = true;
        }

        Collider col = GetComponent<Collider>();
        if (col) col.enabled = false;

        yield return new WaitForSeconds(vanishDelay);

        if (IsServer)
        {
            if (GridManager.Instance.TryGetTile(NetworkObject.OwnerClientId, hero.GridPosition, out var tile))
            {
                tile.RemoveUnit();
            }

            BattleManager.Instance.UnregisterUnit(hero);

            var netObj = GetComponent<NetworkObject>();
            if (netObj && netObj.IsSpawned)
                netObj.Despawn();
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }
}
