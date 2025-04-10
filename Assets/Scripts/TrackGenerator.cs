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
using UnityEditor.ShaderGraph;
using System;
using System.Collections;
using NUnit.Framework;

public class TrackGenerator : MonoBehaviour
{
    #region Variables
    public GameObject[] trackPoints;
    private Vector3[] trackPositions;
    private Vector3 rndPointPos;

    [UnityEngine.Range(5, 20)][SerializeField] private int trackPointNo;
    public GameObject trackPointObj;
    [UnityEngine.Range(1, 10)][SerializeField] private float centreSpaceDist;
    [UnityEngine.Range(10, 100)][SerializeField] private float borderSpaceDist;

    private Transform knotPoint;
    private Transform currentPoint;

    [SerializeField] private HashSet<Transform> track;

    public List<Transform> collinearPoints;

    public delegate void EventHandler();
    public static event EventHandler DestroyObjs;
    public static event EventHandler CarSpawned;


    [SerializeField] private GameObject PointPrefab;
    [SerializeField] private GameObject StartPrefab;
    [SerializeField] private GameObject CarPrefab;


    [SerializeField] private GameObject trackSplineObj;
    private SplineContainer trackSplineContainer;
    private Spline splineTrack;
    private Spline fakeTrack;

    private BezierKnot newKnot;
    private List<BezierKnot> knots;
    private Quaternion knotRotation;

    [SerializeField] private Mesh roadMesh;
    [SerializeField] private MeshFilter roadMeshFilter;
    //private float meshWdith = 1.5f;

    private GameObject plyCar;

    private int currentChkPnt;
    private int nextChkPnt;
    private GameObject[] checkPoints;



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
        trackPositions = new Vector3[trackPointNo];

        //trackPointObj = PointPrefab; // TODO: poissibly not needed

        for (int i = 0; i < trackPointNo; i++)
        {
            rndPointPos = RandomPlacementPos(i);

            trackPositions[i] = rndPointPos;

            //Debug.Log("yes not in range");

            trackPointObj = Instantiate(PointPrefab, rndPointPos, Quaternion.identity);
            //trackPointObj.AddComponent<DestroySelf>();

            knotPoint = trackPointObj.transform;
            //Debug.Log(knotPoint.position);


            trackPoints[i] = trackPointObj;
            //Debug.Log($"Loop .{i}.");

            Destroy(trackPointObj);

        }

        //GameObject.Destroy(trackPointObj);
        ConvexHullGenerator();
    }
    #endregion

    private bool CheckRndPosDistanceFar(Vector3 rndPosIs)
    {

        for (int i = 0; i < trackPointNo; i++)
        {
            if (Vector3.Distance(rndPosIs, trackPositions[i]) > 20)
            {
                //Debug.Log($"far{Vector3.Distance(rndPosIs, trackPositions[i])}");
                i++;
            }
            else
            {
                //Debug.Log($"close{Vector3.Distance(rndPosIs, trackPositions[i])}");
                return false;
            }
        }

        //Debug.Log("good");
        return true;
    }

    #region RandomPlacementPos
    private Vector3 RandomPlacementPos(int i)
    {
        //Debug.Log("rndplace");

        Vector3 rndPos = new Vector3();
        float rndX = Random.Range(0.00f, 100.00f);
        float rndZ = Random.Range(0.00f, 100.00f);
        rndPos = new Vector3(rndX, 1, rndZ);

        if (CheckRndPosDistanceFar(rndPos))
        {
            //Debug.Log("add to list");
            trackPositions[i] = rndPos;
        }
        else
        {
            rndPos = RandomPlacementPos(i);
        }

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

        //Debug.Log("clear list");
        collinearPoints.Clear();

        trackSplineContainer.RemoveSpline(splineTrack);
        //trackSplineContainer.RemoveSpline(fakeTrack);

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

        int leftMostIndex = 0;

        //find left most point
        for (int i = 1; i < trackPoints.Length; i++)
        {
            if (trackPoints[leftMostIndex].transform.position.x > trackPoints[i].transform.position.x)
            {
                leftMostIndex = i;
                //Debug.Log(trackPoints[leftMostIndex].transform.position);

            }
        }

        //List of points to connect

        Transform current = trackPoints[leftMostIndex].transform;


        //Next target pos
        Transform nextTarget = trackPoints[0].transform;

        for (int i = 0; i < trackPoints.Length; i++)
        {
            if (trackPoints[i] == current)
            {
                Debug.Log($"trackpoint = current {i}");
                continue;
            }

            float x1, x2, z1, z2;
            x1 = current.position.x - nextTarget.position.x;
            x2 = current.position.x - trackPoints[i].transform.position.x;

            z1 = current.position.z - nextTarget.position.z;
            z2 = current.position.z - trackPoints[i].transform.position.z;
            float val = (z2 * x1) - (z1 * x2);

            //Debug.Log(val);

            if (val > 0)
            {
                //Debug.Log("val > 0 - new list");
                nextTarget = trackPoints[i].transform;
                collinearPoints = new List<Transform>();
            }

            else if (val == 0)
            {
                //Debug.Log("val = 0 - add target");
                if (Vector2.Distance(current.position, nextTarget.position) < Vector2.Distance(current.position, trackPoints[i].transform.position))
                {
                    collinearPoints.Add(nextTarget);
                    nextTarget = trackPoints[i].transform;

                }
                //else
                //    collinearPoints.Add(trackPoints[i].transform);
            }

            current = nextTarget;
            //Debug.Log(nextTarget);


            if (nextTarget == trackPoints[leftMostIndex])
                break;

        }


        checkPoints = new GameObject[collinearPoints.Count];

        for (int i = 1; i < collinearPoints.Count; i++)
        {
            SplineKnotAdd(collinearPoints[i].transform);
            Vector3 checkPointPos = collinearPoints[i].transform.position;
            checkPointPos.y = 0;
            trackPointObj = Instantiate(PointPrefab,checkPointPos, Quaternion.identity);
            checkPoints[i] = trackPointObj;
            checkPoints[i].SetActive(false);
        }


        SplineKnotCuvre();

        //this.AddComponent<MeshCollider>();

        GameObject newStart = Instantiate(StartPrefab, collinearPoints[1].position, Quaternion.identity);

        Vector3 startPoint = newStart.transform.position;
        startPoint.y = 1;

        //Debug.Log(startPoint);
        if (plyCar == null)
        {

            plyCar = Instantiate(CarPrefab, startPoint, Quaternion.identity);

            if (CarSpawned != null)
            {
                Debug.Log("call");
                CarSpawned();
            }
        }
        else
        {
            plyCar.transform.position = startPoint;
        }


    }

    private void StartRace()
    {
        checkPoints[1].SetActive(true);
        nextChkPnt = 2;
        currentChkPnt = 1;
    }

    private void NextCheckPoint()
    {
        checkPoints[currentChkPnt].SetActive(false);
        nextChkPnt = currentChkPnt + 1;

        checkPoints[nextChkPnt].SetActive(true);
        currentChkPnt += 1;
    }


    #endregion

    private void SplineInfoSetter()
    {
        trackSplineContainer = trackSplineObj.GetComponent<SplineContainer>();
        //Debug.Log(knotPoint.position);
        splineTrack = trackSplineContainer.AddSpline();
        splineTrack.Closed = true;

        knots = new List<BezierKnot>();
    }
    private void SplineKnotAdd(Transform addPoint)
    {
        newKnot.Position = addPoint.position;

        splineTrack.Closed = true;

        //splineTrack.Add(newKnot, TangentMode.AutoSmooth);

        //Debug.Log(newKnot.Rotation);
        //knotRotation = newKnot.Rotation;

        knots.Add(newKnot);
    }

    private void SplineKnotCuvre()
    {
        //splineTrack.SetTangentMode(TangentMode.Continuous);

        for (int i = 0; i < knots.Count; i++)
        {
            BezierKnot pointyKnot = knots[i];

            //splineTrack.Add(pointyKnot, mode: TangentMode.Mirrored);

            //if (i+1 < knots.Count)
            //{
            //    pointyKnot.Rotation = Quaternion.LookRotation(knots[i + 1].Position);
            //}

            //Debug.Log(pointyKnot.Rotation);

            //trackSplineContainer.RemoveSpline(splineTrack);

            //pointyKnot.TangentIn -= new float3(0f, 0f, 10f);
            //pointyKnot.TangentOut += new float3(0f, 0f, 10f);

            //Debug.Log(pointyKnot.Rotation);
            //Debug.Log(newKnot.TangentIn);

            splineTrack.Add(pointyKnot, mode: TangentMode.AutoSmooth);
            //splineTrack.SetTangentMode(i, mode: TangentMode.Mirrored, BezierTangent.Out);
            //splineTrack.SetTangentMode(i, mode: TangentMode.Mirrored, BezierTangent.In);

        }

    }



}





