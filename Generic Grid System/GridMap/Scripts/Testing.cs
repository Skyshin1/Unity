/* 
    ------------------- Code Monkey -------------------

    Thank you for downloading this package
    I hope you find it useful in your projects
    If you have any questions let me know
    Cheers!

               unitycodemonkey.com
    --------------------------------------------------
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;
using static UnityEngine.Rendering.DebugUI;

public class Testing : MonoBehaviour {

    [SerializeField] private HeatMapVisual heatMapVisual;
    [SerializeField] private HeatMapBoolVisual heatMapBoolVisual;
    [SerializeField] private HeatMapGenericVisual heatMapGenericVisual;
    private Grid<HeatMapGridObject> grid;
    private Grid<bool> boolGrid;
    private Grid<StringGridObject> stringGrid;

    private void Start() {
        grid = new Grid<HeatMapGridObject>(20, 10, 8f, Vector3.zero, (Grid<HeatMapGridObject> g, int x, int y) => new HeatMapGridObject(g, x, y));
        //boolGrid = new Grid<bool>(20, 10, 8f, Vector3.zero, (Grid<bool> g, int x, int y) => false);
        //stringGrid = new Grid<StringGridObject>(20, 10, 8f, Vector3.zero, (Grid<StringGridObject> g, int x, int y) => new StringGridObject(g, x, y));

        //heatMapVisual.SetGrid(grid);
        //heatMapBoolVisual.SetGrid(boolGrid);
        heatMapGenericVisual.SetGrid(grid);
    }

    private void Update() {
        Vector3 position = UtilsClass.GetMouseWorldPosition();

        if (Input.GetMouseButtonDown(0)) {
            //boolGrid.SetGridObject(position, true);
            HeatMapGridObject heatMapGridObject = grid.GetGridObject(position);
            if (heatMapGridObject != null) {
                heatMapGridObject.AddValue(5);
            }
        }

        /*
        if (Input.GetKeyDown(KeyCode.A)) { stringGrid.GetGridObject(position).AddLetter("A"); }
        if (Input.GetKeyDown(KeyCode.B)) { stringGrid.GetGridObject(position).AddLetter("B"); }
        if (Input.GetKeyDown(KeyCode.C)) { stringGrid.GetGridObject(position).AddLetter("C"); }

        if (Input.GetKeyDown(KeyCode.Alpha1)) { stringGrid.GetGridObject(position).AddNumber("1"); }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { stringGrid.GetGridObject(position).AddNumber("2"); }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { stringGrid.GetGridObject(position).AddNumber("3"); }
        */
    }
}

public class HeatMapGridObject {

    private const int MIN = 0;
    private const int MAX = 100;

    private Grid<HeatMapGridObject> grid;
    private int x;
    private int y;
    private int value;

    public HeatMapGridObject(Grid<HeatMapGridObject> grid, int x, int y) {
        this.grid = grid;
        this.x = x;
        this.y = y;
    }

    public void AddValue(int addValue) {
        value += addValue;
        value = Mathf.Clamp(value, MIN, MAX);
        grid.TriggerGridObjectChanged(x, y);
    }

    public float GetValueNormalized() {
        return (float)value / MAX;
    }
    //override 关键字用于重写（覆盖）了从 System.Object 类继承而来的 ToString 方法。
    //HeatMapGridObject 类的实例调用 ToString 方法时，得到的将是该实例的 value 属性的值的字符串形式，而不是默认的类型完全限定名
    public override string ToString() {
        return value.ToString();
    }

}

public class StringGridObject {
    
    private Grid<StringGridObject> grid;//为了调用TriggerGridObjectChanged函数，其会触发Grid值改变的事件
    private int x;
    private int y;

    private string letters;
    private string numbers;
    
    public StringGridObject(Grid<StringGridObject> grid, int x, int y) {
        this.grid = grid;
        this.x = x;
        this.y = y;
        letters = "";
        numbers = "";
    }

    public void AddLetter(string letter) {
        letters += letter;
        grid.TriggerGridObjectChanged(x, y);
    }

    public void AddNumber(string number) {
        numbers += number;
        grid.TriggerGridObjectChanged(x, y);
    }

    public override string ToString() {
        return letters + "\n" + numbers;
    }

}
