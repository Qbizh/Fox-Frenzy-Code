using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Mirror;
public class AirstrikeBehavior : EggBehavior
{
    // Start is called before the first frame update
    bool broken = false;
    [Server]
    public override void Break(PlayerController hitPlayer)
    {
        
       if (!broken)
        {
            GetComponent<SpriteRenderer>().sprite = null;
            GetComponent<Animator>().enabled = false;
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(true);
            }
            StartCoroutine(SpawnBombs());
            broken = true;
            GetComponent<BoxCollider2D>().isTrigger = true;
            GetComponent<Rigidbody2D>().velocity = new Vector2(0, 0);
        }
        
    }

    IEnumerator SpawnBombs()
    {
        GameObject cameraBounds = GameObject.FindGameObjectsWithTag("CameraBounds")[0];
        float boundsHeight = cameraBounds.GetComponent<SpriteRenderer>().size.y * Mathf.Abs(cameraBounds.transform.localScale.y);

        float spawnHeight = boundsHeight / 2 + cameraBounds.transform.position.y;

        float boundsWidth = cameraBounds.GetComponent<SpriteRenderer>().size.x * Mathf.Abs(cameraBounds.transform.localScale.x);

        float xSpawnStart = cameraBounds.transform.position.x - boundsWidth / 2;

        float incremental = boundsWidth / 31;
        FMODUnity.RuntimeManager.PlayOneShot("event:/AirStrikeAlarm");
        GetComponent<BaseEgg>().PlayAirstirke();

        GetComponent<Rigidbody2D>().gravityScale = 0;

        while (transform.position.y < spawnHeight)
        {
            transform.position = transform.position + Vector3.up * 0.7f;
            yield return new WaitForSeconds(0.04f);
        }



        yield return new WaitForSeconds(2f);
        for (int i = 1; i < 31; i++)
        {
            GameObject projectile = Instantiate(PrefabPool.instance.prefab, new Vector3(xSpawnStart + incremental * i, spawnHeight), new Quaternion(0, 0, 0, 0));
            NetworkServer.Spawn(projectile);
            projectile.GetComponent<BaseEgg>().ServerInitialize(EggType.Bomb, false);
            projectile.GetComponent<BaseEgg>().state = BaseEgg.EggState.Thrown;
            yield return new WaitForSeconds(0.05f);

        }
        
        
        NetworkServer.UnSpawn(gameObject);
        PrefabPool.instance.Return(gameObject);
    }

    public override void ClientDestroy()
    {
        GetComponent<SpriteRenderer>().sprite = null;
        GetComponent<Animator>().enabled = false;
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(true);
        }
        
    }
}
