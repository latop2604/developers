﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using CodeFluent.Runtime.Utilities;
using CodeFluent.Runtime.Web.Utilities;
using RowShareTool.Utilities;

namespace RowShareTool.Model
{
    public class Server : TreeItem
    {
        public const string CookieName = ".RSAUTH";

        private User _user;
        private Folders _folders;
        private bool _showMessageBoxOnError = true;

        public Server()
            : base(null, true)
        {
            _folders = new Folders(this);
        }

        public bool ShowMessageBoxOnError
        {
            get { return _showMessageBoxOnError; }
            set { _showMessageBoxOnError = value; }
        }

        [Browsable(false)]
        public override string Url
        {
            get
            {
                return DisplayName;
            }
        }

        [Browsable(false)]
        [JsonUtilities(IgnoreWhenSerializing = true)]
        public string Cookie
        {
            get
            {
                return GetProperty<string>();
            }
            set
            {
                SetProperty(value);
            }
        }

        public override void Reload()
        {
            _user = null;
            if (ChildrenClear())
            {
                LoadChildren();
            }
        }

        protected override void LoadChildren()
        {
            base.LoadChildren();
            var user = User;
            if (user != null)
            {
                Children.Add(user);
                Children.Add(_folders);
            }
            else
            {
                var login = new Login(this);
                Children.Add(login);
            }
            OnPropertyChanged(nameof(Children));
        }

        [JsonUtilities(IgnoreWhenSerializing = true)]
        [Browsable(false)]
        public Folders Folders
        {
            get
            {
                return _folders;
            }
        }

        [Browsable(false)]
        [JsonUtilities(IgnoreWhenSerializing = true)]
        public User User
        {
            get
            {
                if (_user == null)
                {
                    var user = new User(this);
                    Call("user", user, null);
                    _user = !string.IsNullOrWhiteSpace(user.Email) ? user : null;
                }
                return _user;
            }
        }

        public List LoadList(string id)
        {
            var folder = new Folder(this);
            var list = new List(folder);
            list.Id = ConvertUtilities.ChangeType(id, Guid.Empty);
            var guid = Guid.NewGuid().ToString();
            list.DisplayName = guid;
            Call("list/load/" + id, list, null);
            if (list.DisplayName == guid) // nothing was read
                return null;

            return list;
        }

        public Server Clone()
        {
            var clone = new Server();
            clone.DisplayName = this.DisplayName;
            clone.Cookie = this.Cookie;
            return clone;
        }

        public object Call(ServerCallParameters parameters, object parent)
        {
            return Call(parameters, null, parent);
        }

        public object Call(string apiCall, object targetObject, object parent)
        {
            if (apiCall == null)
                throw new ArgumentNullException(nameof(apiCall));

            var sp = new ServerCallParameters();
            sp.Api = apiCall;
            return Call(sp, targetObject, parent);
        }

        public object Call(ServerCallParameters parameters, object targetObject, object parent)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            using (var client = new CookieWebClient())
            {
                if (Cookie != null)
                {
                    client.Cookies.Add(new Cookie(CookieName, Cookie, "/", new Uri(Url).Host));
                }

                var uri = new EditableUri(Url + "/api/" + parameters.Api);
                if (!string.IsNullOrWhiteSpace(parameters.Format))
                {
                    uri.Parameters["f"] = parameters.Format;
                }

                if (parameters.Lcid != 0)
                {
                    uri.Parameters["l"] = parameters.Lcid;
                }

                client.Encoding = Encoding.UTF8;

                string s;
                try
                {
                    s = client.DownloadString(uri.ToString());
                }
                catch (WebException e)
                {
                    if (ShowMessageBoxOnError)
                    {
                        var eb = new ErrorBox(e, e.GetErrorText(null));
                        eb.ShowDialog();
                    }
                    throw;
                }

                var options = new JsonUtilitiesOptions();
                options.CreateInstanceCallback = (e) =>
                {
                    Type type = (Type)e.Value;
                    if (typeof(TreeItem).IsAssignableFrom(type))
                    {
                        e.Value = Activator.CreateInstance(type, new object[] { parent });
                        e.Handled = true;
                    }
                };

                if (targetObject != null)
                {
                    JsonUtilities.Deserialize(s, targetObject, options);
                    return null;
                }
                return JsonUtilities.Deserialize(s);
            }
        }

        public object PostCall(ServerCallParameters parameters, object data)
        {
            return PostCall(parameters, null, null, data);
        }

        public object PostCall(ServerCallParameters parameters, object parent, object data)
        {
            return PostCall(parameters, null, parent, data);
        }

        public object PostCall(string apiCall, object targetObject, object data)
        {
            return PostCall(apiCall, targetObject, null, data);
        }

        public object PostCall(string apiCall, object targetObject, object parent, object data)
        {
            if (apiCall == null)
                throw new ArgumentNullException(nameof(apiCall));

            var sp = new ServerCallParameters();
            sp.Api = apiCall;
            return PostCall(sp, targetObject, parent, data);
        }

        public object PostCall(ServerCallParameters parameters, object targetObject, object parent, object data)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            string sdata = SerializeData(data);
            using (var client = new CookieWebClient())
            {
                if (Cookie != null)
                {
                    client.Cookies.Add(new Cookie(CookieName, Cookie, "/", new Uri(Url).Host));
                }

                var uri = new EditableUri(Url + "/api/" + parameters.Api);

                if (parameters.Lcid != 0)
                {
                    uri.Parameters["l"] = parameters.Lcid;
                }
                client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
                client.Encoding = Encoding.UTF8;

                string s;
                try
                {
                    s = client.UploadString(uri.ToString(), sdata);
                }
                catch (WebException e)
                {
                    if (ShowMessageBoxOnError)
                    {
                        var eb = new ErrorBox(e, e.GetErrorText(null));
                        eb.ShowDialog();
                    }
                    throw;
                }

                var options = new JsonUtilitiesOptions();
                options.CreateInstanceCallback = (e) =>
                {
                    var type = (Type)e.Value;
                    if (typeof(TreeItem).IsAssignableFrom(type))
                    {
                        e.Value = Activator.CreateInstance(type, new object[] { parent });
                        e.Handled = true;
                    }
                };

                if (targetObject != null)
                {
                    JsonUtilities.Deserialize(s, targetObject, options);
                    return null;
                }
                return JsonUtilities.Deserialize(s);
            }
        }

        public object PostBlobCall(string apiCall, object targetObject, object parent, object data, Blob blob)
        {
            if (apiCall == null)
                throw new ArgumentNullException(nameof(apiCall));
            if (blob == null)
                throw new ArgumentNullException(nameof(blob));

            var sp = new ServerCallParameters();
            sp.Api = apiCall;
            return PostBlobCall(sp, targetObject, parent, data, new[] { blob });
        }

        public object PostBlobCall(ServerCallParameters parameters, object targetObject, object parent, object data, IEnumerable<Blob> blobs)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            if (blobs == null)
                throw new ArgumentNullException(nameof(blobs));

            string sdata = SerializeData(data);

            using (var httpHandler = new HttpClientHandler())
            using (var client = new HttpClient(httpHandler))
            {
                if (Cookie != null)
                {
                    httpHandler.CookieContainer.Add(new Cookie(CookieName, Cookie, "/", new Uri(Url).Host));
                }
                var uri = new EditableUri(Url + "/api/" + parameters.Api);

                if (parameters.Lcid != 0)
                {
                    uri.Parameters["l"] = parameters.Lcid;
                }

                var message = new HttpRequestMessage(HttpMethod.Post, uri.ToString());

                var mpfdContent = new MultipartFormDataContent();
                mpfdContent.Add(new StringContent(sdata, Encoding.UTF8, "application/json"), "data");

                foreach (var blob in blobs)
                {
                    var byteArrayContent = new StreamContent(File.Open(blob.TempFilePath, FileMode.Open, FileAccess.Read));
                    byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue(blob.ContentType);

                    mpfdContent.Add(byteArrayContent, blob.ColumnName, blob.FileName);
                }

                message.Content = mpfdContent;

                string s;
                try
                {
                    var result = client.SendAsync(message).Result;
                    s = result.Content.ReadAsStringAsync().Result;
                    if (!result.IsSuccessStatusCode)
                    {
                        var x = s.Length + 2;
                    }
                    result.EnsureSuccessStatusCode();
                }
                catch (AggregateException agg)
                {
                    if (ShowMessageBoxOnError)
                    {
                        agg.Handle(e =>
                        {
                            var we = e as WebException;
                            if (we != null)
                            {
                                var eb = new ErrorBox(e, e.GetErrorText(null));
                                eb.ShowDialog();
                            }
                            return false;
                        });
                    }
                    throw;
                }
                catch (HttpRequestException e)
                {
                    if (ShowMessageBoxOnError)
                    {
                        var eb = new ErrorBox(e, e.GetErrorText(null));
                        eb.ShowDialog();
                    }
                    throw;
                }

                var options = new JsonUtilitiesOptions();
                options.CreateInstanceCallback = (e) =>
                {
                    var type = (Type)e.Value;
                    if (typeof(TreeItem).IsAssignableFrom(type))
                    {
                        e.Value = Activator.CreateInstance(type, new object[] { parent });
                        e.Handled = true;
                    }
                };

                if (targetObject != null)
                {
                    JsonUtilities.Deserialize(s, targetObject, options);
                    return null;
                }
                return JsonUtilities.Deserialize(s);
            }
        }

        public string DownloadCall(string apiCall)
        {
            if (apiCall == null)
                throw new ArgumentNullException(nameof(apiCall));

            var sp = new ServerCallParameters();
            sp.Api = apiCall;
            return DownloadCall(sp);
        }

        public string DownloadCall(ServerCallParameters parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            using (var client = new CookieWebClient())
            {
                if (Cookie != null)
                {
                    client.Cookies.Add(new Cookie(CookieName, Cookie, "/", new Uri(Url).Host));
                }

                var uri = new EditableUri(Url + "/" + parameters.Api);
                if (!string.IsNullOrWhiteSpace(parameters.Format))
                {
                    uri.Parameters["f"] = parameters.Format;
                }

                if (parameters.Lcid != 0)
                {
                    uri.Parameters["l"] = parameters.Lcid;
                }

                try
                {
                    var filePath = LongPath.GetTempFileName();
                    client.DownloadFile(uri.ToString(), filePath);
                    return filePath;
                }
                catch (WebException e)
                {
                    if (ShowMessageBoxOnError)
                    {
                        var eb = new ErrorBox(e, e.GetErrorText(null));
                        eb.ShowDialog();
                    }
                    throw;
                }
            }
        }

        private string SerializeData(object data)
        {
            return data is string ? (string)data : JsonUtilities.Serialize(data, JsonSerializationOptions.Default | JsonSerializationOptions.DateFormatIso8601);
        }
    }
}
