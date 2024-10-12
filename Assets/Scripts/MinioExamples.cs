using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Random = UnityEngine.Random;

/// <summary>
/// Minio 符合 AWS S3协议其它OSS参考修改即可
/// 注意：示例提供的地址均为本地部署的服务器，根据需求请合理替换
/// </summary>
public class MinioExamples : MonoBehaviour
{
    [SerializeField]
    private RawImage _imageRenderer;
    private const string _endPoint = "http://ky.minio.growlib.cn";
    private const string _accessKey = "AaFVh5U0cs2KjwOWaa6l";
    private const string _secretKey = "OE3JnRIHDmcli6ZQWQJDdRFDqEOPqJcncAHfjFCq";
    private string _bucketName = "growlib-resource"; // 储存桶名称
    private string _fileName = "test_upload.png";
    
    
    // Start is called before the first frame update
    void Start()
    {
        MinioOSS.Instance.SetConfig(_endPoint,_bucketName,_accessKey,_secretKey);
        string path = $"{Application.streamingAssetsPath}/{Random.Range(1,4)}.png";
        Debug.Log($"====> path:{path}");
        StartCoroutine(RequestImageAsset(PathToUrl(path), obj =>
        {
            Debug.Log($"====>obj:{obj.Length}");
            StartCoroutine(MinioOSS.Instance.PutObject(obj,_fileName, (isSuccess, downloadUrl) =>
            {
                Debug.Log($"是否上传成功？{isSuccess},downloadUrl:{downloadUrl} ");
                if (isSuccess)
                {
                    StartCoroutine(DownloadFile(downloadUrl));
                }
            }));
        }));
    }
    
    private IEnumerator DownloadFile(string imageUrl)
    {
        Debug.Log($"-----下载路径:{imageUrl}");
        using ( UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log("----图片下载成功");
                Texture2D texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                _imageRenderer.GetComponent<RectTransform>().sizeDelta = new Vector2(texture.height,texture.height);
                _imageRenderer.material.mainTexture = texture;
            }   
        }
    }
    
    private IEnumerator RequestImageAsset(string url, Action<byte[]> imgAction)
    {
        using ( UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            webRequest.timeout = 30;
            yield return webRequest.SendWebRequest();

            if (webRequest.isNetworkError)
            {
                Debug.LogError("Download Error:" + webRequest.error);
            }
            else
            {
                //获取二进制数据
                byte[] imgData = webRequest.downloadHandler.data;
                if (null != imgAction)
                {
                    imgAction(imgData);
                }
            }
        }
    }
    
    private string PathToUrl(string path)
    {
        if (string.IsNullOrEmpty(path) || path.StartsWith("jar:file://") || path.StartsWith("file://") || path.StartsWith("http://") || path.StartsWith("https://"))
            return path;
        if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.Android)
            path = "file://" + path;
        else if (Application.platform == RuntimePlatform.WindowsPlayer)
            path = "file:///" + path;
        return path;
    }
}
