using UnityEngine;
using System.Collections;
using System.IO;
using LitJson;

public class GameController : MonoBehaviour {
    public GameObject GameAnchor;
    private const float ScaleMin = 0.0f;
    private const float ScaleMax = 10.0f;
    private bool breatheIn = true; // Start with inhale

    private double currentTime = 0;
    private double timeOfNextBeat;
    private double timeOfMeasureStart;
    private int measureStartIndex = 4; // Starting beat
    private const int BeatsPerMeasure = 4;
    private int nextBeatIndex; 
    private JsonData beatTimes;


	// Use this for initialization
	void Start () {
        string jsonString = File.ReadAllText(Application.dataPath + "/Data/-8587340713348999394_beats_0.json");
        beatTimes = JsonMapper.ToObject(jsonString)["beatTimes"];
        nextBeatIndex = measureStartIndex;
        timeOfMeasureStart = (double)beatTimes[nextBeatIndex];
        timeOfNextBeat = timeOfMeasureStart;
	}
	
	// Update is called once per frame
	void Update () {
        currentTime = Time.time;
        GameAnchor.transform.localScale = GetTransformScale();

        if (currentTime >= timeOfNextBeat) {
            //print(currentTime.ToString());
            if (nextBeatIndex == measureStartIndex + BeatsPerMeasure) {
                measureStartIndex = nextBeatIndex;
                timeOfMeasureStart = (double)beatTimes[measureStartIndex];
                breatheIn = !breatheIn;
            }
            nextBeatIndex++;
            timeOfNextBeat = (double)beatTimes[nextBeatIndex];
        }
    }

    Vector3 GetTransformScale() {
        double lengthOfMeasure = (double)beatTimes[measureStartIndex + BeatsPerMeasure] - timeOfMeasureStart;
        double secondsIntoMeasure = currentTime - timeOfMeasureStart;
        float scale = 0;
        if (secondsIntoMeasure > 0) {
            double percentIntoMeasure = secondsIntoMeasure / lengthOfMeasure;
            double scalePercentage = breatheIn ? percentIntoMeasure : 1.0f - percentIntoMeasure;
            scale = (float)((ScaleMax - ScaleMin) * (scalePercentage) + ScaleMin);
        }
        return new Vector3(scale, scale, 0);
    }
}
