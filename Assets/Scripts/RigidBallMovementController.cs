using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RigidBallMovementController : MonoBehaviour
{
    new Rigidbody rigidbody;
    new Renderer renderer;
    Color originalColor;

    Vector3 vecl = Vector3.zero;
    Vector3 diserVecl = Vector3.zero;

    [SerializeField]
    Transform inputSpace;

    [SerializeField, Range(0, 100)]
    float maxSpeed = 15;
    [SerializeField, Range(0, 100)]
    float maxAccel = 12f;
    [SerializeField, Range(0, 10)]
    float maxAirAccel = 6f;

    [SerializeField, Range(1, 10)]
    float jumpHeight = 2;
    [SerializeField, Min(0)]
    int maxAirJump = 0;
    bool diserJump = false;
    int jumpPhase = 0;

    [SerializeField, Range(0, 90)]
    float maxGroundSlot = 10;
    float maxGroundSlotDot = 15;
    [SerializeField, Range(0, 100)]
    float maxSnapSpeed = 20;
    [SerializeField, Min(0)]
    float maxProbeDistance = 0.5f;
    [SerializeField]
    LayerMask probeMask = -1;

    int groundContactCnt = 0;
    Vector3 groundContactNormal = Vector3.zero;
    int stepsSinceLastOnGround = 0;

    int steepContactCnt = 0;
    Vector3 steepContactNormal = Vector3.zero;


    public bool IsOnGround {
       get {
            return groundContactCnt > 0;
       }
    }

    public bool IsJumping {
        get {
            return jumpPhase > 0;
        }
    }

    public bool isOnSteep {
        get {
            return steepContactCnt > 0;
        }
    }

    private void OnValidate() {
        maxGroundSlotDot = Mathf.Cos(Mathf.Deg2Rad * maxGroundSlot);
    }

    // Start is called before the first frame update
    void Start() {
        rigidbody = gameObject.GetComponent<Rigidbody>();
        renderer = gameObject.GetComponent<Renderer>();
        maxGroundSlotDot = Mathf.Cos(Mathf.Deg2Rad * maxGroundSlot);
        originalColor = renderer.material.GetColor("_Color");
    }

    // Update is called once per frame
    void Update() {
        Vector3 inputVec = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        Vector3.ClampMagnitude(inputVec, 1);
        if (inputSpace) {
            // Vector3 xAxies = inputSpace.right;
            // Vector3 zAxies = inputSpace.forward;
            // xAxies.y = 0;
            // zAxies.y = 0;
            // xAxies.Normalize();
            // zAxies.Normalize();
            // inputVec = inputVec.x * xAxies + inputVec.y * zAxies;
            inputVec = inputSpace.TransformVector(inputVec);
            float inputLen = inputVec.magnitude;
            inputVec.y = 0;
            inputVec = inputVec.normalized * inputLen;
        }
        diserVecl = inputVec * maxSpeed; 
        diserJump = Input.GetButtonDown("Jump");

        if (IsOnGround) {
            renderer.material.SetColor("_Color", originalColor);
        } else if(isOnSteep) {
            renderer.material.SetColor("_Color", Color.yellow);
        } else if (IsJumping) {
            renderer.material.SetColor("_Color", Color.gray);
        } else {
            renderer.material.SetColor("_Color", Color.red);
        }
    }

    private void FixedUpdate() {
        vecl = rigidbody.velocity; // driven by phys engine, then override by user input

        Move();

        if (IsOnGround || ResueCrevasse()) {
            stepsSinceLastOnGround = 0;
            jumpPhase = 0;
        } else {
            stepsSinceLastOnGround++;
        }
        
        if (!IsOnGround && !IsJumping && stepsSinceLastOnGround <= 1 ) { //try to snap character to ground when launch from slot
            SnapToGround();
        }

        if (diserJump) {
            Jump();
            diserJump = false;
        }

        rigidbody.velocity = vecl;
    }

    private void OnCollisionEnter(Collision other) {
        EvaluateCollision(other);
    }

    private void OnCollisionStay(Collision other) {
        if (IsJumping)
            return;
        EvaluateCollision(other);
    }

    private void OnCollisionExit(Collision other) {
        EvaluateCollision(other);
    }

    private void EvaluateCollision(Collision collision) {
        groundContactCnt = 0;
        groundContactNormal = Vector3.zero;
        steepContactCnt = 0;
        steepContactNormal = Vector3.zero;

        for (int i = 0; i < collision.contactCount; i++) {
            Vector3 contactNor = collision.GetContact(i).normal;
            if (contactNor.y >= maxGroundSlotDot) {
                groundContactNormal += contactNor;
                groundContactCnt++;
            } else {
                steepContactCnt++;
                steepContactNormal += contactNor;
            }
        }

        if (groundContactCnt > 1) {
            groundContactNormal.Normalize();
        }

        if (steepContactCnt > 1) {
            steepContactNormal.Normalize();
        }
    }

    void Move() {
        float maxVeclChange = ((IsOnGround || isOnSteep) ? maxAccel : maxAirAccel) * Time.deltaTime;
        Vector3 xAxis = Vector3.right;
        Vector3 zAxis = Vector3.forward;

        if (IsOnGround) {
            xAxis = (Vector3.right - Vector3.Dot(Vector3.right, groundContactNormal) * groundContactNormal).normalized;
            zAxis = (Vector3.forward - Vector3.Dot(Vector3.forward, groundContactNormal) * groundContactNormal).normalized;
        } else if (isOnSteep) {
            float xdotn = Vector3.Dot(Vector3.right, steepContactNormal);
            float zdotn = Vector3.Dot(Vector3.forward, steepContactNormal);
            if (xdotn != 1 && zdotn != 1) { // move axis us paralla with contact normal
                xAxis = (Vector3.right -  xdotn * steepContactNormal).normalized;
                zAxis = (Vector3.forward - zdotn * steepContactNormal).normalized;
            }
        }

        float alignXVecl = Vector3.Dot(vecl, xAxis);
        float alignZVecl = Vector3.Dot(vecl, zAxis);
        float newAlignXVecl = Mathf.MoveTowards(alignXVecl, diserVecl.x, maxVeclChange);
        float newAlignZVecl = Mathf.MoveTowards(alignZVecl, diserVecl.z, maxVeclChange);
        vecl += ((newAlignXVecl - alignXVecl) * xAxis + (newAlignZVecl - alignZVecl) * zAxis);
        //rigidbody.velocity = vecl;
    }

    void Jump() {
        if (jumpPhase > maxAirJump)
            return;

        Vector3 jumpDir = Vector3.zero;
        if (IsOnGround) {
            jumpDir = groundContactNormal;
            groundContactCnt = 0;
            groundContactNormal = Vector3.zero;

        } else if (isOnSteep) {
            jumpDir = steepContactNormal;
            steepContactCnt = 0;
            steepContactNormal = Vector3.zero;

        } else if (jumpPhase > 0) {
            jumpDir = Vector3.up;
        }

        if (jumpDir == Vector3.zero)
            return;
        
        jumpDir = (jumpDir + Vector3.up).normalized; // add jump bias

        float desirJumpSpeed = Mathf.Sqrt(-2 * Physics.gravity.y * jumpHeight); // desire jump speed in y direction
        float aligneSpeed = Vector3.Dot(vecl, jumpDir);
        if (aligneSpeed > 0)
            desirJumpSpeed = Mathf.Max(desirJumpSpeed - aligneSpeed, 0);
        vecl += jumpDir * desirJumpSpeed;
        //rigidbody.velocity = vecl;
       
        jumpPhase++;
    }


    void SnapToGround() {
        float speed = vecl.magnitude;
        if (speed > maxSnapSpeed)
            return;

        if (Physics.Raycast(rigidbody.position, Vector3.down, out RaycastHit hit, maxProbeDistance)) {
            if (hit.normal.y > maxGroundSlotDot) { // if blow charater is ground 
                float vdotn = Vector3.Dot(vecl, hit.normal);
                if (vdotn > 0) {
                    vecl = (vecl - hit.normal * vdotn).normalized * speed;
                    groundContactCnt = 1;
                    groundContactNormal = hit.normal;
                }
            } 
        }
    }

    // 卡在裂缝
    bool ResueCrevasse() {
        if (!IsOnGround && steepContactCnt > 1 && steepContactNormal.y >= maxGroundSlotDot) {
            groundContactCnt = 1;
            groundContactNormal = steepContactNormal;
            steepContactCnt = 0;
            steepContactNormal = Vector3.zero;
            return true;
        }

        return false;
    }

}
