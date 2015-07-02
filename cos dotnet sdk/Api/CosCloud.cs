using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CosApiSdk.Common;
using System.Web;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CosApiSdk.Api
{
    enum FolderPattern { File = 0, Folder, Both };
    class CosCloud
    {
        const string COSAPI_CGI_URL = "http://web.file.myqcloud.com/files/v1/";
        private int appId;
        private string secretId;
        private string secretKey;

        public CosCloud(int appId, string secretId, string secretKey)
        {
            this.appId = appId;
            this.secretId = secretId;
            this.secretKey = secretKey;
        }

        public string UpdateFolder(string bucketName, string folderPath, string bizAttribute)
        {
		    return UpdateFile(bucketName, folderPath, null, bizAttribute);
	    }

        public string UpdateFile(string bucketName, string folderPath, string fileName, string bizAttribute)
        {
            var url = COSAPI_CGI_URL + appId + "/" + bucketName + folderPath + (fileName != null ? fileName : "");
            var fileId = "/" + appId + "/" + bucketName + folderPath + (fileName != null ? fileName : "");
            var data = new Dictionary<string, string>();
            data.Add("op", "update");
            data.Add("biz_attr", bizAttribute);
            var sign = Sign.SignatureOnce(appId, secretId, secretKey, fileId, bucketName);
            var header = new Dictionary<string, string>();
            header.Add("Authorization", sign);
            return Request.SendRequest(url, data, HttpMethod.Post, header);
        }

        public string DeleteFolder(string bucketName, string folderPath)
        {
            return DeleteFile(bucketName, folderPath, null);
        }

        public string DeleteFile(string bucketName, string folderPath, string fileName)
        {
            var url = COSAPI_CGI_URL + appId + "/" + bucketName + folderPath + (fileName != null ? fileName : "");
            var fileId = "/" + appId + "/" + bucketName + folderPath + (fileName != null ? fileName : "");
            var data = new Dictionary<string, string>();
            data.Add("op", "delete");
            var sign = Sign.SignatureOnce(appId, secretId, secretKey, fileId, bucketName);
            var header = new Dictionary<string, string>();
            header.Add("Authorization", sign);
            return Request.SendRequest(url, data, HttpMethod.Post, header);
        }

        public string GetFolderStat(string bucketName, string folderPath)
        {
            return GetFileStat(bucketName, folderPath, null);
        }

        public string GetFileStat(string bucketName, string folderPath, string fileName)
        {
            var url = COSAPI_CGI_URL + appId + "/" + bucketName + folderPath + (fileName != null ? fileName : "");
            var data = new Dictionary<string, string>();
            data.Add("op", "stat");
            var expired = DateTime.Now.ToUnixTime() / 1000 + 2592000;
            var sign = Sign.Signature(appId, secretId, secretKey, expired, bucketName);
            var header = new Dictionary<string, string>();
            header.Add("Authorization", sign);
            return Request.SendRequest(url, data, HttpMethod.Get, header);
        }

        public string CreateFolder(string bucketName, string folderPath)
        {
            var url = COSAPI_CGI_URL + appId + "/" + bucketName + folderPath;
            var data = new Dictionary<string, string>();
            data.Add("op", "create");
            var expired = DateTime.Now.ToUnixTime() / 1000 + 2592000;
            var sign = Sign.Signature(appId, secretId, secretKey, expired, bucketName);
            var header = new Dictionary<string, string>();
            header.Add("Authorization", sign);
            return Request.SendRequest(url, data, HttpMethod.Post, header);
        }

        public string GetFolderList(string bucketName, string folderPath, int num, string offset, int order, FolderPattern pattern, string prefix = null)
        {
            var url = COSAPI_CGI_URL + appId + "/" + bucketName + folderPath + (prefix != null ? prefix : "");
            var data = new Dictionary<string, string>();
            data.Add("op", "list");
            data.Add("num", num.ToString());
            data.Add("offset", offset);
            data.Add("order", order.ToString());
            string[] patternArray = { "eListFileOnly", "eListDirOnly", "eListBoth" };
            data.Add("pattern", patternArray[(int)pattern]);
            var expired = DateTime.Now.ToUnixTime() / 1000 + 2592000;
            var sign = Sign.Signature(appId, secretId, secretKey, expired, bucketName);
            var header = new Dictionary<string, string>();
            header.Add("Authorization", sign);
            return Request.SendRequest(url, data, HttpMethod.Get, header);
        }

        public string UploadFile(string bucketName, string folderPath, string fileName, string uploadFilePath)
        {
            var url = COSAPI_CGI_URL + appId + "/" + bucketName + folderPath + HttpUtility.UrlEncode(fileName);
            var sha1 = SHA1.GetSHA1(uploadFilePath);
            var data = new Dictionary<string, string>();
            data.Add("op", "upload");
            data.Add("sha", sha1);
            var expired = DateTime.Now.ToUnixTime() / 1000 + 2592000;
            var sign = Sign.Signature(appId, secretId, secretKey, expired, bucketName);
            var header = new Dictionary<string, string>();
            header.Add("Authorization", sign);
            return Request.SendRequest(url, data, HttpMethod.Post, header, uploadFilePath);
        }

        public string SliceUploadFileFirstStep(string bucketName, string folderPath, string fileName, string uploadFilePath, int sliceSize)
        {
            var url = COSAPI_CGI_URL + appId + "/" + bucketName + folderPath + HttpUtility.UrlEncode(fileName);
            var sha1 = SHA1.GetSHA1(uploadFilePath);
            var fileSize = new FileInfo(uploadFilePath).Length;
            var data = new Dictionary<string, string>();
            data.Add("op", "upload_slice");
            data.Add("sha", sha1);
            data.Add("filesize", fileSize.ToString());
            data.Add("slice_size", sliceSize.ToString());
            var expired = DateTime.Now.ToUnixTime() / 1000 + 2592000;
            var sign = Sign.Signature(appId, secretId, secretKey, expired, bucketName);
            var header = new Dictionary<string, string>();
            header.Add("Authorization", sign);
            return Request.SendRequest(url, data, HttpMethod.Post, header);
        }

        public string SliceUploadFileFollowStep(string bucketName, string folderPath, string fileName, string uploadFilePath,
            string sessionId, int offset, int sliceSize)
        {
            var url = COSAPI_CGI_URL + appId + "/" + bucketName + folderPath + HttpUtility.UrlEncode(fileName);
            var data = new Dictionary<string, string>();
            data.Add("op", "upload_slice");
            data.Add("session", sessionId);
            data.Add("offset", offset.ToString());
            var expired = DateTime.Now.ToUnixTime() / 1000 + 2592000;
            var sign = Sign.Signature(appId, secretId, secretKey, expired, bucketName);
            var header = new Dictionary<string, string>();
            header.Add("Authorization", sign);
            return Request.SendRequest(url, data, HttpMethod.Post, header, uploadFilePath, offset, sliceSize);
        }

        public string SliceUploadFile(string bucketName, string folderPath, string fileName, string uploadFilePath, int sliceSize = 512 * 1024)
        {
            var result = SliceUploadFileFirstStep(bucketName, folderPath, fileName, uploadFilePath, sliceSize);
            var obj = (JObject)JsonConvert.DeserializeObject(result);
            var code = (int)obj["code"];
            if(code != 0){
                return result;
            }
            var data = obj["data"];
            Console.WriteLine(data["access_url"]);
            if(data["access_url"] != null){
                var accessUrl = data["access_url"];
                Console.WriteLine("命中秒传：" + accessUrl);
                return result;
            }
            else{
                var sessionId = data["session"].ToString();
                sliceSize = (int)data["slice_size"]; 
                var offset = (int)data["offset"];
                var retryCount = 0;
                while(true){
                    result = SliceUploadFileFollowStep(bucketName, folderPath, fileName, uploadFilePath, sessionId, offset, sliceSize);
                    Console.WriteLine(result);
                    obj = (JObject)JsonConvert.DeserializeObject(result);
                    code = (int)obj["code"];
                    if(code != 0){
                        //当上传失败后会重试3次
                        if(retryCount < 3){
                            retryCount++;
                            Console.WriteLine("重试....");
                        }
                        else{
                            return result;
                        }
                    }
                    else{
                        data = obj["data"];
                        if(data["offset"] != null){
                            offset = (int)data["offset"] + sliceSize;
                        }
                        else{
                            break;
                        }
                    }
                }
            }
            return "";
	    }
    }


}
