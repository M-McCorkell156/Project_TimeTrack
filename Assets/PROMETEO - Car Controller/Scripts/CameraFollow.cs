using System.Collections;
using System.Collections.Generic;
using Unity.Splines.Examples;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{

    public Transform carTransform;
    [Range(1, 10)]
    public float followSpeed = 2;
    [Range(1, 10)]
    public float lookSpeed = 5;
    Vector3 initialCameraPosition;
    Vector3 initialCarPosition;
    Vector3 absoluteInitCameraPosition;

    bool isInitialized;

    GameObject car;
    GameObject camPos;

    void Start()
    {
        isInitialized = false;
        //Debug.Log("subed");
        TrackGenerator.CarSpawned += SetCarCam;
    }

    void FixedUpdate()
    {
        if (isInitialized)
        {
            carTransform = car.transform;

            initialCameraPosition = gameObject.transform.position;
            initialCarPosition = carTransform.position;
            absoluteInitCameraPosition = initialCameraPosition - initialCarPosition;

            //Look at car
            Vector3 _lookDirection = (new Vector3(carTransform.position.x, carTransform.position.y, carTransform.position.z)) - transform.position;
            Quaternion _rot = Quaternion.LookRotation(_lookDirection, Vector3.up);
            transform.rotation = Quaternion.Lerp(transform.rotation, _rot, lookSpeed * Time.deltaTime);

            //Move to car

            //Vector3 _targetPos = absoluteInitCameraPosition + carTransform.transform.position;
            //transform.position = Vector3.Lerp(transform.position, _targetPos, followSpeed * Time.deltaTime);
        }

    }
    private void StartRace()
    {

    }

    private void SetCarCam()
    {
        car = GameObject.Find("Prometheus(Clone)");
        camPos = GameObject.Find("CamPos");

        carTransform = car.transform;

        gameObject.transform.parent = camPos.transform;

        gameObject.transform.position = camPos.transform.position;
        gameObject.transform.up += (Vector3.up * 10);

        isInitialized = true;
    }



}
