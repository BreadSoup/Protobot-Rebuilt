using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Splines;

public class ChainGenerator : MonoBehaviour {
    //declaring variables
    static public float Point1x = -10.5f;//a
    static public float Point1y = 5.76f;//b
    static public float Size1 = 5f;//R1
    static public float Point2x = 3.78f;//c
    static public float Point2y = -0.3f;//d
    static public float Size2 = 3f;//R2
    static public float Distance = 0f; //D
    static public float Size3 = Size1 - Size2; //calculated C3
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


    public void OnButtonPress(){ //everything inside this is activated once button is pressed
        Debug.Log("Select 1 of 2 Sprocket. Press Esc to Cancel"); //asks user to click the first sprocket
        float Point1x = 0f; //save the gameobjects x coordinate (IMPORTANT: should update as the game object moves)
        float Point1Y = 0f; //save the gameobjects y coordiante (IMPORTANT: should update as the game object moves)
        Debug.Log("Select 2 of 2 Sprocket. Press Esc to Cancel"); //asks user to click the second sprocket
        float Point2x = 0f; //save the gameobjects x coordinate (IMPORTANT: should update as the game object moves)
        float Point2y = 0f; //save the gameobjects y coordinate (IMPORTANT: should update as the game object moves)

        GameObject ChainContainer = new GameObject(); //Creates game object named ChainContainer
        ChainContainer.name = "Chain Container"; //names object whatever is in Quotations
        ChainContainer.gameObject.transform.Translate(Point1x, Point1y, 0);

        GameObject ChainPoint1 = new GameObject();
        ChainPoint1.name = "ChainPoint1";
        ChainPoint1.gameObject.transform.Translate(Point1x, Point1y, 0);

        GameObject ChainPoint2 = new GameObject();
        ChainPoint2.name = "ChainPoint2";
        ChainPoint2.gameObject.transform.Translate(Point2x, Point2y, 0);

        float Distance = Vector3.Distance(ChainPoint1.transform.position, ChainPoint2.transform.position); //finds distance between two points
        
        //calculating all the variables
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
        float tangent1In = 1;
        float tangent1Out = 1;
        float tangent2In = 1;
        float tangent2Out = 1;
        float tangent3In = 1;
        float tangent3Out = 1;
        float tangent4In = 1;
        float tangent4Out = 1;


        
        
        
        //reading out variables for testing
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
        Debug.Log("i4 " + ipoint4);
        Debug.Log("u4 " + upoint4);

        //adding spline part
        // Add a SplineContainer component to the ChainContainer GameObject.
        var container = ChainContainer.AddComponent<SplineContainer>();

        // Create a new Spline on the SplineContainer.
        var spline = container.AddSpline();

        // Set some knot values.
        var knots = new BezierKnot[4];
        knots[0] = new BezierKnot(new float3(upoint1,  ipoint1, 0f));//tangent 1
        knots[1] = new BezierKnot(new float3(upoint2,  ipoint2, 0f));//tangent 2
        knots[2] = new BezierKnot(new float3(upoint4, ipoint4, 0f));//tangent 4 (not a typo, order calculated is Tangent: 1>2>4>3)
        knots[3] = new BezierKnot(new float3(upoint3, ipoint3, 0f));//tanent 3

        knots[0].TangentIn = new float3(tangent1In, tangent1Out, 0f);
        knots[0].TangentOut = new float3(-tangent1In, -tangent1Out, 0f);

        knots[1].TangentIn = new float3(tangent2In, tangent2Out, 0f);
        knots[1].TangentOut = new float3(-tangent2In, -tangent2Out, 0f);

        knots[2].TangentIn = new float3(tangent4In, tangent4Out, 0f);
        knots[2].TangentOut = new float3(-tangent4In, -tangent4Out, 0f);

        knots[3].TangentIn = new float3(tangent3In, tangent3Out, 0f);
        knots[3].TangentOut = new float3(-tangent3In, -tangent3Out, 0f);
        
        spline.Knots = knots;
        spline.Closed = true;
   }
}