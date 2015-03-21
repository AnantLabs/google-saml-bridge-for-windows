# How to configure your application to support Kerberos is out of the scope of SAML bridge. You should consult with your software vendor directly. The information below is for reference only.

# Kerberos related issues #

Add your content here.


For SAML Bridge to function correctly, Kerberos must be functioning in the content server. Even if Kerberos is enabled, some factors might force the content system to use security protocol other than Kerberos. For example, IIS uses a "Negotiate" protocol to decide whether to use Kerberos, NTLM. Because both protocols allow silent authentication for users, it's not obvious which one is really used. The selection of Kerberos or NTLM depends on several factors, one of which is whether client supports it. Most popular browsers do. The other factor sounds obvious, but it's not always the case: Kerberos must be functioning. In a Windows 2003 network environment, with Active Directory installed somewhere, Kerberos is available. But whether it's working in a IIS web site is quite a different story. Under certain situations Kerberos does not work, and additional steps need to be taken to make it work.

## 1. SPN Issues related to IIS Web site ##

You need to download setspn.exe, which is included in Windows 2003 SP1 support [tools](http://support.microsoft.com/kb/892777). For detailed information, see Microsoft knowledge base article: [How to use SPNs when you configure IIS web applications](http://support.microsoft.com/kb/929650).

### 1.1 A DNS alias is used, Network Service as the Application Pool's identity. ###

When users access a web site using a DNS alias instead of the machine name, you need to run the following command to make Kerberos work:

```
Setspn –A HTTP/www.myIISPortal.com NETBIOS_NAME_OF_IIS_SERVER
```

### 1.2 Using NetBIOS name, a domain accout as the Application Pool's identity. ###

When users access a web site using the machine name (NetBIOS name), but the application pool's identity is not Network Service but a domain account, again you need to create an SPN. However, this time, you need to create the SPN for the domain account:


```
Setspn –A HTTP/NETBIOS_NAME_OF_IIS_SERVER DOMAIN\USER
Setspn –A HTTP/NETBIOS_NAME_OF_IIS_SERVER.DOMAIN DOMAIN\USER (Need to provide fully qualified host name)
```


This will create an SPN for the Active Directory object DOMAIN\USER. You can run setspn -l DOMAIN\USER to find out what SPN accounts are under this object.

Note: You can only have a single SPN associated with any HTTP service (DNS) name, which means you cannot create SPNs for different service accounts mapped to the same HTTP server unless they are on different ports. The SPN can include a port number. For example, if you have executed the above commands but used Network Service as the identity of your Web site's App Pool, the Integrated Authentication would stop working. If you try to access the web site from a second machine with Windows IE, you will be prompted for user name and password but you will be denied access even if you type in the right one.

### 1.3. A DNS alias is configured, and a domain user account as the Application Pool's identity. ###

This is a combination of #1.1 and #1.2. You need to run:

```
Setspn –A HTTP/www.myIISPortal.com DOMAIN\USER
Setspn –A HTTP/physical_server_short_name DOMAIN\USER
Setspn –A HTTP/physical_server_full_name DOMAIN\USER
```

### 1.4. Load balanced web site ###

There is a virtual host name and several physical host names. All the physical machines must use the same domain account for their application pool in order for Kerberos to work. This translates to the same configuration as the previous section. The virtual host name should be used to create the SPN.

```
Setspn –A HTTP/www.myIISCluster.com DOMAIN\USER
Setspn –A HTTP/physical_server_1_short_name DOMAIN\USER
Setspn –A HTTP/physical_server_1_full_name DOMAIN\USER
Setspn –A HTTP/physical_server_2_short_name DOMAIN\USER
Setspn –A HTTP/physical_server_2_full_name DOMAIN\USER
```

## 2. Issues related to Windows Sharepoint ##

### 2.1. Sharepoint SPS 2003 or WSS 2.0 ###

Before SP2 of both products, Sharepoint forces NTLM authentication on the web sites that are extended/created to be Sharepoint sites.

To solve the problem, first we need to enable "Negotiate" authentication for the existing Sharepoint web sites:

Go to Sharepoint server, open a command line window, run the following commands

```
cd C:\Inetpub\Adminscripts
```

to list what's there:

```
cscript adsutil.vbs get w3svc/WebSite/virtual_directory/NTAuthenticationProviders
```

"WebSite" is the unique ID for your web site. You can find this ID from IIS manager. "virtual\_directory" is the alias of the virtual directory that you wanna change the setting. For example,

```
cscript adsutil.vbs get w3svc/1/NTAuthenticationProviders
```

This command will list the auth methods for "default web site". ("1" is the id of default web, and since we are gonna enable for the whole site, we don't need any virtual directory here. This point about virtual directory is not clear in MS article.)

Sharepoint sites will have "NTLM" defined.

To change it:

```
cscript adsutil.vbs set w3svc/ WebSite/root/NTAuthenticationProviders "Negotiate,NTLM"
```

for example:

```
cscript adsutil.vbs set w3svc/1 /NTAuthenticationProviders "Negotiate,NTLM"
```

See this article for details:
[http://support.microsoft.com/kb/215383](http://support.microsoft.com/kb/215383)

The above step will enable an SP site to support Kerberos, and now delegation should work.

Second: to prevent Sharepoint from forcing future web sites to use NTLM.
To achieve this, you need to install Windows Sharepoint Services 2.0 SP2. There is an SP2 for SPS 2003 but it requires SP2 of WSS 2.0 to be installed first. Because SP2003 uses WSS 2.0 as the web engine, I believe the trick is in WSS 2.0 SP2, not in SPS 2003 SP2. Since I don't have an extra machine to try, you can have ADP to try it out by only updating to WSS 2.0 SP2 first and create a dummy SP web site. Then use the above command to find out whether NTLM is forced.

### 2.2. Load balanced Sharepoint sites ###

By default, Sharepoint sites use Networ Service as the identity of the application pool that it creates. However, as we have discussed above, in order for load balanced webs to use Kerberos, a domain user account has to be used in the application pool. If you change the application pool to use a domain account, you will get an error trying to connect to the Sharepoint site using browser. The reason is that Sharepoint uses the identity of the application pool to talk to the databases (there could be multiple databases for configuration and contents), and even create database or tables. To solve this issue, we have to give the domain account access to existing Sharepoint database, and the privilege to create tables/new database.

If you are using SQL Server standard or enterprise edition, you can do this through management console. If you used SQL Server Desktop Engine, there is no UI that comes with it. However, as I found out, you can use the management UI from other editions to connect to the server. For example, SQL Server 2005 Management Stuio.

You need to create a new login, and assign this login the dbadmin, dbowner privileges. There are usually more than one databases used by Sharepoint. Make sure you assign privleges to all of them. For detailed steps, see this Microsoft article: http://support.microsoft.com/kb/823287.

## 3. Clustered File Share using Microsoft Cluster Server (MSCS) ##

Microsoft Cluster Server supports Kerberos so it's possible to support the connector. Here are the detailed [steps](http://support.microsoft.com/kb/224967) on how to setup clustered file share. By default NTLM is used for shared resources in the cluster, that's why you have to explicitly enable Kerberos. See this [knowledge base article](http://support.microsoft.com/?kbid=302389) for information on how to do this.

Basically, you should define a separate group for your file share, and add both an IP and Network Name resources to this group. The Kerberos authentication is defined on Network Name, and after you enable it, that name will show up in the Active Directory under the OU Computers as if it's a physical machine. Note: there are two "names" for a Network Name. After you create a Network Name resource and then open up the property dialog box, there is a name on the General tab, and also there is a name on the Parameters tab. There is a chance that they could be different if someone changes one but not the other. The one on the Parameters tab is the one used to create the node in the Active Directory.



# Details #

Add your content here.  Format your content with:
  * Text in **bold** or _italic_
  * Headings, paragraphs, and lists
  * Automatic links to other wiki pages