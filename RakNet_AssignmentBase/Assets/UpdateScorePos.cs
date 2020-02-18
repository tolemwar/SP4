using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateScorePos : MonoBehaviour
{
    public GameObject ship;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (ship == null)
        {
            Destroy(gameObject);
        }
        Vector3 newPos = new Vector3(ship.transform.position.x, ship.transform.position.y - 0.6f, ship.transform.position.z);
        transform.position = newPos;
    }
}
