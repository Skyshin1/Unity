using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeatMapVisual : MonoBehaviour {
    private Grid grid;
    private Mesh mesh;//通过这个网格来显示热图
    private bool updateMesh;//是否需要更新网格数据以反映热图的变化。

    private void Awake() {
            mesh = new Mesh();
            GetComponent<MeshFilter>().mesh = mesh;//获取当前游戏对象上挂载的MeshFilter组件
    }
    /// <summary>
    /// 用于设置热图基于的Grid类
    /// </summary>
    /// //函数接收一个Grid类型的参数，将其赋值给类中的grid成员变量，以便后续在热图可视化中使用该网格的数据。
    public void SetGrid(Grid grid) {
        this.grid = grid;
        //立即根据新设置的网格数据来更新热图的可视化效果
        UpdateHeatMapVisual();

        grid.OnGridValueChanged += Grid_OnGridValueChanged;
    }

    private void Grid_OnGridValueChanged(object sender, Grid.OnGridValueChangedEventArgs e) {
        //UpdateHeatMapVisual();
        updateMesh = true;
    }

    private void LateUpdate() {
        //由于Grid.Addvalue每帧都会调用，每帧又会触发4次onGridValuechanged事件，所以如果1s有120帧，就要调用UpdateHeatMapVisual480次，
        //所以如果触发该事件，只在最后一帧更新updateHeatMapVisual即可
        if (updateMesh) {
            updateMesh = false;
            UpdateHeatMapVisual();
        }
    }

    private void UpdateHeatMapVisual() {
        //根据网格的宽度和高度（通过grid.GetWidth()和grid.GetHeight()获取）创建用于存储网格顶点、UV 坐标和三角形索引的空数组，并通过out参数返回这些数组。
        MeshUtils.CreateEmptyMeshArrays(grid.GetWidth() * grid.GetHeight(), out Vector3[] vertices, out Vector2[] uv, out int[] triangles);

        //然后通过两层嵌套的循环遍历网格的每一个单元格（由x和y坐标表示），计算当前单元格在整个网格中的索引index
        for (int x = 0; x < grid.GetWidth(); x++) {
            for (int y = 0; y < grid.GetHeight(); y++) {
                int index = x * grid.GetHeight() + y;//y不用乘因为是现在是y在循环，可以列举所有值(固定一个坐标，改变另一个坐标的迭代方法）
                Vector3 quadSize = new Vector3(1, 1) * grid.GetCellSize();
                //获取当前单元格的数值gridValue

                //当Grid.cs“单元格值改变”事件发生时，调用的响应函数中包含UpdateHeatMapVisual(),此部分用以将单元格更新后的值重新与uv贴图对应
                int gridValue = grid.GetValue(x, y);
                //并将其归一化到 0 到 1 之间（通过除以Grid.HEAT_MAP_MAX_VALUE）得到gridValueNormalized，然后创建一个对应的Vector2类型的UV坐标gridValueUV。
                float gridValueNormalized = (float)gridValue / Grid.HEAT_MAP_MAX_VALUE;
                Vector2 gridValueUV = new Vector2(gridValueNormalized, 0f);//因为uv是只在x轴方向有颜色变化的图
                //最后调用MeshUtils.AddToMeshArrays函数，将根据当前单元格的数据计算得到的顶点位置、UV 坐标等信息添加到相应的数组（vertices、uv和triangles）中。
                //新渲染的单元格(quard)是根据中心点渲染在目标mesh的原点处，所以会有偏移，所以向右平移 quadSize * .5f，如果uv的00和11是相同的值即下方所示，则每个单元格内颜色不变
                MeshUtils.AddToMeshArrays(vertices, uv, triangles, index, grid.GetWorldPosition(x, y) + quadSize * .5f, 0f, quadSize, gridValueUV, gridValueUV);
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
    }

}
