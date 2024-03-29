﻿
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class Navidget : MonoBehaviour
{

    private GameObject mainCamera = null;
    private GameObject rightHandController = null;
    private XRController rightXRController = null;

    public float translationFactor = 1.0f;
    public float rotationFactor = 1.0f;

    private Vector3 startPosition = Vector3.zero;//new Vector3(70.28f, 22.26f, 37.78f);
    private Quaternion startRotation = Quaternion.identity; //Vector3(0,312.894073,0)

    //Jimmy: For debug purpose, set to public 
    public bool primaryButtonLF = false;
    private bool secondaryButtonLF = false;

    private LineRenderer rightRayRenderer;

    public LayerMask myLayerMask;

    private GameObject rightRayIntersectionSphere = null;
    private GameObject navidgetIntersectionSphere = null;
    private GameObject navidgetDirectionStick = null;
    private GameObject navidgetPreviewPose = null;

    private RaycastHit rightHit;

    private bool animationFlag = false;
    public float animationDuration = 3.0f; // in seconds

    // YOUR CODE (IF NEEDED) - BEGIN 
    private Vector3 navidgetCenterPosition;
    
    public GameObject cameraOffset;

    // YOUR CODE - END    



    // Start is called before the first frame update
    void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;

        mainCamera = GameObject.Find("Main Camera");
        rightHandController = GameObject.Find("RightHand Controller");
       
        if (rightHandController != null) // guard
        {
            rightXRController = rightHandController.GetComponent<XRController>();

            //rightRayRenderer = gameObject.AddComponent<LineRenderer>();
            rightRayRenderer = rightHandController.AddComponent<LineRenderer>();
            //rightRayRenderer = rightHandController.GetComponent<LineRenderer>();
            rightRayRenderer.name = "Right Ray Renderer";
            rightRayRenderer.startWidth = 0.01f;
            rightRayRenderer.positionCount = 2; // two points (one line segment)

            // geometry for intersection visualization
            rightRayIntersectionSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //rightRayIntersectionSphere.transform.parent = this.gameObject.transform;
            rightRayIntersectionSphere.name = "Right Ray Intersection Sphere";
            rightRayIntersectionSphere.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            rightRayIntersectionSphere.GetComponent<MeshRenderer>().material.color = Color.blue;
            rightRayIntersectionSphere.GetComponent<SphereCollider>().enabled = false; // disable for picking ?!
            rightRayIntersectionSphere.SetActive(false); // hide

            // geometry for Navidget visualization
            Material navidgetSphereMaterial = new Material(Shader.Find("Standard"));
            navidgetSphereMaterial.color = new Color(1.0f, 0.0f, 0.0f, 0.4f);
            navidgetSphereMaterial.SetOverrideTag("RenderType", "Transparent");
            //SetupMaterialWithBlendMode(navidgetSphereMaterial, BlendMode.Transparent);
            navidgetSphereMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            navidgetSphereMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            navidgetSphereMaterial.SetInt("_ZWrite", 0);
            navidgetSphereMaterial.DisableKeyword("_ALPHATEST_ON");
            navidgetSphereMaterial.DisableKeyword("_ALPHABLEND_ON");
            navidgetSphereMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            navidgetSphereMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            navidgetIntersectionSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            navidgetIntersectionSphere.name = "Navidget Intersection Sphere";
            navidgetIntersectionSphere.GetComponent<MeshRenderer>().material = navidgetSphereMaterial;
            //navidgetIntersectionSphere.GetComponent<SphereCollider>().enabled = false; // disable for picking ?!
            navidgetIntersectionSphere.SetActive(false); // hide


            navidgetDirectionStick = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            navidgetDirectionStick.name = "Navidget Direction Stick";
            navidgetDirectionStick.GetComponent<MeshRenderer>().material.color = Color.red;
            navidgetDirectionStick.GetComponent<CapsuleCollider>().enabled = false; // disable for picking ?!
            navidgetDirectionStick.SetActive(false); // hide

            navidgetPreviewPose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            navidgetPreviewPose.name = "Navidget Preview Pose";
            navidgetPreviewPose.GetComponent<MeshRenderer>().material.color = Color.red;
            navidgetPreviewPose.GetComponent<BoxCollider>().enabled = false; // disable for picking ?!
            navidgetPreviewPose.SetActive(false); // hide
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (rightHandController != null) // guard
        {

            // ----------------- Steering stuff -----------------

            // mapping: grip button (middle finger)
            float grip = 0.0f;
            rightXRController.inputDevice.TryGetFeatureValue(CommonUsages.grip, out grip);
            //Debug.Log("middle finger rocker: " + grip);

            // mapping: trigger (index finger)
            float trigger = 0.0f;
            rightXRController.inputDevice.TryGetFeatureValue(CommonUsages.trigger, out trigger);
            //Debug.Log("index finger rocker: " + trigger);

            steering(0.0f, 0.0f, trigger, 0.0f, 0.0f, 0.0f); // map as forward steering input

            // mapping: joystick
            Vector2 joystick;
            rightXRController.inputDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out joystick);

            //steering(joystick.x, 0.0f, joystick.y, 0.0f, 0.0f, 0.0f); // map vertical jostick axis as forward/backward input
            //steering(0.0f, 0.0f, joystick.y, joystick.x, 0.0f, 0.0f);
            steering(0.0f, 0.0f, 0.0f, joystick.y, joystick.x, 0.0f); // map as head & pitch rotation

            // ----------------- NAVIDGET stuff -----------------

            // mapping: primary button (A)
            bool primaryButton = false;
            rightXRController.inputDevice.TryGetFeatureValue(CommonUsages.primaryButton, out primaryButton);
            //Debug.Log("primary button: " + primaryButton);

            // YOUR CODE - BEGIN
            UpdatePrimaryButtonStatus(primaryButton);
            //Check if the primary button is pressed
            if (primaryButton)
            {
                StartCoroutine(SetNavidget());
            }

          
            // YOUR CODE - END

            //if (primaryButton != primaryButtonLF) // state changed
            //{
            //    if (primaryButton) // up (false -> true)
            //    {
            //        if (rightHit.collider != null) // something was hit
            //        {
            //            // start target pose specification

            //            // YOUR CODE - BEGIN


            //            // YOUR CODE - END
            //        }

            //    }
            //    else // down (true -> false)
            //    {
            //        // YOUR CODE - BEGIN

            //        // finish target pose specification and start animation process


            //        // YOUR CODE - END
            //    }
            //}
            //else
            //{
            //    if (primaryButton) // high (true -> true)
            //    {
            //        // YOUR CODE - BEGIN

            //        // update target pose (position and orientation) with respect to oint of interest


            //        // YOUR CODE - END
            //    }
            //}

            //primaryButtonLF = primaryButton;

            // ----------------- Reset stuff -----------------

            // mapping: secondary button (B)
            bool secondaryButton = false;
            rightXRController.inputDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out secondaryButton);
            //Debug.Log("secondary button: " + secondaryButton);

            if (secondaryButton != secondaryButtonLF) // state changed
            {
                if (secondaryButton) // up (0->1)
                {
                    resetXRRig();
                }
            }

            secondaryButtonLF = secondaryButton;


            animateXRRig();
        }
    }

    private void UpdatePrimaryButtonStatus(bool primaryButton)
    {
        
        if (rightXRController.inputDevice.TryGetFeatureValue(CommonUsages.primaryButton, out primaryButton) && primaryButton)
        {
            primaryButtonLF = true;

        }

        else
        {
            primaryButtonLF = false;
        }
    }

    IEnumerator SetNavidget()
    {
        //store navidget location while preview is not yet activated
        //and set the navidget sphere preview
        while (rightHit.collider != null && !navidgetIntersectionSphere.activeSelf)
        {
            //this allows store the navidget location while user presses the button A
            StoreNavidgetPosition();
            //set and activate the preview 
            SetNavidgetSpherePreview();
            //what follow yield return will specify how long Unity will wait before continuing
            //execution will pause and be resumed the following frame
            yield return null;
        }

        //Set cylinder preview with its view direction
        //Set camera preview and store absolute target pose
        if (rightHit.collider.name == navidgetIntersectionSphere.GetComponent<SphereCollider>().name)
        {
            Debug.Log("On surface");
            SetNavidgetStickPose();
        }
        else
        {
            Debug.Log("Not On surface");
        }

        //untill the button A is fully released 
        yield return new WaitUntil(() => !primaryButtonLF);



        //set the animation
        //https://gamedevbeginner.com/the-right-way-to-lerp-in-unity-with-examples/
        var timeElapsed = 0.0f;
        
        var currentPos = gameObject.transform.position;
        var currentRot = gameObject.transform.rotation;
        
        var distance = Vector3.Distance(currentPos, mainCamera.transform.position);
        
        //offset
        navidgetPreviewPose.SetActive(false);
        navidgetPreviewPose.transform.Translate(0, -distance, 0, Space.Self);
        
        while (timeElapsed < animationDuration)
        {

            
            
            gameObject.transform.position = Vector3.Lerp(currentPos, navidgetPreviewPose.transform.position, timeElapsed / animationDuration);


            mainCamera.transform.position = gameObject.transform.position;

            gameObject.transform.rotation = Quaternion.Slerp(currentRot, navidgetPreviewPose.transform.rotation, timeElapsed / animationDuration);

            timeElapsed += Time.deltaTime;
            
            yield return null;

        }

        gameObject.transform.position = navidgetPreviewPose.transform.position;
        gameObject.transform.rotation = navidgetPreviewPose.transform.rotation;

       


        navidgetIntersectionSphere.SetActive(false);
        navidgetDirectionStick.SetActive(false);
        
        

    }

    private void SetNavidgetStickPose()
    {
        //set the hierarchy of sphere->camera pose-> stick 
        navidgetPreviewPose.transform.SetParent(navidgetIntersectionSphere.transform);
        navidgetDirectionStick.transform.SetParent(navidgetIntersectionSphere.transform);

        
        navidgetIntersectionSphere.transform.rotation = Matrix4x4.LookAt(rightRayIntersectionSphere.transform.position, navidgetIntersectionSphere.transform.position, Vector3.up).rotation;
        //set position of camera pose and stick
        navidgetPreviewPose.transform.localPosition = new Vector3(0.0f, 0.0f, -navidgetIntersectionSphere.GetComponent<SphereCollider>().radius * 2);

        navidgetDirectionStick.transform.localPosition = new Vector3(0.0f, 0.0f, -navidgetIntersectionSphere.GetComponent<SphereCollider>().radius);

        //set rotation of stick
        //align the axis
        navidgetDirectionStick.transform.rotation = Quaternion.LookRotation(navidgetIntersectionSphere.transform.up);
   
        //set scale of camera pose and stick
        navidgetPreviewPose.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        navidgetPreviewPose.SetActive(true);
        

        
        navidgetDirectionStick.transform.localScale = new Vector3(0.025f, navidgetIntersectionSphere.GetComponent<SphereCollider>().radius, 0.025f);

        navidgetDirectionStick.SetActive(true);

       
    }

    private void SetNavidgetSpherePreview()
    {
        navidgetIntersectionSphere.transform.position = navidgetCenterPosition;
        navidgetIntersectionSphere.SetActive(true);

       
    }

    private void StoreNavidgetPosition()
    {
        navidgetCenterPosition = rightRayIntersectionSphere.transform.position;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // ray intersection

        // Does the ray intersect any objects
        if (Physics.Raycast(rightHandController.transform.position, rightHandController.transform.TransformDirection(Vector3.forward), out rightHit, Mathf.Infinity, myLayerMask))
        {
            //Debug.Log("Did Hit");
            // update ray visualization
            rightRayRenderer.SetPosition(0, rightHandController.transform.position);
            rightRayRenderer.SetPosition(1, rightHit.point);

            // update intersection sphere visualization
            rightRayIntersectionSphere.SetActive(true); // show
            rightRayIntersectionSphere.transform.position = rightHit.point;
        }
        else
        {
            //Debug.Log("Did not Hit");
            // update ray visualization
            rightRayRenderer.SetPosition(0, rightHandController.transform.position);
            rightRayRenderer.SetPosition(1, rightHandController.transform.position + rightHandController.transform.TransformDirection(Vector3.forward) * 1000);

            // update intersection sphere visualization
            rightRayIntersectionSphere.SetActive(false); // hide
        }   
    }

    private void animateXRRig()
    {
        // YOUR CODE - BEGIN
        // interpolate position -> Vector3.Lerp()

        // interpolate rotation -> Quaternion.Slerp()

        Debug.Log("start animation");

        // YOUR CODE - END        
    }

    private void steering(float X, float Y, float Z, float RX, float RY, float RZ)
    {
        if (animationFlag) return; // abort steering when in animation process

        // translation in absolute controller direction
        Matrix4x4 rotmat = Matrix4x4.Rotate(rightHandController.transform.rotation);
        Matrix4x4 rotmatRig = Matrix4x4.Rotate(transform.rotation);
        Vector4 moveVec = new Vector4(X, Y, Z, 0.0f);
        float length = moveVec.magnitude;
        moveVec.Normalize();
        moveVec = moveVec * Mathf.Pow(length, 3); // exponential transfer function

        moveVec = rotmatRig.inverse * rotmat * moveVec;
        transform.Translate(moveVec * Time.deltaTime * translationFactor); // accumulate translation input


        // Head rotation
        RY = Mathf.Pow(RY, 3); // exponential transfer function
        //transform.Rotate(0.0f, RY * Time.deltaTime * rotationFactor, 0.0f, Space.Self); // rotate around platform center
        transform.RotateAround(mainCamera.transform.position, Vector3.up, RY * Time.deltaTime * rotationFactor); // rotate arround camera position (on platform)

        // Pitch rotation
        RX = Mathf.Pow(RX, 3); // exponential transfer function
        //transform.Rotate(RX * Time.deltaTime * rotationFactor, 0.0f, 0.0f, Space.Self); // rotate around platform center
        //transform.RotateAround(mainCamera.transform.position, Vector3.right, RX * Time.deltaTime * rotationFactor); // rotate arround camera position (on platform)
        Vector3 rightLocal = transform.TransformDirection(Vector3.right);
        transform.RotateAround(mainCamera.transform.position, rightLocal, RX * Time.deltaTime * rotationFactor); // rotate arround camera position (on platform) in local platform coordinate system

        // Roll rotation not implemented
    }


    private void resetXRRig()
    {
        transform.position = startPosition;
        transform.rotation = startRotation;

        animationFlag = false;
    }
}
