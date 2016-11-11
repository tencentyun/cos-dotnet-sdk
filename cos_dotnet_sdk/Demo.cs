using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using QCloud.CosApi.Api;
using QCloud.CosApi.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QCloud.CosApi
{
    class CosDemo
    {
        const int APP_ID = 10000202;
        const string SECRET_ID = "AKIDPNPur5B27qcuRajCEmzKV93U7k8VceqW";
        const string SECRET_KEY = "THrKYU2J9kMEy5JBQt7Bt0N4pYSKWGAz";
        static void Main(string[] args)
        {
            try
            {
                var result = "";
				const string bucketName = "jonnxu5";
                const string localPath = "test.txt";
                const string remotePath = "/sdktest/test.txt";
                const string folder ="/sdktest";

                //创建cos对象
                var cos = new CosCloud(APP_ID, SECRET_ID, SECRET_KEY);

	            //创建文件夹
	            result = cos.CreateFolder(bucketName, folder);
	            Console.WriteLine(result);

	          	//目录更新
	          	var updateParasDic = new Dictionary<string, string>();                
				updateParasDic.Add(CosParameters.PARA_BIZ_ATTR,"new attribute");
				result = cos.UpdateFolder(bucketName, folder, updateParasDic);
				Console.WriteLine(result);

	            //获取文件夹属性
				result = cos.GetFolderStat(bucketName, folder);
				Console.WriteLine(result);
                
	            //上传文件（不论文件是否分片，均使用本接口）
	            var uploadParasDic = new Dictionary<string, string>();                
				uploadParasDic.Add(CosParameters.PARA_BIZ_ATTR,"");
				uploadParasDic.Add(CosParameters.PARA_INSERT_ONLY,"0");
				uploadParasDic.Add(CosParameters.PARA_SLICE_SIZE,SLICE_SIZE.SLIZE_SIZE_3M.ToString());
				result = cos.UploadFile(bucketName, remotePath, localPath, uploadParasDic);
                Console.WriteLine(result);

                //获取文件属性
                result = cos.GetFileStat(bucketName, remotePath);
                Console.WriteLine(result);

                //获取文件列表
              	var foldListParasDic = new Dictionary<string, string>();                
              	foldListParasDic.Add(CosParameters.PARA_NUM,"100");
              	result = cos.GetFolderList(bucketName, folder, foldListParasDic);
                Console.WriteLine(result);

        		//设置可选参数
                var optionParasDic = new Dictionary<string, string>();
				optionParasDic.Add(CosParameters.PARA_BIZ_ATTR,"new attribute");                
				optionParasDic.Add(CosParameters.PARA_AUTHORITY,AUTHORITY.AUTHORITY_PRIVATEPUBLIC);
	            optionParasDic.Add(CosParameters.PARA_CACHE_CONTROL,"no");
	            optionParasDic.Add(CosParameters.PARA_CONTENT_TYPE,"application/text");
	            optionParasDic.Add(CosParameters.PARA_CONTENT_DISPOSITION,"inline filename=\"QC-7677.pdf\"");
	            optionParasDic.Add(CosParameters.PARA_CONTENT_LANGUAGE,"en");
	            optionParasDic.Add("x-cos-meta-test","test");
	            //更新文件
	            result = cos.UpdateFile(bucketName, remotePath, optionParasDic);
                Console.WriteLine(result);

                //获取文件属性
                result = cos.GetFileStat(bucketName, remotePath);
                Console.WriteLine(result);

                //目录列表
                var folderlistParasDic = new Dictionary<string, string>();                
				folderlistParasDic.Add(CosParameters.PARA_NUM,"100");
	            folderlistParasDic.Add(CosParameters.PARA_ORDER,"0");
	            folderlistParasDic.Add(CosParameters.PARA_PATTERN,FolderPattern.PATTERN_BOTH);
	            result = cos.GetFolderList(bucketName, folder, folderlistParasDic);
				Console.WriteLine(result);

				//删除文件
                result = cos.DeleteFile(bucketName, remotePath);
                Console.WriteLine(result);
                
                //删除文件夹
                result = cos.DeleteFolder(bucketName, folder);
                Console.WriteLine(result);
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Console.ReadKey();
        }
    }
}
