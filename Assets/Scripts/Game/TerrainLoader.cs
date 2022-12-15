using Grass_1;
using SonicBloom.Koreo;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ObstacleType 
{
    FloatSingleTrack,
    FixedSingleTrack,
    FixedDoubleTrack,
}

public class TerrainLoader : Singleton<TerrainLoader>
{
    [Header("Terrain")]
    public Transform camTF;
    public float planeSize = 500;
    public Transform plane1;
    public Transform plane2;

    private Transform forwardPlane;
    private Transform backPlane;
    private int currentPlaneCount = 1;

    [Header("Obstacle")]
    public ObstacleType obstacleType = ObstacleType.FixedDoubleTrack;
    [EventID] public string eventID;
    [EventID] public string leftEventID;
    [EventID] public string midEventID;
    [EventID] public string rightEventID;
    public GameObject obstaclePrefab;
    public float obstaclePosX = 15;
    public float obstaclePosY = 3;
    public float obstaclePosZOffset = 3;

    private List<GameObject> obstacles = new List<GameObject>();

    [Header("Curve")]
    public bool reset = true;
    public List<Material> curveMaterials;
    public float curveValueX = 0;
    public float curveValueY = 0;

    private void OnValidate()
    {
        if (reset)
        {
            if (curveMaterials != null)
            {
                for (int i = 0; i < curveMaterials.Count; i++)
                {
                    curveMaterials[i].SetFloat("_CurveValueX", 0);
                    curveMaterials[i].SetFloat("_CurveValueY", 0);
                }
            }
            Shader.SetGlobalFloat("_CurveValueX", 0);
            Shader.SetGlobalFloat("_CurveValueY", 0);
        }
        else 
        {
            SetMaterialData();
        }
    }

    private void OnEnable()
    {
        if (Koreographer.Instance != null)
        {
            ObstacleType obstacleType = GameSettings.Instance.gameDesign.obstacleType;
            switch (obstacleType) 
            {
                case ObstacleType.FloatSingleTrack:
                    Koreographer.Instance.RegisterForEvents(eventID, CreateObstacleBeforePlayer);
                    break;
                case ObstacleType.FixedSingleTrack:
                case ObstacleType.FixedDoubleTrack:
                    Koreographer.Instance.RegisterForEvents(leftEventID, CreateLeftObstacle);
                    Koreographer.Instance.RegisterForEvents(midEventID, CreateMidObstacle);
                    Koreographer.Instance.RegisterForEvents(rightEventID, CreateRightObstacle);
                    break;
            }
        }
    }

    private void OnDisable()
    {
        if (Koreographer.Instance != null)
        {
            ObstacleType obstacleType = GameSettings.Instance.gameDesign.obstacleType;
            switch (obstacleType)
            {
                case ObstacleType.FloatSingleTrack:
                    Koreographer.Instance.UnregisterForEvents(eventID, CreateObstacleBeforePlayer);
                    break;
                case ObstacleType.FixedSingleTrack:
                case ObstacleType.FixedDoubleTrack:
                    Koreographer.Instance.UnregisterForEvents(leftEventID, CreateLeftObstacle);
                    Koreographer.Instance.UnregisterForEvents(midEventID, CreateMidObstacle);
                    Koreographer.Instance.UnregisterForEvents(rightEventID, CreateRightObstacle);
                    break;
            }
        }
    }

    private void Start()
    {
        forwardPlane = plane2;
        backPlane = plane1;

        SetMaterialData();
    }

    private void Update()
    {
        float z = camTF.position.z;
        if (z > planeSize * currentPlaneCount - planeSize * 0.5f) 
        {
            currentPlaneCount++;
            Swap();
        }

        for (int i = 0; i < obstacles.Count; i++) 
        {
            if (obstacles[i].transform.position.z < camTF.position.z) 
            {
                obstacles[i].SetActive(false);
            }
        }
    }

    private void Swap()
    {
        backPlane.position = forwardPlane.position + Vector3.forward * planeSize;

        Transform temp = forwardPlane;
        forwardPlane = backPlane;
        backPlane = temp;

        forwardPlane.GetComponent<GrassTerrain>().SetNewWorldOffset();
    }

    private void SetMaterialData() 
    {
        if (curveMaterials != null)
        {
            for (int i = 0; i < curveMaterials.Count; i++)
            {
                curveMaterials[i].SetFloat("_CurveValueX", curveValueX);
                curveMaterials[i].SetFloat("_CurveValueY", curveValueY);
            }
        }

        Shader.SetGlobalFloat("_CurveValueX", curveValueX);
        Shader.SetGlobalFloat("_CurveValueY", curveValueY);
    }

    private void CreateLeftObstacle(KoreographyEvent ke) 
    {
        if (obstacleType == ObstacleType.FixedSingleTrack)
        {
            CreateObstacle(ke, obstaclePosX * -1);
        }
        else 
        {
            CreateObstacle(ke, obstaclePosX * 0);
            CreateObstacle(ke, obstaclePosX * 1);
        }
    }

    private void CreateMidObstacle(KoreographyEvent ke)
    {
        if (obstacleType == ObstacleType.FixedSingleTrack)
        {
            CreateObstacle(ke, obstaclePosX * 0);
        }
        else
        {
            CreateObstacle(ke, obstaclePosX * 1);
            CreateObstacle(ke, obstaclePosX * -1);
        }
    }

    private void CreateRightObstacle(KoreographyEvent ke)
    {
        if (obstacleType == ObstacleType.FixedSingleTrack)
        {
            CreateObstacle(ke, obstaclePosX * 1);
        }
        else
        {
            CreateObstacle(ke, obstaclePosX * 0);
            CreateObstacle(ke, obstaclePosX * -1);
        }
    }

    private void CreateObstacleBeforePlayer(KoreographyEvent ke)
    {
        CreateObstacle(ke, Airplane.Instance.CurrentWay * obstaclePosX);
    }

    private void CreateObstacle(KoreographyEvent ke, float x) 
    {
        GameObject obstacle = null;
        for (int i = 0; i < obstacles.Count; i++)
        {
            if (!obstacles[i].activeInHierarchy)
            {
                obstacle = obstacles[i];
                obstacle.SetActive(true);
                break;
            }
        }
        if (obstacle == null) 
        {
            obstacle = Instantiate(obstaclePrefab, transform);
            obstacles.Add(obstacle);
        }
        float obstaclePosZOffset = GameSettings.Instance.gameDesign.obstaclePosZOffset;
        obstacle.transform.position = new Vector3(x, obstaclePosY, Airplane.Instance.Pos.z + obstaclePosZOffset);
    }
}
