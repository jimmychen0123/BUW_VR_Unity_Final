﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class ManoeuvringScript : MonoBehaviour
{
    private GameObject mainCamera = null;
    private GameObject platformCenter = null;
    private GameObject rightHandController = null;
    private XRController rightXRController = null;

    private Vector3 startPosition = Vector3.zero;//new Vector3(70.28f, 22.26f, 37.78f);
    private Quaternion startRotation = Quaternion.identity; //Vector3(0,312.894073,0)
    private Quaternion rotTowardsHit = Quaternion.identity;

    public bool gripPressed = false;
    public bool gripReleased = false;
    private bool secondaryButtonLF = false;
    private Vector3 manoeuvringTargetPosition;
    private Vector3 manoeuvringCenterPosition;
    private Vector3 centerOffset;

    private LineRenderer rightRayRenderer;
    private LineRenderer offsetRenderer;

    private bool rayOnFlag = false;

    public LayerMask myLayerMask;

    private GameObject rightRayIntersectionSphere = null;
    private GameObject manoeuvringPositionPreview = null;
    private GameObject manoeuvringCenterPreview = null;
    private GameObject manoeuvringPersonPreview = null;

    private RaycastHit hit;

    // YOUR CODE (IF NEEDED) - BEGIN 
    private float height = 1.0f;
    private Vector3 avatarDirection;


    // YOUR CODE - END    


    // Start is called before the first frame update
    void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;

        mainCamera = GameObject.Find("Main Camera");
        platformCenter = GameObject.Find("Center");
        rightHandController = GameObject.Find("RightHand Controller");
        offsetRenderer = GetComponent<LineRenderer>();
        offsetRenderer.startWidth = 0.01f;
        offsetRenderer.positionCount = 2;

        if (rightHandController != null) // guard
        {
            rightXRController = rightHandController.GetComponent<XRController>();
            rightRayRenderer = rightHandController.AddComponent<LineRenderer>();
            rightRayRenderer.name = "Right Ray Renderer";
            rightRayRenderer.startWidth = 0.01f;
            rightRayRenderer.positionCount = 2;
            rayOnFlag = true;

            // geometry for intersection visualization
            rightRayIntersectionSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rightRayIntersectionSphere.name = "Right Ray Intersection Sphere";
            rightRayIntersectionSphere.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            rightRayIntersectionSphere.GetComponent<MeshRenderer>().material.color = Color.blue;
            rightRayIntersectionSphere.GetComponent<SphereCollider>().enabled = false; // disable for picking ?!
            rightRayIntersectionSphere.SetActive(false); // hide

            // geometry for Navidget visualization
            Material previewMaterial = new Material(Shader.Find("Standard"));
            previewMaterial.color = new Color(1.0f, 0.0f, 0.0f, 0.4f);
            previewMaterial.SetOverrideTag("RenderType", "Transparent");
            previewMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            previewMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            previewMaterial.SetInt("_ZWrite", 0);
            previewMaterial.DisableKeyword("_ALPHATEST_ON");
            previewMaterial.DisableKeyword("_ALPHABLEND_ON");
            previewMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            previewMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            manoeuvringPositionPreview = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            manoeuvringPositionPreview.transform.localScale = new Vector3(1f, 0.02f, 1f);
            manoeuvringPositionPreview.name = "Navidget Intersection Sphere";
            manoeuvringPositionPreview.layer = LayerMask.NameToLayer("Ignore Raycast");
            manoeuvringPositionPreview.GetComponent<MeshRenderer>().material = previewMaterial;
            manoeuvringPositionPreview.SetActive(false); // hide

            manoeuvringCenterPreview = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            manoeuvringCenterPreview.transform.localScale = new Vector3(0.05f, 1f, 0.05f);
            manoeuvringCenterPreview.name = "Navidget Intersection Sphere";
            manoeuvringCenterPreview.layer = LayerMask.NameToLayer("Ignore Raycast");
            manoeuvringCenterPreview.GetComponent<MeshRenderer>().material = previewMaterial;
            manoeuvringCenterPreview.SetActive(false); // hide

            manoeuvringPositionPreview = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            manoeuvringPositionPreview.transform.localScale = new Vector3(1f, 0.02f, 1f);
            manoeuvringPositionPreview.name = "Navidget Intersection Sphere";
            manoeuvringPositionPreview.layer = LayerMask.NameToLayer("Ignore Raycast");
            manoeuvringPositionPreview.GetComponent<MeshRenderer>().material = previewMaterial;
            manoeuvringPositionPreview.SetActive(false); // hide

            manoeuvringPersonPreview = Instantiate(Resources.Load("Prefabs/RealisticAvatar"), startPosition, startRotation) as GameObject;
            manoeuvringPersonPreview.layer = LayerMask.NameToLayer("Ignore Raycast");
            manoeuvringPersonPreview.SetActive(false);

            // YOUR CODE (IF NEEDED) - BEGIN 

            // YOUR CODE - END    

        }
    }

    // Update is called once per frame
    void Update()
    {
        UpdateOffsetToCenter();

        if (rightHandController != null) // guard
        {
            // mapping: joystick
            //Vector2 joystick;
            //rightXRController.inputDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out joystick);

            // mapping: primary button (A)
            //bool primaryButton = false;
            //rightXRController.inputDevice.TryGetFeatureValue(CommonUsages.primaryButton, out primaryButton);

            // mapping: grip button (middle finger)
            float grip = 0.0f;
            rightXRController.inputDevice.TryGetFeatureValue(CommonUsages.grip, out grip);

            UpdateRayVisualization(grip, 0.00001f);

            // YOUR CODE - BEGIN

            UpdateGripInputStatus();
            
            //before manoeuvring, check if the grip button is fully pressed 
            if(rightXRController.inputDevice.TryGetFeatureValue(CommonUsages.grip, out grip) && grip == 1)
            {

                StartCoroutine(SetManoeuvringIndicator());

            }
            
            
            
            

            // YOUR CODE - END    

            // mapping: secondary button (B)
            bool secondaryButton = false;
            rightXRController.inputDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out secondaryButton);

            if (secondaryButton != secondaryButtonLF) // state changed
            {
                if (secondaryButton) // up (0->1)
                {
                    ResetXRRig();
                }
            }

            secondaryButtonLF = secondaryButton;
        }
    }

    private void UpdateGripInputStatus()
    {
        float gripValue;
        if(rightXRController.inputDevice.TryGetFeatureValue(CommonUsages.grip, out gripValue) && gripValue == 1.0f)
        {
            gripPressed = true;
            gripReleased = false;
        }

        if (rightXRController.inputDevice.TryGetFeatureValue(CommonUsages.grip, out gripValue) && gripValue <  0.005f)
        {
            gripReleased = true;
            gripPressed = false;
        }

    }

    private void UpdateOffsetToCenter()
    {
        // Calculate the offset between the platform center and the camera in the xz plane
        Vector3 a = transform.position;
        Vector3 b = new Vector3(mainCamera.transform.position.x, this.transform.position.y, mainCamera.transform.position.z);
        centerOffset = b - a;

        // visualize the offset as a line on the ground
        offsetRenderer.positionCount = 2; // line renderer visualizes a line between N (here 2) vertices
        offsetRenderer.SetPosition(0, a); // set pos 1
        offsetRenderer.SetPosition(1, b); // set pos 2

    }

    private void UpdateRayVisualization(float inputValue, float threshold)
    {
        // Visualize ray if input value is bigger than a certain treshhold
        if (inputValue > threshold && rayOnFlag == false)
        {
            rightRayRenderer.enabled = true;
            rayOnFlag = true;
        }
        else if (inputValue < threshold && rayOnFlag)
        {
            rightRayRenderer.enabled = false;
            rayOnFlag = false;
        }

        // update ray length and intersection point of ray
        if (rayOnFlag)
        { // if ray is on

            // Check if something is hit and set hit point
            if (Physics.Raycast(rightHandController.transform.position,
                                rightHandController.transform.TransformDirection(Vector3.forward),
                                out hit, Mathf.Infinity, myLayerMask))
            {
                rightRayRenderer.SetPosition(0, rightHandController.transform.position);
                rightRayRenderer.SetPosition(1, hit.point);

                rightRayIntersectionSphere.SetActive(true);
                rightRayIntersectionSphere.transform.position = hit.point;
            }
            else
            { // if nothing is hit set ray length to 100
                rightRayRenderer.SetPosition(0, rightHandController.transform.position);
                rightRayRenderer.SetPosition(1, rightHandController.transform.position + rightHandController.transform.TransformDirection(Vector3.forward) * 100);

                rightRayIntersectionSphere.SetActive(false);
            }
        }
        else
        {
            rightRayIntersectionSphere.SetActive(false);
        }
    }

    // YOUR CODE (ADDITIONAL FUNCTIONS)- BEGIN
    IEnumerator SetManoeuvringIndicator()
    {
        
        //store manoeuvring location while preview is not yet activated
        //and the manoevring postion is located(intersection)
        while (!manoeuvringCenterPreview.activeSelf && rightRayIntersectionSphere.activeSelf)
        {
            //this allows to store the manoeuvring location while user slightly presss the trigger for ray intersection
            StoreManoeuvringCenterPosition();
            //set and activate the preview 
            SetManoeuvringCenterPreview();
            //what follow yield return will specify how long Unity will wait before continuing
            //execution will pause and be resumed the following frame
            yield return null;

        }

        //update the target preview
        while (rayOnFlag)
        {
            SetJumpingPosition();
            SetJumpingPreview();
            SetPreviewDirection();
           

            //untill the grip is fully released with threshold given 
            yield return new WaitUntil(()=> gripReleased);
        }

        //after grip released
        UpdateUserPositionDirection();
        manoeuvringCenterPreview.SetActive(false);
        manoeuvringPersonPreview.SetActive(false);
        manoeuvringPositionPreview.SetActive(false);


    }

    private void UpdateUserPositionDirection()
    {
        gameObject.transform.position = manoeuvringTargetPosition;
        gameObject.transform.rotation = rotTowardsHit;
    }

    private void SetPreviewDirection()
    {
        //store the avatars direction before releasing the trigger button
        // Determine which direction to rotate towards
        //https://answers.unity.com/questions/254130/how-do-i-rotate-an-object-towards-a-vector3-point.html
        //https://docs.unity3d.com/ScriptReference/Quaternion.Slerp.html

        avatarDirection = (manoeuvringCenterPosition - manoeuvringTargetPosition).normalized;

        rotTowardsHit.SetLookRotation(avatarDirection);


        manoeuvringPersonPreview.transform.rotation = Quaternion.Slerp(manoeuvringPersonPreview.transform.rotation, rotTowardsHit, Time.deltaTime);
    }

    private void SetJumpingPreview()
    {
        //set the target sephere 
        manoeuvringPositionPreview.transform.position = manoeuvringTargetPosition;
        manoeuvringPositionPreview.SetActive(true);
        //set the avatar with height
        manoeuvringPersonPreview.transform.position = new Vector3(manoeuvringTargetPosition.x, manoeuvringTargetPosition.y + height, manoeuvringTargetPosition.z);
        manoeuvringPersonPreview.SetActive(true);

    }

    private void SetJumpingPosition()
    {
        manoeuvringTargetPosition = rightRayIntersectionSphere.transform.position;
        
    }

    private void SetManoeuvringCenterPreview()
    {
        manoeuvringCenterPreview.transform.position = manoeuvringCenterPosition;
        manoeuvringCenterPreview.SetActive(true);
        
    }

    private void StoreManoeuvringCenterPosition()
    {
        manoeuvringCenterPosition = rightRayIntersectionSphere.transform.position;
        
    }

    // YOUR CODE - END    

    private void ResetXRRig()
    {
        transform.position = startPosition;
        transform.rotation = startRotation;
    }
}
