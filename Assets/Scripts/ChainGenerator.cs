using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Splines;
using Protobot;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using JetBrains.Annotations;
using Protobot.UI;
using UnityEngine.Events;
public class ChainGenerator : MonoBehaviour {
    public GameObject Panel; //sets Chain Generator Display ui
    public Image buttonImage1; // Reference to the button's Image component
    public Image buttonImage2; // Reference to the button's Image component
    public Color selectedColor = Color.green; // Color to use for the selected state
    public Color defaultColor = Color.white; // Default color for the button
    public UnityEvent onSelectionError;
    public UnityEvent onChainGenerated;

    // Settigns for Spline
    public SplineComponent.AlignAxis UpAxis;
    public SplineComponent.AlignAxis ForwardAxis;
    public SplineInstantiate.Method InstantiateMethod;
    public GameObject HighStrength;
    public GameObject SixP;
    
    //declaring variables
    static public string ChainType = "High Strength"; //default: High Strength, others: 6P
    static public float Point1x = -10.5f;//a (This should be the first sprockets X)
    static public float Point1y = 5.76f;//b (This should be the first sprockets Y)
    static public float Point1z = 0;//not used in original math, just incase
    static public float Size1 = 2f;//R1 (This should be the first sprockets radius)
    static public float Point2x = 3.78f;//c (This should be the second sprockets X)
    static public float Point2y = -0.3f;//d (This should be the second sprockets Y)
    static public float Point2z = 0;//not used in original math, just incase
    static public float Size2 = 2f;//R2 (This should be the second sprockets radius)
    static public float Distance = 0f; //D
    static public float Funny = 0f; //make float point error on purpose :)
    static public float Stupid = 0f;//by default is 1, should be -1 if a > c. Should be 1 if a < c.
    static public float Stupid2 = 0f;
    //static public float Size3 = Size1 - Size2; //calculated C3
    [SerializeField] private List<GameObject> disabledObjects = new List<GameObject>();
    static public float Size3(float Size1, float Size2)//R3
    {
        return Size1 - Size2;
    }
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
    public static float Upoint4(float Point2x, float Upoint3, float Upoint6)
    {
        return Point2x + Upoint3 - Upoint6; // c + u3 - u6
    }
    public static float Ipoint4(float Point2y, float Ipoint3, float Ipoint6)
    {
        return Point2y + Ipoint3 - Ipoint6; // d + i3 - i6
    }

    public static float Upoint7(float Size1, float Point1x, float Point2x, float Point1y, float Point2y, float Stupid2, float Funny)
    {
        // Calculate the term inside the square root
        float denominator = 1 + Mathf.Pow((Point2y - Point1y) / (Point2x - Point1x), 2);
        float sqrtTerm = Mathf.Sqrt(Size1 * Size1 / denominator);
    
        // Calculate the result
        float result = Stupid2 * sqrtTerm + Point1x;
    
        return result + Funny; // -NegativeIO * sqrt( R1^2 / (1 + ((d - b) / (c - a))^2) ) + a
    }
    public static float Ipoint7(float Size1, float Point1x, float Point2x, float Point1y, float Point2y, float Stupid2, float Funny)
    {
        // Calculate the term inside the square root
        float denominator = 1 + Mathf.Pow((Point2y - Point1y) / (Point2x - Point1x), 2);
        float sqrtTerm = Mathf.Sqrt(Size1 * Size1 / denominator);
    
        // Calculate the result
        float result = ((Point2y - Point1y) / (Point2x - Point1x)) * (Stupid2 * sqrtTerm) + Point1y;
    
        return result + Funny; // ((d - b) / (c - a)) * (-NegativeIO * sqrt( R1^2 / (1 + ((d - b) / (c - a))^2) )) + b
    }
 
    public static float Upoint8(float Size2, float Point1x, float Point2x, float Point1y, float Point2y, float Stupid, float Funny)
    {
        // Calculate the term inside the square root
        float denominator = 1 + Mathf.Pow((Point2y - Point1y) / (Point2x - Point1x), 2);
        float sqrtTerm = Mathf.Sqrt(Size2 * Size2 / denominator);
    
        // Calculate the result
        float result = Stupid * sqrtTerm + Point2x;
    
        return result + Funny; // NegativeIO * sqrt( R2^2 / (1 + ((d - b) / (c - a))^2) ) + c
    }

    public static float Ipoint8(float Size2, float Point1x, float Point2x, float Point1y, float Point2y, float Stupid, float Funny)
    {
        // Calculate the term inside the square root
        float denominator = 1 + Mathf.Pow((Point2y - Point1y) / (Point2x - Point1x), 2);
        float sqrtTerm = Mathf.Sqrt(Size2 * Size2 / denominator);
    
        // Calculate the result
        float fraction = (Point2y - Point1y) / (Point2x - Point1x);
        float result = fraction * (Stupid * sqrtTerm) + Point2y;

        return result + Funny; // ((d - b) / (c - a)) * (NegativeIO * sqrt( R2^2 / (1 + ((d - b) / (c - a))^2) )) + d
    }

    //calculating midpoints of circles
    public static float Length1(float Upoint7, float Upoint1, float Ipoint7, float Ipoint1, float Point1x, float Point1y)
    {
        // Calculate the midpoint of u values
        float midpointU = (Upoint7 + Upoint1) / 2;
    
        // Calculate the midpoint of i values
        float midpointI = (Ipoint7 + Ipoint1) / 2;
    
        // Calculate the difference terms
        float deltaX = midpointU - Point1x;
        float deltaY = midpointI - Point1y;
    
        // Calculate and return the length using the Pythagorean theorem
        float result = Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);
    
        return result; // sqrt( ((u7 + u1) / 2 - a)^2 + ((i7 + i1) / 2 - b)^2 )
    }
    public static float Length2(float Upoint2, float Upoint8, float Ipoint2, float Ipoint8, float Point2x, float Point2y)
    {
        // Calculate the midpoint of u values
        float midpointU = (Upoint2 + Upoint8) / 2;

        // Calculate the midpoint of i values
        float midpointI = (Ipoint2 + Ipoint8) / 2;

        // Calculate the difference terms
        float deltaX = midpointU - Point2x;
        float deltaY = midpointI - Point2y;

        // Calculate and return the length using the Pythagorean theorem
        float result = Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);

        return result; // sqrt( ((u2 + u8) / 2 - c)^2 + ((i2 + i8) / 2 - d)^2 )
    }

    //points
    public static float Upoint9(float Point1x, float Size1, float Upoint7, float Upoint1, float Length1)//u9
    {
        // Calculate the midpoint of u7 and u1
        float midpointU = (Upoint7 + Upoint1) / 2;
    
        // Calculate the numerator: (midpointU - a)
        float numerator = midpointU - Point1x;
    
        // Calculate the final result: a + R1 * (numerator / Length1)
        float result = Point1x + Size1 * (numerator / Length1);
    
        return result; // a + R1 * ((u7 + u1)/2 - a) / L1
    }
    public static float Ipoint9(float Point1y, float Size1, float Ipoint7, float Ipoint1, float Length1)//i9
    {
        // Calculate the midpoint of i7 and i1
        float midpointI = (Ipoint7 + Ipoint1) / 2;
    
        // Calculate the numerator: (midpointI - b)
        float numerator = midpointI - Point1y;
    
        // Calculate the final result: b + R1 * (numerator / Length1)
        float result = Point1y + Size1 * (numerator / Length1);
    
        return result; // b + R1 * ((i7 + i1)/2 - b) / L1
    }
    public static float Upoint10(float Point1x, float Size1, float Upoint7, float Upoint3, float Length1)//u10
    {
        // Calculate the midpoint of u7 and u3
        float midpointU = (Upoint7 + Upoint3) / 2;
    
        // Calculate the numerator: (midpointU - a)
        float numerator = midpointU - Point1x;
    
        // Calculate the final result: a + R1 * (numerator / Length1)
        float result = Point1x + Size1 * (numerator / Length1);
    
        return result; // a + R1 * ((u7 + u3)/2 - a) / L1
    }
    public static float Ipoint10(float Point1y, float Size1, float Ipoint7, float Ipoint3, float Length1)//i10
    {
        // Calculate the midpoint of i7 and i3
        float midpointI = (Ipoint7 + Ipoint3) / 2;
    
        // Calculate the numerator: (midpointI - b)
        float numerator = midpointI - Point1y;
    
        // Calculate the final result: b + R1 * (numerator / Length1)
        float result = Point1y + Size1 * (numerator / Length1);
    
        return result; // b + R1 * ((i7 + i3)/2 - b) / L1
    }
    public static float Upoint11(float Point2x, float Size2, float Upoint2, float Upoint8, float Length2)//u11
    {
        // Calculate the midpoint of u2 and u8
        float midpointU = (Upoint2 + Upoint8) / 2;
    
        // Calculate the numerator: (midpointU - c)
        float numerator = midpointU - Point2x;
    
        // Calculate the final result: c + R2 * (numerator / Length2)
        float result = Point2x + Size2 * (numerator / Length2);
    
        return result; // c + R2 * ((u2 + u8)/2 - c) / L2
    }
    public static float Ipoint11(float Point2y, float Size2, float Ipoint2, float Ipoint8, float Length2)//i11
    {
        // Calculate the midpoint of i2 and i8
        float midpointI = (Ipoint2 + Ipoint8) / 2;
    
        // Calculate the numerator: (midpointI - d)
        float numerator = midpointI - Point2y;
    
        // Calculate the final result: d + R2 * (numerator / Length2)
        float result = Point2y + Size2 * (numerator / Length2);
    
        return result; // d + R2 * ((i2 + i8)/2 - d) / L2
    }
    public static float Upoint12(float Point2x, float Size2, float Upoint4, float Upoint8, float Length2)//u12
    {
        // Calculate the midpoint of u4 and u8
        float midpointU = (Upoint4 + Upoint8) / 2;
    
        // Calculate the numerator: (midpointU - c)
        float numerator = midpointU - Point2x;
    
        // Calculate the final result: c + R2 * (numerator / Length2)
        float result = Point2x + Size2 * (numerator / Length2);
    
        return result; // c + R2 * ((u4 + u8)/2 - c) / L2
    }
    public static float Ipoint12(float Point2y, float Size2, float Ipoint4, float Ipoint8, float Length2)//i12
    {
        // Calculate the midpoint of i4 and i8
        float midpointI = (Ipoint4 + Ipoint8) / 2;
    
        // Calculate the numerator: (midpointI - d)
        float numerator = midpointI - Point2y;
    
        // Calculate the final result: d + R2 * (numerator / Length2)
        float result = Point2y + Size2 * (numerator / Length2);
    
        return result; // d + R2 * ((i4 + i8)/2 - d) / L4
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

                            // Which sprocket is selected
                            Debug.Log("Saved part ID: " + savedObject.id);
                            if(savedObject.id.Contains("High Strength")){
                                ChainType = "High Strength";
                                if(savedObject.id.Contains("6T")){Size1 = 0.29527f;} // 6T = 0.29527
                                if(savedObject.id.Contains("12T")){Size1 = 0.66189f;} // 12T = 0.66189
                                if(savedObject.id.Contains("18T")){Size1 = 1.034465f;} // 18T = 1.034465
                                if(savedObject.id.Contains("24T")){Size1 = 1.39985f;} // 24T = 1.39985
                                if(savedObject.id.Contains("30T")){Size1 = 1.821215f;} //30T = 1.821215
                            } else if(savedObject.id.Contains("Standard")){
                                ChainType = "6P"; // No chain type for standard yet
                                if(savedObject.id.Contains("10T")){Size1 = 0.15527f;} // 10T = 0.15527
                                if(savedObject.id.Contains("15T")){Size1 = 0.30861f;} // 15T = 0.30861
                                if(savedObject.id.Contains("24T")){Size1 = 0.55137f;} // 24T = 0.55137
                                if(savedObject.id.Contains("40T")){Size1 = 0.89828f;} // 40T = 0.89828
                                if(savedObject.id.Contains("48T")){Size1 = 1.034465f;} //48T = 1.034465
                            } else if(savedObject.id.Contains("6P")){
                                ChainType = "6P";
                                if(savedObject.id.Contains("8T")){Size1 = 0.30861f;} // 8T = 0.30861
                                if(savedObject.id.Contains("16T")){Size1 = 0.58137f;} // 16T = 0.58137
                                if(savedObject.id.Contains("24T")){Size1 = 0.89828f;} // 24T = 0.89828
                                if(savedObject.id.Contains("32T")){Size1 = 1.21557f;} // 32T = 1.21557
                                if(savedObject.id.Contains("40T")){Size1 = 1.533425f;} //40T = 1.533425
                            } else {
                                ChainType = "High Strength"; // set to standard
                            }
                            Debug.Log("Chain Type: " + ChainType);

                            // Size of sprocket Selected

                            // Store the selected object's position in the Point1 variables
                            Point1x = selectedTransform.position.x;
                            Point1y = selectedTransform.position.y;
                            Point1z = selectedTransform.position.z;

                            // Change the button's color to green
                            buttonImage1.color = selectedColor;
                        }
                        else if (selectedObject != firstSprocket) {
                            if (savedObject.id.Contains(ChainType)){
                                // Second sprocket selection
                                Debug.Log("Selected Second Sprocket: " + selectedObject.name);
                                Debug.Log("X: " + selectedTransform.position.x);
                                Debug.Log("Y: " + selectedTransform.position.y);
                                Debug.Log("Z: " + selectedTransform.position.z);
                                
                                Debug.Log("Saved part ID: " + savedObject.id);
                                if(savedObject.id.Contains("High Strength")){
                                    if(savedObject.id.Contains("6T")){Size2 = 0.29527f;} // 6T = 0.29527
                                    if(savedObject.id.Contains("12T")){Size2 = 0.66189f;} // 12T = 0.66189
                                    if(savedObject.id.Contains("18T")){Size2 = 1.034465f;} // 18T = 1.034465
                                    if(savedObject.id.Contains("24T")){Size2 = 1.39985f;} // 24T = 1.39985
                                    if(savedObject.id.Contains("30T")){Size2 = 1.821215f;} //30T = 1.821215
                                } else if(savedObject.id.Contains("Standard")){
                                    if(savedObject.id.Contains("10T")){Size2 = 0.15527f;} // 10T = 0.15527
                                    if(savedObject.id.Contains("15T")){Size2 = 0.30861f;} // 15T = 0.30861
                                    if(savedObject.id.Contains("24T")){Size2 = 0.55137f;} // 24T = 0.55137
                                    if(savedObject.id.Contains("40T")){Size2 = 0.89828f;} // 40T = 0.89828
                                    if(savedObject.id.Contains("48T")){Size2 = 1.034465f;} //48T = 1.034465
                                } else if(savedObject.id.Contains("6P")){
                                    if(savedObject.id.Contains("8T")){Size2 = 0.30861f;} // 8T = 0.30861
                                    if(savedObject.id.Contains("16T")){Size2 = 0.58137f;} // 16T = 0.58137
                                    if(savedObject.id.Contains("24T")){Size2 = 0.89828f;} // 24T = 0.89828
                                    if(savedObject.id.Contains("32T")){Size2 = 1.21557f;} // 32T = 1.21557
                                    if(savedObject.id.Contains("40T")){Size2 = 1.533425f;} //40T = 1.533425
                                } else {
                                    Size2 = 1;
                                }

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
                            } else {
                                onSelectionError.Invoke();
                            }
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
    Debug.Log("This is the Size2 at start of Generate Chain " + Size2);
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

        if (Point1x > Point2x)//by default is 1, should be -1 if a > c. Should be 1 if a < c.
        {
            Stupid = 1f;
            Stupid2 = -1f;
            Debug.Log("Stupid Negative" + Stupid + Stupid2);
        }
        else if (Point1x < Point2x)
        {
            Stupid = -1f;
            Stupid2 = 1f;
            Debug.Log("Stupid Positive" + Stupid + Stupid2);
        }
        else
        {
            float Funny = 0.0001f;
            Debug.Log("Funny Activated!" + Funny); //Activated whenever sprockets have the same x coordinate.
        }

        //calculating all the variables
        float Distance = Vector3.Distance(ChainPoint1.transform.position, ChainPoint2.transform.position); //finds distance between two points
        float size3 = Size3(Size1, Size2);
        float hline = Hline(Distance, Size1, Size2);//H
        float yline = Yline(Hline(Distance, Size1, Size2), Size2);//Y
        float theta1 = Theta1(Size1, Distance, Yline(Hline(Distance, Size1, Size2), Size2));//theta1
        float ypoint1 = Ypoint1(Point2y, Point1y);//x1
        float xpoint1 = Xpoint1(Point2x, Point1x);//y1
        float theta2 = Theta2(Size1, Distance, Yline(Hline(Distance, Size1, Size2), Size2), Ypoint1(Point2y, Point1y), Xpoint1(Point2x, Point1x));//theta2
        float upoint1 = Upoint1(Point1x, Size1, theta2); //X position of first tangent, u1
        float ipoint1 = Ipoint1(Point1y, Size1, theta2); //Y position of first tangent, i1
        float upoint5 = Upoint5(Point1x, size3, theta2); //u5
        float ipoint5 = Ipoint5(Point1y, size3, theta2); //i5
        float upoint2 = Upoint2(Point2x, upoint1, upoint5); //X poisiton of second tangent, u2
        float ipoint2 = Ipoint2(Point2y, ipoint1, ipoint5); //Y position of second tangent, i2
        float theta3 = Theta3(Theta1(Size1, Distance, Yline(Hline(Distance, Size1, Size2), Size2)), Ypoint1(Point2y, Point1y), Xpoint1(Point2x, Point1x));//theta3
        float upoint6 = Upoint6(Point1x, size3, Theta3(Theta1(Size1, Distance, Yline(Hline(Distance, Size1, Size2), Size2)), Ypoint1(Point2y, Point1y), Xpoint1(Point2x, Point1x)));//u6
        float ipoint6 = Ipoint6(Point1y, size3, Theta3(Theta1(Size1, Distance, Yline(Hline(Distance, Size1, Size2), Size2)), Ypoint1(Point2y, Point1y), Xpoint1(Point2x, Point1x)));//i6
        float upoint3 = Upoint3(Point1x, Size1, Theta3(Theta1(Size1, Distance, Yline(Hline(Distance, Size1, Size2), Size2)), Ypoint1(Point2y, Point1y), Xpoint1(Point2x, Point1x))); //X position of third tangent, u3
        float ipoint3 = Ipoint3(Point1y, Size1, Theta3(Theta1(Size1, Distance, Yline(Hline(Distance, Size1, Size2), Size2)), Ypoint1(Point2y, Point1y), Xpoint1(Point2x, Point1x))); //Y position of third tangent, i3
        float upoint4 = Upoint4(Point2x, upoint3, upoint6);//X position of fourth tangent, u4
        float ipoint4 = Ipoint4(Point2y, ipoint3, ipoint6);//Y position of fourth tangent, i4
        float upoint7 = Upoint7(Size1, Point1x, Point2x, Point1y, Point2y, Stupid, Funny);
        float ipoint7 = Ipoint7(Size1, Point1x, Point2x, Point1y, Point2y, Stupid, Funny);
        float upoint8 = Upoint8(Size2, Point1x, Point2x, Point1y, Point2y, Stupid2, Funny);
        float ipoint8 = Ipoint8(Size2, Point1x, Point2x, Point1y, Point2y, Stupid2, Funny);
        float length1 = Length1(upoint7, upoint1, ipoint7, ipoint1, Point1x, Point1y);
        float length2 = Length2(upoint2, upoint8, ipoint2, ipoint8, Point2x, Point2y);
        float upoint9 = Upoint9(Point1x, Size1, upoint7, upoint1, length1);
        float ipoint9 = Ipoint9(Point1y, Size1, ipoint7, ipoint1, length1);
        float upoint10 = Upoint10(Point1x, Size1, upoint7, upoint3, length1);
        float ipoint10 = Ipoint10(Point1y, Size1, ipoint7, ipoint3, length1);
        float upoint11 = Upoint11(Point2x, Size2, upoint2, upoint8, length2);
        float ipoint11 = Ipoint11(Point2y, Size2, ipoint2, ipoint8, length2);
        float upoint12 = Upoint12(Point2x, Size2, upoint4, upoint8, length2);
        float ipoint12 = Ipoint12(Point2y, Size2, ipoint4, ipoint8, length2);

        /*float tangent1In = 1;
        float tangent1Out = 1;
        float tangent2In = 1;
        float tangent2Out = 1;
        float tangent3In = 1;
        float tangent3Out = 1;
        float tangent4In = 1;
        float tangent4Out = 1;*/

        //reading out variables for testing purposes
        /*Debug.Log("R1 " + Size1);
        Debug.Log("a " + Point1x);
        Debug.Log("b " + Point1y);
        Debug.Log("R2 " + Size2);
        Debug.Log("c " + Point2x);
        Debug.Log("d " + Point2y);
    
        Debug.Log("u2 " + upoint2);
        Debug.Log("u8 " + upoint8);
        Debug.Log("i2 " + ipoint2);
        Debug.Log("i8 " + ipoint8);

        Debug.Log("L1 " + length1);
        Debug.Log("L2 " + length2);
        Debug.Log("u9 " + upoint9);
        Debug.Log("i9 " + ipoint9);
        Debug.Log("u10 " + upoint10);
        Debug.Log("i10 " + ipoint10);
        Debug.Log("u11 " + upoint11);
        Debug.Log("i11 " + ipoint11);
        Debug.Log("u12 " + upoint12);
        Debug.Log("i12 " + ipoint12);


        Debug.Log("u2" + upoint2);
        Debug.Log("i2" + ipoint2);
        Debug.Log("u4" + upoint4);
        Debug.Log("i4" + ipoint4);*/

        //adding spline part
        // Add a SplineContainer component to the ChainContainer GameObject.
        var container = ChainContainer.AddComponent<SplineContainer>();
        var instantiate = ChainContainer.AddComponent<SplineInstantiate>();
        // Create a new Spline on the SplineContainer.
        var spline = container.AddSpline();
        container.RemoveSplineAt(0);

        
        // Set some knot values.
        var knots = new BezierKnot[10];
        knots[0] = new BezierKnot(new float3(upoint10,  ipoint10, Point1z));//extra 3
        knots[1] = new BezierKnot(new float3(upoint7,  ipoint7, Point1z));//extra 2
        knots[2] = new BezierKnot(new float3(upoint9,  ipoint9, Point1z));//extra 1
        knots[3] = new BezierKnot(new float3(upoint1,  ipoint1, Point1z));//tangent 1
        knots[4] = new BezierKnot(new float3(upoint2,  ipoint2, Point1z));//tangent 2
        knots[5] = new BezierKnot(new float3(upoint11, ipoint11, Point1z));//extra 6
        knots[6] = new BezierKnot(new float3(upoint8, ipoint8, Point1z));//extra 5
        knots[7] = new BezierKnot(new float3(upoint12, ipoint12, Point1z));//extra 4
        knots[8] = new BezierKnot(new float3(upoint4, ipoint4, Point1z));//tangent 4 (not a typo, order calculated is Tangent: 1>2>4>3)
        knots[9] = new BezierKnot(new float3(upoint3, ipoint3, Point1z));//tanent 3
        

        //Work in progress, all API found here: https://docs.unity3d.com/Packages/com.unity.splines@2.0/api/index.html
        //this specific thing im trying to do can be found here: https://docs.unity3d.com/Packages/com.unity.splines@2.0/api/UnityEngine.Splines.TangentMode.html
        //i just dont know how to code so idk why its not working.

        //spline.SetTangentMode(TangentMode.AutoSmooth); //trying to set the splines knots to "Auto" as shown in the unity editor inside of a spline container
        spline.Knots = knots;
        spline.Closed = true;

        onChainGenerated.Invoke();
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