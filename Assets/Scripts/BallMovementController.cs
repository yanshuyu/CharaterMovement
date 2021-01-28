using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallMovementController : MonoBehaviour
{
    Vector3 vecl = new Vector3();

    [SerializeField, Range(0, 100)]
    float maxSpeed = 10;

    [SerializeField, Range(0, 100)]
    float maxAccel = 1;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 diserVecl = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")) * maxSpeed;
        float maxVeclChange = maxAccel * Time.deltaTime;

        // if (vecl.x < diserVecl.x) {
        //     vecl.x = Mathf.Min(vecl.x + maxVeclChange, diserVecl.x);
        // } else if (vecl.x > diserVecl.x) {
        //     vecl.x = Mathf.Max(vecl.x - maxVeclChange, diserVecl.x);
        // }
        // if (vecl.z < diserVecl.z) {
        //     vecl.z = Mathf.Min(vecl.z + maxVeclChange, diserVecl.z);
        // } else if (vecl.z > diserVecl.z) {
        //     vecl.z = Mathf.Max(vecl.z - maxVeclChange, diserVecl.z);
        // }
        vecl.x = Mathf.MoveTowards(vecl.x, diserVecl.x, maxVeclChange);
        vecl.z = Mathf.MoveTowards(vecl.z, diserVecl.z, maxVeclChange);

        transform.localPosition += vecl * Time.deltaTime;
        
    }
}
