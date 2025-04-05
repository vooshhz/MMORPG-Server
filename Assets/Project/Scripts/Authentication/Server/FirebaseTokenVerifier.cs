using System.Net.Http;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;

public static class FirebaseTokenVerifier
{
    public static bool Verify(string idToken, out string uid)
    {
        uid = null;

        var client = new HttpClient();
        var requestBody = new { token = idToken };
        string json = JsonConvert.SerializeObject(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response = client.PostAsync("http://52.204.110.199:3000/verifyToken", content).Result;

        if (!response.IsSuccessStatusCode)
            return false;

        string resultJson = response.Content.ReadAsStringAsync().Result;
        var responseData = JsonConvert.DeserializeObject<Dictionary<string, string>>(resultJson);

        if (responseData.ContainsKey("uid"))
        {
            uid = responseData["uid"];
            return true;
        }

        return false;
    }
}
