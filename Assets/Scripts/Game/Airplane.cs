using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class Airplane : Singleton<Airplane>
{
    public float forwardSpeed = 10;
    public Transform followCamTF;
    public Transform propellerTF;
    public float propellerRotateSpeed = 10;
    public float wayOffset = 15;
    public float wayChangeAngle = 15;
    public float wayChangeTime = 0.5f;
    public Vector2 shakeMoveRange = new Vector2(0.05f, 0.2f);
    public Vector2 shakeMoveTime = new Vector2(1, 2);

    [Header("Wind")]
    public Vector3 windOffset;
    public float windRaidus = 5;
    public float windStrength = 0.5f;

    private float camOffsetZ;
    private float currentWayPos = 0;
    private int currentWay = 0;
    private bool isMoveY;
    private bool isShakeMove;
    private int shakeMoveDir = 1;

    public int CurrentWay { get { return currentWay; } }
    public Vector3 Pos { get { return transform.position; } }

    // Start is called before the first frame update
    void Start()
    {
        camOffsetZ = followCamTF.position.z - transform.position.z;
    }

    // Update is called once per frame
    void Update()
    {
        Fly();
        ChangeWay();
    }

    private void OnTriggerEnter(Collider other)
    {
        string tag = other.gameObject.tag;
        switch (tag) 
        {
            case "Obstacle":
                Debug.Log("Obstacle");
                //SceneManager.LoadScene(0);
                break;
        }
    }

    private void Fly() 
    {
        float forwardSpeed = GameSettings.Instance.gameDesign.forwardSpeed;
        transform.Translate(transform.forward * forwardSpeed * Time.deltaTime);
        followCamTF.position = new Vector3(followCamTF.position.x, followCamTF.position.y, transform.position.z + camOffsetZ);
        propellerTF.Rotate(Vector3.forward * propellerRotateSpeed * Time.deltaTime, Space.World);
        if (!isMoveY && !isShakeMove) 
        {
            isShakeMove = true;
            shakeMoveDir = -shakeMoveDir;
            transform.DOMoveY(
                transform.position.y + shakeMoveDir * Random.Range(shakeMoveRange.x, shakeMoveRange.y),
                Random.Range(shakeMoveTime.x, shakeMoveTime.y)).OnComplete(() => isShakeMove = false);
        }

        Shader.SetGlobalVector("_PlayerPos", transform.position + windOffset);
        Shader.SetGlobalVector("_PlayerParams", new Vector4(windRaidus, windStrength, 0, 0));
    }

    private void ChangeWay() 
    {
        float wayChangeTime = GameSettings.Instance.gameDesign.wayChangeTime;
        if (Input.GetKeyDown(KeyCode.LeftArrow) && currentWay >= 0)
        {
            currentWayPos -= wayOffset;
            currentWay -= 1;
            transform.DORotate(Vector3.forward * wayChangeAngle, wayChangeTime * 0.75f).OnComplete(
                ()=> transform.DORotate(Vector3.zero, wayChangeTime * 0.25f));
            transform.DOMoveX(currentWayPos, wayChangeTime);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) && currentWay <= 0)
        {
            currentWayPos += wayOffset;
            currentWay += 1;
            transform.DORotate(Vector3.forward * -wayChangeAngle, wayChangeTime * 0.75f).OnComplete(
                () => transform.DORotate(Vector3.zero, wayChangeTime * 0.25f));
            transform.DOMoveX(currentWayPos, wayChangeTime);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position + windOffset, windRaidus);
    }
}
