using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Grass_1
{
    public struct GrassInfo
    {
        public Matrix4x4 localToWorld;
    }

    [ExecuteInEditMode]
    public class GrassTerrain : MonoBehaviour
    {
        /// <summary> ���д��ڼ���״̬�Ĳݵ� </summary>
        private static HashSet<GrassTerrain> actives = new HashSet<GrassTerrain>();

        /// <summary> ���д��ڼ���״̬�Ĳݵ� </summary>
        public static IReadOnlyCollection<GrassTerrain> Actives
        {
            get
            {
                return actives;
            }
        }

        /// <summary> �ڱ༭�������� </summary>
        public bool executeInEditMode;
        /// <summary> ������ </summary>
        [SerializeField] private Mesh grassMesh;
        /// <summary> �ݲ��� </summary>
        [SerializeField] private Material grassMaterial;
        /// <summary> �ݴ�С </summary>
        [SerializeField] private Vector2 grassSize = new Vector2(1, 1);
        /// <summary> ��������� </summary>
        [SerializeField] private int grassMaxCount = 1000000;
        /// <summary> ÿ1ƽ���ڲ����� </summary>
        [SerializeField] private int grassCountPerUnit = 10;

        /// <summary> ����ʵ����� </summary>
        private int seed;
        /// <summary> �ݵ�ʵ������ </summary>
        private ComputeBuffer grassBuffer;
        /// <summary> �ݵ����� </summary>
        private int grassCount;
        /// <summary> ��ǰ�ݲ��� </summary>
        private Material currentGrassMaterial;

        /// <summary> �ݵ����� </summary>
        public Mesh GrassMesh { get { return grassMesh; } }
        /// <summary> �ݵĲ��� </summary>
        public Material GrassMaterial
        {
            get 
            { 
                if (!currentGrassMaterial || currentGrassMaterial.shader != grassMaterial.shader) 
                    currentGrassMaterial = new Material(grassMaterial); 
                return currentGrassMaterial; 
            } 
        }
        /// <summary> �ݵ����� </summary>
        public int GrassCount { get { return grassCount; } }
        /// <summary> �ݵ�ʵ������ </summary>
        public ComputeBuffer GrassBuffer
        {
            get
            {
                if (grassBuffer != null)
                {
                    return grassBuffer;
                }

                var terrianMesh = GetComponent<MeshFilter>().sharedMesh;
                var grassIndex = 0;

                var grassInfos = new List<GrassInfo>();
                Random.InitState(seed);

                var indices = terrianMesh.triangles;
                var vertices = terrianMesh.vertices;
                var viewFace = Camera.main.transform.forward;

                for (var j = 0; j < indices.Length / 3; j++)
                {
                    var index1 = indices[j * 3];
                    var index2 = indices[j * 3 + 1];
                    var index3 = indices[j * 3 + 2];

                    var v1 = vertices[index1];
                    var v2 = vertices[index2];
                    var v3 = vertices[index3];

                    //��õ�����
                    var normal = GrassMathUtil.GetNormalOfTriangle(v1, v2, v3);
                    //����up���淨�����ת��Ԫ��
                    var upToNormal = Quaternion.FromToRotation(Vector3.up, normal);
                    //�������
                    var arena = GrassMathUtil.GetAreaOfTriangle(v1, v2, v3);
                    //�����ڸ��������У���Ҫ��ֲ������
                    var countPerTriangle = Mathf.Max(1, grassCountPerUnit * arena);
                    //���������ݻ���
                    for (var i = 0; i < countPerTriangle; i++)
                    {
                        var localPos = GrassMathUtil.GetRandomPointOfTriangle(v1, v2, v3);
                        var worldPos = transform.TransformPoint(localPos);
                        float rotateAngle = Random.Range(0, 360);
                        var localToWorld = Matrix4x4.TRS(worldPos, upToNormal * Quaternion.Euler(0, rotateAngle, 0), Vector3.one);

                        var grassInfo = new GrassInfo()
                        {
                            localToWorld = localToWorld,
                        };
                        grassInfos.Add(grassInfo);
                        grassIndex++;
                        if (grassIndex >= grassMaxCount)
                        {
                            break;
                        }
                    }
                    if (grassIndex >= grassMaxCount)
                    {
                        break;
                    }
                }
                grassCount = grassIndex;
                grassBuffer = new ComputeBuffer(grassCount, 64);
                grassBuffer.SetData(grassInfos);
                return grassBuffer;
            }
        }

        /// <summary> �ݵĲ������� </summary>
        private MaterialPropertyBlock materialPropertyBlock;
        // <summary> �ݵĲ������� </summary>
        public MaterialPropertyBlock MaterialPropertyBlock
        {
            get
            {
                if (materialPropertyBlock == null)
                {
                    materialPropertyBlock = new MaterialPropertyBlock();
                }
                return materialPropertyBlock;
            }
        }

        private Vector3 origionPos;

        private void OnValidate()
        {
            ForceUpdateGrassBuffer();
        }

        private void Awake()
        {
            seed = System.Guid.NewGuid().GetHashCode();
            origionPos = transform.position;
            ForceUpdateGrassBuffer();
        }

        private void OnEnable()
        {
            actives.Add(this);
        }

        private void OnDisable()
        {
            actives.Remove(this);
            if (grassBuffer != null)
            {
                grassBuffer.Dispose();
                grassBuffer = null;
            }
        }

        [ContextMenu("ForceUpdate")]
        public void ForceUpdateGrassBuffer()
        {
            if (grassBuffer != null)
            {
                grassBuffer.Dispose();
                grassBuffer = null;
            }
            SetMaterialProperty();
        }

        public void SetMaterialProperty() 
        {
            MaterialPropertyBlock.SetBuffer("_GrassInfos", GrassBuffer);
            MaterialPropertyBlock.SetVector("_GrassSize", grassSize);
        }

        public void SetNewWorldOffset() 
        {
            GrassMaterial.SetVector("_WorldPosOffset", transform.position - origionPos);
        }
    }
}

