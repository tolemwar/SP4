using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionCheck : MonoBehaviour
{
    public GameObject explosion;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }



    private void OnCollisionEnter2D(Collision2D collision)
    {
        //Debug.Log(gameObject.name + " Collided with "+ collision.gameObject.name);

        //if (gameObject.name == "ShipObj(Clone)")
        //{
        //    //    Debug.Log("moving");
        //    gameObject.GetComponent<ShipMovement>().pVelocity = collision.gameObject.GetComponent<ShipMovement>().pVelocity;
        //}
    }

}
