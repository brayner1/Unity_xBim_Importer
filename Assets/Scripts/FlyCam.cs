using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyCam : MonoBehaviour {

	// Use this for initialization
	void Start () {
	}

    // Update is called once per frame
    float flySpeed = 5;
    bool isEnabled;
 
    bool shift;
    bool ctrl;
    float accelerationAmount = 30;
    float accelerationRatio = 0.5f;
    float slowDownRatio = 0.5f;

    float lastX = -99999;
    float lastY = -99999;

    void Update()
    {
        //use shift to speed up flight
        if (Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.RightShift))
        {
            flySpeed = (flySpeed < 15.0f)? flySpeed + accelerationRatio : flySpeed;
        }

        //use ctrl to slow up flight
        if (Input.GetKeyUp(KeyCode.LeftAlt) || Input.GetKeyUp(KeyCode.RightAlt))
        {
            flySpeed = (flySpeed > 0.5f)? flySpeed - slowDownRatio : flySpeed;
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (lastX != -99999 && lastY != -99999)
            {
                float offsetX = Input.mousePosition.x - lastX;
                float offsetY = Input.mousePosition.y - lastY;
                //transform.Rotate()
            }
            lastX = Input.mousePosition.x;
            lastY = Input.mousePosition.y;
        }
        //
        if (Input.GetAxis("Vertical") != 0)
        {
            transform.Translate(Vector3.forward * flySpeed * Input.GetAxis("Vertical"));
        }


        if (Input.GetAxis("Horizontal") != 0)
        {
            transform.Translate(Vector3.right * flySpeed * Input.GetAxis("Horizontal"));
        }


        if (Input.GetKey(KeyCode.E))
        {
            transform.Translate(Vector3.up * flySpeed);
        }
        else if (Input.GetKey(KeyCode.Q))
        {
            transform.Translate(Vector3.down * flySpeed);
        }
        //if (Input.GetKeyDown(KeyCode.F12))
        //    switchCamera();


    }
}
