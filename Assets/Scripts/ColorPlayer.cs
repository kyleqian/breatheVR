using UnityEngine;
using System.Collections;
using System.Net;
using SimpleJSON;

public class ColorPlayer : MonoBehaviour {

    public GameObject circle;

    private HttpWebRequest request;
    private const string Url = "http://localhost:22002/NeuLogAPI?GetSensorValue:[Respiration],[1]";
    private int nullCounter = 0;
    private const int APICallsPerSecond = 40;

    private string message = "";

    private int airPressure = 0;
    private int lowPressure = int.MaxValue;
    private int highPressure = int.MinValue;
    private int pressureRange = int.MinValue;

    private float preCalibrationSeconds = 2.0f;
    private float calibrationSeconds = 10.0f;
    private bool calibrating = true;

    public const float Speed = 1;
    private Renderer rend;

    // Use this for initialization
    void Start() {
        // The API seems to be capped somewhere around 30-40 calls/s
        InvokeRepeating("QueryAPI", 0.0f, 1.0f / APICallsPerSecond);
        message = "Calibrating...";

        rend = GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update() {
        if (calibrating) {
            // Precalibration
            if (preCalibrationSeconds > 0) {
                preCalibrationSeconds -= Time.smoothDeltaTime;
                return;
            }

            // Records the range of pressure
            calibrationSeconds -= Time.smoothDeltaTime;
            if (calibrationSeconds > 0) {
                //message = highPressure.ToString() + " to " + lowPressure.ToString();

                if (airPressure < lowPressure && airPressure > 0) lowPressure = airPressure;
                if (airPressure > highPressure) highPressure = airPressure;
            }
            if (calibrationSeconds <= 0) {
                pressureRange = highPressure - lowPressure;
                calibrating = false;
            }
        }

        if (!calibrating) {
            float doubleBoundedpercentageOfMax = Mathf.Max(0.0f, Mathf.Min(1.0f, (float)(airPressure - lowPressure) / pressureRange));
            float lowerBoundedPercentageOfMax = Mathf.Max(0.0f, (float)(airPressure - lowPressure) / pressureRange);
            message = doubleBoundedpercentageOfMax.ToString("F2");

            rend.material.SetColor("_Color", HSBColor.ToColor(new HSBColor(doubleBoundedpercentageOfMax, 1, 1)));
        }
    }

    // Coroutine to ping the Neulog API
    void QueryAPI() {
        WWW www = new WWW(Url);
        StartCoroutine(WaitForAPIResponse(www));
    }

    // Handles the API responses
    IEnumerator WaitForAPIResponse(WWW www) {
        yield return www;

        var responseJSON = JSON.Parse(www.text);
        if (responseJSON != null) {
            airPressure = responseJSON["GetSensorValue"][0].AsInt;
        } else {
            // Probably means over API limit
            nullCounter++;
            print(nullCounter.ToString());
        }
    }

    void OnGUI() {
        int w = Screen.width, h = Screen.height;

        GUIStyle style = new GUIStyle();

        Rect rect = new Rect(0, 0, w, h * 2 / 100);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = h * 6 / 100;
        style.normal.textColor = new Color(0, 0, 0, 1.0f);
        string text = message;
        GUI.Label(rect, text, style);
    }
}
