using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Design", menuName = "Settings/Design")]
public class GameDesign : ScriptableObject
{
    [Tooltip("飞机前进的速度")]
    public float forwardSpeed = 100;
    [Tooltip("飞机切换轨道的时间")]
    public float wayChangeTime = 0.5f;
    [Tooltip("生成障碍物的三种方式")]
    public ObstacleType obstacleType = ObstacleType.FixedDoubleTrack;
    [Tooltip("障碍物生成在飞机前方的距离")]
    public float obstaclePosZOffset = 100;
}
