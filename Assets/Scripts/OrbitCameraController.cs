using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class OrbitCameraController : MonoBehaviour
{
    [SerializeField]
    Transform targetTransform;
    
    [SerializeField, Min(0)]
    float followDistance = 5;

    [SerializeField, Range(0, 5)]
    float focusRange = 2.5f;

    [SerializeField, Range(0, 1)]
    float autoFocusDamping = 0.5f;

    [SerializeField]
    Vector2 orbitAngles = new Vector2(45, 0);

    [SerializeField]
    Vector2 minMaxVerticalAngles = new Vector2(-30, 89);

    [SerializeField, Min(0)]
    float rotationSpeed = 90f;

    [SerializeField, Min(0)]
    float autoAlignRotateDelay = 3f;
    float lastManualRotateTime = 0f;
    Vector3 lastFocusPos = Vector3.zero;

    Vector3 focusPos = Vector3.zero;

    private void OnValidate() {
        minMaxVerticalAngles.x = Mathf.Clamp(minMaxVerticalAngles.x, -89, minMaxVerticalAngles.y);
        minMaxVerticalAngles.y = Mathf.Clamp(minMaxVerticalAngles.y, minMaxVerticalAngles.x, 89); 
    }

    // Start is called before the first frame update
    void Start() {
       focusPos = targetTransform.position;
       lastManualRotateTime = Time.unscaledTime;
       lastFocusPos = focusPos;
    }


    private void LateUpdate() {
        UpdateFocusPosition();
        bool hasRotation = UpdateRotation() || AutoAlignRotation();
        Quaternion lookRot = hasRotation ? Quaternion.Euler(orbitAngles) : gameObject.transform.localRotation;
        Vector3 lookDir =  lookRot * Vector3.forward;
        Vector3 lookPos = focusPos - lookDir * followDistance;
        gameObject.transform.SetPositionAndRotation(lookPos, lookRot);
    }

    private void UpdateFocusPosition() {
        lastFocusPos = focusPos;
        float distance = Vector3.Distance(focusPos, targetTransform.position);
        float t = 1f;
        if (distance > 0.05f && autoFocusDamping > 0) {
            t = Mathf.Pow(autoFocusDamping, Time.unscaledDeltaTime);
        }
        if (distance > focusRange) {
            t =  Mathf.Min(t, focusRange / distance);
        }

        focusPos = Vector3.Lerp(targetTransform.position, focusPos, t);
    }

    private bool UpdateRotation() {
        Vector2 input = new Vector2(Input.GetAxis("Vertical Camera"), Input.GetAxis("Horizontal Camera"));
        float epsilon = 0.01f;
        if (Mathf.Abs(input.x) > epsilon || Mathf.Abs(input.y) > epsilon) {
            orbitAngles += input * rotationSpeed * Time.unscaledDeltaTime;
            orbitAngles.x = Mathf.Clamp(orbitAngles.x, minMaxVerticalAngles.x, minMaxVerticalAngles.y);
            
            if (orbitAngles.y < 0)
                orbitAngles.y += 360;
            if (orbitAngles.y > 360)
                orbitAngles.y -= 360;

            lastManualRotateTime = Time.unscaledTime;
            return true;
        }

        return false;
    }

    bool AutoAlignRotation() { // automic rotate camera around y axis to follow player
        if (Time.unscaledTime - lastManualRotateTime < autoAlignRotateDelay)
            return false;

        Vector2 xzMovement = new Vector2(focusPos.x - lastFocusPos.x, focusPos.z - lastFocusPos.z);
        float lenSq = xzMovement.SqrMagnitude();
        if (lenSq < 0.00001)
            return false;
        
        Vector2 moveDir = xzMovement / Mathf.Sqrt(lenSq);
        float angle = Mathf.Acos(moveDir.y) * Mathf.Rad2Deg;
        if (moveDir.x < 0)
            angle = 360 - angle;

        orbitAngles.y = angle;    

        return true;
    }
}
