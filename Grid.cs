using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;

public class Grid<TGridObject> {

    public const int HEAT_MAP_MAX_VALUE = 100;
    public const int HEAT_MAP_MIN_VALUE = 0;

    //发生“单元格的值改变”，用以通知热值更新
    public event EventHandler<OnGridValueChangedEventArgs> OnGridValueChanged;
    public class OnGridValueChangedEventArgs : EventArgs {
        public int x;
        public int y;
    }

    private int width;
    private int height;
    private float cellSize;//表示每个单元格的大小，用于确定网格在实际空间中的尺寸比例（在 Unity 的世界坐标体系中可能是米等单位）。
    private Vector3 originPosition;//代表网格在世界坐标中的起始位置，比如左下角顶点的位置，通过它和 cellSize、width、height 可以确定整个网格在世界坐标中的布局。
    private TGridObject[,] gridArray;//网络坐标xy对应的值

    public Grid(int width, int height, float cellSize, Vector3 originPosition) {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.originPosition = originPosition;

        gridArray = new int[width, height];

        bool showDebug = true;
        if (showDebug) {
            TextMesh[,] debugTextArray = new TextMesh[width, height];

            for (int x = 0; x < gridArray.GetLength(0); x++) {
                for (int y = 0; y < gridArray.GetLength(1); y++) {
                    debugTextArray[x, y] = UtilsClass.CreateWorldText(gridArray[x, y].ToString(), null, GetWorldPosition(x, y) + new Vector3(cellSize, cellSize) * .5f, 30, Color.white, TextAnchor.MiddleCenter);
                    Debug.DrawLine(GetWorldPosition(x, y), GetWorldPosition(x, y + 1), Color.white, 100f);
                    Debug.DrawLine(GetWorldPosition(x, y), GetWorldPosition(x + 1, y), Color.white, 100f);
                }
            }
            Debug.DrawLine(GetWorldPosition(0, height), GetWorldPosition(width, height), Color.white, 100f);
            Debug.DrawLine(GetWorldPosition(width, 0), GetWorldPosition(width, height), Color.white, 100f);

            OnGridValueChanged += (object sender, OnGridValueChangedEventArgs eventArgs) => {
                debugTextArray[eventArgs.x, eventArgs.y].text = gridArray[eventArgs.x, eventArgs.y].ToString();
            };
        }
    }

    public int GetWidth() {
        return width;
    }

    public int GetHeight() {
        return height;
    }

    public float GetCellSize() {
        return cellSize;
    }

    /// <summary>
    /// 根据传入的单元格在网格中的坐标x和y，计算并返回该单元格在世界坐标中的位置
    /// </summary>
    public Vector3 GetWorldPosition(int x, int y) {
        return new Vector3(x, y) * cellSize + originPosition;
    }

    //通过out返回多个值，也可通过返回二维向量做到这一点
    /// <summary>
    /// 将给定的世界坐标worldPosition转换为基于cellSize为单位的网格坐标
    /// </summary>
    private void GetXY(Vector3 worldPosition, out int x, out int y) {
        //先通过减去originPosition来消除起始位置的偏移量，然后分别将x和y方向上剩余的值除以cellSize并向下取整
        x = Mathf.FloorToInt((worldPosition - originPosition).x / cellSize);
        y = Mathf.FloorToInt((worldPosition - originPosition).y / cellSize);
    }

    /// <summary>
    /// 输入Cellsize为单位的坐标系下的坐标，设置网格中指定坐标（x和y）的单元格的值。
    /// </summary>
    public void SetValue(int x, int y, int value) {
        if (x >= 0 && y >= 0 && x < width && y < height) {
            gridArray[x, y] = Mathf.Clamp(value, HEAT_MAP_MIN_VALUE, HEAT_MAP_MAX_VALUE);
            if (OnGridValueChanged != null) OnGridValueChanged(this, new OnGridValueChangedEventArgs { x = x, y = y });
        }
    }

    /// <summary>
    /// 首先通过调用GetXY方法将世界坐标转换为基于Cellsize为单位的网格坐标，然后再调用前面的SetValue方法来设置对应单元格的值。
    /// </summary>
    public void SetValue(Vector3 worldPosition, int value) {
        int x, y;
        GetXY(worldPosition, out x, out y);
        SetValue(x, y, value);
    }
    /// <summary>
    /// 传入世界坐标，所属网格单元格的当前值基础上增加一个指定的值value
    /// </summary>
    /// <param name="x">要添加值的的单元格的网络x坐标</param>
    /// <param name="y">要添加值的的单元格的网络y坐标</param>
    /// <param name="value"></param>
    public void AddValue(int x, int y, int value) {
        SetValue(x, y, GetValue(x, y) + value);
    }

    /// <summary>
    /// 输入Cellsize为单位的坐标系下的坐标，获取网格单元格值的方法
    /// </summary>
    public int GetValue(int x, int y) {
        if (x >= 0 && y >= 0 && x < width && y < height) {
            return gridArray[x, y];
        } else {
            return 0;
        }
    }

    /// <summary>
    /// 先通过调用GetXY方法将其转换为网格坐标，然后再调用前面的GetValue方法获取对应的单元格的值。
    /// </summary>
    public int GetValue(Vector3 worldPosition) {
        int x, y;
        GetXY(worldPosition, out x, out y);
        return GetValue(x, y);
    }

    /// <summary>
    /// 这个方法用于根据给定的世界坐标worldPosition、一个值value、全值范围fullValueRange和总值范围totalRange，在一定范围内增加网格单元格的值。
    /// </summary>
    /// <param name="worldPosition">update中获取并传入过来鼠标按下时的世界空间位置，即在哪里作为热图的原点</param>
    /// <param name="value">单元格值的最大值 -100</param>
    /// <param name="fullValueRange">多大半径范围内的单元格为最大值 -5</param>
    /// <param name="totalRange">半径的总长 -40</param>
    public void AddValue(Vector3 worldPosition, int value, int fullValueRange, int totalRange) {
        //单位格值的衰减量，最大值/总半径 - 开始衰减的半径（值衰减的总长度）起到一种根据距离衰减增加值的作用。
        int lowerValueAmount = Mathf.RoundToInt((float)value / (totalRange - fullValueRange));
        //先将热图生成的原点的世界坐标转化为网络坐标
        GetXY(worldPosition, out int originX, out int originY);
        //这样的循环设置使得每次内层循环的范围会随着外层循环的 x 值增加而逐渐缩小，整体上遍历的区域呈现出一个三角形的形状（右上三角部分）
        for (int x = 0; x < totalRange; x++) {  
            for (int y = 0; y < totalRange - x; y++) {
                int radius = x + y;
                int addValueAmount = value;
                if (radius >= fullValueRange) {
                    addValueAmount -= lowerValueAmount * (radius - fullValueRange);//这样就实现了距离中心越远，增加值越小的效果，模拟了一种值的衰减机制。
                }
                //右上三角
                AddValue(originX + x, originY + y, addValueAmount);
                //中间重合部分不加值
                if (x != 0) {
                    //左上三角
                    AddValue(originX - x, originY + y, addValueAmount);
                }
                if (y != 0) {
                    //右下三角
                    AddValue(originX + x, originY - y, addValueAmount);
                    if (x != 0) {
                        //左下三角，。当结合后续对其他象限（左上、左下、右下）的处理时，就能覆盖到一个类似菱形的区域。
                        AddValue(originX - x, originY - y, addValueAmount);
                    }
                }
            }
        }
    }



}
