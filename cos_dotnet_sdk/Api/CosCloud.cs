using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QCloud.CosApi.Common;
using QCloud.CosApi.Util;
using System.Web;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace QCloud.CosApi.Api
{
	class CosCloud
	{
		const string COSAPI_CGI_URL = "http://web.file.myqcloud.com/files/v1/";
		//文件大于20M时采用分片上传,小于等于20M时采用单文件上传
		const int SLICE_UPLOAD_FILE_SIZE = 20 * 1024 * 1024;
		//用户计算用户签名超时时间
		const int SIGN_EXPIRED_TIME = 180;
		//HTTP请求超时时间
		const int HTTP_TIMEOUT_TIME = 60;
		private int appId;
		private string secretId;
		private string secretKey;
		private int timeOut;
		private Request httpRequest;
		/// <summary>
		/// CosCloud 构造方法
		/// </summary>
		/// <param name="appId">授权appid</param>
		/// <param name="secretId">授权secret id</param>
		/// <param name="secretKey">授权secret key</param>
		/// <param name="timeOut">网络超时,默认60秒</param>
		public CosCloud(int appId, string secretId, string secretKey, int timeOut = HTTP_TIMEOUT_TIME)
		{
			this.appId = appId;
			this.secretId = secretId;
			this.secretKey = secretKey;
			this.timeOut = timeOut * 1000;
			this.httpRequest = new Request();
		}

		/// <summary>
		/// 更新文件夹信息
		/// </summary>
		/// <param name="bucketName"> bucket名称</param>
		/// <param name="remotePath">远程文件夹路径</param>
		/// <param name="parameterDic">可选参数Dictionary</param>
		/// 包含如下可选参数：biz_attr:目录属性
		/// <returns></returns>
		public string UpdateFolder(string bucketName, string remotePath, Dictionary<string, string>  parameterDic = null)
		{
		    if (!checkPathValid(remotePath))
			{
				return constructResult(ERRORCode.ERROR_CODE_PATH_NOT_VALID,"path contain invalid char");
			}
			remotePath = HttpUtils.StandardizationRemotePath(remotePath);
			return UpdateFile(bucketName, remotePath, parameterDic);
		}

		/// <summary>
		/// 更新文件信息
		/// </summary>
		/// <param name="bucketName"> bucket名称</param>
		/// <param name="remotePath">远程文件路径</param>
		/// <param name="parameterDic">参数Dictionary</param>
		/// 包含如下可选参数：
		/// biz_attr:文件属性
		/// authority: eInvalid（继承bucket的权限）、eWRPrivate(私有读写)、eWPrivateRPublic(公有读私有写)
		/// 以下参数会打包到custom_headers对象中,携带到cos系统
		/// Cache-Control:
		/// Content-Type:
		/// Content-Disposition:
		/// Content-Language:
		/// x-cos-meta-: 以"x-cos-meta-"为前缀的参数
		/// <returns></returns>
		public string UpdateFile(string bucketName, string remotePath, Dictionary<string, string> parameterDic = null)
		{
		    if (!checkPathValid(remotePath))
			{
				return constructResult(ERRORCode.ERROR_CODE_PATH_NOT_VALID,"path contain invalid char");
			}
			var url = generateURL(bucketName, remotePath);
			var data = new Dictionary<string, object>();
			var customerHeaders = new Dictionary<string,object>();
			int flag = 0;
			data.Add("op", "update");

			//将biz_attr设置到data中
			if (addParameter(CosParameters.PARA_BIZ_ATTR , ref data, ref parameterDic))
			{
				flag |= Flag.FLAG_BIZ_ATTR;
			}			

			//将authority设置到data中
			if (addAuthority(ref data, ref parameterDic))
			{
				flag |= Flag.FLAG_AUTHORITY;
			}

			//将customer_headers设置到data["custom_headers"]中
			if (parameterDic != null && setCustomerHeaders(ref customerHeaders, ref parameterDic)) {
				data.Add(CosParameters.PARA_CUSTOM_HEADERS, customerHeaders);
				flag |= Flag.FLAG_CUSTOMER_HEADER;
			}

			if (flag != 0 && flag != 1) {
				data.Add(CosParameters.PARA_FLAG, flag);
			}
            
			var sign = Sign.SignatureOnce(appId, secretId, secretKey, (remotePath.StartsWith("/") ? "" : "/") + remotePath, bucketName);
			var header = new Dictionary<string, string>();
			header.Add("Authorization", sign);
			header.Add("Content-Type", "application/json");
			return httpRequest.SendRequest(url, ref data, HttpMethod.Post, ref header, timeOut);
		}
        
		/// <summary>
		/// 删除文件夹
		/// </summary>
		/// <param name="bucketName">bucket名称</param>
		/// <param name="remotePath">远程文件夹路径</param>
		/// <returns></returns>
		public string DeleteFolder(string bucketName, string remotePath)
		{
		    if (!checkPathValid(remotePath))
			{
				return constructResult(ERRORCode.ERROR_CODE_PATH_NOT_VALID,"path contain invalid char");
			}
			remotePath = HttpUtils.StandardizationRemotePath(remotePath);
			return DeleteFile(bucketName, remotePath);
		}

		/// <summary>
		/// 删除文件
		/// </summary>
		/// <param name="bucketName">bucket名称</param>
		/// <param name="remotePath">远程文件路径</param>
		/// <returns></returns>
		public string DeleteFile(string bucketName, string remotePath)
		{
		    if (!checkPathValid(remotePath))
			{
				return constructResult(ERRORCode.ERROR_CODE_PATH_NOT_VALID,"path contain invalid char");
			}
			var url = generateURL(bucketName, remotePath);
			var data = new Dictionary<string, object>();
			data.Add("op", "delete");
			var sign = Sign.SignatureOnce(appId, secretId, secretKey, (remotePath.StartsWith("/") ? "" : "/") + remotePath, bucketName);
			var header = new Dictionary<string, string>();
			header.Add("Authorization", sign);
			header.Add("Content-Type", "application/json");

			return httpRequest.SendRequest(url, ref data, HttpMethod.Post, ref header, timeOut);
		}

		/// <summary>
		/// 获取文件夹信息
		/// </summary>
		/// <param name="bucketName">bucket名称</param>
		/// <param name="remotePath">远程文件夹路径</param>
		/// <returns></returns>
		public string GetFolderStat(string bucketName, string remotePath)
		{
		    if (!checkPathValid(remotePath))
			{
				return constructResult(ERRORCode.ERROR_CODE_PATH_NOT_VALID,"path contain invalid char");
			}
			remotePath = HttpUtils.StandardizationRemotePath(remotePath);
			return GetFileStat(bucketName, remotePath);
		}

		/// <summary>
		/// 获取文件信息
		/// </summary>
		/// <param name="bucketName">bucket名称</param>
		/// <param name="remotePath">远程文件路径</param>
		/// <returns></returns>
		public string GetFileStat(string bucketName, string remotePath)
		{
		    if (!checkPathValid(remotePath))
			{
				return constructResult(ERRORCode.ERROR_CODE_PATH_NOT_VALID,"path contain invalid char");
			}
			var url = generateURL(bucketName, remotePath);
			var data = new Dictionary<string, object>();
			data.Add("op", "stat");
			var expired = getExpiredTime();
			var sign = Sign.Signature(appId, secretId, secretKey, expired, bucketName);
			var header = new Dictionary<string, string>();
			header.Add("Authorization", sign);
			return httpRequest.SendRequest(url, ref data, HttpMethod.Get, ref header, timeOut);
		}

		/// <summary>
		/// 创建文件夹
		/// </summary>
		/// <param name="bucketName">bucket名称</param>
		/// <param name="remotePath">远程文件夹路径</param>
		/// <param name="parameterDic">参数Dictionary</param>
		/// 包含如下可选参数：biz_attr:目录属性
		/// <returns></returns>
		public string CreateFolder(string bucketName, string remotePath, Dictionary<string, string> parameterDic = null)
		{
		    if (!checkPathValid(remotePath))
			{
				return constructResult(ERRORCode.ERROR_CODE_PATH_NOT_VALID,"path contain invalid char");
			}
			remotePath = HttpUtils.StandardizationRemotePath(remotePath);
			var url = generateURL(bucketName, remotePath);
			var data = new Dictionary<string, object>();
			data.Add("op", "create");
			addParameter(CosParameters.PARA_BIZ_ATTR, ref data, ref parameterDic);
			var expired = getExpiredTime();
			var sign = Sign.Signature(appId, secretId, secretKey, expired, bucketName);
			var header = new Dictionary<string, string>();
			header.Add("Authorization", sign);
			header.Add("Content-Type", "application/json");
			return httpRequest.SendRequest(url, ref data, HttpMethod.Post, ref header, timeOut);
		}

		/// <summary>
		/// 目录列表,前缀搜索
		/// </summary>
		/// <param name="bucketName">bucket名称</param>
		/// <param name="remotePath">远程文件夹路径</param>
		/// <param name="parameterDic">参数Dictionary</param>
		/// 包含如下可选参数：
		/// num:拉取的总数,最大199，如果不带,则默认num=199
		/// context:透传字段,用于翻页,前端不需理解,需要往前/往后翻页则透传回来
		/// order:默认正序(=0), 填1为反序
		/// pattern:拉取模式:只是文件，只是文件夹，全部
		/// prefix:读取文件/文件夹前缀
		/// <returns></returns>
		public string GetFolderList(string bucketName, string remotePath, Dictionary<string, string>  parameterDic)
		{
		    if (!checkPathValid(remotePath))
			{
				return constructResult(ERRORCode.ERROR_CODE_PATH_NOT_VALID,"path contain invalid char");
			}
			string prefix = "";
			if (parameterDic != null && parameterDic.ContainsKey(CosParameters.PARA_PREFIX))
				prefix = parameterDic[CosParameters.PARA_PREFIX];

			remotePath = HttpUtils.StandardizationRemotePath(remotePath);
			var url = generateURL(bucketName, remotePath, prefix);			
			var data = new Dictionary<string, object>();
			data.Add("op", "list");
			if (!addFolderListNum(ref data, ref parameterDic))
			{
				return constructResult(ERRORCode.ERROR_CODE_PARAMETER_ERROE,"parameter num value invalidate");
			}

			if (!addOrder(ref data, ref parameterDic))
			{
				return constructResult(ERRORCode.ERROR_CODE_PARAMETER_ERROE,"parameter order value invalidate");
			}

			addParameter(CosParameters.PARA_CONTEXT, ref data, ref parameterDic);

			if (!addPattern(ref data,ref parameterDic))
			{
				return constructResult(ERRORCode.ERROR_CODE_PARAMETER_ERROE,"parameter pattern value invalidate");
			}
			
			var expired = getExpiredTime();
			var sign = Sign.Signature(appId, secretId, secretKey, expired, bucketName);
			var header = new Dictionary<string, string>();
			header.Add("Authorization", sign);
			return httpRequest.SendRequest(url, ref data, HttpMethod.Get, ref header, timeOut);
		}
        
		/// <summary>
		/// 文件上传
		/// 说明: 根据文件大小判断使用单文件上传还是分片上传,当文件大于20M时,内部会进行分片上传,可以携带分片大小sliceSize
		/// </summary>
		/// <param name="bucketName">bucket名称</param>
		/// <param name="remotePath">远程文件路径</param>
		/// <param name="localPath">本地文件路径</param>
		/// <param name="parameterDic">参数Dictionary</param>
		/// 包含如下可选参数
		/// bizAttribute：文件属性
		/// insertOnly： 0:同名文件覆盖, 1:同名文件不覆盖
		/// sliceSize: 分片大小，可选取值为:512*1024，1*1024*1024，2*1024*1024，3*1024*1024
		/// <returns></returns>
		public string UploadFile(string bucketName, string remotePath, string localPath, Dictionary<string, string>  parameterDic = null)
		{
			if (!File.Exists(localPath)) {
				return constructResult(ERRORCode.ERROR_CODE_FILE_NOT_EXIST,"local file not exist");
			}

		    if (!checkPathValid(remotePath))
			{
				return constructResult(ERRORCode.ERROR_CODE_PATH_NOT_VALID,"remotePath contain invalid char");
			}

			string bizAttribute = "";
			if (parameterDic != null && parameterDic.ContainsKey(CosParameters.PARA_BIZ_ATTR))
				bizAttribute = parameterDic[CosParameters.PARA_BIZ_ATTR];
			
			int insertOnly = 1;
			if (parameterDic != null && parameterDic.ContainsKey(CosParameters.PARA_INSERT_ONLY)) {
				try {
					insertOnly = Int32.Parse(parameterDic[CosParameters.PARA_INSERT_ONLY]);	
				} catch (Exception e) {
					Console.WriteLine(e.Message);
					return constructResult(ERRORCode.ERROR_CODE_PARAMETER_ERROE,"parameter insertOnly value invalidate");
				}
			}
			
			
			var fileSize = new FileInfo(localPath).Length;
			if (fileSize <= SLICE_UPLOAD_FILE_SIZE) {
				return Upload(bucketName, remotePath, localPath, bizAttribute, insertOnly);
			} else {
				int sliceSize = SLICE_SIZE.SLIZE_SIZE_1M;
				if (parameterDic != null && parameterDic.ContainsKey(CosParameters.PARA_SLICE_SIZE)){
					sliceSize = Int32.Parse(parameterDic[CosParameters.PARA_SLICE_SIZE]);
				}
				int slice_size = getSliceSize(sliceSize);
				return SliceUploadFile(bucketName, remotePath, localPath, bizAttribute, slice_size,insertOnly);
			}
		}
        
		/// <summary>
		/// 移动（重命名）文件
		/// </summary>
		/// <param name="bucketName">bucket名称</param>
		/// <param name="srcPath">源文件路径</param>
		/// <param name="dstPath">目标文件路径</param>
 		/// <param name="toOverWrite">是否覆盖写</param>
		/// <returns></returns>
		public string MoveFile(string bucketName, string srcPath, string dstPath, int toOverWrite)
		{
		    if (!checkPathValid(srcPath))
			{
				return constructResult(ERRORCode.ERROR_CODE_PATH_NOT_VALID,"srcPath contain invalid char");
			}

		    if (!checkPathValid(dstPath))
			{
				return constructResult(ERRORCode.ERROR_CODE_PATH_NOT_VALID,"dstPath contain invalid char");
			}
			var url = generateURL(bucketName, srcPath);
			var data = new Dictionary<string, object>();
			data.Add("op","move");
			data.Add("dest_fileid",dstPath);
			data.Add("to_over_write",toOverWrite == 0 ? 0 : 1) ;

			var expired = getExpiredTime();
			var sign = Sign.SignatureOnce(appId, secretId, secretKey, (srcPath.StartsWith("/") ? "" : "/") + srcPath, bucketName);
			var header = new Dictionary<string, string>();
			header.Add("Authorization", sign);
			header.Add("Content-Type", "application/json");
			return httpRequest.SendRequest(url, ref data, HttpMethod.Post, ref header, timeOut);
		}
		
		/// <summary>
		/// 单个文件上传
		/// </summary>
		/// <param name="bucketName">bucket名称</param>
		/// <param name="remotePath">远程文件路径</param>
		/// <param name="localPath">本地文件路径</param>
		/// <param name="biz_attr">biz_attr属性</param>
		/// <param name="insertOnly">同名文件是否覆盖</param>
		/// <returns></returns>
		private string Upload(string bucketName, string remotePath, string localPath, 
		                          string bizAttribute="", int insertOnly=1)
		{
			var url = generateURL(bucketName, remotePath);
			var sha1 = SHA1.GetSHA1(localPath);
			var data = new Dictionary<string, object>();
			data.Add("op", "upload");
			data.Add("sha", sha1);
			data.Add("biz_attr", bizAttribute);
			data.Add("insertOnly", insertOnly);

			var expired = getExpiredTime();
			var sign = Sign.Signature(appId, secretId, secretKey, expired, bucketName);
			var header = new Dictionary<string, string>();
			header.Add("Authorization", sign);
			return httpRequest.SendRequest(url, ref data, HttpMethod.Post, ref header, timeOut, localPath);
		}

		/// <summary>
		/// 分片上传
		/// </summary>
		/// <param name="bucketName">bucket名称</param>
		/// <param name="remotePath">远程文件路径</param>
		/// <param name="localPath">本地文件路径</param>
		/// <param name="bizAttribute">biz属性</param>
		/// <param name="sliceSize">切片大小（字节）,默认为1M</param>
		/// <param name="insertOnly">是否覆盖同名文件</param>
		/// <param name="paras"></param> 
		/// <returns></returns>
		private string SliceUploadFile(string bucketName, string remotePath, string localPath, 
		                               string bizAttribute="", int sliceSize = SLICE_SIZE.SLIZE_SIZE_1M, int insertOnly = 1)
		{
			var result = SliceUploadFileFirstStep(bucketName, remotePath, localPath, sliceSize, bizAttribute, insertOnly );
			var obj = (JObject)JsonConvert.DeserializeObject(result);
			var code = (int)obj["code"];
			if (code != 0) {
				return result;
			}
			var data = obj["data"];
			if (data["access_url"] != null) {
				var accessUrl = data["access_url"];
				Console.WriteLine("命中秒传：" + accessUrl);
				return result;
			} else {
				var sessionId = data["session"].ToString();
				sliceSize = (int)data["slice_size"]; 
				var offset = (long)data["offset"];
				var retryCount = 0;
				while (true) {
					result = SliceUploadFileFollowStep(bucketName, remotePath, localPath, sessionId, offset, sliceSize);
					Console.WriteLine(result);
					obj = (JObject)JsonConvert.DeserializeObject(result);
					code = (int)obj["code"];
					if (code != 0) {
						//当上传失败后会重试3次
						if (retryCount < 3) {
							retryCount++;
							Console.WriteLine("重试....");
						} else {
							return result;
						}
					} else {
						data = obj["data"];
						if (data["offset"] != null) {
							offset = (long)data["offset"] + sliceSize;
						} else {
							break;
						}
					}
				}
			}
			return "";
		}
        
		/// <summary>
		/// 分片上传第一步
		/// </summary>
		/// <param name="bucketName">bucket名称</param>
		/// <param name="remotePath">远程文件路径</param>
		/// <param name="localPath">本地文件路径</param>
		/// <param name="sliceSize">切片大小（字节）</param>
		/// <param name="paras">保存的是其他可选参数,具体参数请参考官网API</param> 
		/// <returns></returns>
		private string SliceUploadFileFirstStep(string bucketName, string remotePath, string localPath, 
		                                        int sliceSize, string biz_attr = "", int insertOnly = 1)
		{
			var url = generateURL(bucketName, remotePath);
			var sha1 = SHA1.GetSHA1(localPath);
			var fileSize = new FileInfo(localPath).Length;
			var data = new Dictionary<string, object>();
			data.Add("op", "upload_slice");
			data.Add("sha", sha1);
			data.Add("filesize", fileSize.ToString());
			data.Add("slice_size", sliceSize.ToString());
			data.Add("biz_attr", biz_attr);
			data.Add("insertOnly", insertOnly);

			var expired = getExpiredTime();
			var sign = Sign.Signature(appId, secretId, secretKey, expired, bucketName);
			var header = new Dictionary<string, string>();
			header.Add("Authorization", sign);
			return httpRequest.SendRequest(url, ref data, HttpMethod.Post, ref header, timeOut);
		}

		/// <summary>
		/// 分片上传后续步骤
		/// </summary>
		/// <param name="bucketName">bucket名称</param>
		/// <param name="remotePath">远程文件路径</param>
		/// <param name="localPath">本地文件路径</param>
		/// <param name="sessionId">分片上传会话ID</param>
		/// <param name="offset">文件分片偏移量</param>
		/// <param name="sliceSize">切片大小（字节）</param>
		/// <returns></returns>
		private string SliceUploadFileFollowStep(string bucketName, string remotePath, string localPath,
			string sessionId, long offset, int sliceSize)
		{
			var url = generateURL(bucketName, remotePath);
			var data = new Dictionary<string, object>();
			data.Add("op", "upload_slice");
			data.Add("session", sessionId);
			data.Add("offset", offset.ToString());
			var expired = getExpiredTime();
			var sign = Sign.Signature(appId, secretId, secretKey, expired, bucketName);
			var header = new Dictionary<string, string>();
			header.Add("Authorization", sign);
			return httpRequest.SendRequest(url, ref data, HttpMethod.Post, ref header, timeOut, localPath, offset, sliceSize);
		}

		/// <summary>
		/// 内部方法：增加参数insertOnly到data中
		/// </summary>
		/// <returns></returns>
		private bool addInsertOnly(ref Dictionary<string,object> data, ref Dictionary<string, string> paras)
		{
			string val = "";
			if (data == null || paras == null) {
				return false;
			}

			if (paras.ContainsKey(CosParameters.PARA_INSERT_ONLY)) {
				val = paras[CosParameters.PARA_INSERT_ONLY];
				if (val == null ) return false;
				val = val.Trim();
				if (!val.Equals("1") && !val.Equals("0")) {
					Console.WriteLine("insertOnly value error, please refer to the RestfullAPI, it will be ignored");
				} else {
					data.Add(CosParameters.PARA_INSERT_ONLY, val);            		
				}
			}
            
			return true;
		}
		
		/// <summary>
		/// 内部方法：增加参数到data中
		/// </summary>
		/// <returns></returns>
		private bool addParameter(string paraName, ref Dictionary<string,object> data, ref Dictionary<string, string> paras)
		{
			string val = "";
			if (data == null || paras == null || paraName == null) {
				return false;
			}

			if (paras.ContainsKey(paraName)) {
				val = paras[paraName];
				data.Add(paraName, val);
				return true;
			}
			return false;
		}
		
		/// <summary>
		/// 内部方法：增加参数Pattern到data中
		/// </summary>
		/// <returns></returns>
		private bool addPattern(ref Dictionary<string,object> data, ref Dictionary<string, string> paras)
		{
			string val = "";
			if (data == null || paras == null) {
				return false;
			}

			if (paras.ContainsKey(CosParameters.PARA_PATTERN)) {
				val = paras[CosParameters.PARA_PATTERN];
				if (val.Equals(FolderPattern.PATTERN_FILE) || val.Equals(FolderPattern.PATTERN_DIR) || val.Equals(FolderPattern.PATTERN_BOTH))
				{
					data.Add(CosParameters.PARA_PATTERN, val);
				} else {
					return false;
				}
			}
			else 
			{
				data.Add(CosParameters.PARA_PATTERN, FolderPattern.PATTERN_BOTH);
			}
            
			return true;
		}

		/// <summary>
		/// 内部方法：增加参数order到data中
		/// </summary>
		/// <returns></returns>
		private bool addOrder(ref Dictionary<string,object> data, ref Dictionary<string, string> paras)
		{
			int val = -1;
			if (data == null || paras == null) {
				return false;
			}

			if (paras.ContainsKey(CosParameters.PARA_ORDER)) {
				try{
					val = Int32.Parse(paras[CosParameters.PARA_ORDER]);
				} catch (Exception e) {
					Console.WriteLine(e.Message);
					return false;
				}
				
				if (val == 0 || val == 1)
				{
					data.Add(CosParameters.PARA_ORDER, val);
				} else {
					return false;
				}
			}
			else 
			{
				data.Add(CosParameters.PARA_ORDER, 0);
			}
            
			return true;
		}
		
		/// <summary>
		/// 内部方法：增加参数num到data中
		/// </summary>
		/// <returns></returns>
		private bool addFolderListNum(ref Dictionary<string,object> data, ref Dictionary<string, string> paras)
		{
			int listnum = 20;
			if (data == null || paras == null) {
				return false;
			}

			if (paras.ContainsKey(CosParameters.PARA_NUM)) {
				try {
					listnum = Int32.Parse(paras[CosParameters.PARA_NUM]);
				} catch (Exception e) {
					Console.WriteLine(e.Message);
					return false;
				}
				
				if (listnum <= 0 || listnum >= 200)
					return false;
				data.Add(CosParameters.PARA_NUM, listnum);
			} else {
				data.Add(CosParameters.PARA_NUM, 199);
			}
            
			return true;
		}
		
		/// <summary>
		/// 内部方法：增加参数authority到data中
		/// </summary>
		/// <returns></returns>		
		private bool addAuthority(ref Dictionary<string, object> data, ref Dictionary<string, string> paras)
		{
			string val = "";
			if (data == null || paras == null) {
				return false;
			}

			if (paras.ContainsKey(CosParameters.PARA_AUTHORITY)) {
				val = paras[CosParameters.PARA_AUTHORITY];
				if (!isAuthorityValid(val)) {
					Console.WriteLine("authority value error, please refer to the RestfullAPI");
					return false;
				} else {
					data.Add(CosParameters.PARA_AUTHORITY, val);
					return true;
				}
			}
            
			return false;
		}
        
        /// <summary>
		/// 内部方法：增加用户自定义参数到data中
		/// </summary>
		/// <returns></returns>	
		private bool addCustomerHeaders(ref Dictionary<string,object> data, ref Dictionary<string, string> paras)
		{

			if (data == null || paras == null) {
				return false;
			}
			
			foreach (var dic in paras) {			
				if (isCustomerHeader(dic.Key)) {
					if (!data.ContainsKey(dic.Key)) {
						data.Add(dic.Key, dic.Value);	
					}
				}
			}
            
			return true;
		}
        
        /// <summary>
		/// 内部方法：增加用户自定义参数到data中
		/// </summary>
		/// <returns></returns>			
		private bool setCustomerHeaders(ref Dictionary<string,object> data, ref Dictionary<string,string> parameterDic)
		{
			bool flag = false;
			if (parameterDic.Count == 0) {
				return flag;
			}

			foreach (var dic in parameterDic) {
				if (isCustomerHeader(dic.Key) && !data.ContainsKey(dic.Key)) {
					data.Add(dic.Key, dic.Value);
					flag  = true;
				}
			}

			return flag;
		}
        
        /// <summary>
		/// 内部方法：判断是否为用户自定义参数
		/// </summary>
		/// <returns></returns>				
		private bool isCustomerHeader(string key)
		{
			if (key.Equals(CosParameters.PARA_CACHE_CONTROL)
			          || key.Equals(CosParameters.PARA_CONTENT_TYPE)
			          || key.Equals(CosParameters.PARA_CONTENT_DISPOSITION)
			          || key.Equals(CosParameters.PARA_CONTENT_LANGUAGE)
			          || key.Equals(CosParameters.PARA_CONTENT_ENCODING)
			          || key.StartsWith(CosParameters.PARA_X_COS_META_PREFIX)) {
				return true;
			}

			return false;
		}
        
        /// <summary>
		/// 内部方法：判断参数authority参数值是否合法
		/// </summary>
		/// <returns></returns>				
		private bool isAuthorityValid(string authority)
		{
			if (authority == null) {
				return false;
			}

			if (authority.Equals("eInvalid") || authority.Equals("eWRPrivate") || authority.Equals("eWPrivateRPublic")) {
				return true;
			}
        	
			return false;
		}

		/// <summary>
		/// 内部方法：超时时间(当前系统时间+300秒)
		/// </summary>
		/// <returns></returns>	
		private long getExpiredTime()
		{
			return DateTime.Now.ToUnixTime() / 1000 + SIGN_EXPIRED_TIME;
		}
		
		/// <summary>
		/// 内部方法：用户传入的slice_size进行规范
		/// </summary>
		/// <returns></returns>	
		private int getSliceSize(int sliceSize)
		{
			int size = SLICE_SIZE.SLIZE_SIZE_1M;
			if (sliceSize < SLICE_SIZE.SLIZE_SIZE_1M)
			{
				size = SLICE_SIZE.SLIZE_SIZE_512K;
			} else if (sliceSize < SLICE_SIZE.SLIZE_SIZE_2M) {
				size = SLICE_SIZE.SLIZE_SIZE_1M;
			} else if (sliceSize < SLICE_SIZE.SLIZE_SIZE_3M) {
				size = SLICE_SIZE.SLIZE_SIZE_2M;
			} else {
				size = SLICE_SIZE.SLIZE_SIZE_3M;
			}

			return size;
		}
		
		/// <summary>
		/// 内部方法：构造URL
		/// </summary>
		/// <returns></returns>
		private string generateURL(string bucketName, string remotePath)
		{
			string url = COSAPI_CGI_URL + this.appId + "/" + bucketName + HttpUtils.EncodeRemotePath(remotePath);
			return url;
		}
		
		/// <summary>
		/// 内部方法：构造URL
		/// </summary>
		/// <returns></returns>
		private string generateURL(string bucketName, string remotePath, string prefix)
		{
			string url = COSAPI_CGI_URL + this.appId + "/" + bucketName + HttpUtils.EncodeRemotePath(remotePath) + HttpUtility.UrlEncode(prefix);
			return url;
		}
		
		/// <summary>
		/// 内部方法：构造结果响应
		/// </summary>
		/// <returns></returns>
		private string constructResult(int code, string message)
		{
			var result = new Dictionary<string, object>();
			result.Add("code", code);
			result.Add("message", message);
			return JsonConvert.SerializeObject(result);
		}

		/// <summary>
		/// 内部方法：检测文件夹名字的合法性
		/// 保留符号（英文半角符号）不可以使用。例如：'/' , '?' , '*' , ':' , '|' , '\' , '<' , '>' , '"'
		/// </summary>
		/// <returns></returns>
		private bool checkPathValid(string url)
		{
			//匹配连续两个\中间存在0个或多个空格
			const string pa = "[?*:|<>]";
			if (Regex.IsMatch(url, pa)) {
				Console.WriteLine("aaaa");
				return false;
			}
			const string sPattern = "/ */";
			if (Regex.IsMatch(url, sPattern) ){
				Console.WriteLine("bbb");
				return false;
			}
			
			return true;
		}
	}
}
