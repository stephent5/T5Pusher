<%@ WebHandler Language="C#" Class="AuthHandler" %>

using System;
using System.Web;
using System.Security.Cryptography;
using System.Text;
using System.Configuration;

public class AuthHandler : IHttpHandler {
    
    public void ProcessRequest (HttpContext context) {
        context.Response.ContentType = "text/json";

        String connectionid = context.Request.QueryString["connectionid"];
        if (String.IsNullOrEmpty(connectionid))
        {
            connectionid = context.Request.Params["connectionid"];
        }

        String groupName = context.Request.QueryString["groupName"];
        if (String.IsNullOrEmpty(groupName))
        {
            groupName = context.Request.Params["groupName"];
        }

        String broadcastString = context.Request.QueryString["broadcast"];
        if (String.IsNullOrEmpty(broadcastString))
        {
            broadcastString = context.Request.Params["broadcast"];
        }
        int broadcast = Convert.ToInt32(broadcastString);

        String deleteString = context.Request.QueryString["delete"];
        if (String.IsNullOrEmpty(deleteString))
        {
            deleteString = context.Request.Params["delete"];
        }
        int delete = Convert.ToInt32(deleteString);

        if (broadcast == 1)
        {
            //can do extra validation here on the user if this is going to be a broadcast 
            //as to be a user who can broadcast you would probably be a more authorised user
        }

        if (String.IsNullOrEmpty(groupName))
        {
            //this is just a connection authorisation request!!
            context.Response.Write(GenerateConnectionAuthorisationString(connectionid));
        }
        else
        {
            //this is a request to create a hash for a secure channel!!
            context.Response.Write(GenerateConnectionAuthorisationStringForSecureChannel(connectionid, groupName, broadcast, delete));
        }
    }  

    public String GenerateConnectionAuthorisationString(string connectionid)
    {
        String appkey = ConfigurationManager.AppSettings["T5PusherAppKey"];
        String appSecret = ConfigurationManager.AppSettings["T5PusherAppSecret"];

        String returnJSON = "{}";

        if (!String.IsNullOrEmpty(connectionid))
        {
            String hash = getMd5Hash(appSecret + ":" + connectionid);

            returnJSON = @"{""auth"":""" + appkey + ":" + hash + @"""}";
        }

        return returnJSON;
    }

    public String GenerateConnectionAuthorisationStringForSecureChannel(string connectionid, string groupName, int broadcast, int delete)
    {
        String appkey = ConfigurationManager.AppSettings["T5PusherAppKey"];
        String appSecret = ConfigurationManager.AppSettings["T5PusherAppSecret"];

        String returnJSON = "{}";

        if (!String.IsNullOrEmpty(connectionid))
        {
            String hash;
            if (broadcast > 0)
            {
                hash = getMd5Hash(appSecret + ":" + connectionid + ":" + groupName + ":broadcast"); 
            }
            else
            {
                String deletePart = "";
                if (delete == 1)
                {
                    deletePart = ":delete";
                }
                hash = getMd5Hash(appSecret + ":" + connectionid + ":" + groupName + deletePart); 
            }
            
            returnJSON = @"{""auth"":""" + appkey + ":" + hash + @"""}";
        }

        return returnJSON;
    }


    public String getMd5Hash(String input)
    {

        MD5 md5Hasher = MD5.Create();

        // Convert the input string to a byte array and compute the hash.
        byte[] data = md5Hasher.ComputeHash(System.Text.Encoding.Default.GetBytes(input));

        // Create a new Stringbuilder to collect the bytes
        // and create a string.
        StringBuilder sBuilder = new StringBuilder();

        // Loop through each byte of the hashed data 
        // and format each one as a hexadecimal string.
        int i = 0;
        for (i = 0; i <= data.Length - 1; i++)
        {
            sBuilder.Append(data[i].ToString("x2"));
        }

        // Return the hexadecimal string.
        return sBuilder.ToString();
    }
 
    public bool IsReusable {
        get {
            return false;
        }
    }

}