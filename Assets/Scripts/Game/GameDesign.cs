using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Design", menuName = "Settings/Design")]
public class GameDesign : ScriptableObject
{
    [Tooltip("�ɻ�ǰ�����ٶ�")]
    public float forwardSpeed = 100;
    [Tooltip("�ɻ��л������ʱ��")]
    public float wayChangeTime = 0.5f;
    [Tooltip("�����ϰ�������ַ�ʽ")]
    public ObstacleType obstacleType = ObstacleType.FixedDoubleTrack;
    [Tooltip("�ϰ��������ڷɻ�ǰ���ľ���")]
    public float obstaclePosZOffset = 100;
}
