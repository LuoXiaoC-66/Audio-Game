using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Grass_1
{
    public class GrassMathUtil
    {
        /// <summary> �����������һ�� </summary>
        public static Vector3 GetRandomPointOfTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            // ��ʵ��ƽ���ı���������һ�㣬����y > 1 - x��Ҫ��תһ��
            var x = Random.Range(0, 1f);
            var y = Random.Range(0, 1f);
            if (y > 1 - x)
            {
                var temp = y;
                y = 1 - x;
                x = 1 - temp;
            }
            var vx = p2 - p1;
            var vy = p3 - p1;
            return p1 + x * vx + y * vy;
        }

        /// <summary> ��������� </summary>
        public static float GetAreaOfTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            var vx = p2 - p1;
            var vy = p3 - p1;
            var dotvxy = Vector3.Dot(vx, vy);
            var sqrArea = vx.sqrMagnitude * vy.sqrMagnitude - dotvxy * dotvxy;
            return 0.5f * Mathf.Sqrt(sqrArea);
        }

        /// <summary> �����η��� </summary>
        public static Vector3 GetNormalOfTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            var vx = p2 - p1;
            var vy = p3 - p1;
            return Vector3.Cross(vx, vy);
        }
    }
}