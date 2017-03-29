﻿using javnov.Selenium.Axe.Properties;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using System;
using System.Net;

namespace javnov.Selenium.Axe
{
    /// <summary>
    /// Fluent style builder for invoking aXe. Instantiate a new Builder and configure testing with the include(),
    /// exclude(), and options() methods before calling analyze() to run.
    /// </summary>
    public class AxeBuilder
    {
        private readonly IWebDriver _webDriver;
        private readonly IncludeExcludeManager _includeExcludeManager = new IncludeExcludeManager();

        public string Options { get; set; } = "null";

        /// <summary>
        /// Initialize an instance of <see cref="AxeBuilder"/>
        /// </summary>
        /// <param name="webDriver">Selenium driver to use</param>
        public AxeBuilder(IWebDriver webDriver)        {
            if (webDriver == null)
                throw new ArgumentNullException(nameof(webDriver));

            _webDriver = webDriver;
            _webDriver.Inject();
        }

        /// <summary>
        /// Initialize an instance of <see cref="AxeBuilder"/>
        /// </summary>
        /// <param name="webDriver">Selenium driver to use</param>
        /// <param name="axeScriptUrl">aXe script URL</param>
        public AxeBuilder(IWebDriver webDriver, Uri axeScriptUrl) : this(webDriver, axeScriptUrl, new WebClient()) { }

        /// <summary>
        /// Initialize an instance of <see cref="AxeBuilder"/>
        /// </summary>
        /// <param name="webDriver">Selenium driver to use</param>
        /// <param name="axeScriptUrl">aXe script URL</param>
        /// <param name="webClient">Webclient to use to get aXe script's content</param>
        public AxeBuilder(IWebDriver webDriver, Uri axeScriptUrl, WebClient webClient)
        {
            if (webDriver == null)
                throw new ArgumentNullException(nameof(webDriver));

            if (axeScriptUrl == null)
                throw new ArgumentNullException(nameof(axeScriptUrl));

            if (webClient == null)
                throw new ArgumentNullException(nameof(webClient));

            var contentDownloader = new CachedContentDownloader(webClient);
            _webDriver.Inject(axeScriptUrl, contentDownloader);
        }

        /// <summary>
        /// Include selectors
        /// </summary>
        /// <param name="selectors">Any valid CSS selectors</param>
        /// <returns></returns>
        public AxeBuilder Include(params string[] selectors)
        {
            _includeExcludeManager.Include(selectors);
            return this;
        }

        /// <summary>
        /// Exclude selectors
        /// </summary>
        /// <param name="selectors">Any valid CSS selectors</param>
        /// <returns></returns>
        public AxeBuilder Exclude(params string[] selectors)
        {
            _includeExcludeManager.Exclude(selectors);
            return this;
        }

        /// <summary>
        /// Run aXe against a specific WebElement.
        /// </summary>
        /// <param name="context"> A WebElement to test</param>
        /// <returns>An aXe results document</returns>
        /// @author <a href="mailto:jdmesalosada@gmail.com">Julian Mesa</a>
        public JObject Analyze(IWebElement context)
        {
            string command = string.Format("axe.a11yCheck(arguments[0], {0}, arguments[arguments.length - 1]);", Options);
            return Execute(command, context);
        }

        /// <summary>
        /// Run aXe against the page.
        /// </summary>
        /// <returns>An aXe results document</returns>
        /// @author <a href="mailto:jdmesalosada@gmail.com">Julian Mesa</a>
        public JObject Analyze()
        {
            string command;

            if (_includeExcludeManager.HasMoreThanOneSelectorsToIncludeOrSomeToExclude())
            {
                command = $"axe.a11yCheck({_includeExcludeManager.ToJson()}, {Options}, arguments[arguments.length - 1]);";
            }
            else if (_includeExcludeManager.HasOneItemToInclude())
            {
                string itemToInclude = _includeExcludeManager.GetFirstItemToInclude().Replace("'", "");
                command = $"axe.a11yCheck('{itemToInclude}', {Options}, arguments[arguments.length - 1]);";
            }
            else
            {
                command = $"axe.a11yCheck(document, {Options}, arguments[arguments.length - 1]);";
            }

            return Execute(command);
        }

        private JObject Execute(string command, params object[] args)
        {
            _webDriver.Manage().Timeouts().SetScriptTimeout(TimeSpan.FromSeconds(30));
            object response = ((IJavaScriptExecutor)_webDriver).ExecuteAsyncScript(command, args);
            return new JObject(response);
        }
    }
}