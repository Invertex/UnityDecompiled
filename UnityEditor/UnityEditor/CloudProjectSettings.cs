using System;
using UnityEditor.Connect;

namespace UnityEditor
{
	public class CloudProjectSettings
	{
		public static string userId
		{
			get
			{
				return UnityConnect.instance.GetUserId();
			}
		}

		public static string userName
		{
			get
			{
				return UnityConnect.instance.GetUserName();
			}
		}

		public static string accessToken
		{
			get
			{
				return UnityConnect.instance.GetAccessToken();
			}
		}

		public static string projectId
		{
			get
			{
				return UnityConnect.instance.GetProjectGUID();
			}
		}

		public static string projectName
		{
			get
			{
				return UnityConnect.instance.GetProjectName();
			}
		}

		public static string organizationId
		{
			get
			{
				return UnityConnect.instance.GetOrganizationId();
			}
		}

		public static string organizationName
		{
			get
			{
				return UnityConnect.instance.GetOrganizationName();
			}
		}
	}
}
