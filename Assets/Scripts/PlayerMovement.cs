using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float PlayerSpeed;

    private float playerCurrentSpeed;

    private Transform playerTransform;

    // Start is called before the first frame update
    void Start()
    {
        playerTransform = GetComponent<Transform>();
        playerCurrentSpeed = PlayerSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButton("Sprint"))
        {
            playerCurrentSpeed = PlayerSpeed * 2;
        }
        else
        {
            playerCurrentSpeed = PlayerSpeed;
        }

        // Input management
        var horizontalMov = Input.GetAxisRaw("Horizontal");
        if (horizontalMov != 0)
        {
            playerTransform.transform.position = new Vector3(
                playerTransform.transform.position.x + horizontalMov * playerCurrentSpeed * Time.deltaTime,
                playerTransform.transform.position.y,
                playerTransform.transform.position.z);
        }

        var verticalMov = Input.GetAxisRaw("Vertical");
        if (verticalMov != 0)
        {
            playerTransform.transform.position = new Vector3(
            playerTransform.transform.position.x,
            playerTransform.transform.position.y + verticalMov * playerCurrentSpeed * Time.deltaTime,
            playerTransform.transform.position.z);
        }


        Camera.main.transform.position = new Vector3(playerTransform.position.x, playerTransform.position.y, -10);
    }
}
