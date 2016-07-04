using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using LitJson;
using System.IO;

public class ClickDetectorController : MonoBehaviour {
    public Beats beats = new Beats();
    private JsonData beatsJSON;
    private int writeCount = 0;


	// Use this for initialization
	void Start () {
    }

    // Update is called once per frame
    void Update () {
        string timeStamp = Time.time.ToString("F3");

	    if (Input.GetMouseButtonDown(0)) {
            beats.beatTimes.Add(timeStamp);
            print(timeStamp);
        }

        if (Input.GetMouseButtonDown(1)) {
            beatsJSON = JsonMapper.ToJson(beats);
            File.WriteAllText(Application.dataPath + string.Format("/Data/{0}_beats_{1}.json", System.DateTime.Now.ToBinary(), writeCount), beatsJSON.ToString());
            writeCount++;
        }
	}
}

public class Beats {
    public List<string> beatTimes = new List<string>();
}