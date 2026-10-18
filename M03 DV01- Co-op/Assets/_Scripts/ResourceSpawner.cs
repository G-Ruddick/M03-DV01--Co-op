using UnityEngine;
using Unity.Netcode;

public class ResourceSpawner : NetworkBehaviour {
    [SerializeField] private NetworkObject woodPrefab, stonePrefab;

    public void SpawnResource(ObjectType type, Vector3 position) {
        if (IsServer == false) {
            return;
        }

        NetworkObject resource = type == ObjectType.Wood ? woodPrefab : stonePrefab;

        GameObject instance = Instantiate(
            resource.gameObject, 
            position,
            Quaternion.Euler(0, Random.Range(0, 360), 0)    
        );

        instance.GetComponent<NetworkObject>().Spawn();
    }
}
