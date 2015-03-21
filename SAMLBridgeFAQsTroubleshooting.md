# SAML Bridge FAQs/Troubleshooting #
This document goes through some FAQs and troubleshooting procedures that can be used when working with the SAML Bridge.

## FAQs ##

### Testing the SAML Bridge Login.aspx page ###

Q: When I test the Login.aspx on the SAML Bridge and navigate directly to the page no user name is displayed.

A: This is due to not configuring Windows Integrated Authentication for the Login.aspx page in IIS or not removing "Anonynmous Access" from the page in IIS.

Q:  When I go to Login.aspx  on the SAML Bridge, I receive a message that says "ac.log permission denied."

A:  This is due to NETWORK SERVICE not having "Full" permissions for ac.log which is in the saml-bridge folder.  Add full file permissions for NETWORK SERVICE to the file using Windows Explorer.

Q: When I go to Login.aspx on the SAML Bridge I receive a 404 from IIS even though I can see that the page exists.

A: This will happen when the ASP .NET web extension in IIS is prohibited.  To allow this extension, go to IIS Manager tree view, under the host name, click Web Service Extensions. In the Web Service Extensions panel, look for ASP.NET v2.0 or later.  Click on  the Allow button.

Q: I am prompted for a password when I go to the Login.aspx page or any other page i know is using kerberos.

A: Does your browser support Kerberos?  See below for info on configuring your browser to use Kerberos.

Q: I am unable to run the SAML Bridge under the NETWORK SERVICE account.

A: This can happen if you have configured IIS as stated in this article.  http://support.microsoft.com/?kbid=332167 Reverse these steps.

### Using the GSA Simulator ###

Q:  When I go to gsa simulator I recieve a message that says "gsa.log permission denied."

A:  This is due to NETWORK SERVICE not having "Full" permissions to the gsa.log in the gsa-simulator folder.  Add full file permissions for NETWORK SERVICE using Windows Explorer.

Q: When running a search on the gsa simulator I get a 404 response.

A: Verify that in the web.config file for the gsa simulator the field that starts with 'add key="ac"'  points to the SAML Bridge.  Also verify that in the web.config file for the SAML Bridge, the field that starts with "add key="artifact\_consumer"  is pointing to the gsa simulator.

Q:  I am getting a "Deny" in the gsa-simulator.

A:  First verify that Kerberos is working when you go directly to the site by checking that the Content Server supports Kerberos and that your borwser supports kerberos.  See the info below on how to do this.  The next step is to make sure that delegation is properly setup.  See this link:  http://code.google.com/apis/searchappliance/documentation/50/admin/wia.html#prereq_activedir   Note that we have seen issues  where specifying a port when running setspn can cause delegation to fail.  Also verify that the gsa-simulator and the saml-bridge are running in IIS app pools that have the NETWORK SERVICE identity.

Q: In the gsa-simulator I am getting an "Indetermintent" response

A: First make sure that the URL that you are testing is accessible to the SAML Bridge, and that it supports kerberos.  If it does, then you will need to check the ac.log.  If you see this message:  "Either a requied impersonation level was not provided...."  then most likely the .net framework is wrong version.  See the info below to set .net framework in IIS to 2.0.

### Troubleshooting the GSA and SAML Bridge ###

Q: The Search Appliance is prompting me for a username and password.

A: This will happen if you have not selected the "Disable prompt for Basic authentication or NTLM authentication" on the " Google Search Appliance > Serving > Access Control" page of the admin console, or if you have a connector configured on the "Google Search Appliance > Connector Administration > Connectors" page in the admin console.

Q: The field "Disable prompt for Basic authentication or NTLM authentication" is not available on the " Google Search Appliance > Serving > Access Control" admin console page.

A: This will happen if you do not have content defined in the "Google Search Appliance > Crawl and Index > Crawler Access" page in the Admin Console.  It will also happen if you have LDAP configured on the " Google Search Appliance > Administration > LDAP Setup" in the admin conosle.

Q: When running a search on the GSA I get a 404

A: Make sure that GSA is pointing to the right URLs.  This is configured in the admin console on the " Google Search Appliance > Serving > Access Control" page.  Verify that the 'User Login URL', 'Artifact Resolver URL', and 'Authorization URL' fields point to the correct URLs on the SAML Bridge. Also verify that in the web.config file for the SAML Bridge, the field that starts with "add key="artifact\_consumer"  is pointing to the appliance.

Q: A 500 error is returned by the appliance

A: This can happen if the SAML bridge web.config file is pointing to a different GSA hostname (or IP) than the hostname that you used when initially doing your search.  This is because the GSA sets a cookie on your browser when you initially do a search, and the browser will not send this cookie to the appliance when the SAML bridge redirects you back to the appliance.  If you do not have the cookie, the appliance will abort the search.  To make sure you don't see this issue, be sure to query the appliance with the exact same name that you use in the web.config file for the SAML Bridge.

### Troubleshooting Kerberos and content servers ###

Q:  Is configuring Kerberos on my content server supported?

A:  Generally it is not supported, but please see this document http://code.google.com/p/google-saml-bridge-for-windows/wiki/ConfigKerberos
and the following FAQs for additional help.

Q: How do I tell if my content server supports Kerberos.

A: See the info below for more info on how to find the Negotiate Header

Q: The content server is sending the Negotiate header, but my browser is still prompting me for a password.

A:  See the info below on configuring your Browser for Kerberos.

Q:  Sharepoint is prompting for a password.  Kerberos does not seem to be configured on Sharepont.

A: Sharepoint forces NTLM when it is installed.  See this link to setup Kerberos with sharepoint:   http://support.microsoft.com/kb/215383

Q: Does the SAML Bridge support DFS?

A: DFS is not a supported content server, but some have managed to get it to work with the SAML Bridge.

## Troubleshooting checks/methods ##

### How to verify Windows Versions ###

  * **IIS version** - To verify the version of IIS, do this: From the Start menu, point to Administrative Tools, then click Internet Information Services (IIS) Manager. In IIS Manager, choose Help > About.  It should be version 5 or later.
  * **.net framework version used by IIS** - To verify the version, in the IIS Manager tree view, under the host name, click Web Service Extensions. In the Web Service Extensions panel, look for ASP.NET v2.0 or later.
  * **Windows Functional Domain Level** - To verify the windows functional domain level, from the start menu on the Domain Controller, open "Active Directory Domains and Trusts."  Select your domain and right click on it.  Select properties.  This will bring up the "General" tab.  In the middle of the page the domain functional level will show.  Should say "Windows Server 2003."

### Kerberos content server verification ###

Is Kerberos an authentication method for the content server?  You can install a header viewing tool like iehttpheaders IE plugin (http://www.blunck.se/iehttpheaders/iehttpheaders.html)  or the httpheaders firefox extension to see the headers.  You should see this header ( WWW-Authenticate: Negotiate) if Kerberos is supported on the content server.  Here is an example of a full http header:
```

HTTP/1.1 401 Unauthorized
Content-Length: 1656
Content-Type: text/html
Server: Microsoft-IIS/6.0
WWW-Authenticate: Negotiate
WWW-Authenticate: NTLM

```
### Configuring your browser to use Kerberos ###

  * **Configuring Internet Explorer to support Kerberos for the specified content server** -In IE you will need to add the content site to the Intranet sites for your browser to allow kerberos.  Tools ->  Internet Options -> Security Tab -> Local Intranet.  Also click on the "custom level" and verify that   underneath "User Authentication -> Logon"  you see "Automatic  logon with current username and password." is selected.  If this still does not fix the issue, users have reported that updated the browser to 7.0 fixes this problem.

  * **Configuring Firefox to support Kerberos for the specified content server** - You can edit the about.config param network.negotiate-auth.trusted.uris.  See this URL for more info on firefox and Kerberos: http://www.grolmsnet.de/kerbtut/firefox.html

### Troubleshooting IIS problems on the SAML Bridge ###

  * **Are the Content Server and the SAML Bridge on the same machine?** - See this URL:  http://support.microsoft.com/kb/896861/


  * **How to force IIS to use a certain version of  .net framework:** - If the are multiple .NET versions on your server, it is possible that IIS is using the wrong version.  You can reconfigure the .NET framework for IIS as follows:

> cd C:\WINDOWS\Microsoft.NET\Framework\your-version\
> aspnet\_regiis.exe -i

> When the command is done reconfiguring your IIS server to use the specified version of .NET, it displays a message like the following:

> Finished installing  ASP.NET (2.0.50727).