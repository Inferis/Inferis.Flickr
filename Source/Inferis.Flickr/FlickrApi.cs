using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml;
using System.Xml.Linq;

namespace Inferis.Flickr {
    public class FlickrApi : DynamicObject {
        private readonly string apikey;

        protected FlickrApi(string apikey)
        {
            this.apikey = apikey;
        }

        public static dynamic WithKey(string key)
        {
            return new FlickrApi(key);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = new Executor(string.Concat(binder.Name.Substring(0, 1).ToLower(), binder.Name.Substring(1)), InvokeCall);
            return true;
        }

        private dynamic InvokeCall(string path, IDictionary<string, object> args)
        {
            args["apiKey"] = apikey;

            var urlArgs = string.Join("&", args.Select(a => string.Concat(Underscorize(a.Key), "=", (a.Value ?? "").ToString())));
            var url = string.Format("http://api.flickr.com/services/rest/?method=flickr.{0}&{1}", path, urlArgs);

            var response = WebRequest.Create(url).GetResponse();
            using (var reader = new StreamReader(response.GetResponseStream())) {
                return XDocument.Parse(reader.ReadToEnd());
            }
        }

        private string Underscorize(string key)
        {
            return new string(key.SelectMany(c => char.IsUpper(c) ? new[] { '_', char.ToLower(c) } : new[] { c }).ToArray());
        }

        private class Executor : DynamicObject {
            private string path;
            private readonly Func<string, IDictionary<string, object>, dynamic> invoke;

            public Executor(string part, Func<string, IDictionary<string, object>, dynamic> invoke)
            {
                path = part;
                this.invoke = invoke;
            }

            public override bool TryGetMember(GetMemberBinder binder, out object result)
            {
                result = new Executor(string.Concat(path, ".", binder.Name.Substring(0, 1).ToLower(), binder.Name.Substring(1)), invoke);
                return true;
            }

            public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
            {
                if (binder.CallInfo.ArgumentCount == 0 || binder.CallInfo.ArgumentCount != args.Length)
                    throw new InvalidOperationException("You must used named parameters for the flickr api.");

                var invokeArgs = Enumerable.Range(0, binder.CallInfo.ArgumentCount)
                    .ToDictionary(i => binder.CallInfo.ArgumentNames[i], i => args[i]);

                result = invoke(string.Concat(path, ".", binder.Name.Substring(0, 1).ToLower(), binder.Name.Substring(1)), invokeArgs);
                return true;
            }
        }
    }

    public class XmlProxy : DynamicObject
    {
        public XmlProxy(string xml)
        {

        }
    }
}
