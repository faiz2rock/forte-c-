﻿using System;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Http;
using Newtonsoft.Json;

namespace Forte
{
    internal static class Requestor
    {
        public static string PostString<T>(string url, T content, string username, string password, string authAccountID)
        {
            string jsonString = JsonConvert.SerializeObject(content);
            var wr = GetResponse(url, "POST", jsonString, username, password, authAccountID);
            return wr;
        }

        public static string Get(string URL, string strUser, string strPasswd, string authAccountID)
        {
            string getResp = GetResponse(URL, "GET", "", strUser, strPasswd, authAccountID);
            return getResp;
        }

        public static string PutString<T>(string url, T content, string username, string password, string authAccountID)
        {
            string jsonString = JsonConvert.SerializeObject(content);
            var wr = GetResponse(url, "PUT", jsonString, username, password, authAccountID);
            return wr;
        }

        public static string Delete(string URL, string strUser, string strPasswd, string authAccountID)
        {
            string getResp = GetResponse(URL, "DELETE", "", strUser, strPasswd, authAccountID);
            if (getResp != String.Empty && (getResp.IndexOf("#ERROR#") == -1))
            {
                return getResp;
            }
            else
            {
                return "#ERROR#";
            }
        }

        private static string ReadStream(Stream stream)
        {
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        internal static string GetResponse(String URL, String method, string requestBody, string strUser, string strPasswd, string authAccountID)
        {
            HttpResponseMessage response;
            using (var client = new HttpClient())
            {
                string authheadertext = Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", strUser, strPasswd)));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authheadertext);
                client.DefaultRequestHeaders.Add("X-Forte-Auth-Account-Id", authAccountID);

                StringContent content = new System.Net.Http.StringContent(requestBody, Encoding.UTF8, "application/json");
                try
                {
                    switch ((method).ToUpper())
                    {
                        default:
                        case "GET":
                            response = client.GetAsync(URL).Result;
                            break;

                        case "POST":
                            response = client.PostAsync(URL, content).Result;
                            break;

                        case "PUT":
                            response = client.PutAsync(URL, content).Result;
                            break;

                        case "DELETE":
                            response = client.DeleteAsync(URL).Result;
                            break;
                    }
                }
                catch (AggregateException webException)
                {
                    if (webException.Data != null)
                    {
                        var forteError = new ForteError();
                        forteError.ErrorType = "AggregateException";
                        forteError.Message = webException.InnerException.ToString();
                        var statusCode = HttpStatusCode.Forbidden;
                        throw new ForteException(statusCode, forteError, forteError.Message);
                    }

                    throw;
                }
                catch (WebException webException)
                {
                    if (webException.Response != null)
                    {
                        var statusCode = ((HttpWebResponse)webException.Response).StatusCode;

                        var forteError = new ForteError();

                        if (webException.Response.ResponseUri.ToString().Contains("oauth"))
                            forteError = Mapper<ForteError>.MapFromJson(ReadStream(webException.Response.GetResponseStream()));
                        else
                            forteError = Mapper<ForteError>.MapFromJson(ReadStream(webException.Response.GetResponseStream()), "error");

                        throw new ForteException(statusCode, forteError, forteError.Message);
                    }

                    throw;
                }
                catch (Exception ex)
                {
                    throw ex;
                }


                if (response != null && response.IsSuccessStatusCode)
                {
                    var resultMessage = response.Content.ReadAsStringAsync().Result;

                    switch ((method).ToUpper())
                    {
                        case "POST":
                            resultMessage = "HttpStatus Code: " + response.StatusCode
                                 + "\r\n"
                                 + "Record Successfully."
                                 + "\r\n"
                                 + "||"
                                 + resultMessage;
                            break;

                        case "PUT":
                            resultMessage = "HttpStatus Code: " + response.StatusCode
                              + "\r\n"
                              + "Record Successfully Updated."
                              + "\r\n"
                              + "||"
                              + resultMessage;

                            break;

                        case "DELETE":
                            resultMessage = "HttpStatus Code: " + response.StatusCode
                              + "\r\n"
                              + "Record Successfully Deleted."
                              + "\r\n"
                              + "||"
                              + resultMessage;

                            break;

                        default:
                            break;
                    }
                    return resultMessage;
                }
                else if (response != null && !(response.IsSuccessStatusCode))
                {
                    var resultMessage = "HttpStatus Code: " + response.StatusCode
                                        + "||" + "#ERROR# "
                                        + response.Content.ReadAsStringAsync().Result;
                    return resultMessage;
                }
                else
                {
                    return "#ERROR# " + response.ToString();
                }
            }


        }
    }
}