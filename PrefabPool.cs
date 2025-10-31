using UnityEngine;
using Mirror;
using System;
public class PrefabPool : MonoBehaviour
{
        // singleton for easier access from other scripts
    public static PrefabPool instance;

    [Header("Settings")]
    public GameObject prefab;

    public GameObject yolkTrap;

    public GameObject bearTrap;

    public GameObject landMine;

    [Header("Debug")]
    public int currentCount;
    //public Pool<GameObject> pool;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(this);

        } else
        {
            instance = this;
            //var prefabAssetId = prefab.GetComponent <NetworkIdentity>().assetId;
            //NetworkClient.RegisterSpawnHandler(prefabAssetId, SpawnPrefab, UnSpawnPrefab);
            //NetworkClient.RegisterPrefab(yolkTrap, SpawnYolkTrap, UnSpawnYolkTrap);
            //NetworkClient.RegisterPrefab(bearTrap, SpawnBearTrap, UnSpawnBearTrap);
        }
    }

    void Start()
    {
        //InitializePool();

        //NetworkClient.RegisterPrefab(prefab, SpawnHandler, UnspawnHandler);
        
        
    }

    public GameObject SpawnPrefab(SpawnMessage msg)
    {
        return Instantiate(prefab, msg.position, msg.rotation);
    }

    public void UnSpawnPrefab(GameObject spawned)
    {
        Destroy(spawned);
    }


    public GameObject SpawnBearTrap(SpawnMessage msg)
    {
        return Instantiate(bearTrap, msg.position, msg.rotation);
    }

    public void UnSpawnBearTrap(GameObject spawned)
    {
        Destroy(spawned);
    }
    public GameObject SpawnYolkTrap(SpawnMessage msg)
    {
        return Instantiate(yolkTrap, msg.position, msg.rotation);
    }

    public void UnSpawnYolkTrap(GameObject spawned)
    {
        Destroy(spawned);
    }

    public void Return(GameObject spawned)
    {
        Destroy(spawned);
    }

   /* // used by NetworkClient.RegisterPrefab
    GameObject SpawnHandler(SpawnMessage msg) => Get(msg.position, msg.rotation);

    // used by NetworkClient.RegisterPrefab
    void UnspawnHandler(GameObject spawned) => Return(spawned);

    void OnDestroy()
    {
        NetworkClient.UnregisterPrefab(prefab);
    }

    void InitializePool()
    {
        // create pool with generator function
        pool = new Pool<GameObject>(CreateNew, 25);
    }

    GameObject CreateNew()
    {
        // use this object as parent so that objects dont crowd hierarchy
        GameObject next = Instantiate(prefab, transform);
        next.name = $"{prefab.name}_pooled_{currentCount}";
        next.SetActive(false);
        currentCount++;
        return next;
    }

    // Used to take Object from Pool.
    // Should be used on server to get the next Object
    // Used on client by NetworkClient to spawn objects
    public GameObject Get(Vector3 position, Quaternion rotation)
    {
        GameObject next = pool.Get();

        // set position/rotation and set active
        next.transform.position = position;
        next.transform.rotation = rotation;
        next.SetActive(true);
        return next;
    }

    // Used to put object back into pool so they can b
    // Should be used on server after unspawning an object
    // Used on client by NetworkClient to unspawn objects
    public void Return(GameObject spawned)
    {
        // disable object
        spawned.SetActive(false);

        // add back to pool
        pool.Return(spawned);
    }*/
}

