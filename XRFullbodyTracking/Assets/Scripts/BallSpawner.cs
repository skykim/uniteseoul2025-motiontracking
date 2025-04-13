using System.Collections;
using UnityEngine;

public class BallSpawner : MonoBehaviour
{
    public GameObject fireballPrefab;
    public GameObject iceballPrefab;
    public Transform startPosition;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(SpawnBall());
    }

    IEnumerator SpawnBall()
    {
        //every two second spawn a fireball
        while (true)
        {
            yield return new WaitForSeconds(2f);

            //get random number between 0 and 1
            float randomValue = Random.Range(0f, 1f);
            if (randomValue < 0.5f)
            {
                // Spawn a fireball
                SpawnFireball();
            }
            else
            {
                // Spawn an iceball
                SpawnIceball();
            }
        }
    }

    void SpawnFireball()
    {
        float randomY = Random.Range(-0.5f, 0.8f);
        float randomX = Random.Range(-1f, 1f);
        Vector3 randomPosition = new Vector3(startPosition.position.x + randomX, startPosition.position.y + randomY, startPosition.position.z);

        GameObject fireball = Instantiate(fireballPrefab, randomPosition, startPosition.rotation);
        Destroy(fireball, 5f); // Destroy the fireball after 5 seconds
    }

    void SpawnIceball()
    {
        float randomY = Random.Range(-0.5f, 0.8f);
        float randomX = Random.Range(-1f, 1f);
        Vector3 randomPosition = new Vector3(startPosition.position.x + randomX, startPosition.position.y + randomY, startPosition.position.z);

        GameObject iceball = Instantiate(iceballPrefab, randomPosition, startPosition.rotation);
        Destroy(iceball, 5f); // Destroy the iceball after 5 seconds
    }
}
