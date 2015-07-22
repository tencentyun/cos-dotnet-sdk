# tencentyun-cos-dotnet-sdk
dotnet sdk for [腾讯云对象存储服务](http://wiki.qcloud.com/wiki/COS%E4%BA%A7%E5%93%81%E4%BB%8B%E7%BB%8D)

## 安装（直接下载源码集成）

### 直接下载源码集成
从github下载源码装入到您的程序中
调用请参考示例

## 修改配置
修改Program.cs内的appid等信息为您的配置

## 上传、查询、删除程序示例
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QCloud.CosApi.Api;
using QCloud.CosApi.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QCloud.CosApi
{
    class Program
    {
        const int APP_ID = 1000000;
        const string SECRET_ID = "SECRET_ID";
        const string SECRET_KEY = "SECRET_KEY";
        static void Main(string[] args)
        {
            try
            {
                var result = "";
                var bucketName = "r_test";
                var cos = new CosCloud(APP_ID, SECRET_ID, SECRET_KEY);
                var start = DateTime.Now.ToUnixTime();
                //result = cos.GetFolderList(bucketName, "/", 20, "", 0, FolderPattern.Both);
                //result = cos.CreateFolder(bucketName, "/sdk/");
                //result = cos.UploadFile(bucketName, "/sdk/xx.txt", @"D:\aa.txt");
                //result = cos.UpdateFile(bucketName, "/sdk/xx.txt", "test file");
                //result = cos.GetFileStat(bucketName, "/sdk/xx.txt");
                //result = cos.UpdateFolder(bucketName, "/sdk/", "test folder");
                //result = cos.GetFolderStat(bucketName, "/sdk/");
                //result = cos.DeleteFile(bucketName, "/sdk/xx.txt");
                //result = cos.DeleteFolder(bucketName, "/sdk/");
                result = cos.SliceUploadFile(bucketName, "/红警II共和国之辉(简体中文版).rar", "F:\\红警II共和国之辉(简体中文版).rar", 512 * 1024);
                var end = DateTime.Now.ToUnixTime();
                Console.WriteLine(result);
                Console.WriteLine("总用时：" + (end - start) + "毫秒");
                Console.ReadKey();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Console.ReadKey();
        }
    }
}


```