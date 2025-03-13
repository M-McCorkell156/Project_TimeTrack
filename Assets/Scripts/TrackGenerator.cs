using static UnityEditor.PlayerSettings.Switch;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using System.Linq;
using Unity.Burst.Intrinsics;
using UnityEngine.Splines;
using Unity.Mathematics;
using Random = UnityEngine.Random;

public class TrackGenerator : MonoBehaviour
{
    #region Variables
    public GameObject[] trackPoints;
    [Range(5, 20)][SerializeField] private int trackPointNo;
    public GameObject trackPointObj;
    [Range(1, 10)][SerializeField] private float centreSpaceDist;
    [Range(10, 100)][SerializeField] private float borderSpaceDist;

    private Transform knotPoint;
    private Transform currentPoint;

    [SerializeField] private HashSet<Transform> track;

    public List<Transform> collinearPoints;

    public delegate void EventHandler();
    public static event EventHandler DestroyObjs;

    [SerializeField] private GameObject PointPrefab;


    [SerializeField] private GameObject trackSplineObj;
    private SplineContainer trackSplineContainer;
    private Spline splineTrack;

    private BezierKnot startKnot;
    #endregion

    #region Basic Methods
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (trackPoints.Length != 0)
            {
                //Debug.Log("clear tracks");
                ClearTrack();
            }
            //Debug.Log("Now gen");
            GeneratePoints();

        }
    }
    #endregion

    #region GeneratePoints
    private void GeneratePoints()
    {
        trackPoints = new GameObject[trackPointNo];
        //trackPointObj = PointPrefab; // TODO: poissibly not needed

        for (int i = 0; i < trackPointNo; i++)
        {

            Vector3 rndPointPos = RandomPlacementPos();

            trackPointObj = Instantiate(PointPrefab, rndPointPos, Quaternion.identity);
            //trackPointObj.AddComponent<DestroySelf>();

            knotPoint = trackPointObj.transform;
            //Debug.Log(knotPoint.position);


            trackPoints[i] = trackPointObj;
            //Debug.Log($"Loop .{i}.");

        }

        //GameObject.Destroy(trackPointObj);
        ConvexHullGenerator();
    }
    #endregion

    #region RandomPlacementPos
    private Vector3 RandomPlacementPos()
    {
        Vector3 rndPos = new Vector3();
        float rndX = Random.Range(0.00f, 100.00f);
        float rndZ = Random.Range(0.00f, 100.00f);
        rndPos = new Vector3(rndX, 1, rndZ);
        return rndPos;
    }

    #endregion

    #region ClearTrack
    private void ClearTrack()
    {

        //for (int i = 0; i < trackPoints.Length; i++)
        //{
        //    Destroy(trackPoints[i].gameObject);
        //}

        trackPoints = null;

        Debug.Log("clear list");
        collinearPoints.Clear();

        trackSplineContainer.RemoveSpline(splineTrack);

        if (DestroyObjs != null)
        {
            DestroyObjs();
        }
    }

    #endregion 

    #region ConvexHullGenerator
    private void ConvexHullGenerator()
    {
        SplineInfoSetter();

        track = new HashSet<Transform>();
        int leftMostIndex = 0;

        //find left most point
        for (int i = 1; i < trackPoints.Length; i++)
        {
            if (trackPoints[leftMostIndex].transform.position.x > trackPoints[i].transform.position.x)
            {
                leftMostIndex = i;
            }
        }

        //List of points to connect

        Transform current = trackPoints[leftMostIndex].transform;


        //Debug.Log("kill me??");
        Transform nextTarget = trackPoints[0].transform;

        for (int i = 0; i < trackPoints.Length; i++)
        {
            if (trackPoints[i] == current)
                continue;

            float x1, x2, z1, z2;
            x1 = current.position.x - nextTarget.position.x;
            x2 = current.position.x - trackPoints[i].transform.position.x;

            z1 = current.position.z - nextTarget.position.z;
            z2 = current.position.z - trackPoints[i].transform.position.z;
            float val = (z2 * x1) - (z1 * x2);

            //Debug.Log(val);

            if (val > 0)
            {
                //Debug.Log("val > 0");
                nextTarget = trackPoints[i].transform;
                collinearPoints = new List<Transform>();
            }

            else if (val == 0)
            {
                //Debug.Log("val = 0");
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


            track.Add(nextTarget);
            current = nextTarget;
            //Debug.Log(nextTarget);

            SplineKnotAdd(nextTarget);

            if (nextTarget == trackPoints[leftMostIndex])
                break;

        }

    }

    #endregion

    private void SplineInfoSetter()
    {
        trackSplineContainer = trackSplineObj.GetComponent<SplineContainer>();
        //Debug.Log(knotPoint.position);
        splineTrack = trackSplineContainer.AddSpline();
        splineTrack.Closed = true; 
    }
    private void SplineKnotAdd(Transform addPoint)
    {
        startKnot.Position = addPoint.position;
        splineTrack.Add(startKnot, TangentMode.AutoSmooth);        
        //Debug.Log("new knot");
    }
}



