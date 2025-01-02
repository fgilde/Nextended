using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nextended.Core.Contracts;
using Nextended.Core.Extensions;

namespace Nextended.Core.Encode
{
    public abstract class StringEncodingBase<T> : IStringEncodingExt
        where T : StringEncodingBase<T>
    {
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        public T SetEncoding(Encoding value)
        {
            Encoding = value;
            return (T)this;
        }

        private readonly Dictionary<string, IList<Func<string, string>>> executers = new Dictionary<string, IList<Func<string, string>>>();
        public IDictionary<string, string> Replacements { get; set; } = new Dictionary<string, string>
        {
            {"/", "-"}
        };

        public string Encode(string plainText) => AfterEncode(EncodeCore(BeforeEncode(plainText)));

        public string Decode(string encodedText) => AfterDecode(DecodeCore(BeforeDecode(encodedText)));

        protected abstract string EncodeCore(string str);
        protected abstract string DecodeCore(string str);

        IStringEncodingExt IStringEncodingExt.AfterEncode(Func<string, string> onAfterEncode) { return AfterEncode(onAfterEncode); }

        IStringEncodingExt IStringEncodingExt.BeforeDecode(Func<string, string> onBeforeDecode) { return BeforeDecode(onBeforeDecode); }

        IStringEncodingExt IStringEncodingExt.AfterDecode(Func<string, string> onAfterDecode) { return AfterDecode(onAfterDecode); }

        IStringEncodingExt IStringEncodingExt.BeforeEncode(Func<string, string> onBeforeEncode) { return BeforeEncode(onBeforeEncode); }

        public T ClearReplacements()
        {
            Replacements.Clear();
            return (T) this;
        }

        public T AddReplacements(string key, string value)
        {
            Replacements.Add(key, value);
            return (T) this;
        }

        public T AddReplacements(params KeyValuePair<string, string>[] pairs)
        {
            Replacements.AddRange(pairs);
            return (T) this;
        }

        public T SetReplacements(IDictionary<string, string> replacements)
        {
            Replacements = replacements;
            return (T) this;
        }

        public T BeforeEncode(Func<string, string> onBeforeEncode)
        {
            EnsureExecuter(nameof(BeforeEncode)).Add(onBeforeEncode);
            return (T) this;
        }
        
        public T AfterEncode(Func<string, string> onAfterEncode)
        {
            EnsureExecuter(nameof(AfterEncode)).Add(onAfterEncode);
            return (T) this;
        }

        public T BeforeDecode(Func<string, string> onBeforeDecode)
        {
            EnsureExecuter(nameof(BeforeDecode)).Add(onBeforeDecode);
            return (T) this;
        }

        public T AfterDecode(Func<string, string> onAfterDecode)
        {
            EnsureExecuter(nameof(AfterDecode)).Add(onAfterDecode);
            return (T) this;
        }

        private string BeforeEncode(string str)
        {
            (executers.Get(nameof(BeforeEncode)) ?? Enumerable.Empty<Func<string, string>>()).Apply(func => str = func(str));
            return str;
        }
        private string AfterEncode(string str)
        {
            str = (Replacements ?? new Dictionary<string, string>()).Where(p => !string.IsNullOrEmpty(p.Key)).Aggregate(str, (current, pair) => current.Replace(pair.Key, pair.Value));

            (executers.Get(nameof(AfterEncode)) ?? Enumerable.Empty<Func<string, string>>())
                .Apply(func => str = func(str));

            return str;
        }

        private string BeforeDecode(string str)
        {            
            (executers.Get(nameof(BeforeDecode)) ?? Enumerable.Empty<Func<string, string>>())
                .Apply(func => str = func(str));
           

            return (Replacements ?? new Dictionary<string, string>()).Where(p => !string.IsNullOrEmpty(p.Value)).Aggregate(str, (current, pair) => current.Replace(pair.Value, pair.Key));
        }


        private string AfterDecode(string str)
        {
            (executers.Get(nameof(AfterDecode)) ?? Enumerable.Empty<Func<string, string>>()).Apply(func => str = func(str));
            return str;
        }

        private IList<Func<string, string>> EnsureExecuter(string name)
        {
            if (!executers.ContainsKey(name))
                executers.Add(name, new List<Func<string, string>>());
            return executers[name];
        }
    }
}