using UnityEngine;
using UnityEngine.Networking;

using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;


public class TestStreaming : MonoBehaviour
{
    
    public int now_i = 0;
    public int startTime;
    public int nowTime;
    public int processTime;
    [SerializeField] private float interval = 1f;
    [SerializeField] private float tmpTime = 0;
    int FrameCounts;
    string[] urlList;
    int[] sumPointsList;
    public int sumPoints;
    double[] processingTime;
    
    private MeshFilter comp;


    // Start is called before the first frame update
    void Start()
    {
        string xmlURL = "http://172.16.51.34/test.xml";
        StartCoroutine("GetXmlRequest", xmlURL);
        comp = GetComponent<MeshFilter>();

    }

    void Update(){
        tmpTime += Time.deltaTime;
        if (tmpTime >= interval){
            sumPoints = sumPointsList[now_i];
            Debug.Log("----------------------------------");
            Debug.Log("now Load URL : " + urlList[now_i]);
            StartCoroutine("TestGetRequest", urlList[now_i]);  
            now_i = (now_i+1) % FrameCounts;
            if(now_i == FrameCounts-1){
                Debug.Log("-----------------------------------Ave process Time:" + processingTime.Average());
            }
            tmpTime =0;
        }
    }
    
    IEnumerator GetXmlRequest(string url)
    {
        startTime = DateTime.Now.Millisecond;
        //Prepare Get by URL
        UnityWebRequest webRequest = UnityWebRequest.Get(url);
        yield return webRequest.SendWebRequest();
        if((webRequest.result == UnityWebRequest.Result.ConnectionError) || (webRequest.result == UnityWebRequest.Result.ProtocolError)) {
            Debug.Log(webRequest.error);
        }
        else
        {
            //Successful Comunication 
            Debug.Log("XML SUCCESS!!!");
            byte[] results = webRequest.downloadHandler.data;
            string serverString = System.Text.Encoding.ASCII.GetString(results);

            XElement xml = XElement.Parse(serverString);
            IEnumerable<XElement> xelements = xml.Elements("Representation");

            FrameCounts = int.Parse(xml.Attribute("FrameCounts").Value);
            sumPointsList = new int[FrameCounts];
            urlList = new string[FrameCounts];
            processingTime = new double[FrameCounts];

            Debug.Log(FrameCounts);
            int index = 0;
            foreach (XElement xelement in xelements){
                sumPointsList[index] = int.Parse(xelement.Element("NumPoints").Value);
                int id = int.Parse(xelement.Element("id").Value);
                urlList[index] = xelement.Element("BaseURL").Value;
                if(index == FrameCounts-1){
                    Debug.Log("id : " + id + "   Load sumPoints:" + sumPointsList[index] + "   BaseURL:" + urlList[index]);
                }
                index++;
            }

            nowTime = DateTime.Now.Millisecond;
            if(startTime > nowTime){
                processTime = nowTime+(1000-startTime);
            }else{
                processTime = nowTime-startTime;
            }

            Debug.Log("Load time for XML file :" + processTime + "ms");
            Debug.Log("----------------------------------");
        }
    }

    IEnumerator TestGetRequest(string url)
    {
        var sw = new System.Diagnostics.Stopwatch();
        var swRTT = new System.Diagnostics.Stopwatch();
        sw.Start();
        swRTT.Start();
        //Prepare Get by URL
        UnityWebRequest webRequest = UnityWebRequest.Get(url);
        yield return webRequest.SendWebRequest();

        if((webRequest.result == UnityWebRequest.Result.ConnectionError) || (webRequest.result == UnityWebRequest.Result.ProtocolError)) {
            Debug.Log(webRequest.error);
        }
        else
        {
            swRTT.Stop();
            Debug.Log("RTT:" + swRTT.Elapsed.TotalMilliseconds);
            //Successful Comunication 
            // char[] del = {'\n', ' '};
            var rawPointsList = webRequest.downloadHandler.text.Split(' '); 
            comp.mesh = VisualizerParallel.createMesh(sumPoints, rawPointsList);
            // Mesh mesh = VisualizerGPU.createMesh(sumPoints,webRequest.downloadHandler.text);
            
            sw.Stop();
            processingTime[now_i] = sw.Elapsed.TotalMilliseconds;
            Debug.Log("process time:" + processTime + "ms");
        }
    }
    
}