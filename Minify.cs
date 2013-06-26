internal static class Minify
    {
        internal static string JS(string js)
        {
            return new Minifier().MinifyJavaScript(js, new CodeSettings()
            {
                EvalTreatment = EvalTreatment.MakeImmediateSafe,
                PreserveImportantComments = false
            });
        }

        internal static string CSS(string css)
        {
            Minifier minifier = new Minifier();
            CssSettings settings = new CssSettings()
            {
                CommentMode = CssComment.None
            };
            return minifier.MinifyStyleSheet(css, settings);
        }
    }
