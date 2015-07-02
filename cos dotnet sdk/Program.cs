using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CosApiSdk.Api;
using CosApiSdk.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CosApiSdk
{
    class Program
    {
        const int APP_ID = 1000000;
        const string SECRET_ID = "SECRET_ID";
        const string SECRET_KEY = "SECRET_KEY";
        static void Main(string[] args)
        {
            var cos = new CosCloud(APP_ID, SECRET_ID, SECRET_KEY);
            var start = DateTime.Now.ToUnixTime();
            //var result = cos.GetFolderList("robin_test", "/", 20, "", 0, FolderPattern.Both);
            //var result = cos.UpdateFile("robin_test", "/", "aa.txt", "test file");
            //var result = cos.DeleteFile("robin_test", "/sdk/", "bb.txt");
            //var result = cos.DeleteFolder("robin_test", "/sdk/");
            //var result = cos.GetFolderStat("robin_test", "/");
            //var result = cos.CreateFolder("robin_test", "/sdk/");
            //var result = cos.UploadFile("robin_test", "/", "xx.txt", @"D:\xx.txt");
            //var result = cos.SliceUploadFileFirstStep("robin_test", "/", "红警II共和国之辉(简体中文版).rar", "F:\\红警II共和国之辉(简体中文版).rar", 512 * 1024);
            var result = cos.SliceUploadFile("robin_test", "/", "BBC.与恐龙同行01、新鲜血液[BBC.与龙同行]BBC.Walking.With.Dinosaurs.2000.XviD.AC3.CD1-XHBL.avi", @"E:\电影\BBC.与恐龙同行01、新鲜血液[BBC.与龙同行]BBC.Walking.With.Dinosaurs.2000.XviD.AC3.CD1-XHBL.avi", 3 * 1024 * 1024);
            var end = DateTime.Now.ToUnixTime();
            Console.WriteLine(result);
            Console.WriteLine("总用时：" + (end - start) + "毫秒");
            Console.ReadKey();
        }
    }
}
