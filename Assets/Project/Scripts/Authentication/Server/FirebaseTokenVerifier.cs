using System;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace MyGame.Server
{
    public static class FirebaseTokenVerifier
    {
        public static bool Verify(string idToken, out string uid)
{
    uid = null;
    
    if (string.IsNullOrEmpty(idToken))
    {
        Debug.LogError("Token verification failed: Token is null or empty");
        return false;
    }

    try
    {
        var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(10); // Add timeout
        
        var requestBody = new { token = idToken };
        string json = JsonConvert.SerializeObject(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        Debug.Log($"Sending token verification request to verification server...");
        HttpResponseMessage response = client.PostAsync("http://52.204.110.199:3000/verifyToken", content).Result;

        if (!response.IsSuccessStatusCode)
        {
            Debug.LogError($"Token verification request failed with status code: {response.StatusCode}");
            return false;
        }

        string resultJson = response.Content.ReadAsStringAsync().Result;
        Debug.Log($"Token verification response: {resultJson}");
        
        var responseData = JsonConvert.DeserializeObject<Dictionary<string, string>>(resultJson);

        if (responseData != null && responseData.ContainsKey("uid"))
        {
            uid = responseData["uid"];
            Debug.Log($"Successfully extracted UID from token: {uid}");
            return true;
        }
        
        Debug.LogError("Token verification response did not contain a valid UID");
        return false;
    }
    catch (HttpRequestException ex)
    {
        Debug.LogError($"Network error during token verification: {ex.Message}");
        return false;
    }
    catch (TaskCanceledException)
    {
        Debug.LogError("Token verification timed out");
        return false;
    }
    catch (Exception ex)
    {
        Debug.LogError($"Exception during token verification: {ex.Message}");
        return false;
    }
}   
    }
}