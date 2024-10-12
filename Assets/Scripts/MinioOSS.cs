using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Minio Oss (Minio oss 符合aws s3协议，其它oss可参考修改)
/// Minio官方文档：https://minio.org.cn/docs/minio/linux/developers/dotnet/minio-dotnet.html
/// </summary>
public class MinioOSS
{
    private string _endPoint;
    private string _bucketName;
    private string _accessKey;
    private string _secretKey;

    public void SetConfig(string endpoint, string bucketName, string accessKey, string secretKey)
    {
        _endPoint = endpoint;
        _bucketName = bucketName;
        _accessKey = accessKey;
        _secretKey = secretKey;
    }
    
    /// <summary>
    /// 上传对象
    /// </summary>
    /// <param name="fileData">上传的数据</param>
    /// <param name="fileName">文件名</param>
    /// <param name="callback">true上传成功，返回下载地址; false上传失败，返回错误信息;</param>
    /// <returns></returns>
    public IEnumerator PutObject(byte[] fileData, string fileName, Action<bool, string> callback = null)
    {
        if (string.IsNullOrEmpty(_endPoint) 
            || string.IsNullOrEmpty(_bucketName)
            || string.IsNullOrEmpty(_accessKey)
            || string.IsNullOrEmpty(_secretKey))
        {
            //Debug.LogError("Not initialized, please call <SetConfig> to initialize.");
            callback?.Invoke(false,"Not initialized, please call <SetConfig> to initialize.");
            yield break;
        }

        string url = $"{_endPoint}/{_bucketName}/{fileName}";
        string date = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
        string contentType = "application/octet-stream";

        using (UnityWebRequest request = new UnityWebRequest(url, "PUT"))
        {
            request.uploadHandler = new UploadHandlerRaw(fileData);
            request.downloadHandler = new DownloadHandlerBuffer();

            // 计算签名
            string stringToSign = $"PUT\n\n{contentType}\n{date}\n/{_bucketName}/{fileName}";
            string signature = GetSignature(stringToSign, _secretKey);

            // 设置请求头
            request.SetRequestHeader("Host", new Uri(_endPoint).Host);
            request.SetRequestHeader("Date", date);
            request.SetRequestHeader("Content-Type", contentType);
            request.SetRequestHeader("Authorization", $"AWS {_accessKey}:{signature}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                //Debug.Log($"File uploaded successfully, Download url: {_endPoint}/{_bucketName}/{fileName}");
                callback?.Invoke(true,$"{_endPoint}/{_bucketName}/{fileName}");
            }
            else
            {
                //Debug.LogError($"Error uploading file: {request.error}");
                callback?.Invoke(false,$"Error uploading file: {request.error}");
            }  
        }
    }

    private string GetSignature(string stringToSign, string secretKey)
    {
        byte[] secretKeyBytes = Encoding.UTF8.GetBytes(secretKey);
        using (HMACSHA1 hmac = new HMACSHA1(secretKeyBytes))
        {
            byte[] dataBytes = Encoding.UTF8.GetBytes(stringToSign);
            byte[] resultBytes = hmac.ComputeHash(dataBytes);
            return Convert.ToBase64String(resultBytes);
        }
    }

    private static MinioOSS _instance;
    public static MinioOSS Instance => _instance ??= new MinioOSS();
}
