using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Splines;
using Protobot;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class ChainGenerator : MonoBehaviour {
    public GameObject Panel; //sets Chain Generator Display ui
    public Image buttonImage1; // Reference to the button's Image component
    public Image buttonImage2; // Reference to the button's Image component
    public Color selectedColor = Color.green; // Color to use for the selected state
    public Color defaultColor = Color.white; // Default color for the button
    
    //declaring variables
    static public float Point1x = -10.5f;//a (This should be the first sprockets X)
    static public float Point1y = 5.76f;//b (This should be the first sprockets Y)
    static public float Point1z = 0;//not used in original math, just incase
    static public float Size1 = 2f;//R1 (This should be the first sprockets radius)
    static public float Point2x = 3.78f;//c (This should be the second sprockets X)
    static public float Point2y = -0.3f;//d (This should be the second sprockets Y)
    static public float Point2z = 0;//not used in original math, just incase
    static public float Size2 = 1f;//R2 (This should be the second sprockets radius)
    static public float Distance = 0f; //D
    static public float Size3 = Size1 - Size2; //calculated C3
    [SerializeField] private List<GameObject> disabledObjects = new List<GameObject>();
    static public float Hline(float Distance, float Size1, float Size2)//H
    {
        return Mathf.Sqrt(Distance * Distance - Mathf.Pow(Size1 - Size2, 2));//sqrt(D^2 - (R1 - R2)^2)
    }
    public static float Yline(float Hline, float Size2)//Y
    {
        return Mathf.Sqrt(Hline * Hline + Size2 * Size2);//sqrt(H^2 + R2^2)
    }
    public static float Theta1(float Size1, float Distance, float Yline)//theta1
    {
        return Mathf.Acos((Size1 * Size1 + Distance * Distance - Yline * Yline) / (2 * Size1 * Distance));// arrcos((R1^2 + D^2 - Y^2) / (2 * R1 * D))
    }
    public static float Ypoint1(float Point2y, float Point1y)//y1
    {
        return Point2y - Point1y;//d - b
    }
    public static float Xpoint1(float Point2x, float Point1x)//x1
    {
        return Point2x - Point1x; //c - a
    }
    public static float Theta2(float Size1, float Distance, float Yline, float Ypoint1, float Xpoint1)//theta2
    {
        float theta1 = Theta1(Size1, Distance, Yline);
        return theta1 + Mathf.Atan2(Ypoint1, Xpoint1);//Theta1 + arctan(y1, x1)
    }
    public static float Upoint1(float Point1x, float Size1, float Theta2)//u1
    {
        return Point1x + Size1 * Mathf.Cos(Theta2);//a + R1 * cos(Theta2)
    }
    public static float Ipoint1(float Point1y, float Size1, float Theta2)//i1
    {
        return Point1y + Size1 * Mathf.Sin(Theta2);//b + R1 * sin(Theta2)
    }
    public static float Upoint5(float Point1x, float Size3, float Theta2)//u5
    {
        return Point1x + Size3 * Mathf.Cos(Theta2);//a + R3 * cos(Theta2)
    }
    public static float Ipoint5(float Point1y, float Size3, float Theta2)//i5
    {
        return Point1y + Size3 * Mathf.Sin(Theta2);//b + R3 * sin(Theta2)
    }
    public static float Upoint2(float Point2x, float Upoint1, float Upoint5)//u2
    {
        return Point2x + Upoint1 - Upoint5;//c + u1 - u5
    }
    public static float Ipoint2(float Point2y, float Ipoint1, float Ipoint5)//i2
    {
        return Point2y + Ipoint1 - Ipoint5;//d + i1 - i5
    }
    public static float Theta3(float Theta1, float Ypoint1, float Xpoint1)//theta3
    {
        return Theta1 + Mathf.Atan2(-Ypoint1, Xpoint1);//Theta1 + arctan(-y1, x1)
    }
    public static float Upoint6(float Point1x, float Size3, float Theta3)//u6
    {
        return Point1x + Size3 * Mathf.Cos(Theta3);//a + R3 * cos(Theta3)
    }
    public static float Ipoint6(float Point1y, float Size3, float Theta3)//i6
    {
        return Point1y - Size3 * Mathf.Sin(Theta3);//b - R3 * sin(Theta3)
    }
    public static float Upoint3(float Point1x, float Size1, float Theta3)//u3
    {
        return Point1x + Size1 * Mathf.Cos(Theta3); //a + R1 * cos(Theta3)
    }
    public static float Ipoint3(float Point1y, float Size1, float Theta3)//i3
    {
        return Point1y - Size1 * Mathf.Sin(Theta3); //b - R1 * sin(Theta3)
    }
    public static float Upoint4(float Point2x, float Point1x, float Size1, float Point1y, float Size3, float Theta3)//u4
    {
        float upoint3 = Upoint3(Point1x, Size1, Theta3);
        float upoint6 = Upoint6(Point1y, Size3, Theta3);
        return Point2x + upoint3 - upoint6; //c + u3 - u6 (var according to desmos calculation)
    }
    public static float Ipoint4(float Point2y, float Point1x, float Size1, float Point1y, float Size3, float Theta3)//i4
    {
        float ipoint3 = Ipoint3(Point1x, Size1, Theta3);
        float ipoint6 = Ipoint6(Point1y, Size3, Theta3);
        return Point2y + ipoint3 - ipoint6; //d + i3 - i6 (var according to desmos calculation)
    }


    void Start()
    {
        Panel.gameObject.SetActive (false);
    }

    private bool isSelectingSprocket = false;
    private bool isFirstSprocketSelected = false;
    private GameObject firstSprocket;

    public void OnToggle(){ //everything inside this is activated once button is pressed
        //some where in this script should be the sprocket selection, should happen after the button is pressed.

        //this for loop disables all objects that are not sprockets temporaliy so that users can easily access them
        //look at the function `CancelToolUi` for a demonstration on how to re-enable all objects
        // Reset the button's color
        buttonImage1.color = defaultColor;
        buttonImage2.color = defaultColor; 

        var savedObjects = GameObject.FindObjectsOfType<SavedObject>();
        var isOn = gameObject.GetComponentInParent<Toggle>().isOn;
        if (isOn)
        {
            isSelectingSprocket = true;
            StartCoroutine(SelectSprocket());

            for (int i = 0; i < savedObjects.Length; i++)
            {
                if (!savedObjects[i].id.Contains("SPKT"))
                {
                    savedObjects[i].gameObject.SetActive(false);
                    disabledObjects.Add(savedObjects[i].gameObject);
                    Panel.gameObject.SetActive (true);
                }
            }
        }
        else
        {
            isSelectingSprocket = false;
            isFirstSprocketSelected = false;

            for (int i = 0; i < disabledObjects.Count; i++)
            {
                disabledObjects[i].SetActive(true);
                Panel.gameObject.SetActive (false);
            }
            disabledObjects.Clear();
        }
        
    }

    private IEnumerator SelectSprocket() { // This coroutine is responsible for selecting a game object in the scene
        while (isSelectingSprocket) {
            if (Mouse.current.leftButton.isPressed) { // Check if the left mouse button is pressed
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());// Create a ray from the camera through the mouse position
                RaycastHit hit;

                // Create a layer mask that only includes the "Default" layer
                // This ensures that we only hit game objects on this layer and ignore UI elements and other layers
                int layerMask = 1 << LayerMask.NameToLayer("Default");

                // Perform a raycast from the camera through the mouse position, only hitting objects on the "Default" layer
                if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask)) {
                    GameObject selectedObject = hit.transform.gameObject;// Get the game object that was hit by the raycast
                    Transform selectedTransform = selectedObject.transform;
                    
                    // Check if the object has a SavedObject component with an id of "SPKT"
                    SavedObject savedObject = selectedObject.GetComponent<SavedObject>();
                    if (savedObject != null && savedObject.id.Contains("SPKT")) {
                        if (!isFirstSprocketSelected) { // First sprocket selection
                            isFirstSprocketSelected = true;
                            firstSprocket = selectedObject;

                            // Log some information about the selected object
                            Debug.Log("Selected First Sprocket: " + selectedObject.name);
                            Debug.Log("X: " + selectedTransform.position.x);
                            Debug.Log("Y: " + selectedTransform.position.y);
                            Debug.Log("Z: " + selectedTransform.position.z);

                            // Store the selected object's position in the Point1 variables
                            Point1x = selectedTransform.position.x;
                            Point1y = selectedTransform.position.y;
                            Point1z = selectedTransform.position.z;

                            // Change the button's color to green
                            buttonImage1.color = selectedColor;
                        }
                        else if (selectedObject != firstSprocket) {
                            // Second sprocket selection
                            Debug.Log("Selected Second Sprocket: " + selectedObject.name);
                            Debug.Log("X: " + selectedTransform.position.x);
                            Debug.Log("Y: " + selectedTransform.position.y);
                            Debug.Log("Z: " + selectedTransform.position.z);

                            // Store the selected object's position in the Point2 variables
                            Point2x = selectedTransform.position.x;
                            Point2y = selectedTransform.position.y;
                            Point2z = selectedTransform.position.z;

                            // Change the button's color to green
                            buttonImage2.color = selectedColor;

                            // Stop the coroutine since we've selected an object
                            isFirstSprocketSelected = false;
                            isSelectingSprocket = false;

                            // Generate the chain
                            GenerateChain();
                        }
                    }
                }
            }

            yield return null;
        }
    }



    //this function generates all the chains and calculations. (will be called once both sprockets are selected)
   public void GenerateChain()
   {
        //generates empty game objects to be used as calculations
        GameObject ChainContainer = new GameObject(); //Creates game object named ChainContainer
        ChainContainer.name = "Chain Container"; //names object whatever is in Quotations
        ChainContainer.gameObject.transform.Translate(0, 0, 0);

        GameObject ChainPoint1 = new GameObject();
        ChainPoint1.name = "ChainPoint1";
        ChainPoint1.gameObject.transform.Translate(Point1x, Point1y, 0);

        GameObject ChainPoint2 = new GameObject();
        ChainPoint2.name = "ChainPoint2";
        ChainPoint2.gameObject.transform.Translate(Point2x, Point2y, 0);

        //calculating all the variables
        float Distance = Vector3.Distance(ChainPoint1.transform.position, ChainPoint2.transform.position); //finds distance between two points
        float hline = Hline(Distance, Size1, Size2);//H
        float yline = Yline(Hline(Distance, Size1, Size2), Size2);//Y
        float theta1 = Theta1(Size1, Distance, Yline(Hline(Distance, Size1, Size2), Size2));//theta1
        float ypoint1 = Ypoint1(Point2y, Point1y);//x1
        float xpoint1 = Xpoint1(Point2x, Point1x);//y1
        float theta2 = Theta2(Size1, Distance, Yline(Hline(Distance, Size1, Size2), Size2), Ypoint1(Point2y, Point1y), Xpoint1(Point2x, Point1x));//theta2
        float upoint1 = Upoint1(Point1x, Size1, theta2); //X position of first tangent, u1
        float ipoint1 = Ipoint1(Point1y, Size1, theta2); //Y position of first tangent, i1
        float upoint5 = Upoint5(Point1x, Size3, theta2); //u5
        float ipoint5 = Ipoint5(Point1y, Size3, theta2); //i5
        float upoint2 = Upoint2(Point2x, upoint1, upoint5); //X poisiton of second tangent, u2
        float ipoint2 = Ipoint2(Point2y, ipoint1, ipoint5); //Y position of second tangent, i2
        float theta3 = Theta3(Theta1(Size1, Distance, Yline(Hline(Distance, Size1, Size2), Size2)), Ypoint1(Point2y, Point1y), Xpoint1(Point2x, Point1x));//theta3
        float upoint6 = Upoint6(Point1x, Size3, Theta3(Theta1(Size1, Distance, Yline(Hline(Distance, Size1, Size2), Size2)), Ypoint1(Point2y, Point1y), Xpoint1(Point2x, Point1x)));//u6
        float ipoint6 = Ipoint6(Point1y, Size3, Theta3(Theta1(Size1, Distance, Yline(Hline(Distance, Size1, Size2), Size2)), Ypoint1(Point2y, Point1y), Xpoint1(Point2x, Point1x)));//i6
        float upoint3 = Upoint3(Point1x, Size1, Theta3(Theta1(Size1, Distance, Yline(Hline(Distance, Size1, Size2), Size2)), Ypoint1(Point2y, Point1y), Xpoint1(Point2x, Point1x))); //X position of third tangent, u3
        float ipoint3 = Ipoint3(Point1y, Size1, Theta3(Theta1(Size1, Distance, Yline(Hline(Distance, Size1, Size2), Size2)), Ypoint1(Point2y, Point1y), Xpoint1(Point2x, Point1x))); //Y position of third tangent, i3
        float upoint4 = Upoint4(Point2x, Point1x, Size1, Point1y, Size3, Theta3(Theta1(Size1, Distance, Yline(Hline(Distance, Size1, Size2), Size2)), Ypoint1(Point2y, Point1y), Xpoint1(Point2x, Point1x)));//X position of fourth tangent, u4
        float ipoint4 = Ipoint4(Point2y, Point1x, Size1, Point1y, Size3, Theta3(Theta1(Size1, Distance, Yline(Hline(Distance, Size1, Size2), Size2)), Ypoint1(Point2y, Point1y), Xpoint1(Point2x, Point1x)));//Y position of fourth tangent, i4
        /*float tangent1In = 1;
        float tangent1Out = 1;
        float tangent2In = 1;
        float tangent2Out = 1;
        float tangent3In = 1;
        float tangent3Out = 1;
        float tangent4In = 1;
        float tangent4Out = 1;*/

        //reading out variables for testing purposes
        Debug.Log("R1 " + Size1);
        Debug.Log("a " + Point1x);
        Debug.Log("b " + Point1y);
        Debug.Log("R2 " + Size2);
        Debug.Log("c " + Point2x);
        Debug.Log("d " + Point2y);
        Debug.Log("D " + Distance);
        Debug.Log("R3 " + Size3);
        Debug.Log("H " + hline);
        Debug.Log("Y " + yline);
        Debug.Log("theta1 " + theta1);
        Debug.Log("u1 " + upoint1);
        Debug.Log("i1 " + ipoint1);
        Debug.Log("theta2 " + theta2);
        Debug.Log("y1 " + ypoint1);
        Debug.Log("x1 " + xpoint1);
        Debug.Log("u5 " + upoint5);
        Debug.Log("i5 " + ipoint5);
        Debug.Log("u2 " + upoint2);
        Debug.Log("i2 " + ipoint2);
        Debug.Log("theta3 " + theta3);
        Debug.Log("u6 " + upoint6);
        Debug.Log("i6 " + ipoint6);
        Debug.Log("u3 " + upoint3);
        Debug.Log("i3 " + ipoint3);
        Debug.Log("i4 " + ipoint4); //FOR SOME REASON THESE TWO VARIABLES GET MISCALCULATED, ive checked dozens of times, all the math is correct, idk whats happening maybe its something with unity math im not understanding
        Debug.Log("u4 " + upoint4); //^

        //adding spline part
        // Add a SplineContainer component to the ChainContainer GameObject.
        var container = ChainContainer.AddComponent<SplineContainer>();

        // Create a new Spline on the SplineContainer.
        var spline = container.AddSpline();
        container.RemoveSplineAt(0);

        
        // Set some knot values.
        var knots = new BezierKnot[4];
        knots[0] = new BezierKnot(new float3(upoint1,  ipoint1, 0f));//tangent 1
        knots[1] = new BezierKnot(new float3(upoint2,  ipoint2, 0f));//tangent 2
        knots[2] = new BezierKnot(new float3(upoint4, ipoint4, 0f));//tangent 4 (not a typo, order calculated is Tangent: 1>2>4>3)
        knots[3] = new BezierKnot(new float3(upoint3, ipoint3, 0f));//tanent 3

        //Work in progress, all API found here: https://docs.unity3d.com/Packages/com.unity.splines@2.0/api/index.html
        //this specific thing im trying to do can be found here: https://docs.unity3d.com/Packages/com.unity.splines@2.0/api/UnityEngine.Splines.TangentMode.html
        //i just dont know how to code so idk why its not working.

        //spline.SetTangentMode(TangentMode.AutoSmooth);//trying to set the splines knots to "Auto" as shown in the unity editor inside of a spline container
        spline.Knots = knots;
        spline.Closed = true;
   }

    //this function is assigned the the gameobject chain tool toggle and activates whenever escape is pressed
    public void CancelToolUi()
    {
        for (int i = 0; i < disabledObjects.Count; i++)
        {
            disabledObjects[i].SetActive(true);
            Panel.gameObject.SetActive (false);
        }
        disabledObjects.Clear();
    }
}