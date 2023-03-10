using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


class CameraData
{
    public List<string> intrinsics;
    public List<string> extrinsics;

    public struct Data { public string intrinsics; public string extrinsics; }

    public CameraData()
    {
        intrinsics = new List<string>();
        extrinsics = new List<string>();
    }

    float3x3 GetIntrinsics(Camera cam)
    {
        float pixel_aspect_ratio = (float)cam.pixelWidth / (float)cam.pixelHeight;

        float alpha_u = cam.focalLength * ((float)cam.pixelWidth / cam.sensorSize.x);
        float alpha_v = cam.focalLength * pixel_aspect_ratio * ((float)cam.pixelHeight / cam.sensorSize.y);

        float u_0 = (float)cam.pixelWidth / 2;
        float v_0 = (float)cam.pixelHeight / 2;

        //IntrinsicMatrix in row major
        float3x3 camIntriMatrix = new float3x3(new float3(alpha_u, 0f, u_0),
                                               new float3(0f, alpha_v, v_0),
                                               new float3(0f, 0f, 1f));
        return camIntriMatrix;
    }

    public void AddCamera(Camera camera, Transform agent)
    {
        float3x3 i = GetIntrinsics(camera);
        Matrix4x4 e = Matrix4x4.TRS(camera.transform.localPosition, agent.localRotation, camera.transform.localScale);

        intrinsics.Add(string.Format("[[{0}, {1}, {2}], [{3}, {4}, {5}], [{6}, {7}, {8}]]",
            i.c0.x, i.c0.y, i.c0.z,
            i.c1.x, i.c1.y, i.c1.z,
            i.c2.x, i.c2.y, i.c2.z));

        extrinsics.Add(string.Format("[[{0}, {1}, {2}, {3}], [{4}, {5}, {6}, {7}], [{8}, {9}, {10}, {11}], [{12}, {13}, {14}, {15}]]",
            e.m00, e.m01, e.m02, e.m03,
            e.m10, e.m11, e.m12, e.m13,
            e.m20, e.m21, e.m22, e.m23,
            e.m30, e.m31, e.m32, e.m33));
    }

    public Data Objectify()
    {
        Data data = new Data();
        data.intrinsics = "[";
        data.extrinsics = "[";

        for (int i = 0; i < intrinsics.Count; i++)
        {
            if (i > 0)
            {
                data.intrinsics += ", ";
                data.extrinsics += ", ";
            }

            data.intrinsics += intrinsics[i];
            data.extrinsics += extrinsics[i];
        }

        data.intrinsics += "]";
        data.extrinsics += "]";

        return data;
    }
}

