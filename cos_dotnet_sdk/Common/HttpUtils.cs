/*
 * Created by SharpDevelop.
 * User: jonnxu
 * Date: 2016/5/24
 * Time: 16:11
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Web;

namespace QCloud.CosApi.Util
{
	public static class HttpUtils
	{
		/// <summary>
		/// 远程路径Encode处理
		/// </summary>
		/// <param name="remotePath"></param>
		/// <returns></returns>
		public static string EncodeRemotePath(string remotePath)
		{
			if (remotePath == "/") {
				return remotePath;
			}
			var endWith = remotePath.EndsWith("/");
			String[] part = remotePath.Split('/');
			remotePath = "";
			foreach (var s in part) {
				if (s != "") {
					if (remotePath != "") {
						remotePath += "/";
					}
					remotePath += HttpUtility.UrlEncode(s);
				}
			}
			remotePath = (remotePath.StartsWith("/") ? "" : "/") + remotePath + (endWith ? "/" : "");
			return remotePath;
		}

		/// <summary>
		/// 标准化远程路径
		/// </summary>
		/// <param name="remotePath">要标准化的远程路径</param>
		/// <returns></returns>
		public static string StandardizationRemotePath(string remotePath)
		{
			if (!remotePath.StartsWith("/")) {
				remotePath = "/" + remotePath;
			}
			if (!remotePath.EndsWith("/")) {
				remotePath += "/";
			}
			return remotePath;
		}
	}
}
