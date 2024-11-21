using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshUtils {
    
    private static readonly Vector3 Vector3zero = Vector3.zero;
    private static readonly Vector3 Vector3one = Vector3.one;
    private static readonly Vector3 Vector3yDown = new Vector3(0,-1);

    
    private static Quaternion[] cachedQuaternionEulerArr;

    //缓存欧拉角(0, 0, i)（其中i从 0 到 359）表示的旋转对应的四元数。如果cachedQuaternionEulerArr已经被初始化（不为null）
	//则直接返回，避免重复初始化。否则，创建一个长度为 360 的Quaternion数组，并通过循环为每个元素设置对应的四元数。
    private static void CacheQuaternionEuler() {
        if (cachedQuaternionEulerArr != null) return;
        cachedQuaternionEulerArr = new Quaternion[360];
        for (int i=0; i<360; i++) {
            cachedQuaternionEulerArr[i] = Quaternion.Euler(0,0,i);
        }
    }


    /// <summary>
    /// 输入欧拉角（内部会保证输入的是0-359间的角度），返回对应的四元数。
    /// </summary>
    private static Quaternion GetQuaternionEuler(float rotFloat) {
        //首先将浮点数四舍五入为整数rot，然后确保rot的值在 0 到 359 之间（通过取模和处理负数的情况）。
        //如果cachedQuaternionEulerArr还未初始化，就调用CacheQuaternionEuler方法进行初始化。
        int rot = Mathf.RoundToInt(rotFloat);
        rot = rot % 360;
        if (rot < 0) rot += 360;
        //if (rot >= 360) rot -= 360;
        if (cachedQuaternionEulerArr == null) CacheQuaternionEuler();
        return cachedQuaternionEulerArr[rot];
    }

    /// <summary>
    /// 创建一个空的Mesh对象。它初始化了一个新的Mesh实例，并将其顶点数组、UV 坐标数组和三角形索引数组都设置为空数组然后返回这个空的Mesh对象。
    /// </summary>
    public static Mesh CreateEmptyMesh() {
        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[0];
        mesh.uv = new Vector2[0];
        mesh.triangles = new int[0];
        return mesh;
    }



    /// <summary>
    /// 输入网格的四边形(网格)数量quadCount，创建用于存储网格数据的数组
    /// </summary>
    //分别创建了顶点数组vertices（每个四边形有 4 个顶点，所以长度为4 * quadCount）、UV 坐标数组uvs（同样与四边形数量相关，长度为4 * quadCount）
    //和三角形索引数组triangles（每个四边形由 2 个三角形组成，每个三角形有 3 个索引，所以长度为6 * quadCount）。通过out参数将这些数组返回给调用者。
    public static void CreateEmptyMeshArrays(int quadCount, out Vector3[] vertices, out Vector2[] uvs, out int[] triangles) {
		vertices = new Vector3[4 * quadCount];
		uvs = new Vector2[4 * quadCount];
		triangles = new int[6 * quadCount];
    }

    /// <summary>
    /// 这个公共静态方法用于创建一个具有特定位置、旋转、尺寸和 UV 坐标的Mesh对象。它实际上是调用了AddToMesh方法，并传入null作为第一个参数表示要创建一个新的Mesh，而不是在已有的Mesh基础上添加。
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="rot"></param>
    /// <param name="baseSize"></param>
    /// <param name="uv00"></param>
    /// <param name="uv11"></param>
    /// <returns></returns>
    public static Mesh CreateMesh(Vector3 pos, float rot, Vector3 baseSize, Vector2 uv00, Vector2 uv11) {
        return AddToMesh(null, pos, rot, baseSize, uv00, uv11);
    }

    /// <summary>
    /// 向已有的Mesh对象添加数据
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="pos"></param>
    /// <param name="rot"></param>
    /// <param name="baseSize"></param>
    /// <param name="uv00"></param>
    /// <param name="uv11"></param>
    /// <returns></returns>
    public static Mesh AddToMesh(Mesh mesh, Vector3 pos, float rot, Vector3 baseSize, Vector2 uv00, Vector2 uv11) {
        //如果传入的mesh为null，则先创建一个空的Mesh对象。
        if (mesh == null) {
            mesh = CreateEmptyMesh();
        }
        //首先创建了新的顶点数组、UV 坐标数组和三角形索引数组，其长度分别比传入的mesh的对应数组长度多 4（用于添加新的四边形数据）
		//多 4 和多 6（因为每个四边形由 2 个三角形组成，每个三角形有 3 个索引）。
        Vector3[] vertices = new Vector3[4 + mesh.vertices.Length];
		Vector2[] uvs = new Vector2[4 + mesh.uv.Length];
		int[] triangles = new int[6 + mesh.triangles.Length];

        //然后将传入的mesh的现有数据复制到新创建的数组中。
        mesh.vertices.CopyTo(vertices, 0);
        mesh.uv.CopyTo(uvs, 0);
        mesh.triangles.CopyTo(triangles, 0);

        //通过计算得到要添加的四边形在新数组中的索引index，并根据这个索引确定顶点、UV 坐标和三角形索引的具体位置（如vIndex等）。
        int index = vertices.Length / 4 - 1;
		//Relocate vertices
		int vIndex = index*4;
		int vIndex0 = vIndex;//代表了当前要构建的四边形单元的第一个顶点在顶点数组中的索引。
        int vIndex1 = vIndex+1;//代表了当前四边形单元的第二个顶点在顶点数组中的索引。
        int vIndex2 = vIndex+2;//...
		int vIndex3 = vIndex+3;

        //乘以 0.5 可以确保四边形的大小在不同的计算方式（倾斜和非倾斜）下保持一致。如果不乘以 0.5，四边形的实际尺寸可能会比预期的大一倍，导致图形渲染出现不符合预期的大小和比例。
        baseSize *= .5f;

        //根据baseSize的是否倾斜（x和y分量是否相等）来分别计算新四边形的顶点位置，通过调用GetQuaternionEuler方法获取合适的旋转四元数来进行位置变换。
        bool skewed = baseSize.x != baseSize.y;
        if (skewed) {
			vertices[vIndex0] = pos+GetQuaternionEuler(rot)*new Vector3(-baseSize.x,  baseSize.y);
			vertices[vIndex1] = pos+GetQuaternionEuler(rot)*new Vector3(-baseSize.x, -baseSize.y);
			vertices[vIndex2] = pos+GetQuaternionEuler(rot)*new Vector3( baseSize.x, -baseSize.y);
			vertices[vIndex3] = pos+GetQuaternionEuler(rot)*baseSize;
		} else {
			vertices[vIndex0] = pos+GetQuaternionEuler(rot-270)*baseSize;
			vertices[vIndex1] = pos+GetQuaternionEuler(rot-180)*baseSize;
			vertices[vIndex2] = pos+GetQuaternionEuler(rot- 90)*baseSize;
			vertices[vIndex3] = pos+GetQuaternionEuler(rot-  0)*baseSize;
		}

        //Relocate UVs 同样地，设置新四边形的 UV 坐标
        uvs[vIndex0] = new Vector2(uv00.x, uv11.y);
		uvs[vIndex1] = new Vector2(uv00.x, uv00.y);
		uvs[vIndex2] = new Vector2(uv11.x, uv00.y);
		uvs[vIndex3] = new Vector2(uv11.x, uv11.y);
		
		//Create triangles
		int tIndex = index*6;

        //最后创建三角形索引，将新的顶点、UV 坐标和三角形索引数组赋值给传入的mesh对象，并返回这个更新后的mesh
        triangles[tIndex+0] = vIndex0;
		triangles[tIndex+1] = vIndex3;
		triangles[tIndex+2] = vIndex1;
		
		triangles[tIndex+3] = vIndex1;
		triangles[tIndex+4] = vIndex3;
		triangles[tIndex+5] = vIndex2;
            
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.uv = uvs;

        //mesh.bounds = bounds;

        return mesh;
    }

    /// <summary>
    /// 直接向给定的顶点数组、UV 坐标数组和三角形索引数组中添加一个单元格的数据。
    /// </summary>
    /// <param name="vertices">要添加新数据的存储顶点数组</param>
    /// <param name="uvs">要添加新数据的UV数组</param>
    /// <param name="triangles">要添加新数据的存储三角形索引的数组</param>
    /// <param name="index">新添加的单元格的索引</param>
    /// <param name="pos">新添加的单元格的网络坐标</param>
    /// <param name="rot">新添加单元格的旋转数据</param>
    /// <param name="baseSize">新添加单元格的大小，注意是三维向量的形式</param>
    /// <param name="uv00">作为单元格的贴图的uv坐标的（0，0）点</param>
    /// <param name="uv11">作为单元格的贴图的uv坐标的（1，1）点</param>
    public static void AddToMeshArrays(Vector3[] vertices, Vector2[] uvs, int[] triangles, int index, Vector3 pos, float rot, Vector3 baseSize, Vector2 uv00, Vector2 uv11) {
        //它的操作过程与AddToMesh方法中添加四边形数据到Mesh对象的过程类似，
    	//包括根据baseSize的情况计算顶点位置、设置 UV 坐标和创建三角形索引等操作，只是这里是直接操作传入的数组，而不是通过Mesh对象来间接操作。
        //Relocate vertices
		int vIndex = index*4;
		int vIndex0 = vIndex;
		int vIndex1 = vIndex+1;
		int vIndex2 = vIndex+2;
		int vIndex3 = vIndex+3;

       
        baseSize *= .5f;

        bool skewed = baseSize.x != baseSize.y;
        if (skewed) {
			vertices[vIndex0] = pos+GetQuaternionEuler(rot)*new Vector3(-baseSize.x,  baseSize.y);
			vertices[vIndex1] = pos+GetQuaternionEuler(rot)*new Vector3(-baseSize.x, -baseSize.y);
			vertices[vIndex2] = pos+GetQuaternionEuler(rot)*new Vector3( baseSize.x, -baseSize.y);
			vertices[vIndex3] = pos+GetQuaternionEuler(rot)*baseSize;
		} else {
			vertices[vIndex0] = pos+GetQuaternionEuler(rot-270)*baseSize;
			vertices[vIndex1] = pos+GetQuaternionEuler(rot-180)*baseSize;
			vertices[vIndex2] = pos+GetQuaternionEuler(rot- 90)*baseSize;
			vertices[vIndex3] = pos+GetQuaternionEuler(rot-  0)*baseSize;
		}
		
		//Relocate UVs
		uvs[vIndex0] = new Vector2(uv00.x, uv11.y);
		uvs[vIndex1] = new Vector2(uv00.x, uv00.y);
		uvs[vIndex2] = new Vector2(uv11.x, uv00.y);
		uvs[vIndex3] = new Vector2(uv11.x, uv11.y);
		
		//Create triangles
		int tIndex = index*6;
		
		triangles[tIndex+0] = vIndex0;
		triangles[tIndex+1] = vIndex3;
		triangles[tIndex+2] = vIndex1;
		
		triangles[tIndex+3] = vIndex1;
		triangles[tIndex+4] = vIndex3;
		triangles[tIndex+5] = vIndex2;
    }
}
