using static UnityEditor.PlayerSettings.Switch;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class TrackGenerator : MonoBehaviour
{
    private GameObject[] trackPoints;
    [Range(5, 20)][SerializeField] private int trackPointNo;
    private GameObject trackPointObj;
    [Range(1, 10)][SerializeField] private float centreSpaceDist;
    [Range(10, 100)][SerializeField] private float borderSpaceDist;

    private Transform firstPoint;
    private Transform currentPoint;

    [SerializeField] private HashSet<Transform> track;

    public List<Transform> collinearPoints;

    private void Start()
    {

    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ClearTrack();
            GeneratePoints();
        }

    }
    private void GeneratePoints()
    {
        trackPoints = new GameObject[trackPointNo];
        trackPointObj = new GameObject();

        for (int i = 0; i < trackPointNo; i++)
        {

            Vector3 rndPointPos = RandomPlacementPos();

            Instantiate(trackPointObj, rndPointPos, Quaternion.identity);

            if (i == 0)
            {
                firstPoint = trackPointObj.transform;
            }

            trackPoints[i] = trackPointObj;
            //Debug.Log($"Loop .{i}.");
        }
        GameObject.Destroy(trackPointObj);
        ConvexHullGenerator();
    }
    private Vector3 RandomPlacementPos()
    {
        Vector3 rndPos = new Vector3();
        float rndX = Random.Range(-50.00f, 50.00f);
        float rndZ = Random.Range(-50.00f, 50.00f);
        rndPos = new Vector3(rndX, 1, rndZ);
        return rndPos;
    }
    private void ClearTrack()
    {
        if (trackPoints != null)
            Debug.Log("clear yes");
        {
            for (int i = 0; i < trackPoints.Length; i++)
            {
                Destroy(trackPoints[i].gameObject);
                trackPoints = null;
            }
        }
    }

    private void ConvexHullGenerator()
    {
        track = new HashSet<Transform>();
        int leftMostIndex = 0;

        //find left most point
        for (int i = 1; i < trackPoints.Length; i++)
        {
            if (trackPoints[leftMostIndex].transform.position.x > trackPoints[i].transform.position.x)
                leftMostIndex = i;
        }

        //List of points to connect

        Transform current = trackPoints[leftMostIndex].transform;

        Debug.Log("kill me??");
        Transform nextTarget = trackPoints[0].transform;

        for (int i = 1; i < trackPoints.Length; i++)
        {
            if (trackPoints[i] == current)
                continue;

            float x1, x2, y1, y2;
            x1 = current.position.x - nextTarget.position.x;
            x2 = current.position.x - trackPoints[i].transform.position.x;

            y1 = current.position.y - nextTarget.position.y;
            y2 = current.position.y - trackPoints[i].transform.position.y;
            float val = (y2 * x1) - (y1 * x2);

            if (val > 0)
            {
                nextTarget = trackPoints[i].transform;
                collinearPoints = new List<Transform>();
            }

            else if (val == 0)
            {
                if (Vector2.Distance(current.position, nextTarget.position) < Vector2.Distance(current.position, trackPoints[i].transform.position))
                {
                    collinearPoints.Add(nextTarget);
                    nextTarget = trackPoints[i].transform;
                }
                else
                    collinearPoints.Add(trackPoints[i].transform);
            }


            foreach (Transform t in collinearPoints)
                track.Add(t);

            if (nextTarget == trackPoints[leftMostIndex])
                break;

            track.Add(nextTarget);
            current = nextTarget;
        }

    }
}



