using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JiraWorkaround
{
    public class Jira
    {
        private string URL;

        private string username;
        private string password;

        private string lastQueryResponse;

        private Issue[] issues;
        
        public Jira(string _url)
        {
            URL = _url;

            lastQueryResponse = "";

            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        public void SetCredentials(string _username, string _password)
        {
            if (String.IsNullOrEmpty(_username) || String.IsNullOrEmpty(_password))
            {
                Console.WriteLine("Invalid username or password.");
                return;
            }

            username = _username;
            password = _password;
        }
        
        public void MakeQuery(string argument, string data, string method)
        {
            try
            {
                string finalURL = URL;

                if(argument != null)
                    finalURL = string.Format("{0}/{1}/", URL, argument);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(finalURL);

                request.Method = method;
                request.ContentType = "application/json";

                string base64Credentials = GetEncodedCredentials(username, password);
                request.Headers.Add("Authorization", "Basic " + base64Credentials);

                if (data != null)
                {
                    using (StreamWriter writeStream = new StreamWriter(request.GetRequestStream()))
                    {
                        writeStream.Write(data);
                    }
                }

                HttpWebResponse queryResponse = request.GetResponse() as HttpWebResponse;
                
                string response = string.Empty;

                using (StreamReader readStream = new StreamReader(queryResponse.GetResponseStream()))
                {
                    response = readStream.ReadToEnd();
                }

                lastQueryResponse = response;
            }
            catch (WebException e)
            {
                using (WebResponse response = e.Response)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse) response;
                    Console.WriteLine("Error code: {0}", httpResponse.StatusCode);
                    using (Stream data_ = response.GetResponseStream())
                    using (var reader = new StreamReader(data_))
                    {
                        string text_ = reader.ReadToEnd();
                        Console.WriteLine(text_);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public string GetQueryResponse()
        {
            return lastQueryResponse;
        }

        Issue ParseIssue(JObject data)
        {
            Issue issue = new Issue
            {
                Id = data["id"].ToString(),
                Self = data["self"].ToString(),
                Key = data["key"].ToString(),
                Summary = data["fields"]["summary"].ToString(),
                Description = data["fields"]["description"].ToString(),
                Labels = JArray.Parse(data["fields"]["labels"].ToString()).ToObject<string[]>()
            };

            return issue;
        }

        public Issue GetIssue()
        {
            try
            {
                if (string.IsNullOrEmpty(lastQueryResponse))
                    throw new Exception("Last query is empty. Make a new query or put valid arguments (for example, issue key) to this function.");

                Issue issue = new Issue();
                JObject response = JObject.Parse(lastQueryResponse);

                issue = ParseIssue(response);

                return issue;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public Issue GetIssue(string issueKey)
        {
            try
            {
                MakeQuery("issue/" + issueKey + "/", null, "GET");

                Issue issue = new Issue();
                JObject response = JObject.Parse(lastQueryResponse);

                issue = ParseIssue(response);

                return issue;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public Issue[] GetAllIssues()
        {
            try
            {
                if (string.IsNullOrEmpty(lastQueryResponse))
                {
                    Console.WriteLine("Last query is empty. Making a new query.");
                    MakeQuery("search/", null, "GET");
                }

                JObject response = JObject.Parse(lastQueryResponse);

                if (!response.ContainsKey("issues"))
                {
                    Console.WriteLine("No issues found. Making a new query.");
                    MakeQuery("search/", null, "GET");
                    response = JObject.Parse(lastQueryResponse);
                }

                var parsedData = JArray.Parse(response["issues"].ToString());
                var arrayOfIssues = parsedData.ToObject<JObject[]>();

                issues = new Issue[parsedData.Count];

                for (int i = 0; i < issues.Length; i++)
                {
                    issues[i] = ParseIssue(arrayOfIssues[i]);
                }

                return issues;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public void CreateIssue(string _projectKey, string _summary, string _description, string _issueType, List<string> _labels)
        {
            JObject issueData = JObject.FromObject(new
                {
                    fields = new
                    {
                        project = new
                        {
                            key = _projectKey
                        },
                        summary = _summary,
                        description = _description,
                        issuetype = new
                        {
                            name = _issueType
                        },
                        labels = _labels
                    }
                }
            );
            
            MakeQuery("issue/", issueData.ToString(Formatting.None), "POST");
        }

        private string GetEncodedCredentials(string _username, string _password)
        {
            string mergedCredentials = string.Format("{0}:{1}", _username, _password);
            byte[] byteCredentials = Encoding.UTF8.GetBytes(mergedCredentials);
            return Convert.ToBase64String(byteCredentials);
        }
    }
}
