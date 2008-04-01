/*
 * Copyright (C) 2006 Google Inc.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not
 * use this file except in compliance with the License. You may obtain a copy of
 * the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 */

using System;
using System.Web;
using System.IO;
using System.Security;
using System.Security.Principal;
using System.Web.UI;
using System.Net;
using System.Collections;

namespace SAMLServices.Wia
{
	/// <summary>
	/// Implementation for AA interfaces, Windows Integrated Auth and IIS security check
	/// </summary>
	public class AuthImpl : IAuthn, IAuthz
	{
		System.Web.UI.Page page;
		public AuthImpl(System.Web.UI.Page page)
		{
			this.page = page;
		}
		#region IAuthn Members
		/// <summary>
		// Method to obtain a user's acount name
		/// </summary>
		/// <returns>user account user@domain</returns>
		public String GetUserIdentity()
		{
			String principal = page.User.Identity.Name;
			String[] prins = principal.Split('\\');
			//Kerberos accepts this format
			if (prins.Length == 2)
				principal = prins[1] + "@" + prins[0];
			return principal;
		}

		/// <summary>
		/// Test impersonation directly without GSA involved.
		/// Expect the request having a parameter "subject"
		/// </summary>
		public void Diagnose()
		{
			HttpRequest Request = page.Request;
			HttpResponse Response = page.Response;
			String samlRequest = Request.Params["SAMLRequest"];			
			// Put user code to initialize the page here
			Common.printHeader(Response);
			String subject  = Request.Params["subject"];
			if (subject == null)
			{
				Response.Write("Application Pool Identity  = "  + WindowsIdentity.GetCurrent().Name);
				Response.Write("<br>");
				Response.Write("Your Windows account  = " + page.User.Identity.Name);
				Response.Write("<p>");
				Response.Write("<b>Use Login.aspx?subject=user@domain to test impersonation.</b>");
			}else
			//Test Impersonation
			{
				WindowsIdentity wi = new WindowsIdentity(subject);
				if (wi != null)
					Response.Write("<br>Obtained Windows identity for user " + subject);
				WindowsImpersonationContext wic = null;
				wic = wi.Impersonate();
				Response.Write("<br>This message was written using the identity of " + subject);
				Response.Write("<br>Impersonation successful!");
				if( wic != null)
					wic.Undo();
				Response.Write("<p><b>Now you can test content authorization using GSA Simulator!</b>");
			}
			Common.printFooter(Response);
		}
		#endregion

		#region IAuthz Members

		/// <summary>
		/// Method to determine whether a user has acces to a given URL
		/// </summary>
		/// <param name="url">Target URL</param>
		/// <param name="subject">account to be tested, in the form of username@domain</param>
		/// <returns>Permit|Deny|Intermediate</returns>

		public String GetPermission(String url, String subject)
		{
			Common.debug("inside AuthImpl::GetPermission");
			Common.debug("url=" + url);
			Common.debug("subject=" + subject);
			// Convert the user name from domainName\userName format to 
			// userName@domainName format if necessary
	
			// The WindowsIdentity(string) constructor uses the new
			// Kerberos S4U extension to get a logon for the user
			// without a password.

			// Default AuthZ decision is set to "Deny"
			String status = "Deny";
			// Attempt to impersonate the user and verify access to the URL
			WindowsImpersonationContext wic = null;
			try
			{
				status = "before impersonation";
				Common.debug("before WindowsIdentity");
				// Create a Windows Identity
				WindowsIdentity wi = new WindowsIdentity(subject);
				if (wi == null)
				{
					Common.error("Couldn't get WindowsIdentity for account " + subject);
					return "Indeterminate";
				}
				Common.debug("name=" + wi.Name + ", authed=" + wi.IsAuthenticated);				
				Common.debug("after WindowsIdentity");
				// Impersonate the user
				wic = wi.Impersonate();
				Common.debug("after impersonate");
				// Attempt to access the network resources as this user
				String result = GetURL(url);
				Common.debug("http Head response should be empty, is it? = " + result);
				// Successfully retrieved URL, so set AuthZ decision to "Permit"
				status = result;
			}
			catch(SecurityException e)
			{
				Common.error("AuthImpl::caught SecurityException");
				// Determine what sort of exception was thrown by checking the response status
				Common.error("e = " + e.ToString());
				Common.error("msg = " + e.Message);
				Common.error("grantedset = " + e.GrantedSet);
				Common.error("innerException= " + e.InnerException);
				Common.error("PermissionState = " + e.PermissionState);
				Common.error("PermissionType = " + e.PermissionType);
				Common.error("RefusedSet = " + e.RefusedSet);
				Common.error("TargetSet = " + e.TargetSite);
				status = "Indeterminate";
				return status;
			}
			catch(WebException e)
			{
				if( wic != null)
				{
					wic.Undo();
					wic = null;
				}
				Common.debug("AuthImpl::caught WebException");
				// Determine what sort of exception was thrown by checking the response status
				Common.debug("e = " + e.ToString());
				Common.debug("resp = " + e.Response);
				HttpWebResponse resp = (HttpWebResponse)((WebException)e).Response;
				if (resp != null)
					Common.debug("status = " + resp.StatusCode.ToString());
				else
				{
					Common.debug("response is null");
					status = "Indeterminate";
					return status;
				}
				// If an "unauthorized" response was received, set AuthZ decision to "Deny"
				if (resp.StatusCode == HttpStatusCode.Unauthorized)
					status = "Deny";
					// Accepted, Continue, or Redirect responses indicate authorized access
				else if (resp.StatusCode == HttpStatusCode.Accepted 
					|| resp.StatusCode == HttpStatusCode.Continue
					|| resp.StatusCode == HttpStatusCode.Redirect)
					status = "Permit";
					// Some other response error occured
					// Setting the AuthZ decision to "Indeterminate" allows the GSA to use other
					// AuthZ methods (i.e. Basic, NTLM, SSO) to determine access
				else
					status = "Indeterminate";
			}
			catch(UnauthorizedAccessException e) //can't write to the log file
			{
				if( wic != null)
				{
					wic.Undo();
					wic = null;
				}
				Common.debug(e.Message);
				status = e.Message;
			}
			catch(Exception e)
			{
				if( wic != null)
				{
					wic.Undo();
					wic = null;
				}
				// Some undetermined exception occured
				// Setting the AuthZ decision to "Indeterminate" allows the GSA to use other
				// AuthZ methods (i.e. Basic, NTLM, SSO) to determine access
				Common.error("AuthImpl::caught exception");
				Common.error(e.Message);
				status = "Indeterminate, unknown exception, check ac.log";
			}
			finally
			{
				// Make sure to remove the impersonation token
				if( wic != null)
					wic.Undo();
				Common.debug("exit AuthImpl::GetPermission::finally status=" + status);
			}
			Common.debug("exit AuthImpl::GetPermission return status=" + status);
			return status;
		}

		/// <summary>
		/// Method to perform an HTTP GET request for a URL.
		///This is used in combination with user impersonation
		/// to determine whether the user has access to the document
		/// </summary>
		/// <param name="url">target URL</param>
		/// <param name="cred">The credential to be used when accessing the URL</param>
		/// <returns></returns>
		public static String GetURL(String url, ICredentials cred)
		{
			Common.debug("inside GetURL internal");
			HttpWebRequest web = (HttpWebRequest) WebRequest.Create(url);
			web.AllowAutoRedirect = false;

			web.Method = "HEAD";
			web.Credentials = cred;
			
			//web.Credentials = CredentialCache.DefaultNetworkCredentials;
			//web.Credentials = new NetworkCredential("username", "password", "myDomain");
			Common.debug("Sending a Head request to target URL");
			//you can set a proxy if you need to
			//web.Proxy = new WebProxy("http://proxyhost.abccorp.com", 3128);
			// Get/Read the response from the remote HTTP server
			HttpWebResponse response  = (HttpWebResponse)web.GetResponse();
			if (Common.LOG_LEVEL == Common.DEBUG)
			{
				Stream responseStream = response.GetResponseStream();
				StreamReader reader = new StreamReader (responseStream);
				String res = reader.ReadToEnd ();
				responseStream.Close();
			}
			return Common.handleDeny(response);
		}

		/// <summary>
		/// Access URL using the default (current process identity's) credential
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		public String GetURL(String url)
		{
			Common.debug("GetURL =" + url);
			if (url.StartsWith("http"))
				return GetURL(url, CredentialCache.DefaultCredentials);
			else if (url.StartsWith("smb"))
			{
				String file = url;
				file = file.Replace("smb://", "\\\\");
				return GetFile(file);
			}
			else if (url.StartsWith("\\\\"))
			{
				return GetFile(url);
			}
			return "Deny";
		}

		public String GetFile(String url)
		{
			Common.debug("GetFile: " + url);
			//urldecode, because GSA sends URL for file in encoded format
			url = System.Web.HttpUtility.UrlDecode(url);
			Common.debug("afer : " + url);
			FileInfo fi = new FileInfo(url);
			Common.debug("after new FileInfo: ");
			FileStream fs = fi.OpenRead();
			Common.debug("after open read");
			fs.Close();
			Common.debug("after filestream close");
			return "Permit";
		}

		#endregion			
	}
}
