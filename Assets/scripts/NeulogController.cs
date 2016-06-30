using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using SimpleJSON;

public class NeulogController : MonoBehaviour {

	//public Light lightSource;
	//public Light flameLightSource;
	//public ParticleSystem flame;
	//private float maxFlameStartSize;
	//private float maxFlameStartSpeed;
 //   private const float MaxIntensity = 8.0f; // Actual max intensity of particle at time of writing

	private HttpWebRequest request;
	private const string Url = "http://localhost:22002/NeuLogAPI?GetSensorValue:[Respiration],[1]";
    private int nullCounter = 0;
    private const int APICallsPerSecond = 40;

    private string message = "";

	private int airPressure = 0;
	private int lowPressure = int.MaxValue;
	private int highPressure = int.MinValue;
	private int pressureRange = int.MinValue;

    private const float DetectionDelay = 0.8f;
    private Queue<float> previousPeaks = new Queue<float>();
    private const int MaxPreviousPeaks = 4;
    private int previousPeakHeight = int.MinValue;
    private float previousPeakTime = 0.0f;
    private int previousValleyHeight = int.MaxValue;
    private float previousValleyTime = 0.0f;
    private int neitherPeakOrValley = 0; // 0 is neither, 1 is peak, 2 is valley

    //private float rateOfAirPressureChange = 0.0f;
    //private float timeOfLastPing = 0.0f;
    //private int previousAirPressure = 0;

    private float preCalibrationSeconds = 2.0f;
	private float calibrationSeconds = 10.0f;
	private bool calibrating = true;

	// Use this for initialization
	void Start() {
        // The API seems to be capped somewhere around 30-40 calls/s
        InvokeRepeating("QueryAPI", 0.0f, 1.0f/APICallsPerSecond);

  //      if (flame) {
		//	maxFlameStartSize = flame.startSize;
		//	maxFlameStartSpeed = flame.startSpeed;
		//	flame.startSize = 0;
		//	flame.startSpeed = 0;
		//	flameLightSource.intensity = 0;
		//}
		message = "Calibrating...";
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
            float percentageOfMax = Mathf.Max(0.0f, Mathf.Min(1.0f, (float)(airPressure - lowPressure) / pressureRange));
            //message = percentageOfMax.ToString();

   //         // Flame behavior
   //         if (lightSource.isActiveAndEnabled) {
			//	lightSource.intensity = MaxIntensity * percentageOfMax;
			//} else if (flameLightSource && flame) {
			//	flame.startSize = maxFlameStartSize * percentageOfMax;
			//	flame.startSpeed = maxFlameStartSpeed * percentageOfMax;
			//	flameLightSource.intensity = MaxIntensity * percentageOfMax;
			//}
        }
    }

    // Coroutine to ping the Neulog API
    void QueryAPI() {
		WWW www = new WWW (Url);
        StartCoroutine(WaitForAPIResponse(www));
	}

    // Handles the API responses
    IEnumerator WaitForAPIResponse(WWW www) {
        yield return www;

        var responseJSON = JSON.Parse(www.text);
        if (responseJSON != null) {
            //previousAirPressure = airPressure;
            airPressure = responseJSON["GetSensorValue"][0].AsInt;

            //float timeOfCurrentPing = Time.time;
            // Keep track of breathing speed by calculating the slope of each secant line between breath measures (rise is airPressure, run is time)
            //if (timeOfLastPing > 0.0f) {
            //    float airPressureDelta = airPressure - previousAirPressure;
            //    float timeDelta = timeOfCurrentPing - timeOfLastPing;
            //    if (timeDelta > 0.0f && airPressureDelta != 0.0f) {
            //        rateOfAirPressureChange = airPressureDelta / timeDelta;
            //    }
            //}
            //int rateDisplay = (int)Mathf.Log(Mathf.Abs(rateOfAirPressureChange), 8);
            ////message =  string.Format("{0}", rateDisplay.ToString());
            //timeOfLastPing = timeOfCurrentPing;

            float timeNow = Time.time;

            if (neitherPeakOrValley != 2) {
                if (airPressure > previousPeakHeight) {
                    previousPeakHeight = airPressure;
                    previousPeakTime = timeNow;
                }

                if (timeNow - previousPeakTime >= DetectionDelay) {
                    if (previousPeaks.Count == MaxPreviousPeaks) previousPeaks.Dequeue();
                    previousPeaks.Enqueue(previousPeakTime);

                    peakValleyChange(2);
                }
            }

            if (neitherPeakOrValley != 1) {
                if (airPressure < previousValleyHeight) {
                    previousValleyHeight = airPressure;
                    previousValleyTime = timeNow;
                }

                if (timeNow - previousValleyTime >= DetectionDelay) {
                    peakValleyChange(1);
                }
            }

            if (previousPeaks.Count >= 2) {
                var previousPeaksArray = previousPeaks.ToArray();
                float averageTimeBetweenPeaks = 0;
                for (int i = previousPeaks.Count - 1; i > 0; --i) averageTimeBetweenPeaks += (previousPeaksArray[i] - previousPeaksArray[i - 1]);
                averageTimeBetweenPeaks /= previousPeaks.Count;
                message = averageTimeBetweenPeaks.ToString();
            }

        } else {
            // Probably means over API limit
            nullCounter++;
            print(nullCounter.ToString());
        }
    }

    void peakValleyChange(int changeTo) {
        neitherPeakOrValley = changeTo;
        previousPeakHeight = int.MinValue;
        previousValleyHeight = int.MaxValue;
        previousPeakTime = 0.0f;
        previousValleyTime = 0.0f;
        //message = neitherPeakOrValley.ToString();
    }

	void OnGUI() {
		int w = Screen.width, h = Screen.height;

		GUIStyle style = new GUIStyle();

		Rect rect = new Rect(0, 0, w, h * 2 / 100);
		style.alignment = TextAnchor.UpperLeft;
		style.fontSize = h * 6 / 100;
		style.normal.textColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
		string text = message;
		GUI.Label(rect, text, style);
	}
}
