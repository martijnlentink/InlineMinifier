public class BundleAndMinifyResponseFilter : MemoryStream
    {
        private StringBuilder _jsBuilder = new StringBuilder();
        private StringBuilder _cssBuilder = new StringBuilder();
        private readonly Stream _baseFilter;
        private bool _bundleMinifyCss;
        private bool _bundleMinifyJs;
        private bool _bundleMinifyIsEnabled;
        private TagPosition _cssPosition;
        private TagPosition _scriptPosition;

        public enum TagPosition
        {
            StartHead,
            EndHead,
            StartBody,
            EndBody,
        }

        private enum BeforeOrAfter 
        {
            Before,
            After
        }

        public BundleAndMinifyResponseFilter(Stream baseFilter, bool bundleMinifyCss = true, 
            bool bundleMinifyJs = true, bool bundleMinifyOnDebug = true, TagPosition cssPosition = TagPosition.EndHead,
            TagPosition scriptPosition = TagPosition.EndBody)
        {
            this._baseFilter = baseFilter;
            this._bundleMinifyCss = bundleMinifyCss;
            this._bundleMinifyJs = bundleMinifyJs;
            this._bundleMinifyIsEnabled = bundleMinifyOnDebug || (HttpContext.Current == null || !HttpContext.Current.IsDebuggingEnabled);
            this._cssPosition = cssPosition;
            this._scriptPosition = scriptPosition;
        }

        public override void Close()
        {
            string s = OmitInlineStyleTag(OmitInlineScriptTag(Encoding.UTF8.GetString(GetBuffer())));
            //var scriptTag = GetReplacementByPosition(_scriptPosition);
            //var cssTag = GetReplacementByPosition(_cssPosition);
            if (_bundleMinifyIsEnabled && _bundleMinifyJs && _jsBuilder.Length > 0)
                ReplaceEm(_scriptPosition, ref s, "<script>" + Minify.JS(_jsBuilder.ToString()) + "</script>");//s = s.Replace(scriptTag, "<script>" + Minify.JS(_jsBuilder.ToString()) + "</script>" + scriptTag);
            if (_bundleMinifyIsEnabled && _bundleMinifyCss && _cssBuilder.Length > 0)
                ReplaceEm(_cssPosition, ref s,
                                         "<style>" + Minify.CSS(_cssBuilder.ToString()) + "</style>");//s = s.Replace(cssTag, "<style>" + Minify.CSS(_cssBuilder.ToString()) + "</style>" + cssTag);
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            _baseFilter.Write(bytes, 0, bytes.Length);
            _baseFilter.Close();
            base.Close();
        }

        protected void ReplaceEm(TagPosition position, ref string html, string newTag)
        {
            string matchString;
            bool isInsertBefore = false;
            switch (position)
            {
                case TagPosition.StartHead:
                    matchString = "<head( .*)?>";
                    break;
                case TagPosition.EndHead:
                    isInsertBefore = true;
                    matchString = "</head>";
                    break;
                case TagPosition.StartBody:
                    matchString = "<body( .*)?>";
                    break;
                case TagPosition.EndBody:
                    isInsertBefore = true;
                    matchString = "</body>";
                    break;
                default:
                    throw new ArgumentOutOfRangeException("position");
            }

            html = Regex.Replace(html, matchString, isInsertBefore
                                                 ? (MatchEvaluator) (match => newTag + match)
                                                 : (match => match + newTag));
        }

        protected string OmitInlineScriptTag(string html)
        {
            if (!_bundleMinifyJs || !_bundleMinifyIsEnabled)
                return html;
            else
                return new Regex("(?<Tag><script(?<Attributes>[^>]*)>(?<InnerContent>([^<]|<[^/]|</[^s]|</s[^c])*)</script>)", RegexOptions.IgnoreCase | RegexOptions.Multiline).Replace(html, (MatchEvaluator)(m =>
                {
                    GroupCollection local_0 = m.Groups;
                    if (local_0["Attributes"].Value.Contains(" src="))
                        return local_0["Tag"].Value;
                    _jsBuilder.Append(local_0["InnerContent"].Value + ";");
                    return "";
                }));
        }

        protected string OmitInlineStyleTag(string html)
        {
            if (!this._bundleMinifyCss || !this._bundleMinifyIsEnabled)
                return html;
            else
                return new Regex("<style[^>]*>(?<InnerContent>([^<]|<[^/]|</[^s]|</s[^t])*)</style>", RegexOptions.IgnoreCase | RegexOptions.Multiline).Replace(html, (MatchEvaluator)(m =>
                {
                    this._cssBuilder.Append(m.Groups["InnerContent"].Value);
                    return "";
                }));
        }
    }
